using LobbyAPI.Interfaces;

namespace LobbyAPI.Middlewares;

public class SessionIDMiddleware
{
    private readonly RequestDelegate _next;
    
    public SessionIDMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, IPlayerRepository playerRepo)
    {
        if (!context.Request.Headers.TryGetValue("sessionId", out var sessionId))
        {
            context.Response.StatusCode = 400; // Bad Request
            await context.Response.WriteAsync("Session ID is required.");
            return;
        }

        var player = await playerRepo.GetPlayer(sessionId);
        if (player == null)
        {
            context.Response.StatusCode = 401; // Unauthorized
            await context.Response.WriteAsync("Invalid Session ID.");
            return;
        }

        // Set the player context for downstream use if needed
        context.Items["Player"] = player;

        await _next(context);
    }
}