using System.Collections.Generic;
using CleanTime.Locations.Domain;
using CleanTime.Progression.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Progression.Application
{
    internal sealed class UnlockLocationHandler : ICommandHandler<UnlockLocationCommand>
    {
        private readonly ILocationProgressRepository _locations;
        private readonly IEventBus _events;

        public UnlockLocationHandler(ILocationProgressRepository locations, IEventBus events)
        {
            _locations = locations;
            _events = events;
        }

        public Result Handle(UnlockLocationCommand command)
        {
            var progress = _locations.Get(command.LocationId);
            if (progress.IsFailure)
                return progress;

            if (progress.Value.Unlock())
            {
                _locations.Save(progress.Value);
                _events.Publish(new LocationUnlockedEvent(command.LocationId));
            }

            return Result.Success();
        }
    }

    internal sealed class UnlockCompanyHandler : ICommandHandler<UnlockCompanyCommand>
    {
        private readonly IProgressionRepository _progression;
        private readonly IEventBus _events;

        public UnlockCompanyHandler(IProgressionRepository progression, IEventBus events)
        {
            _progression = progression;
            _events = events;
        }

        public Result Handle(UnlockCompanyCommand command)
        {
            var current = _progression.Get();
            if (current.IsFailure)
                return current;

            if (current.Value.UnlockCompany())
            {
                _progression.Save(current.Value);
                _events.Publish(new CompanyUnlockedEvent());
            }

            return Result.Success();
        }
    }

    internal sealed class ApplyStoryMilestoneHandler : ICommandHandler<ApplyStoryMilestoneCommand>
    {
        private readonly IProgressionRepository _progression;
        private readonly IEventBus _events;

        public ApplyStoryMilestoneHandler(IProgressionRepository progression, IEventBus events)
        {
            _progression = progression;
            _events = events;
        }

        public Result Handle(ApplyStoryMilestoneCommand command)
        {
            var current = _progression.Get();
            if (current.IsFailure)
                return current;

            if (current.Value.ReachMilestone(command.MilestoneId))
            {
                _progression.Save(current.Value);
                _events.Publish(new StoryMilestoneReachedEvent(command.MilestoneId));
            }

            return Result.Success();
        }
    }

    /// <summary>Evaluates progression rules and location unlock requirements and applies what is earned.</summary>
    internal sealed class EvaluateProgressionHandler : ICommandHandler<EvaluateProgressionCommand>
    {
        private readonly IProgressionRepository _progression;
        private readonly ILocationDefinitionProvider _definitions;
        private readonly ILocationProgressRepository _locations;
        private readonly LocationUnlockEvaluator _unlockEvaluator;
        private readonly IReadOnlyList<IProgressionRule> _rules;
        private readonly ICommandHandler<UnlockLocationCommand> _unlockLocation;
        private readonly ICommandHandler<UnlockCompanyCommand> _unlockCompany;
        private readonly ICommandHandler<ApplyStoryMilestoneCommand> _milestone;
        private readonly IEventBus _events;

        public EvaluateProgressionHandler(
            IProgressionRepository progression,
            ILocationDefinitionProvider definitions,
            ILocationProgressRepository locations,
            LocationUnlockEvaluator unlockEvaluator,
            IReadOnlyList<IProgressionRule> rules,
            ICommandHandler<UnlockLocationCommand> unlockLocation,
            ICommandHandler<UnlockCompanyCommand> unlockCompany,
            ICommandHandler<ApplyStoryMilestoneCommand> milestone,
            IEventBus events)
        {
            _progression = progression;
            _definitions = definitions;
            _locations = locations;
            _unlockEvaluator = unlockEvaluator;
            _rules = rules;
            _unlockLocation = unlockLocation;
            _unlockCompany = unlockCompany;
            _milestone = milestone;
            _events = events;
        }

        public Result Handle(EvaluateProgressionCommand command)
        {
            EvaluateLocations();
            return EvaluateRules();
        }

        private void EvaluateLocations()
        {
            var completions = new Dictionary<LocationId, int>();
            foreach (var progress in _locations.GetAll())
                completions[progress.LocationId] = progress.CompletionCount;

            var snapshot = new ProgressionSnapshot(completions);

            foreach (var definition in _definitions.GetAll())
            {
                var progress = _locations.Get(definition.Id);
                if (progress.IsFailure || progress.Value.Unlocked)
                    continue;

                if (_unlockEvaluator.IsUnlockable(definition, snapshot))
                    _unlockLocation.Handle(new UnlockLocationCommand(definition.Id));
            }
        }

        private Result EvaluateRules()
        {
            var current = _progression.Get();
            if (current.IsFailure)
                return current;

            foreach (var rule in _rules)
            {
                foreach (var unlock in rule.Evaluate(current.Value))
                {
                    switch (unlock.Kind)
                    {
                        case ProgressionUnlockKind.Company:
                            _unlockCompany.Handle(new UnlockCompanyCommand());
                            break;

                        case ProgressionUnlockKind.JobTier:
                            if (current.Value.RaiseJobTier(unlock.Value))
                            {
                                _progression.Save(current.Value);
                                _events.Publish(new JobTierUnlockedEvent(unlock.Value));
                            }

                            break;

                        case ProgressionUnlockKind.StoryMilestone:
                            _milestone.Handle(new ApplyStoryMilestoneCommand(unlock.Key));
                            break;
                    }
                }
            }

            return Result.Success();
        }
    }
}
