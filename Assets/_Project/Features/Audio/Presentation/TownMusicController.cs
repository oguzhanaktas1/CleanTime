using System;
using UnityEngine;
using CleanTime.Audio.Application;
using CleanTime.GameTime.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;

namespace CleanTime.Audio.Presentation
{
    /// <summary>DayPhaseChanged -> music cue for that phase. Does nothing audible until cues have clips.</summary>
    public sealed class TownMusicController : MonoBehaviour, ISceneService
    {
        private IDisposable _subscription;
        private IMusicService _music;

        public void Construct(IServiceResolver services)
        {
            _music = services.Resolve<IMusicService>();
            _subscription = services.Resolve<IEventBus>().Subscribe<DayPhaseChangedEvent>(e => _music.PlayMusic(AudioCues.TownMusic(e.Current)));
            _music.PlayMusic(AudioCues.TownMusic(DayPhase.Morning));
        }

        private void OnDestroy() => _subscription?.Dispose();
    }
}
