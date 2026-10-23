using Deftpower.Onb.Assessment.Models;
using Deftpower.Onb.Assessment.Pagination;

namespace Deftpower.Onb.Assessment.Repositories;

public interface IChargingSessionRepository
{
    public Task<ChargingSession> Add(ChargingSession chargingSession, CancellationToken cancellationToken);

    public Task<ChargingSession> Update(ChargingSession chargingSession, CancellationToken cancellationToken);

    public Task<IReadOnlyList<ChargingSession>> GetAll(string userId, PaginationParams paginationParams, CancellationToken cancellationToken);

    public Task<ChargingSession?> GetBySessionId(string sessionId, CancellationToken cancellationToken);
}