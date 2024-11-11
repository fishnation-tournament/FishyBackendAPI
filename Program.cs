using FishyAPI.Tools;
using FishyAPI.Tools.DBInteractions;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

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

app.MapGet("/Maps/GetMaps", () =>
{
    List<DataTypes.Map> maps = MapTools.GetMaps(interactionHelper);
    return maps;
}).WithName("GetMaps").WithOpenApi();

app.MapGet("/Maps/GetMapBySeason/{season}", (int season) =>
{
    List<DataTypes.Map> maps = MapTools.GetMapsBySeason(interactionHelper, season);
    return maps;
}).WithName("GetMapsBySeason").WithOpenApi();

app.MapGet("/Maps/GetMapById/{MID}", (ulong MID) =>
{
    DataTypes.Map map = MapTools.GetMapById(interactionHelper, MID);
    return map;
}).WithName("GetMapById").WithOpenApi();

app.MapGet("/Maps/SearchMapByName/{searchTerm}", (string searchTerm) =>
{
    List<DataTypes.Map> maps = MapTools.GetMapsByName(interactionHelper, searchTerm);
    return maps;
}).WithName("SearchMapsByName").WithOpenApi();

app.MapGet("/Users/GetUsers", () =>
{
    List<DataTypes.User> users = UserTools.GetUsers(interactionHelper);
    return users;
}).WithName("GetUsers").WithOpenApi();

app.MapGet("/Users/GetUserById/{UID}", (ulong UID) =>
{
    DataTypes.User user = UserTools.GetUserById(interactionHelper, UID);
    return user;
}).WithName("GetUserById").WithOpenApi();

app.MapGet("/Users/SearchUserByName/{searchTerm}", (string searchTerm) =>
{
    List<DataTypes.User> users = UserTools.GetUserByName(interactionHelper, searchTerm);
    return users;
}).WithName("SearchUserByName").WithOpenApi();

app.MapGet("/Maps/GetMapPools", () =>
{
    List<DataTypes.MapPool> mapPools = MapTools.GetMapPools(interactionHelper);
    return mapPools;
}).WithName("GetMapPools").WithOpenApi();

app.MapGet("/Maps/GetMapPoolById/{MapPoolID}", (ulong MapPoolID) =>
{
    DataTypes.MapPool mapPool = MapTools.GetMapPoolById(interactionHelper, MapPoolID);
    return mapPool;
}).WithName("GetMapPoolById").WithOpenApi();

app.MapGet("/Maps/SearchMapPoolByName/{searchTerm}", (string searchTerm) =>
{
    List<DataTypes.MapPool> mapPools = MapTools.GetMapPoolByName(interactionHelper, searchTerm);
    return mapPools;
}).WithName("SearchMapPoolByName").WithOpenApi();

app.MapGet("/Maps/GetMapPoolsBySeason/{season}", (int season) =>
{
    List<DataTypes.MapPool> mapPools = MapTools.GetMapPoolsBySeason(interactionHelper, season);
    return mapPools;
}).WithName("GetMapPoolsBySeason").WithOpenApi();

app.MapGet("/Scores/Qualifiers/{QualifierID}", (ulong QualifierID) =>
{
    List<DataTypes.QualifierScore> scores = ScoreTools.GetQualifierScores(interactionHelper, QualifierID);
    return scores;
}).WithName("GetQualifierScores").WithOpenApi();

app.MapGet("/Matches/GetAll", () =>
{
    List<DataTypes.Match> matches = MatchTools.GetMatches(interactionHelper);
    return matches;
}).WithName("GetMatches").WithOpenApi();

app.MapGet("/Matches/GetMatchById/{MID}", (ulong MID) =>
{
    DataTypes.Match match = MatchTools.GetMatchById(interactionHelper, MID);
    return match;
}).WithName("GetMatchById").WithOpenApi();

app.MapGet("/Matches/GetMatchesBySeason/{Season}", (int Season) =>
{
    List<DataTypes.Match> matches = MatchTools.GetMatchesBySeason(interactionHelper, Season);
    return matches;
}).WithName("GetMatchesBySeason").WithOpenApi();

app.Run();