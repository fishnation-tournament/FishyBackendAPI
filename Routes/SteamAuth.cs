using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Web;
using FishyAPI.Tools;
using FishyAPI.Tools.Authentication;
using FishyAPI.Tools.DBInteractions;
using Json;

namespace FishyAPI.Routes;

public static class SteamAuth
{
    public static void MapAuthRoutes(WebApplication app, Tokenizer apiToken, ConnectionManager connManager)
    {
        app.MapGet("/Auth/Steam", async Task (HttpContext context) =>
        {
            NameValueCollection parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["openid.ns"] = "http://specs.openid.net/auth/2.0";
            parameters["openid.mode"] = "checkid_setup";
            parameters["openid.claimed_id"] = "http://specs.openid.net/auth/2.0/identifier_select";
            parameters["openid.identity"] = "http://specs.openid.net/auth/2.0/identifier_select";
            parameters["openid.return_to"] = "https://api.fishnation.xyz/auth/steam/callback";
            parameters["openid.realm"] = "https://api.fishnation.xyz";

            context.Response.Redirect($"https://steamcommunity.com/openid/login?{parameters}");
        }).WithName("SteamAuth").WithOpenApi();

        string steamApiKey = Environment.GetEnvironmentVariable("STEAM_API_KEY") ?? throw new InvalidOperationException("The steam api key is not in the environment variables");
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

            // Extract steam id
            string? claimedId = context.Request.Query["openid.claimed_id"];
            if (claimedId == null)
            {
                // Just in case. Should never happen.
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Steam auth failed, missing claimed_id.");
                return;
            }
            string steamIDString = claimedId.Split('/').Last();

            // Grab their steam profile
            NameValueCollection parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["key"] = steamApiKey;
            parameters["steamids"] = steamIDString;
            HttpResponseMessage response = await httpClient.GetAsync($"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?{parameters}");
            if (!response.IsSuccessStatusCode)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Failed to fetch Steam user profile.");
                return;
            }
            IDictionary<string, object> responseContent = JsonParser.FromJson(await response.Content.ReadAsStringAsync());
            if (!responseContent.TryGetValue("response", out var responseObj) ||
                responseObj is not IDictionary<string, object> innerResponse)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Failed to fetch Steam user profile.");
                return;
            }
            if (!innerResponse.TryGetValue("players", out var playersObj) ||
                playersObj is not IList<object> players)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Failed to fetch Steam user profile.");
                return;
            }
            if (players.Count == 0 ||
                players[0] is not IDictionary<string, object> steamUserData)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Failed to fetch Steam user profile.");
                return;
            }
            ulong steamID = ulong.Parse(steamIDString);

            string token = "";

            DBConnection dbConn = connManager.IssueConnection();

            if (UserTools.CheckUserExistsSteamID(dbConn.Connection, steamID))
            {
                var existingUser = UserTools.GetUserBySteamId(dbConn.Connection, steamID);
                dbConn.Close();
                token = apiToken.GenerateToken(existingUser, existingUser.Role);
                var websiteRedirect = $"https://fishnation.xyz/auth/steam/callback?token={token}";
                context.Response.Redirect(websiteRedirect);
                return;
            }

            string steamUsername = (string)steamUserData["personaname"];
            string steamPfpLink = (string)steamUserData["avatar"];

            DataTypes.User newUser = UserTools.AddUser(dbConn.Connection, new FrontFacingDataTypes.FrontFacingUser
            {
                SSID = null,
                OptBLUID = null,
                Username = steamUsername,
                UserPfpLink = steamPfpLink,
                UserBio = "",
                SteamID = steamID,
                SteamUsername = steamUsername,
                RegistrationDate = DateTime.Now,
                Role = "User",
                FrontendRole = "Unassigned"
            });
            dbConn.Close();

            token = apiToken.GenerateToken(newUser, "User");
            var Redirect = $"https://fishnation.xyz/auth/steam/callback?token={token}";

            context.Response.Redirect(Redirect);
        }).WithName("SteamAuthCallback").WithOpenApi().ExcludeFromDescription();
    }
}