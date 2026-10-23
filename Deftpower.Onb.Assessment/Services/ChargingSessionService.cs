using Deftpower.Onb.Assessment.Models;
using Deftpower.Onb.Assessment.Pagination;
using Deftpower.Onb.Assessment.Repositories;

namespace Deftpower.Onb.Assessment.Services;

public class ChargingSessionService(IChargingSessionRepository repository, IUserRepository userRepository) : IChargingSessionService
{
    public async Task<CreateChargingSessionResponse> CreateChargingSession(CreateChargingSessionRequest request, CancellationToken cancellationToken)
    {
        if (!request.IsValid())
        {
            throw new ArgumentException("Invalid request");
        }
        
        // User validation/auth
        if (!await userRepository.IsActive(request.UserId, cancellationToken))
        {
            throw new UnauthorizedAccessException("User is not allowed to start a charging session");
        }

        var newSession = ChargingSession.Create(request.UserId, request.ChargePointId,  request.StartTime,  request.EndTime);
        
        newSession = await repository.Add(newSession, cancellationToken);

        return new CreateChargingSessionResponse
        {
            SessionId = newSession.SessionId,
        };
    }

    public async Task UpdateChargingSession(UpdateChargingSessionRequest request, CancellationToken cancellationToken)
    {
        if (!request.IsValid())
        {
            throw new ArgumentException("Invalid request");
        }

        var session = await repository.GetBySessionId(request.SessionId, cancellationToken);
        ArgumentNullException.ThrowIfNull(session);

        // session owner validation/auth

        session.Update(request.EndTime, request.EnergyKwh);
        
        await repository.Update(session, cancellationToken);
    }

    public async Task<IReadOnlyList<ChargingSessionResponse>> GetAllChargingSessions(string userId, PaginationParams paginationParams, CancellationToken cancellationToken)
    {
        // only for active user

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("Invalid user id", nameof(userId));
        }

        if (!paginationParams.IsValid())
        {
            throw new ArgumentException("Invalid pagination params",  nameof(paginationParams));
        }
        
        var sessions = await repository.GetAll(userId, paginationParams, cancellationToken);
        return sessions.Select(x => new ChargingSessionResponse
        {
            SessionId = x.SessionId,
            UserId = x.UserId,
            ChargePointId = x.ChargePointId,
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            EnergyKwh = x.EnergyKwh,
        }).ToList();
    }
}