using CleanTime.Shared.Kernel;

namespace CleanTime.Employees.Domain
{
    public sealed record EmployeeHiredEvent(EmployeeId EmployeeId, EmployeeDefinitionId DefinitionId) : IDomainEvent;

    public sealed record EmployeeFiredEvent(EmployeeId EmployeeId) : IDomainEvent;

    public sealed record EmployeeAssignedEvent(EmployeeId EmployeeId, JobDefinitionId JobDefinitionId, int FinishDay) : IDomainEvent;

    public sealed record EmployeeJobStartedEvent(EmployeeId EmployeeId, JobDefinitionId JobDefinitionId) : IDomainEvent;

    public sealed record EmployeeJobCompletedEvent(EmployeeId EmployeeId, JobDefinitionId JobDefinitionId, float Quality) : IDomainEvent;

    public sealed record EmployeeRevenueGeneratedEvent(EmployeeId EmployeeId, Money Revenue) : IDomainEvent;
}
