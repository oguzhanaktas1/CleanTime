using System.Collections.Generic;
using CleanTime.Employees.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Employees.Application
{
    public sealed record HireEmployeeCommand(EmployeeDefinitionId DefinitionId) : ICommand;

    public sealed record FireEmployeeCommand(EmployeeId EmployeeId) : ICommand;

    public sealed record AssignEmployeeCommand(EmployeeId EmployeeId, JobDefinitionId JobDefinitionId) : ICommand;

    public sealed record CancelEmployeeAssignmentCommand(EmployeeId EmployeeId) : ICommand;

    /// <summary>Resolves every working assignment that finishes on or before the given day.</summary>
    public sealed record ResolveEmployeeJobsCommand(int UpToDay) : ICommand;

    public sealed record GetEmployeesQuery : IQuery<IReadOnlyList<Employee>>;

    /// <summary>Hired employees that are idle and can take an assignment.</summary>
    public sealed record GetAvailableEmployeesQuery : IQuery<IReadOnlyList<Employee>>;
}
