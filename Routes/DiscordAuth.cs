using System.Net.Http.Headers;
using System.Text.Json;
using FishyAPI.Tools;
using FishyAPI.Tools.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace FishyAPI.Routes;

public static class DiscordAuth
{
    public static void MapAuthRoutes(WebApplication app, Tokenizer apiToken)
    {
        app.MapGet("/auth/discord", async (HttpContext context) =>
        {
            var clientId = Environment.GetEnvironmentVariable("DISCORD_APPID");
            var redirectUri = "https://yourapp.com/auth/discord/callback";
            var scope = "identify email";
            var discordAuthUrl = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}";

            context.Response.Redirect(discordAuthUrl);
        });

        app.MapGet("/auth/discord/callback", async (HttpContext context) =>
        {
            var code = context.Request.Query["code"];
            var clientId = Environment.GetEnvironmentVariable("DISCORD_APPID");
            var clientSecret = Environment.GetEnvironmentVariable("DISCORD_SECRET");
            var redirectUri = "https://yourapp.com/auth/discord/callback";

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
            string token = apiToken.GenerateToken(userData["id"], "user");

            // Return the token
            return Results.Ok(new { token });
        });
    }
}