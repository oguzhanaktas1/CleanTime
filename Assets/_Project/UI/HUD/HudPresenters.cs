using System.Collections.Generic;
using CleanTime.Economy.Application;
using CleanTime.Economy.Domain;
using CleanTime.Equipment.Application;
using CleanTime.Equipment.Domain;
using CleanTime.GameTime.Application;
using CleanTime.GameTime.Domain;
using CleanTime.Quests.Application;
using CleanTime.Quests.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.UI.Common;

namespace CleanTime.UI.HUD
{
    public interface IWalletView
    {
        void SetBalance(Money balance);
    }

    public interface IDayPhaseView
    {
        void SetDate(GameDate date);
    }

    public interface ISelectedEquipmentView
    {
        void SetSelected(EquipmentId equipmentId);
    }

    public interface IQuestTrackerView
    {
        void SetQuests(IReadOnlyList<QuestInstance> activeQuests);
    }

    public sealed class WalletPresenter : PresenterBase
    {
        public WalletPresenter(IEventBus events, IQueryHandler<GetPlayerWalletQuery, Money> wallet, IWalletView view)
        {
            view.SetBalance(wallet.Handle(new GetPlayerWalletQuery()));
            Track(events.Subscribe<MoneyCreditedEvent>(e => view.SetBalance(e.BalanceAfter)));
            Track(events.Subscribe<MoneyDebitedEvent>(e => view.SetBalance(e.BalanceAfter)));
        }
    }

    public sealed class DayPhasePresenter : PresenterBase
    {
        public DayPhasePresenter(IEventBus events, IQueryHandler<GetCurrentGameDateQuery, GameDate> date, IDayPhaseView view)
        {
            view.SetDate(date.Handle(new GetCurrentGameDateQuery()));
            Track(events.Subscribe<DayPhaseChangedEvent>(e => view.SetDate(new GameDate(e.Day, e.Current))));
            Track(events.Subscribe<GameDayStartedEvent>(e => view.SetDate(new GameDate(e.Day, DayPhase.Morning))));
        }
    }

    public sealed class SelectedEquipmentPresenter : PresenterBase
    {
        public SelectedEquipmentPresenter(IEventBus events, ISelectedEquipmentQuery selected, ISelectedEquipmentView view)
        {
            view.SetSelected(selected.GetSelected());
            Track(events.Subscribe<EquipmentChangedEvent>(e => view.SetSelected(e.Current)));
        }
    }

    public sealed class QuestTrackerPresenter : PresenterBase
    {
        private readonly IQueryHandler<GetActiveQuestsQuery, IReadOnlyList<QuestInstance>> _quests;
        private readonly IQuestTrackerView _view;

        public QuestTrackerPresenter(
            IEventBus events,
            IQueryHandler<GetActiveQuestsQuery, IReadOnlyList<QuestInstance>> quests,
            IQuestTrackerView view)
        {
            _quests = quests;
            _view = view;
            Refresh();

            Track(events.Subscribe<QuestActivatedEvent>(_ => Refresh()));
            Track(events.Subscribe<QuestProgressChangedEvent>(_ => Refresh()));
            Track(events.Subscribe<QuestCompletedEvent>(_ => Refresh()));
        }

        private void Refresh() => _view.SetQuests(_quests.Handle(new GetActiveQuestsQuery()));
    }
}
