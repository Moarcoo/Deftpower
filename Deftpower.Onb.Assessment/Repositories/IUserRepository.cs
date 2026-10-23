namespace Deftpower.Onb.Assessment.Repositories;

public interface IUserRepository
{
    Task<bool> IsActive(string userId, CancellationToken cancellationToken);
}