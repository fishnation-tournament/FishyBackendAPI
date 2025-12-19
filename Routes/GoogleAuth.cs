using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using FishyAPI.Tools;
using FishyAPI.Tools.Authentication;
using FishyAPI.Tools.DBInteractions;
using Json;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class GoogleAuth
{
    public static void MapAuthRoutes(WebApplication app, Tokenizer apiToken, ConnectionManager connManager)
    {
        app.MapGet("/Auth/Google", async Task (HttpContext context) =>
        {
            NameValueCollection parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["client_id"] = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            parameters["response_type"] = "code";
            parameters["scope"] = "openid email profile";
            parameters["redirect_uri"] = "https://api.fishnation.xyz/auth/google/callback";
            parameters["access_type"] = "online";

            var parameterString = parameters.ToString();
            var googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?{parameterString}";

            context.Response.Redirect(googleAuthUrl);
        }).WithName("GoogleAuth").WithOpenApi();

        app.MapGet("/Auth/Google/Callback", async Task (HttpContext context) =>
        {
            var code = context.Request.Query["code"];
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
            var redirectUri = "https://api.fishnation.xyz/auth/google/callback";

            using var httpClient = new HttpClient();
            var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new[]
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

            // Get user info from Google
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var userResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");

            var userContent = await userResponse.Content.ReadAsStringAsync();
            var userData = JsonParser.FromJson(userContent);

            // Generate JWT token
            string token = "";

            using MySqlConnection dbConn = connManager.IssueConnection();

            if (UserTools.CheckUserExistsGID(dbConn, userData["id"].ToString()))
            {
                var user = UserTools.GetUserByGoogleId(dbConn, userData["id"].ToString());
                token = apiToken.GenerateToken(user, user.Role, DateTime.Now.AddDays(7));
                var websiteRedirect = $"https://fishnation.xyz/auth/google/callback?token={token}";
                context.Response.Redirect(websiteRedirect);
                return;
            }


            FrontFacingDataTypes.FrontFacingUser User = new FrontFacingDataTypes.FrontFacingUser
            {
                SSID = null,
                OptBLUID = null,
                Username = userData["name"]?.ToString() ?? userData["email"].ToString(),
                UserPfpLink = userData["picture"]?.ToString(),
                UserBio = "",
                DiscordID = null,
                DiscordUsername = null,
                GoogleID = userData["id"].ToString(),
                GoogleEmail = userData["email"].ToString(),
                RegistrationDate = DateTime.Now,
                Role = "User",
                FrontendRole = "Unassigned"
            };

            DataTypes.User newUser = UserTools.AddUser(dbConn, User);

            token = apiToken.GenerateToken(newUser, "User", DateTime.Now.AddDays(7));
            var Redirect = $"https://fishnation.xyz/auth/google/callback?token={token}";

            // Return the token
            context.Response.Redirect(Redirect);
        }).WithName("GoogleAuthCallback").WithOpenApi().ExcludeFromDescription();
    }
}
