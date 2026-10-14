using System;
using CleanTime.GameTime.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Employees.Application
{
    /// <summary>GameDayEnded -> resolve every employee job that is due (day-end flow, architecture §138).</summary>
    internal sealed class EmployeeDayProjection : IDisposable
    {
        private readonly IDisposable _subscription;

        public EmployeeDayProjection(IEventBus events, ICommandHandler<ResolveEmployeeJobsCommand> resolve)
        {
            _subscription = events.Subscribe<GameDayEndedEvent>(e => resolve.Handle(new ResolveEmployeeJobsCommand(e.Day)));
        }

        public void Dispose() => _subscription.Dispose();
    }
}
