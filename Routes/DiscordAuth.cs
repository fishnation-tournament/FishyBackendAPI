using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.Authentication;
using FishyAPI.Tools.DBInteractions;
using Json;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class DiscordAuth
{
    public static void MapAuthRoutes(WebApplication app, Tokenizer apiToken, ConnectionManager connManager)
    {
        app.MapGet("/Auth/Discord", (HttpContext context) =>
        {
            NameValueCollection parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["client_id"] = Environment.GetEnvironmentVariable("DISCORD_APPID");
            parameters["client_secret"] = Environment.GetEnvironmentVariable("DISCORD_SECRET");
            parameters["response_type"] = "code";
            parameters["grant_type"] = "authorization_code";
            parameters["scope"] = "identify";
            parameters["redirect_uri"] = $"{PublicUrls.Api}/auth/discord/callback";

            var parameterString = parameters.ToString();
            var discordAuthUrl = $"https://discord.com/api/oauth2/authorize?{parameterString}";

            context.Response.Redirect(discordAuthUrl);
        }).WithName("DiscordAuth").WithOpenApi();

        app.MapGet("/Auth/Discord/Callback", async Task (HttpContext context) =>
        {
            var code = context.Request.Query["code"];
            var clientId = Environment.GetEnvironmentVariable("DISCORD_APPID");
            var clientSecret = Environment.GetEnvironmentVariable("DISCORD_SECRET");
            var redirectUri = $"{PublicUrls.Api}/auth/discord/callback";

            using var httpClient = new HttpClient();
            var tokenResponse = await httpClient.PostAsync("https://discord.com/api/oauth2/token", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            }));

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonParser.FromJson(tokenContent);

            var accessToken = tokenData["access_token"].ToString();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenData["token_type"].ToString(), accessToken);
            var userResponse = await httpClient.GetAsync("https://discord.com/api/users/@me");

            var userContent = await userResponse.Content.ReadAsStringAsync();
            var userData = JsonParser.FromJson(userContent);

            // Generate JWT token
            string token = "";

            using MySqlConnection dbConn = connManager.IssueConnection();
            bool userExists = UserTools.CheckUserExistsDID(dbConn, ulong.Parse(userData["id"].ToString()));
            
            
            if (userExists)
            {
                var user = UserTools.GetUserByDiscordId(dbConn, ulong.Parse(userData["id"].ToString()));
                UserTools.UpdateUser(dbConn, await user.UpdateProfilesAsync());
                
                
                Console.WriteLine($"User {user.Username} logged in with Discord. Checking for profile updates...");
                
                if(user.SSID == null || user.OptBLUID == null)
                {
                    ulong? blid = UserTools.GetBLID(userData["id"].ToString());
                    
                    if (blid != null)
                    {
                        user.OptBLUID = blid;
                    }
                    UserTools.UpdateUser(dbConn, user);
                }
                
                token = apiToken.GenerateToken(user, DateTime.Now.AddDays(7));
                var websiteRedirect = $"{PublicUrls.Site}/auth/discord/callback?token={token}";
                context.Response.Redirect(websiteRedirect);
                return;
            }


            AddUser User = new AddUser
            {
                SSID = null,
                OptBLUID = null,
                Username = userData["username"].ToString(),
                UserPfpLink = $"https://cdn.discordapp.com/avatars/{userData["id"].ToString()}/{userData["avatar"].ToString()}",
                UserBio = "",
                DiscordID = ulong.Parse(userData["id"].ToString()),
                DiscordUsername = userData["username"].ToString(),
                RegistrationDate = DateTime.Now,
                Title = null
            };

            User newUser;
            try
            {
                newUser = UserTools.AddUser(dbConn, User);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Another callback inserted this Discord user first; fetch and continue login.
                newUser = UserTools.GetUserByDiscordId(dbConn, ulong.Parse(userData["id"].ToString()));
            }

            token = apiToken.GenerateToken(newUser, DateTime.Now.AddDays(7));
            var Redirect = $"{PublicUrls.Site}/auth/discord/callback?token={token}";

            // Return the token
            context.Response.Redirect(Redirect);
        }).WithName("DiscordAuthCallback").WithOpenApi().ExcludeFromDescription();
    }
}
