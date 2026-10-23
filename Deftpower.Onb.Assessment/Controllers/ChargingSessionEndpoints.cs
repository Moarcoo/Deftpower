using Deftpower.Onb.Assessment.Models;
using Deftpower.Onb.Assessment.Pagination;
using Deftpower.Onb.Assessment.Services;

namespace Deftpower.Onb.Assessment.Controllers;

public static class ChargingSessionEndpoints 
{
    public static void AddChargingSessionEndpoints(this WebApplication app)
    {
        app.MapPost("/api/users/{userId}/sessions", async (IChargingSessionService chargingSessionService, string userId, CreateChargingSessionRequest  request, CancellationToken cancellationToken) =>
        {
             if (!string.Equals(userId, request.UserId, StringComparison.Ordinal))
             {
                 return Results.BadRequest("Route userId does not match request userId");
             }

             var session = await chargingSessionService.CreateChargingSession(request, cancellationToken);
             return Results.Ok(session);
        })
        .WithName("CreateChargingSession")
        .Produces<CreateChargingSessionResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        
        app.MapPut("/api/sessions", async (IChargingSessionService chargingSessionService, UpdateChargingSessionRequest  request, CancellationToken cancellationToken) =>
        {
            await chargingSessionService.UpdateChargingSession(request, cancellationToken);
            return Results.Ok();
        })
        .WithName("UpdateChargingSession")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        
        app.MapGet("/api/users/{userId}/sessions", async (IChargingSessionService chargingSessionService, string userId, [AsParameters] PaginationParams paginationParams, CancellationToken cancellationToken) =>
        {
            var sessions = await chargingSessionService.GetAllChargingSessions(userId, paginationParams, cancellationToken);
            if (sessions.Count > 0)
            {
                return Results.Ok(sessions);
            }

            return Results.NotFound();
        })
        .WithName("GetAllChargingSessions")
        .Produces<IReadOnlyList<ChargingSessionResponse>>()
        .Produces(StatusCodes.Status404NotFound);
    }
}   