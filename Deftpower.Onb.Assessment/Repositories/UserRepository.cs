namespace Deftpower.Onb.Assessment.Repositories;

public class UserRepository : IUserRepository
{
    private static readonly HashSet<string> BlacklistedUserIds = ["user-1", "Q"];

    public Task<bool> IsActive(string userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(!BlacklistedUserIds.Contains(userId));
    }
}
