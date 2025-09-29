using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FishyAPI.Tools;
using FishyAPI.Tools.DBInteractions;
using FishyAPI.Routes;
using FishyAPI.Tools.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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



ConnectionManager connManager = new ConnectionManager(
    DBServer,
    DBName,
    DBUser,
    DBPassword);

var dbConn = connManager.IssueConnection();

if (dbConn == null)
{
    Console.WriteLine("Failed to connect to database");
    return;
}

dbConn.Close();

var builder = WebApplication.CreateBuilder(args);


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);
    options.ListenAnyIP(443, httpsOptions =>
    {
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
            ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey))
        };
    });

builder.Services.AddAuthorization(options =>
{   options.AddPolicy("Admin", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    options.AddPolicy("Organizer", policy => policy.RequireClaim(ClaimTypes.Role, "Organizer", "Admin"));
    options.AddPolicy("User", policy => policy.RequireClaim(ClaimTypes.Role, "User", "Organizer", "Admin"));
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

Tokenizer apiTokenizer = new Tokenizer(SecretKey);

MapRoutes.MapMapRoutes(app, connManager);
ScoresRoutes.RegisterScoresRoutes(app, connManager);
UserRoutes.MapUserRoutes(app, connManager);
DiscordAuth.MapAuthRoutes(app, apiTokenizer, connManager);

app.Run();