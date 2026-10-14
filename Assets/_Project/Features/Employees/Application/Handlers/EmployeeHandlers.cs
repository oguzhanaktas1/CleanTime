using System.Collections.Generic;
using System.Linq;
using CleanTime.Company.Domain;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Employees.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Employees.Application
{
    internal sealed class HireEmployeeHandler : ICommandHandler<HireEmployeeCommand>
    {
        private readonly IEmployeeDefinitionProvider _definitions;
        private readonly IEmployeeRepository _employees;
        private readonly ICompanyRepository _company;
        private readonly IWalletService _wallet;
        private readonly IIdGenerator _ids;
        private readonly IGameClock _clock;
        private readonly IEventBus _events;

        public HireEmployeeHandler(
            IEmployeeDefinitionProvider definitions,
            IEmployeeRepository employees,
            ICompanyRepository company,
            IWalletService wallet,
            IIdGenerator ids,
            IGameClock clock,
            IEventBus events)
        {
            _definitions = definitions;
            _employees = employees;
            _company = company;
            _wallet = wallet;
            _ids = ids;
            _clock = clock;
            _events = events;
        }

        public Result Handle(HireEmployeeCommand command)
        {
            var definition = _definitions.Get(command.DefinitionId);
            if (definition.IsFailure)
                return Result.Failure(EmployeeErrors.UnknownDefinition, $"Unknown employee '{command.DefinitionId}'.");

            var company = _company.Get();
            if (company.IsFailure)
                return Result.Failure(EmployeeErrors.NoCompany, "Create a company before hiring employees.");

            if (_employees.GetAll().Count >= company.Value.EmployeeCapacity)
                return Result.Failure(EmployeeErrors.CapacityReached, "The company cannot employ more people.");

            var paid = _wallet.Pay(definition.Value.HireCost, TransactionReason.EmployeeHire, command.DefinitionId.Value);
            if (paid.IsFailure)
                return paid;

            var employee = new Employee(
                new EmployeeId(_ids.NewId()),
                command.DefinitionId,
                definition.Value.BaseStats,
                _clock.Now.Day);

            _employees.Add(employee);
            _events.Publish(new EmployeeHiredEvent(employee.Id, employee.DefinitionId));
            return Result.Success();
        }
    }

    internal sealed class FireEmployeeHandler : ICommandHandler<FireEmployeeCommand>
    {
        private readonly IEmployeeRepository _employees;
        private readonly IEmployeeAssignmentRepository _assignments;
        private readonly IEventBus _events;

        public FireEmployeeHandler(IEmployeeRepository employees, IEmployeeAssignmentRepository assignments, IEventBus events)
        {
            _employees = employees;
            _assignments = assignments;
            _events = events;
        }

        public Result Handle(FireEmployeeCommand command)
        {
            var employee = _employees.Get(command.EmployeeId);
            if (employee.IsFailure)
                return employee;

            var working = _assignments.GetWorkingFor(command.EmployeeId);
            if (working.IsSuccess)
                _assignments.Remove(working.Value.Id);

            _employees.Remove(command.EmployeeId);
            _events.Publish(new EmployeeFiredEvent(command.EmployeeId));
            return Result.Success();
        }
    }

    internal sealed class AssignEmployeeHandler : ICommandHandler<AssignEmployeeCommand>
    {
        private readonly IEmployeeRepository _employees;
        private readonly IEmployeeAssignmentRepository _assignments;
        private readonly IJobDefinitionProvider _jobs;
        private readonly IIdGenerator _ids;
        private readonly IRandomSource _random;
        private readonly IGameClock _clock;
        private readonly IEventBus _events;

        public AssignEmployeeHandler(
            IEmployeeRepository employees,
            IEmployeeAssignmentRepository assignments,
            IJobDefinitionProvider jobs,
            IIdGenerator ids,
            IRandomSource random,
            IGameClock clock,
            IEventBus events)
        {
            _employees = employees;
            _assignments = assignments;
            _jobs = jobs;
            _ids = ids;
            _random = random;
            _clock = clock;
            _events = events;
        }

        public Result Handle(AssignEmployeeCommand command)
        {
            var employee = _employees.Get(command.EmployeeId);
            if (employee.IsFailure)
                return employee;

            if (!employee.Value.IsIdle)
                return Result.Failure(EmployeeErrors.NotIdle, "The employee is already busy.");

            var job = _jobs.Get(command.JobDefinitionId);
            if (job.IsFailure)
                return Result.Failure(EmployeeErrors.UnknownJob, $"Unknown job '{command.JobDefinitionId}'.");

            var day = _clock.Now.Day;
            var finishDay = day + employee.Value.EstimateDurationDays(job.Value.Difficulty);

            var assigned = employee.Value.Assign();
            if (assigned.IsFailure)
                return assigned;

            employee.Value.Start();

            _assignments.Add(new EmployeeAssignment(
                _ids.NewId(),
                command.EmployeeId,
                command.JobDefinitionId,
                day,
                finishDay,
                _random.Next(0, int.MaxValue)));
            _employees.Save(employee.Value);

            _events.Publish(new EmployeeAssignedEvent(command.EmployeeId, command.JobDefinitionId, finishDay));
            _events.Publish(new EmployeeJobStartedEvent(command.EmployeeId, command.JobDefinitionId));
            return Result.Success();
        }
    }

    internal sealed class CancelEmployeeAssignmentHandler : ICommandHandler<CancelEmployeeAssignmentCommand>
    {
        private readonly IEmployeeRepository _employees;
        private readonly IEmployeeAssignmentRepository _assignments;

        public CancelEmployeeAssignmentHandler(IEmployeeRepository employees, IEmployeeAssignmentRepository assignments)
        {
            _employees = employees;
            _assignments = assignments;
        }

        public Result Handle(CancelEmployeeAssignmentCommand command)
        {
            var employee = _employees.Get(command.EmployeeId);
            if (employee.IsFailure)
                return employee;

            var working = _assignments.GetWorkingFor(command.EmployeeId);
            if (working.IsFailure)
                return Result.Failure(EmployeeErrors.NoAssignment, "The employee has no assignment to cancel.");

            var cancelled = employee.Value.CancelAssignment();
            if (cancelled.IsFailure)
                return cancelled;

            _assignments.Remove(working.Value.Id);
            _employees.Save(employee.Value);
            return Result.Success();
        }
    }

    internal sealed class ResolveEmployeeJobsHandler : ICommandHandler<ResolveEmployeeJobsCommand>
    {
        private readonly IEmployeeRepository _employees;
        private readonly IEmployeeAssignmentRepository _assignments;
        private readonly IJobDefinitionProvider _jobs;
        private readonly IEmployeeJobSimulator _simulator;
        private readonly IEmployeeRevenueCalculator _revenue;
        private readonly IEventBus _events;

        public ResolveEmployeeJobsHandler(
            IEmployeeRepository employees,
            IEmployeeAssignmentRepository assignments,
            IJobDefinitionProvider jobs,
            IEmployeeJobSimulator simulator,
            IEmployeeRevenueCalculator revenue,
            IEventBus events)
        {
            _employees = employees;
            _assignments = assignments;
            _jobs = jobs;
            _simulator = simulator;
            _revenue = revenue;
            _events = events;
        }

        public Result Handle(ResolveEmployeeJobsCommand command)
        {
            foreach (var assignment in _assignments.GetDue(command.UpToDay).ToList())
            {
                var employee = _employees.Get(assignment.EmployeeId);
                var job = _jobs.Get(assignment.JobDefinitionId);

                if (employee.IsFailure || job.IsFailure)
                {
                    _assignments.Remove(assignment.Id);
                    continue;
                }

                var outcome = _simulator.Simulate(employee.Value, job.Value, assignment.DeterministicSeed);
                var revenue = _revenue.Calculate(employee.Value, outcome);

                assignment.Resolve(outcome.Quality, outcome.Reward);
                _assignments.Save(assignment);

                employee.Value.Resolve();
                employee.Value.Collect();
                _employees.Save(employee.Value);

                _events.Publish(new EmployeeJobCompletedEvent(employee.Value.Id, job.Value.Id, outcome.Quality));

                if (revenue.Cents > 0)
                    _events.Publish(new EmployeeRevenueGeneratedEvent(employee.Value.Id, revenue));
            }

            return Result.Success();
        }
    }

    internal sealed class GetEmployeesHandler : IQueryHandler<GetEmployeesQuery, IReadOnlyList<Employee>>
    {
        private readonly IEmployeeRepository _employees;

        public GetEmployeesHandler(IEmployeeRepository employees)
        {
            _employees = employees;
        }

        public IReadOnlyList<Employee> Handle(GetEmployeesQuery query) => _employees.GetAll();
    }

    internal sealed class GetAvailableEmployeesHandler : IQueryHandler<GetAvailableEmployeesQuery, IReadOnlyList<Employee>>
    {
        private readonly IEmployeeRepository _employees;

        public GetAvailableEmployeesHandler(IEmployeeRepository employees)
        {
            _employees = employees;
        }

        public IReadOnlyList<Employee> Handle(GetAvailableEmployeesQuery query) =>
            _employees.GetAll().Where(e => e.IsIdle).ToList();
    }
}
