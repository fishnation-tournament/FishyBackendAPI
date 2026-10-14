using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FishyAPI.Tools;
using FishyAPI.Tools.DBInteractions;
using FishyAPI.Routes;
using FishyAPI.Tools.Authentication;
using FishyAPI.Tools.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;

DotNetEnv.Env.TraversePath().Load("./.env");
Console.WriteLine("Testing Credentials for SQL Server");

//Envireonment Variables
var DBServer = Environment.GetEnvironmentVariable("DATABASE_SERVER");
var DBName = Environment.GetEnvironmentVariable("DATABASE_NAME");
var DBUser = Environment.GetEnvironmentVariable("DATABASE_USER");
var DBPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
var CertPath = Environment.GetEnvironmentVariable("CERT_PATH");
var CertKey = Environment.GetEnvironmentVariable("CERT_KEY");
var SecretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
var SteamKey = Environment.GetEnvironmentVariable("STEAM_API_KEY")?.Trim();

Console.WriteLine($"Steam Key: {SteamKey}");

ConnectionManager connManager = new ConnectionManager(
    DBServer,
    DBName,
    DBUser,
    DBPassword);

try
{
    using MySqlConnection dbConn = connManager.IssueConnection();
}
catch
{
    Console.WriteLine("Failed to connect to database");
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(80);
        options.ListenAnyIP(443, httpsOptions =>
        {
            httpsOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
            try
            {
                var certPem = File.ReadAllText(CertPath);
                var keyPem = File.ReadAllText(CertKey);
                var certificate = new X509Certificate2(
                    X509Certificate2.CreateFromPem(certPem, keyPem));
                httpsOptions.UseHttps(certificate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not find certificate files at {CertPath} and {CertKey}");
                Console.WriteLine($"Failed to load certificate: {ex.Message}");
                throw;
            }
        });
    });

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey))
        };
    });

// Endpoints require permissions (see Tools/Authorization/AccessPolicy.cs); one policy per permission.
builder.Services.AddAuthorization(options => options.AddPermissionPolicies());
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseWebSockets();

app.UseHttpsRedirection();

app.Use(async (context, next) => {
    if (context.Request.Query.TryGetValue("token", out var token) &&
        !context.Request.Headers.ContainsKey("Authorization"))
    {
        context.Request.Headers.Append("Authorization", $"Bearer {token}");
    }
    await next();
});

app.UseAuthentication();

// Revocation check: reject API tokens that have been revoked in the database.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        context.User.FindFirstValue("token_type") == "api")
    {
        var jti = context.User.FindFirstValue("jti");
        if (jti == null)
        {
            context.Response.StatusCode = 401;
            return;
        }
        using MySqlConnection revokeConn = connManager.IssueConnection();
        if (ApiTokenTools.IsTokenRevoked(revokeConn, jti))
        {
            context.Response.StatusCode = 401;
            return;
        }
    }
    await next();
});

// Resolve the caller's roles and permissions from user_roles (current as of this request).
app.UseUserAccess(connManager);

app.UseAuthorization();

// Audit log middleware: captures all authenticated PUT, POST, and DELETE requests.
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    bool isMutating = method == "PUT" || method == "POST" || method == "DELETE";

    if (isMutating)
        context.Request.EnableBuffering();

    await next();

    if (!isMutating) return;

    string? body = null;
    if (context.Request.Body.CanSeek)
    {
        context.Request.Body.Seek(0, SeekOrigin.Begin);
        using var bodyReader = new StreamReader(context.Request.Body, leaveOpen: true);
        body = await bodyReader.ReadToEndAsync();
    }

    if (context.Response.StatusCode == 400)
    {
        Console.WriteLine($"[400] {method} {context.Request.Path} | Body: {body}");
    }

    if (context.User.Identity?.IsAuthenticated == true)
    {
        var uidClaim = context.User.FindFirstValue("uid");
        if (uidClaim != null && ulong.TryParse(uidClaim, out ulong uid))
        {
            var roles = context.GetAccess().Roles;
            var role = roles.Count == 0 ? "none" : string.Join(",", roles);
            if (role.Length > 50) role = role[..50];
            var tokenType = context.User.FindFirstValue("token_type") ?? "session";
            var actionContext = context.Request.Path.Value ?? "";

            try
            {
                using MySqlConnection auditConn = connManager.IssueConnection();
                AuditLogTools.InsertLog(auditConn, uid, role, method, actionContext, body, tokenType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audit log write failed: {ex.Message}");
            }
        }
    }
});

Tokenizer apiTokenizer = new Tokenizer(SecretKey);

MapRoutes.MapMapRoutes(app, connManager);
FlairRoutes.MapFlairRoutes(app, connManager);
ScoresRoutes.RegisterScoresRoutes(app, connManager);
PlayerRoutes.MapPlayerRoutes(app, connManager);
UserRoutes.MapUserRoutes(app, connManager);
DiscordAuth.MapAuthRoutes(app, apiTokenizer, connManager);
GoogleAuth.MapAuthRoutes(app, apiTokenizer, connManager);
SteamAuth.MapAuthRoutes(app, SteamKey, connManager);
ApiTokenRoutes.MapApiTokenRoutes(app, apiTokenizer, connManager);
SeasonRoutes.MapSeasonRoutes(app, connManager);
TournamentSeriesRoutes.MapTournamentSeriesRoutes(app, connManager);
TeamRoutes.MapTeamRoutes(app, connManager);
MatchLineupRoutes.MapMatchLineupRoutes(app, connManager);
AuditLogRoutes.MapAuditLogRoutes(app, connManager);
PickBanRoutes.MapPickBanRoutes(app, connManager);
MatchScheduleRoutes.MapMatchScheduleRoutes(app, connManager);
MatchWebSocketRoutes.MapMatchWebSocketRoutes(app);
StreamTokenRoutes.MapStreamTokenRoutes(app, connManager);
RoleRoutes.MapRoleRoutes(app, connManager);

// Refuse to start if any write endpoint is unprotected or uses something other than a permission.
AccessControl.ValidateEndpointSecurity(app);

using (MySqlConnection roleCheckConn = connManager.IssueConnection())
{
    List<string> missingRoles = RoleTools.MissingRoleKeys(roleCheckConn, AccessPolicy.RolesWithGrants);
    if (missingRoles.Count > 0)
        Console.WriteLine($"WARNING: AccessPolicy grants permissions to roles missing from the roles table: {string.Join(", ", missingRoles)}");
}

app.Run();
