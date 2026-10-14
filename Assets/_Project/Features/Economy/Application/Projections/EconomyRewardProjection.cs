using System;
using CleanTime.Economy.Domain;
using CleanTime.Employees.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Economy.Application
{
    /// <summary>Turns reward-bearing events from other modules into ledgered wallet credits.</summary>
    internal sealed class EconomyRewardProjection : IDisposable
    {
        private readonly IWalletService _wallet;
        private readonly ILogger _logger;
        private readonly IDisposable _jobs;
        private readonly IDisposable _employees;

        public EconomyRewardProjection(IEventBus events, IWalletService wallet, ILogger logger)
        {
            _wallet = wallet;
            _logger = logger;
            _jobs = events.Subscribe<JobCompletedEvent>(OnJobCompleted);
            _employees = events.Subscribe<EmployeeRevenueGeneratedEvent>(OnEmployeeRevenue);
        }

        public void Dispose()
        {
            _jobs.Dispose();
            _employees.Dispose();
        }

        private void OnJobCompleted(JobCompletedEvent evt) =>
            Grant(evt.Reward, TransactionReason.JobReward, evt.JobId.Value);

        private void OnEmployeeRevenue(EmployeeRevenueGeneratedEvent evt) =>
            Grant(evt.Revenue, TransactionReason.EmployeeRevenue, evt.EmployeeId.Value);

        private void Grant(Money amount, TransactionReason reason, string referenceId)
        {
            if (amount.Cents <= 0)
                return;

            var result = _wallet.Credit(amount, reason, referenceId);
            if (result.IsFailure)
                _logger.Warning($"Could not credit {reason} ({referenceId}): {result.Error}");
        }
    }
}
