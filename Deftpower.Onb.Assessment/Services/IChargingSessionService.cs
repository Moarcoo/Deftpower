using Deftpower.Onb.Assessment.Models;
using Deftpower.Onb.Assessment.Pagination;

namespace Deftpower.Onb.Assessment.Services;

public interface IChargingSessionService
{
    public Task<CreateChargingSessionResponse> CreateChargingSession(CreateChargingSessionRequest request, CancellationToken cancellationToken);
    public Task UpdateChargingSession(UpdateChargingSessionRequest request, CancellationToken cancellationToken);
    public Task<IReadOnlyList<ChargingSessionResponse>> GetAllChargingSessions(string userId, PaginationParams paginationParams, CancellationToken cancellationToken);
}