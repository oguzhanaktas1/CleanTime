using System.Collections.Generic;
using System.Linq;
using CleanTime.Employees.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Employees.Infrastructure
{
    public sealed class InMemoryEmployeeRepository : IEmployeeRepository
    {
        private readonly Dictionary<EmployeeId, Employee> _employees = new Dictionary<EmployeeId, Employee>();

        public Result<Employee> Get(EmployeeId id) => _employees.TryGetValue(id, out var employee)
            ? Result<Employee>.Success(employee)
            : Result<Employee>.Failure(EmployeeErrors.NotFound, $"Unknown employee '{id}'.");

        public IReadOnlyList<Employee> GetAll() => _employees.Values.ToList();

        public Result Add(Employee employee)
        {
            _employees[employee.Id] = employee;
            return Result.Success();
        }

        public Result Save(Employee employee)
        {
            _employees[employee.Id] = employee;
            return Result.Success();
        }

        public Result Remove(EmployeeId id)
        {
            _employees.Remove(id);
            return Result.Success();
        }
    }

    public sealed class InMemoryEmployeeAssignmentRepository : IEmployeeAssignmentRepository
    {
        private readonly Dictionary<string, EmployeeAssignment> _assignments = new Dictionary<string, EmployeeAssignment>();

        public Result Add(EmployeeAssignment assignment)
        {
            _assignments[assignment.Id] = assignment;
            return Result.Success();
        }

        public Result Save(EmployeeAssignment assignment)
        {
            _assignments[assignment.Id] = assignment;
            return Result.Success();
        }

        public Result<EmployeeAssignment> GetWorkingFor(EmployeeId employeeId)
        {
            var found = _assignments.Values.FirstOrDefault(a =>
                a.EmployeeId == employeeId && a.Status == AssignmentStatus.Working);

            return found != null
                ? Result<EmployeeAssignment>.Success(found)
                : Result<EmployeeAssignment>.Failure(EmployeeErrors.NoAssignment, "No working assignment.");
        }

        public IReadOnlyList<EmployeeAssignment> GetDue(int day) =>
            _assignments.Values.Where(a => a.IsDue(day)).OrderBy(a => a.FinishDay).ThenBy(a => a.Id).ToList();

        public IReadOnlyList<EmployeeAssignment> GetAll() => _assignments.Values.ToList();

        public void Remove(string assignmentId) => _assignments.Remove(assignmentId);
    }

    public sealed class EmployeesInfrastructureModule : FeatureModule
    {
        public override string Name => "Employees.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IEmployeeRepository>(_ => new InMemoryEmployeeRepository());
            services.RegisterSingleton<IEmployeeAssignmentRepository>(_ => new InMemoryEmployeeAssignmentRepository());
        }
    }
}
