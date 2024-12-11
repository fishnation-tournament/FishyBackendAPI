using FishyAPI.Tools;
using FishyAPI.Tools.DBInteractions;
using FishyAPI.Routes;
using FishyAPI.Tools.Authentication;

DotNetEnv.Env.TraversePath().Load("./.env");
Console.WriteLine("Attempting to connect to the database");
//Establish connection to the database
var dbConn = DBConnection.Instance();
dbConn.Server = Environment.GetEnvironmentVariable("DATABASE_SERVER");
dbConn.DatabaseName = Environment.GetEnvironmentVariable("DATABASE_NAME");
dbConn.UserName = Environment.GetEnvironmentVariable("DATABASE_USER");
dbConn.Password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");

if (dbConn.IsConnect())
{
    Console.WriteLine("Connected to the database");
}
else
{
    Console.WriteLine("Failed to connect to the database");
    return;
}

SQLInteraction interactionHelper = new SQLInteraction(dbConn.Connection);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

Tokenizer apiTokenizer = new Tokenizer(Environment.GetEnvironmentVariable("TOKEN_SECRET"));

MapRoutes.MapMapRoutes(app, interactionHelper);
ScoresRoutes.RegisterScoresRoutes(app, interactionHelper);
UserRoutes.MapUserRoutes(app, interactionHelper);
DiscordAuth.MapAuthRoutes(app, apiTokenizer);

app.Run();