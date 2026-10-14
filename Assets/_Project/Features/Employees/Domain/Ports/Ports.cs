using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Employees.Domain
{
    public interface IEmployeeDefinitionProvider : IDefinitionProvider<EmployeeDefinitionId, EmployeeDefinitionSnapshot>
    {
    }

    public interface IEmployeeRepository
    {
        Result<Employee> Get(EmployeeId id);

        IReadOnlyList<Employee> GetAll();

        Result Add(Employee employee);

        Result Save(Employee employee);

        Result Remove(EmployeeId id);
    }

    public interface IEmployeeAssignmentRepository
    {
        Result Add(EmployeeAssignment assignment);

        Result Save(EmployeeAssignment assignment);

        /// <summary>The unresolved assignment of the employee, if any.</summary>
        Result<EmployeeAssignment> GetWorkingFor(EmployeeId employeeId);

        IReadOnlyList<EmployeeAssignment> GetDue(int day);

        IReadOnlyList<EmployeeAssignment> GetAll();

        void Remove(string assignmentId);
    }
}
