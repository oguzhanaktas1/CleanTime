using System;
using UnityEngine;
using UnityEngine.Events;
using CleanTime.Audio.Application;
using CleanTime.Cleaning.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Presentation
{
    public sealed class CleaningProgressHudPresenter : IDisposable
    {
        private readonly ICleaningProgressView _view;
        private readonly IDisposable _subscription;

        public CleaningProgressHudPresenter(IEventBus events, ICleaningProgressView view)
        {
            _view = view;
            _subscription = events.Subscribe<PropertyCleaningProgressChangedEvent>(e => _view.SetProgress(e.Progress));
        }

        public void Dispose() => _subscription.Dispose();
    }
}
