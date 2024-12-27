using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using FishyAPI.Tools;
using FishyAPI.Tools.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace FishyAPI.Routes;

public static class DiscordAuth
{
    public static void MapAuthRoutes(WebApplication app, Tokenizer apiToken, SQLInteraction interactionHelper)
    {
        app.MapGet("/Auth/Discord", async (HttpContext context) =>
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

        app.MapGet("/Auth/Discord/Callback", async (HttpContext context) =>
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
            var tokenData = JsonSerializer.Deserialize<Dictionary<string, string>>(tokenContent);

            var accessToken = tokenData["access_token"];

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var userResponse = await httpClient.GetAsync("https://discord.com/api/users/@me");

            var userContent = await userResponse.Content.ReadAsStringAsync();
            var userData = JsonSerializer.Deserialize<Dictionary<string, string>>(userContent);

            // Generate JWT token
            string token = apiToken.GenerateToken(userData["id"], "basicClient");

            if(UserTools.CheckUserExistsDID(interactionHelper, ulong.Parse(userData["id"])))
            {
                return Results.Ok(new { token });
            }
            
            UserTools.AddUser(interactionHelper, new DataTypes.User
            {
                UID = 1,
                OptBLUID = 1,
                Username = userData["username"],
                UserPfpLink = $"https://cdn.discordapp.com/avatars/{userData["id"]}/{userData["avatar"]}",
                UserBio = "",
                DiscordID = ulong.Parse(userData["id"]),
                DiscordUsername = userData["username"],
                RegistrationDate = DateTime.Now,
                Role = "basicClient"
            });   
            
            // Return the token
            return Results.Ok(new { token });
        }).WithName("DiscordAuthCallback").WithOpenApi();
    }
}