using System.Collections.Generic;
using CleanTime.Jobs.Domain;
using CleanTime.Locations.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Application
{
    public sealed class JobsModule : FeatureModule
    {
        private readonly CustomerEvaluationWeights _weights;
        private readonly JobEvaluationSettings _settings;
        private readonly IJobRewardCalculator _rewardCalculator;

        public JobsModule(
            CustomerEvaluationWeights weights = null,
            JobEvaluationSettings settings = null,
            IJobRewardCalculator rewardCalculator = null)
        {
            _weights = weights ?? CustomerEvaluationWeights.Default;
            _settings = settings ?? new JobEvaluationSettings();
            _rewardCalculator = rewardCalculator ?? new QualityScaledRewardCalculator();
        }

        public override string Name => "Jobs";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterInstance(_settings);
            services.RegisterSingleton<ICustomerRatingCalculator>(_ => new CustomerRatingCalculator(_weights));
            services.RegisterSingleton(_ => _rewardCalculator);
            services.RegisterSingleton<IJobAvailabilityPolicy>(
                r => new LocationUnlockedJobAvailabilityPolicy(r.Resolve<ILocationProgressRepository>()));

            services.RegisterSingleton<ICommandHandler<AcceptJobCommand>>(r => new AcceptJobHandler(
                r.Resolve<IJobDefinitionProvider>(),
                r.Resolve<IJobRepository>(),
                r.Resolve<IJobAvailabilityPolicy>(),
                r.Resolve<IIdGenerator>(),
                r.Resolve<IRandomSource>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<StartJobCommand>>(r => new StartJobHandler(
                r.Resolve<IJobRepository>(),
                r.Resolve<IGameClock>(),
                r.Resolve<IUtcClock>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<CompleteJobCommand>>(r => new CompleteJobHandler(
                r.Resolve<IJobRepository>(),
                r.Resolve<ICustomerRatingCalculator>(),
                r.Resolve<IJobRewardCalculator>(),
                r.Resolve<JobEvaluationSettings>(),
                r.Resolve<IGameClock>(),
                r.Resolve<IUtcClock>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<AbandonJobCommand>>(
                r => new AbandonJobHandler(r.Resolve<IJobRepository>(), r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<FinishJobResultCommand>>(
                r => new FinishJobResultHandler(r.Resolve<IJobRepository>(), r.Resolve<IEventBus>()));

            services.RegisterSingleton<IQueryHandler<GetAvailableJobsQuery, IReadOnlyList<JobDefinitionSnapshot>>>(
                r => new GetAvailableJobsHandler(r.Resolve<IJobDefinitionProvider>(), r.Resolve<IJobAvailabilityPolicy>()));

            services.RegisterSingleton<IQueryHandler<GetActiveJobQuery, Result<Job>>>(
                r => new GetActiveJobHandler(r.Resolve<IJobRepository>()));

            services.RegisterSingleton<IQueryHandler<GetJobProgressQuery, float>>(
                r => new GetJobProgressHandler(r.Resolve<IJobRepository>()));

            services.RegisterSingleton(r => new JobCoordinator(
                r.Resolve<IJobRepository>(),
                r.Resolve<ICommandHandler<CompleteJobCommand>>(),
                r.Resolve<IEventBus>(),
                r.Resolve<ILogger>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<JobCoordinator>();
        }
    }
}
