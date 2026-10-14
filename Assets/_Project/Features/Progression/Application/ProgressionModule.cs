using System.Collections.Generic;
using CleanTime.Locations.Domain;
using CleanTime.Progression.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Progression.Application
{
    /// <summary>Balance knobs for player-wide progression. Placeholder defaults until the design is final.</summary>
    public sealed class ProgressionSettings
    {
        public ProgressionSettings(int companyUnlockJobCount = 18, IReadOnlyList<int> jobTierThresholds = null)
        {
            CompanyUnlockJobCount = companyUnlockJobCount;
            JobTierThresholds = jobTierThresholds ?? new[] { 6, 12, 24, 36 };
        }

        public int CompanyUnlockJobCount { get; }

        public IReadOnlyList<int> JobTierThresholds { get; }
    }

    public sealed class ProgressionModule : FeatureModule
    {
        private readonly ProgressionSettings _settings;

        public ProgressionModule(ProgressionSettings settings = null)
        {
            _settings = settings ?? new ProgressionSettings();
        }

        public override string Name => "Progression";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton<IProgressionRule>(_ => new CompanyUnlockRule(_settings.CompanyUnlockJobCount));
            services.RegisterSingleton<IProgressionRule>(_ => new JobTierRule(_settings.JobTierThresholds));

            services.RegisterSingleton<ICommandHandler<UnlockLocationCommand>>(r => new UnlockLocationHandler(
                r.Resolve<ILocationProgressRepository>(), r.Resolve<IEventBus>()));
            services.RegisterSingleton<ICommandHandler<UnlockCompanyCommand>>(r => new UnlockCompanyHandler(
                r.Resolve<IProgressionRepository>(), r.Resolve<IEventBus>()));
            services.RegisterSingleton<ICommandHandler<ApplyStoryMilestoneCommand>>(r => new ApplyStoryMilestoneHandler(
                r.Resolve<IProgressionRepository>(), r.Resolve<IEventBus>()));

            services.RegisterSingleton<ICommandHandler<EvaluateProgressionCommand>>(r => new EvaluateProgressionHandler(
                r.Resolve<IProgressionRepository>(),
                r.Resolve<ILocationDefinitionProvider>(),
                r.Resolve<ILocationProgressRepository>(),
                r.Resolve<LocationUnlockEvaluator>(),
                r.ResolveAll<IProgressionRule>(),
                r.Resolve<ICommandHandler<UnlockLocationCommand>>(),
                r.Resolve<ICommandHandler<UnlockCompanyCommand>>(),
                r.Resolve<ICommandHandler<ApplyStoryMilestoneCommand>>(),
                r.Resolve<IEventBus>()));

            services.RegisterSingleton(r => new ProgressionProjection(
                r.Resolve<IProgressionRepository>(),
                r.Resolve<ICommandHandler<EvaluateProgressionCommand>>(),
                r.Resolve<IEventBus>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<ProgressionProjection>();
        }
    }
}
