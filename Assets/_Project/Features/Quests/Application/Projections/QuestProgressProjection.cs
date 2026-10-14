using System;
using System.Collections.Generic;
using CleanTime.Cleaning.Domain;
using CleanTime.Dialogue.Domain;
using CleanTime.Economy.Domain;
using CleanTime.Employees.Domain;
using CleanTime.Equipment.Domain;
using CleanTime.Gifts.Domain;
using CleanTime.Jobs.Domain;
using CleanTime.Quests.Domain;
using CleanTime.Relationships.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Quests.Application
{
    /// <summary>
    /// Observes domain events from other modules and feeds them to active quests as generic signals,
    /// so no feature needs quest-specific checks (architecture §41, §129).
    /// </summary>
    internal sealed class QuestProgressProjection : IDisposable
    {
        private readonly IQuestRepository _quests;
        private readonly IQuestObjectiveEvaluator _evaluator;
        private readonly IEventBus _events;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public QuestProgressProjection(IEventBus events, IQuestRepository quests, IQuestObjectiveEvaluator evaluator)
        {
            _events = events;
            _quests = quests;
            _evaluator = evaluator;

            Observe<CleaningTargetCompletedEvent>(e => QuestSignal.CleanTarget(e.DirtTypeId.Value));
            Observe<PropertyCompletedEvent>(e => QuestSignal.CleanProperty(e.LocationId.Value));
            Observe<JobCompletedEvent>(e => QuestSignal.CompleteJob(e.DefinitionId.Value));
            Observe<MoneyCreditedEvent>(e => QuestSignal.EarnMoney(e.Amount.Cents));
            Observe<GiftGivenEvent>(e => QuestSignal.GiveGift(e.NpcId.Value));
            Observe<EmployeeHiredEvent>(e => QuestSignal.HireEmployee(e.DefinitionId.Value));
            Observe<EquipmentPurchasedEvent>(e => QuestSignal.BuyEquipment(e.EquipmentId.Value));
            Observe<RelationshipLevelChangedEvent>(e => QuestSignal.RelationshipLevel(e.NpcId.Value, e.Level));
            Observe<DialogueEndedEvent>(e => e.NpcId.IsEmpty ? null : QuestSignal.TalkToNpc(e.NpcId.Value));
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        private void Observe<T>(Func<T, QuestSignal> toSignal)
            where T : IDomainEvent
        {
            _subscriptions.Add(_events.Subscribe<T>(evt =>
            {
                var signal = toSignal(evt);
                if (signal != null)
                    Apply(signal);
            }));
        }

        private void Apply(QuestSignal signal)
        {
            foreach (var quest in _quests.GetActive())
            {
                if (!quest.TryApply(signal, _evaluator))
                    continue;

                _quests.Save(quest);
                _events.Publish(new QuestProgressChangedEvent(quest.Id));

                if (quest.TryComplete())
                {
                    _quests.Save(quest);
                    _events.Publish(new QuestCompletedEvent(quest.Id));
                }
            }
        }
    }
}
