using System;
using System.Collections.Generic;
using CleanTime.Cleaning.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Presentation
{
    /// <summary>Maps cleaning events onto the matching target views.</summary>
    public sealed class CleaningTargetPresenter : IDisposable
    {
        private readonly IReadOnlyDictionary<string, CleaningTargetView> _views;
        private readonly IDisposable _hits;
        private readonly IDisposable _completed;

        public CleaningTargetPresenter(IEventBus events, IReadOnlyDictionary<string, CleaningTargetView> views)
        {
            _views = views;
            _hits = events.Subscribe<CleaningHitAppliedEvent>(OnHit);
            _completed = events.Subscribe<CleaningTargetCompletedEvent>(OnCompleted);
        }

        public void Dispose()
        {
            _hits.Dispose();
            _completed.Dispose();
        }

        private void OnHit(CleaningHitAppliedEvent evt)
        {
            if (_views.TryGetValue(evt.TargetInstanceId, out var view) && view != null)
                view.SetProgress(1f - evt.CurrentHealth / evt.MaxHealth);
        }

        private void OnCompleted(CleaningTargetCompletedEvent evt)
        {
            if (_views.TryGetValue(evt.TargetInstanceId, out var view) && view != null)
                view.PlayCompletedFeedback();
        }
    }
}
