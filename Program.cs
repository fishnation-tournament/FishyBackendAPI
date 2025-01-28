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

ConnectionManager connManager = new ConnectionManager(
    Environment.GetEnvironmentVariable("DATABASE_SERVER"),
    Environment.GetEnvironmentVariable("DATABASE_NAME"),
    Environment.GetEnvironmentVariable("DATABASE_USER"),
    Environment.GetEnvironmentVariable("DATABASE_PASSWORD"));

var dbConn = connManager.IssueConnection();

if (dbConn == null)
{
    Console.WriteLine("Failed to connect to database");
    return;
}

dbConn.Close();

SQLInteraction interactionHelper = new SQLInteraction(dbConn.Connection);

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(80);
        options.ListenAnyIP(443, httpsOptions =>
        {
            try
            {
                var certificate = new X509Certificate2(
                    Environment.GetEnvironmentVariable("CERT_PATH"),
                    Environment.GetEnvironmentVariable("CERT_PASSWORD")
                );
                httpsOptions.UseHttps(certificate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not find certificate files at {Environment.GetEnvironmentVariable("FULLCHAIN_PATH")} and {Environment.GetEnvironmentVariable("PRIVATEKEY_PATH")}");
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("SECRET_KEY")))
            };
        });

    builder.Services.AddAuthorization(options =>
    {   options.AddPolicy("Admin", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
        options.AddPolicy("Organizer", policy => policy.RequireClaim(ClaimTypes.Role, "Organizer", "Admin"));
        options.AddPolicy("User", policy => policy.RequireClaim(ClaimTypes.Role, "User", "Organizer", "Admin"));
    });


}

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

if(!app.Environment.IsDevelopment())
{
    app.UseAuthentication();
    app.UseAuthorization();
}


Tokenizer apiTokenizer = new Tokenizer(Environment.GetEnvironmentVariable("SECRET_KEY"));

MapRoutes.MapMapRoutes(app, interactionHelper);
ScoresRoutes.RegisterScoresRoutes(app, interactionHelper);
UserRoutes.MapUserRoutes(app, interactionHelper);
DiscordAuth.MapAuthRoutes(app, apiTokenizer, connManager);

app.Run();