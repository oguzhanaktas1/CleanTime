using CleanTime.Relationships.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Relationships.Application
{
    internal sealed class ApplyRelationshipChangeHandler : ICommandHandler<ApplyRelationshipChangeCommand>
    {
        private readonly IRelationshipRepository _relationships;
        private readonly IRelationshipLevelPolicy _levels;
        private readonly IGameClock _clock;
        private readonly IEventBus _events;

        public ApplyRelationshipChangeHandler(
            IRelationshipRepository relationships,
            IRelationshipLevelPolicy levels,
            IGameClock clock,
            IEventBus events)
        {
            _relationships = relationships;
            _levels = levels;
            _clock = clock;
            _events = events;
        }

        public Result Handle(ApplyRelationshipChangeCommand command)
        {
            var found = _relationships.Get(command.NpcId);
            if (found.IsFailure)
                return found;

            var relationship = found.Value;
            relationship.AddPoints(command.Delta, _clock.Now.Day);
            _events.Publish(new RelationshipPointsChangedEvent(command.NpcId, command.Delta, relationship.Points));

            EvaluateLevel(relationship);
            return _relationships.Save(relationship);
        }

        /// <summary>Raises level / milestone events for every level crossed (a big gift can skip several).</summary>
        private void EvaluateLevel(Relationship relationship)
        {
            var previous = relationship.Level;
            var next = _levels.LevelFor(relationship.NpcId, relationship.Points);

            if (!relationship.SetLevel(next))
                return;

            _events.Publish(new RelationshipLevelChangedEvent(relationship.NpcId, previous, next));

            for (var level = previous + 1; level <= next; level++)
            {
                var threshold = _levels.ThresholdAt(relationship.NpcId, level);
                if (threshold != null && threshold.MilestoneId.Length > 0)
                    _events.Publish(new RelationshipMilestoneReachedEvent(relationship.NpcId, threshold.MilestoneId, level));
            }
        }
    }

    internal sealed class GetRelationshipHandler : IQueryHandler<GetRelationshipQuery, Result<Relationship>>
    {
        private readonly IRelationshipRepository _relationships;

        public GetRelationshipHandler(IRelationshipRepository relationships)
        {
            _relationships = relationships;
        }

        public Result<Relationship> Handle(GetRelationshipQuery query) => _relationships.Get(query.NpcId);
    }
}
