using FishyAPI.Tools;

namespace FishyAPI.Routes;

public static class MatchWebSocketRoutes
{
    public static void MapMatchWebSocketRoutes(WebApplication app)
    {
        // Connect with: ws://host/ws/match/{matchId}
        // Clients receive JSON events when scores are posted or picks/bans are entered.
        // Event shape: { "event": "<eventType>", "matchId": <id>, "data": { ... } }
        //
        // Event types:
        //   score_posted      — a match score was submitted
        //   score_updated     — an existing match score was corrected
        //   pick_added        — a map was picked
        //   ban_added         — a map was banned
        //   pick_ban_removed  — a pick/ban entry was deleted
        //   match_updated     — match metadata (status, bestOf, etc.) changed
        //   replay_called     — a player called a replay; includes voided scores and updated match state

        app.MapGet("/ws/match/{matchId}", async (HttpContext context, ulong matchId) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("WebSocket connection required.");
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await MatchWebSocketManager.HandleConnection(matchId, socket);
        }).AllowAnonymous();
    }
}
