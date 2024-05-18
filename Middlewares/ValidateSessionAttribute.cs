using LobbyAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LobbyAPI.Middlewares;
public class ValidateSessionAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue("sessionId", out var sessionId))
        {
            context.Result = new BadRequestObjectResult("Session ID is required.");
            return;
        }

        var sessionRepo = httpContext.RequestServices.GetService<ISessionRepository>();
        var session = await sessionRepo.Valid(sessionId); 
        if (!session)
        {
            context.Result = new UnauthorizedObjectResult("Invalid Session ID.");
            return;
        }
        
        // Store sessionId in HttpContext.Items
        httpContext.Items["SessionID"] = sessionId;

        // Set sessionId in the static class
        CustomContexts.SessionId = sessionId;

        // Log before continuing to the next action
        Console.WriteLine("Session ID before next: " + httpContext.Items["SessionID"]);

        await next();

        // Log after all subsequent actions and middlewares have executed
        Console.WriteLine("Session ID after next: " + httpContext.Items["SessionID"]);
    }
}

public static class CustomContexts
{
    public static string SessionId { get; set; }
}