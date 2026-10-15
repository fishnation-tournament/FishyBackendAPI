using System.Net;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Tools;

/// <summary>
/// Once a night, re-fetches the ScoreSaber and BeatLeader profiles (rank, pp, etc.) of every registered player.
/// Requests are spread evenly across a window so ScoreSaber, BeatLeader and our database only ever see a trickle.
///
/// Environment variables:
///   PROFILE_REFRESH_ENABLED          "false" turns the job off (default on)
///   PROFILE_REFRESH_HOUR_UTC         hour of day the run starts, 0-23 (default 9, early morning in the Americas)
///   PROFILE_REFRESH_WINDOW_MINUTES   how long the run is spread over (default 180)
/// </summary>
public class PlayerProfileRefreshService : BackgroundService
{
    // Never go faster than this between players, however short the window or long the player list.
    private static readonly TimeSpan MinDelayBetweenPlayers = TimeSpan.FromSeconds(5);
    // Gap between the ScoreSaber and BeatLeader calls for one player.
    private static readonly TimeSpan DelayBetweenSources = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DefaultRetryAfter = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MaxRetryAfter = TimeSpan.FromMinutes(10);
    // If an API keeps failing it's probably down; stop for the night rather than keep knocking.
    private const int MaxConsecutiveFailures = 5;

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly ConnectionManager connManager;
    private readonly bool enabled;
    private readonly int startHourUtc;
    private readonly TimeSpan window;

    public PlayerProfileRefreshService(ConnectionManager connManager)
    {
        this.connManager = connManager;
        enabled = !string.Equals(Environment.GetEnvironmentVariable("PROFILE_REFRESH_ENABLED")?.Trim(), "false",
            StringComparison.OrdinalIgnoreCase);
        startHourUtc = int.TryParse(Environment.GetEnvironmentVariable("PROFILE_REFRESH_HOUR_UTC"), out int hour) && hour is >= 0 and <= 23
            ? hour
            : 9;
        window = TimeSpan.FromMinutes(
            int.TryParse(Environment.GetEnvironmentVariable("PROFILE_REFRESH_WINDOW_MINUTES"), out int minutes) && minutes > 0
                ? minutes
                : 180);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!enabled)
        {
            Console.WriteLine("[ProfileRefresh] Disabled by PROFILE_REFRESH_ENABLED.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime nextRun = NextRunUtc(DateTime.UtcNow);
            Console.WriteLine($"[ProfileRefresh] Next run at {nextRun:yyyy-MM-dd HH:mm} UTC, spread over {window.TotalMinutes:0} minutes.");

            try
            {
                await Task.Delay(nextRun - DateTime.UtcNow, stoppingToken);
                await RefreshAllAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Never let one bad night take the job (or the API) down; try again tomorrow.
                Console.WriteLine($"[ProfileRefresh] Run failed: {ex.Message}");
            }
        }
    }

    private DateTime NextRunUtc(DateTime now)
    {
        DateTime today = now.Date.AddHours(startHourUtc);
        return today > now ? today : today.AddDays(1);
    }

    private async Task RefreshAllAsync(CancellationToken stoppingToken)
    {
        List<(ulong UID, ulong? SSID, ulong? OptBLUID)> players;
        using (MySqlConnection conn = connManager.IssueConnection())
        {
            players = UserTools.GetPlayerProfileIds(conn);
        }

        if (players.Count == 0)
        {
            Console.WriteLine("[ProfileRefresh] No players with linked profiles; nothing to do.");
            return;
        }

        TimeSpan spacing = players.Count > 1 ? window / players.Count : TimeSpan.Zero;
        if (spacing < MinDelayBetweenPlayers) spacing = MinDelayBetweenPlayers;
        Console.WriteLine($"[ProfileRefresh] Refreshing {players.Count} players, one every {spacing.TotalSeconds:0.#}s.");

        int updated = 0, failed = 0, consecutiveFailures = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (i > 0) await Task.Delay(spacing, stoppingToken);

            var (uid, ssid, blid) = players[i];
            string? scoreSaberProfile = null, beatLeaderProfile = null;
            bool anyFailed = false;

            if (ssid.HasValue)
            {
                scoreSaberProfile = await FetchAsync("ScoreSaber", uid,
                    () => UserProfiles.RequestScoreSaberProfileAsync(HttpClient, ssid.Value, stoppingToken), stoppingToken);
                anyFailed |= scoreSaberProfile == null;
            }

            if (blid.HasValue)
            {
                if (ssid.HasValue) await Task.Delay(DelayBetweenSources, stoppingToken);
                beatLeaderProfile = await FetchAsync("BeatLeader", uid,
                    () => UserProfiles.RequestBeatLeaderProfileAsync(HttpClient, blid.Value, stoppingToken), stoppingToken);
                anyFailed |= beatLeaderProfile == null;
            }

            if (scoreSaberProfile != null || beatLeaderProfile != null)
            {
                // Short-lived connection per write so the run never holds one open for hours.
                using MySqlConnection conn = connManager.IssueConnection();
                UserTools.UpdateUserProfiles(conn, uid, scoreSaberProfile, beatLeaderProfile);
                updated++;
            }

            if (anyFailed)
            {
                failed++;
                if (++consecutiveFailures >= MaxConsecutiveFailures)
                {
                    Console.WriteLine($"[ProfileRefresh] {consecutiveFailures} players in a row failed; stopping until tomorrow " +
                                      $"({i + 1}/{players.Count} processed).");
                    break;
                }
            }
            else
            {
                consecutiveFailures = 0;
            }
        }

        Console.WriteLine($"[ProfileRefresh] Done: {updated} updated, {failed} with at least one failed fetch.");
    }

    /// <summary>Returns the response body, or null on failure. Waits out a rate limit once before giving up.</summary>
    private static async Task<string?> FetchAsync(string source, ulong uid, Func<Task<HttpResponseMessage>> request,
        CancellationToken stoppingToken)
    {
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await request();
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync(stoppingToken);

                bool throttled = response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;
                if (!throttled || attempt == 2)
                {
                    Console.WriteLine($"[ProfileRefresh] {source} returned {(int)response.StatusCode} for UID {uid}.");
                    return null;
                }

                TimeSpan wait = response.Headers.RetryAfter?.Delta ?? DefaultRetryAfter;
                if (wait > MaxRetryAfter) wait = MaxRetryAfter;
                Console.WriteLine($"[ProfileRefresh] {source} is throttling us; waiting {wait.TotalSeconds:0}s.");
                await Task.Delay(wait, stoppingToken);
            }
            catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine($"[ProfileRefresh] {source} request for UID {uid} failed: {ex.Message}");
                return null;
            }
        }

        return null;
    }
}
