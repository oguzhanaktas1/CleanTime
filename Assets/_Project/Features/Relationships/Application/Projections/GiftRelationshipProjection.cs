using System;
using CleanTime.Gifts.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Relationships.Application
{
    /// <summary>GiftGiven -> relationship points (architecture §136).</summary>
    internal sealed class GiftRelationshipProjection : IDisposable
    {
        private readonly IDisposable _subscription;

        public GiftRelationshipProjection(IEventBus events, ICommandHandler<ApplyRelationshipChangeCommand> apply)
        {
            _subscription = events.Subscribe<GiftGivenEvent>(
                e => apply.Handle(new ApplyRelationshipChangeCommand(e.NpcId, e.RelationshipGain)));
        }

        public void Dispose() => _subscription.Dispose();
    }
}
