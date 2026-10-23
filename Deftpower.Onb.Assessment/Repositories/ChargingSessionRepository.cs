using Deftpower.Onb.Assessment.Data;
using Deftpower.Onb.Assessment.Models;
using Deftpower.Onb.Assessment.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Deftpower.Onb.Assessment.Repositories;

public class ChargingSessionRepository(ChargingSessionDbContext context) : IChargingSessionRepository
{
    public async Task<ChargingSession> Add(ChargingSession chargingSession, CancellationToken cancellationToken)
    {
        context.ChargingSessions.Add(chargingSession);
        await context.SaveChangesAsync(cancellationToken);
        return chargingSession;
    }

    public async Task<ChargingSession> Update(ChargingSession chargingSession, CancellationToken cancellationToken)
    {
        context.ChargingSessions.Update(chargingSession);
        await context.SaveChangesAsync(cancellationToken);
        return chargingSession;
    }

    public async Task<IReadOnlyList<ChargingSession>> GetAll(string userId, PaginationParams paginationParams, CancellationToken cancellationToken)
    {
        return await context.ChargingSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.StartTime)
            .Skip(paginationParams.Skip)
            .Take(paginationParams.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<ChargingSession?> GetBySessionId(string sessionId, CancellationToken cancellationToken)
    {
        return context.ChargingSessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
    }
}
