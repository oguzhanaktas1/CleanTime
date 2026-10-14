using System.Collections.Generic;
using CleanTime.Company.Domain;
using CleanTime.Economy.Application;
using CleanTime.Employees.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Employees.Application
{
    public sealed class EmployeesModule : FeatureModule
    {
        private readonly IEmployeeRevenueCalculator _revenueCalculator;

        public EmployeesModule(IEmployeeRevenueCalculator revenueCalculator = null)
        {
            _revenueCalculator = revenueCalculator ?? new ShareEmployeeRevenueCalculator();
        }

        public override string Name => "Employees";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IEmployeeJobSimulator>(_ => new EmployeeJobSimulator());
            services.RegisterInstance(_revenueCalculator);

            services.RegisterSingleton<ICommandHandler<HireEmployeeCommand>>(r => new HireEmployeeHandler(
                r.Resolve<IEmployeeDefinitionProvider>(),
                r.Resolve<IEmployeeRepository>(),
                r.Resolve<ICompanyRepository>(),
                r.Resolve<IWalletService>(),
                r.Resolve<IIdGenerator>(),
                r.Resolve<IGameClock>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<FireEmployeeCommand>>(r => new FireEmployeeHandler(
                r.Resolve<IEmployeeRepository>(),
                r.Resolve<IEmployeeAssignmentRepository>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<AssignEmployeeCommand>>(r => new AssignEmployeeHandler(
                r.Resolve<IEmployeeRepository>(),
                r.Resolve<IEmployeeAssignmentRepository>(),
                r.Resolve<IJobDefinitionProvider>(),
                r.Resolve<IIdGenerator>(),
                r.Resolve<IRandomSource>(),
                r.Resolve<IGameClock>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<CancelEmployeeAssignmentCommand>>(r => new CancelEmployeeAssignmentHandler(
                r.Resolve<IEmployeeRepository>(),
                r.Resolve<IEmployeeAssignmentRepository>()));

            services.RegisterSingleton<ICommandHandler<ResolveEmployeeJobsCommand>>(r => new ResolveEmployeeJobsHandler(
                r.Resolve<IEmployeeRepository>(),
                r.Resolve<IEmployeeAssignmentRepository>(),
                r.Resolve<IJobDefinitionProvider>(),
                r.Resolve<IEmployeeJobSimulator>(),
                r.Resolve<IEmployeeRevenueCalculator>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<IQueryHandler<GetEmployeesQuery, IReadOnlyList<Employee>>>(
                r => new GetEmployeesHandler(r.Resolve<IEmployeeRepository>()));
            services.RegisterSingleton<IQueryHandler<GetAvailableEmployeesQuery, IReadOnlyList<Employee>>>(
                r => new GetAvailableEmployeesHandler(r.Resolve<IEmployeeRepository>()));

            services.RegisterSingleton(r => new EmployeeDayProjection(
                r.Resolve<IEventBus>(),
                r.Resolve<ICommandHandler<ResolveEmployeeJobsCommand>>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<EmployeeDayProjection>();
        }
    }
}
