using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using FishyAPI.Tools;
using FishyAPI.Tools.Authentication;
using FishyAPI.Tools.DBInteractions;
using Json;
using Microsoft.AspNetCore.Mvc;

namespace FishyAPI.Routes;

public static class DiscordAuth
{
    public static void MapAuthRoutes(WebApplication app, Tokenizer apiToken, ConnectionManager connManager)
    {
        app.MapGet("/Auth/Discord", async Task (HttpContext context) =>
        {
            NameValueCollection parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["client_id"] = Environment.GetEnvironmentVariable("DISCORD_APPID");
            parameters["client_secret"] = Environment.GetEnvironmentVariable("DISCORD_SECRET");
            parameters["response_type"] = "code";
            parameters["grant_type"] = "authorization_code";
            parameters["scope"] = "identify";
            parameters["redirect_uri"] = "https://api.fishnation.xyz/auth/discord/callback";
            
            var parameterString = parameters.ToString();
            var discordAuthUrl = $"https://discord.com/api/oauth2/authorize?{parameterString}";

            context.Response.Redirect(discordAuthUrl);
        }).WithName("DiscordAuth").WithOpenApi();

        app.MapGet("/Auth/Discord/Callback", async Task (HttpContext context) =>
        {
            var code = context.Request.Query["code"];
            var clientId = Environment.GetEnvironmentVariable("DISCORD_APPID");
            var clientSecret = Environment.GetEnvironmentVariable("DISCORD_SECRET");
            var redirectUri = "https://api.fishnation.xyz/auth/discord/callback";

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
            Console.WriteLine("User content is " + userContent);
            var userData = JsonParser.FromJson(userContent);
            
            Console.WriteLine($"Content is {Newtonsoft.Json.JsonConvert.SerializeObject(userData)}");

            // Generate JWT token
            Console.WriteLine(userData["id"].ToString());
            string token = "";
            
            var dbConn = connManager.IssueConnection();
            var interactionHelper = new SQLInteraction(dbConn.Connection);

            if(UserTools.CheckUserExistsDID(interactionHelper, ulong.Parse(userData["id"].ToString())))
            {
                var user = UserTools.GetUserByDiscordId(interactionHelper, ulong.Parse(userData["id"].ToString()));
                Console.WriteLine($"User {user.Username} already exists with role {user.Role}");
                token = apiToken.GenerateToken(user, user.Role);
                var websiteRedirect = $"https://fishnation.xyz/auth/discord/callback?token={token}";
                context.Response.Redirect(websiteRedirect);
                return;
            }
            
            
            DataTypes.User newUser = new DataTypes.User
            {
                SSID = null,
                OptBLUID = null,
                Username = userData["username"].ToString(),
                UserPfpLink = $"https://cdn.discordapp.com/avatars/{userData["id"].ToString()}/{userData["avatar"].ToString()}",
                UserBio = "null",
                DiscordID = ulong.Parse(userData["id"].ToString()),
                DiscordUsername = userData["username"].ToString(),
                RegistrationDate = DateTime.Now,
                Role = "User",
                FrontendRole = "Unassigned"
            };

            UserTools.AddUser(interactionHelper, newUser);
            
            token = apiToken.GenerateToken(newUser, "User");
            var Redirect = $"https://fishnation.xyz/auth/discord/callback?token={token}";
            
            dbConn.Close();
            
            // Return the token
            context.Response.Redirect(Redirect);
        }).WithName("DiscordAuthCallback").WithOpenApi().ExcludeFromDescription();;
    }
}