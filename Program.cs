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