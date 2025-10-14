public interface IGoalPeriodService
{
    Task<List<string>> GetAllGoalPeriodsAsync(CancellationToken cancellationToken = default);
}
