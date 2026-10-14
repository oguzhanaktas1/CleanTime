using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CleanTime.Cleaning.Presentation;
using CleanTime.Economy.Application;
using CleanTime.Equipment.Application;
using CleanTime.Equipment.Domain;
using CleanTime.GameTime.Application;
using CleanTime.GameTime.Domain;
using CleanTime.Interaction.Presentation;
using CleanTime.Quests.Application;
using CleanTime.Quests.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;
using System;

namespace CleanTime.UI.HUD
{
    /// <summary>Creates the HUD presenters once the scene services exist. Views not assigned are skipped.</summary>
    public sealed class HudBinder : MonoBehaviour, ISceneService
    {
        [SerializeField] private WalletView walletView;
        [SerializeField] private DayPhaseView dayPhaseView;
        [SerializeField] private SelectedEquipmentView selectedEquipmentView;
        [SerializeField] private QuestTrackerView questTrackerView;
        [SerializeField] private CleaningProgressView cleaningProgressView;
        [SerializeField] private CleaningSceneRoot cleaningRoot;

        private readonly List<IDisposable> _presenters = new List<IDisposable>();

        public void Construct(IServiceResolver services)
        {
            var events = services.Resolve<IEventBus>();

            if (walletView != null)
                _presenters.Add(new WalletPresenter(events, services.Resolve<IQueryHandler<GetPlayerWalletQuery, Money>>(), walletView));

            if (dayPhaseView != null)
                _presenters.Add(new DayPhasePresenter(events, services.Resolve<IQueryHandler<GetCurrentGameDateQuery, GameDate>>(), dayPhaseView));

            if (selectedEquipmentView != null)
                _presenters.Add(new SelectedEquipmentPresenter(events, services.Resolve<ISelectedEquipmentQuery>(), selectedEquipmentView));

            if (questTrackerView != null)
                _presenters.Add(new QuestTrackerPresenter(events, services.Resolve<IQueryHandler<GetActiveQuestsQuery, IReadOnlyList<QuestInstance>>>(), questTrackerView));

            if (cleaningProgressView != null && cleaningRoot != null)
                cleaningRoot.BindProgressView(events, cleaningProgressView);
        }

        private void OnDestroy()
        {
            foreach (var presenter in _presenters)
                presenter.Dispose();

            _presenters.Clear();
        }
    }
}
