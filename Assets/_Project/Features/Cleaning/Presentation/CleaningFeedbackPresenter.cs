using System;
using UnityEngine;
using UnityEngine.Events;
using CleanTime.Audio.Application;
using CleanTime.Cleaning.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Presentation
{
    /// <summary>Routes cleaning events to feedback views and audio cues. The domain never plays sounds.</summary>
    public sealed class CleaningFeedbackPresenter : IDisposable
    {
        private readonly IAudioService _audio;
        private readonly CleaningFeedbackView _feedback;
        private readonly WrongToolFeedbackView _wrongTool;
        private readonly IDisposable[] _subscriptions;

        public CleaningFeedbackPresenter(
            IEventBus events,
            IAudioService audio,
            CleaningFeedbackView feedback,
            WrongToolFeedbackView wrongTool)
        {
            _audio = audio;
            _feedback = feedback;
            _wrongTool = wrongTool;

            _subscriptions = new[]
            {
                events.Subscribe<CleaningHitAppliedEvent>(_ => OnHit()),
                events.Subscribe<CleaningTargetCompletedEvent>(_ => OnTargetCompleted()),
                events.Subscribe<RoomCompletedEvent>(_ => OnRoomCompleted()),
                events.Subscribe<WrongEquipmentUsedEvent>(_ => OnWrongTool()),
            };
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }

        private void OnHit()
        {
            _audio.PlaySfx(AudioCues.CleaningHit);
            if (_feedback != null) _feedback.PlayHit();
        }

        private void OnTargetCompleted()
        {
            _audio.PlaySfx(AudioCues.CleaningTargetCompleted);
            if (_feedback != null) _feedback.PlayTargetCompleted();
        }

        private void OnRoomCompleted()
        {
            _audio.PlaySfx(AudioCues.RoomCompleted);
            if (_feedback != null) _feedback.PlayRoomCompleted();
        }

        private void OnWrongTool()
        {
            _audio.PlaySfx(AudioCues.WrongTool);
            if (_wrongTool != null) _wrongTool.Show();
        }
    }
}
