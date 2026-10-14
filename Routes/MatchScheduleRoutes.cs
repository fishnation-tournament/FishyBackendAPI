using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;
using Sprache;

namespace FishyAPI.Routes;

public static class MatchScheduleRoutes
{
    public static void MapMatchScheduleRoutes(WebApplication app, ConnectionManager connManager)
    {
        app.MapGet("/Schedule/Upcoming", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(MatchScheduleTools.GetUpcomingSchedules(conn));
        }).WithName("GetUpcomingSchedules").WithOpenApi();

        app.MapGet("/Schedule/All", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(MatchScheduleTools.GetSchedules(conn));
        }).WithName("GetSchedules").WithOpenApi();

        app.MapGet("/Schedule/Match/{matchId}", (ulong matchId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(MatchScheduleTools.GetSchedulesByMatch(conn, matchId));
        }).WithName("GetSchedulesByMatch").WithOpenApi();

        app.MapGet("/Schedule/{scheduleId}", (int scheduleId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchSchedule? schedule = MatchScheduleTools.GetScheduleById(conn, scheduleId);
            return schedule.HasValue ? Results.Ok(schedule.Value) : Results.NotFound();
        }).WithName("GetScheduleById").WithOpenApi();

        app.MapPost("/Schedule", (CreateMatchScheduleRequest request) =>
        {
            if (request.ScheduledAt == default)
                return Results.BadRequest("ScheduledAt is required.");

            using MySqlConnection conn = connManager.IssueConnection();
            MatchSchedule created = MatchScheduleTools.CreateSchedule(conn, request);
            return Results.Created($"/Schedule/{created.ScheduleID}", created);
        }).WithName("CreateMatchSchedule").WithOpenApi().RequirePermission(Permission.ScheduleManage);

        app.MapDelete("/Schedule/{scheduleId}", (int scheduleId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            bool deleted = MatchScheduleTools.DeleteSchedule(conn, scheduleId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteMatchSchedule").WithOpenApi().RequirePermission(Permission.ScheduleManage);
    }
}
