using System.Collections.Specialized;
using System.Text.Json;
using System.Web;
using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class SteamAuth
{
    private static bool TryParseSteamId64(string? raw, out ulong steamId)
    {
        steamId = 0;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        // SteamID64 is a 17-digit unsigned integer in practice.
        if (raw.Length != 17 || raw.Any(c => !char.IsDigit(c)))
        {
            return false;
        }

        return ulong.TryParse(raw, out steamId);
    }

    private static bool TryExtractSteamIdFromClaimedId(string? claimedId, out ulong steamId)
    {
        steamId = 0;
        if (string.IsNullOrWhiteSpace(claimedId))
        {
            return false;
        }

        string candidate = claimedId.Split('/').LastOrDefault() ?? string.Empty;
        return TryParseSteamId64(candidate, out steamId);
    }

    private static bool TryExtractSteamIdFromProfileResponse(string responseJson, out ulong steamId)
    {
        steamId = 0;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseJson);
            if (!doc.RootElement.TryGetProperty("response", out JsonElement responseElement) ||
                !responseElement.TryGetProperty("players", out JsonElement playersElement) ||
                playersElement.ValueKind != JsonValueKind.Array ||
                playersElement.GetArrayLength() == 0)
            {
                return false;
            }

            JsonElement firstPlayer = playersElement[0];
            if (!firstPlayer.TryGetProperty("steamid", out JsonElement steamIdElement) ||
                steamIdElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            return TryParseSteamId64(steamIdElement.GetString(), out steamId);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static void MapAuthRoutes(WebApplication app, string steamKey, ConnectionManager connManager)
    {
        app.MapGet("/Auth/Steam", (HttpContext context) =>
        {
            var token = context.Request.Query["token"].ToString();

            NameValueCollection parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["openid.ns"] = "http://specs.openid.net/auth/2.0";
            parameters["openid.mode"] = "checkid_setup";
            parameters["openid.claimed_id"] = "http://specs.openid.net/auth/2.0/identifier_select";
            parameters["openid.identity"] = "http://specs.openid.net/auth/2.0/identifier_select";
            parameters["openid.return_to"] = $"https://api.fishnation.xyz/auth/steam/callback?token={token}";
            parameters["openid.realm"] = "https://api.fishnation.xyz";

            context.Response.Redirect($"https://steamcommunity.com/openid/login?{parameters}");
        }).WithName("SteamAuth").WithOpenApi().RequirePermission(Permission.AccountManageSelf);
        
        app.MapGet("/Auth/Steam/Callback", async Task (HttpContext context) =>
        {
            using var httpClient = new HttpClient();

            // Check parameters to make sure it's a valid auth from steam
            var postParams = context.Request.Query.Keys.ToDictionary(k => k, k => context.Request.Query[k].ToString());
            postParams["openid.mode"] = "check_authentication";
            var checkAuthResponse = await httpClient.PostAsync("https://steamcommunity.com/openid/login", new FormUrlEncodedContent(postParams));
            string checkAuthContent = await checkAuthResponse.Content.ReadAsStringAsync();
            if (!checkAuthContent.Contains("is_valid:true"))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Steam auth failed, invalid auth.");
                return;
            }

            // Extract and validate steam id from OpenID callback.
            string? claimedId = context.Request.Query["openid.claimed_id"];
            if (!TryExtractSteamIdFromClaimedId(claimedId, out ulong steamID))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Steam auth failed, invalid SteamID in claimed_id.");
                return;
            }
            string steamIDString = steamID.ToString();
            
            Console.WriteLine($"Steam key loaded: {!string.IsNullOrEmpty(steamKey)}");
            
            // Grab their steam profile
            NameValueCollection parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["key"] = steamKey;
            parameters["steamids"] = steamIDString;
            HttpResponseMessage response = await httpClient.GetAsync($"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?{parameters}");
            if (!response.IsSuccessStatusCode)
            {
                context.Response.StatusCode = 500;
                Console.WriteLine($"Failed to fetch Steam user profile. Status code: {response.StatusCode}. {await response.Content.ReadAsStringAsync()}");
                await context.Response.WriteAsync("Failed to fetch Steam user profile.");
                return;
            }

            string steamProfileJson = await response.Content.ReadAsStringAsync();
            if (!TryExtractSteamIdFromProfileResponse(steamProfileJson, out ulong profileSteamId))
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Failed to fetch Steam user profile.");
                return;
            }

            // Defense-in-depth: ensure OpenID identity and Steam API identity match.
            if (profileSteamId != steamID)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Steam auth failed, mismatched Steam profile ID.");
                return;
            }

            using MySqlConnection dbConn = connManager.IssueConnection();

            if (UserTools.CheckUserExistsSteamID(dbConn, steamID))
            {
                NameValueCollection respParameters = HttpUtility.ParseQueryString(string.Empty);
                respParameters["sysMessage"] = "You already have an account linked with this Steam account.";

                var websiteRedirect = $"https://fishnation.xyz/me?{respParameters}";
                context.Response.Redirect(websiteRedirect);
                return;
            }

            if (!ulong.TryParse(context.User.Claims.First(c => c.Type == "uid").Value, out ulong currentUid))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized user context.");
                return;
            }

            User user = UserTools.GetUserById(dbConn, currentUid);
            user.SSID = steamID;
            
            if(user.OptBLUID == null || user.OptBLUID == 0)
            {
                user.OptBLUID = steamID;
            }
            
            UserTools.UpdateUser(dbConn, user);
            
            UserTools.UpdateUser(dbConn, await user.UpdateProfilesAsync());

            NameValueCollection respParam = HttpUtility.ParseQueryString(string.Empty);
            respParam["sysMessage"] = "Your account has been successfully linked to your Steam account.";

            var redirect = $"https://fishnation.xyz/me?{respParam}";
            context.Response.Redirect(redirect);
            return;
        }).WithName("SteamAuthCallback").WithOpenApi().ExcludeFromDescription().RequirePermission(Permission.AccountManageSelf);
    }
}
