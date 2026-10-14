using System;
using CleanTime.Settings.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Audio.Application
{
    /// <summary>Plays one-shot sound effects by cue id. Domain code never plays sounds; adapters do.</summary>
    public interface IAudioService
    {
        void PlaySfx(string cueId);
    }

    public interface IMusicService
    {
        void PlayMusic(string cueId);

        void StopMusic();
    }

    /// <summary>Applies the player's volume settings to the actual output.</summary>
    public interface IAudioVolume
    {
        void SetVolumes(float master, float music, float sfx);
    }

    /// <summary>Stable cue ids shared between gameplay adapters and the cue library.</summary>
    public static class AudioCues
    {
        public const string CleaningHit = "sfx.cleaning.hit";
        public const string CleaningTargetCompleted = "sfx.cleaning.target_completed";
        public const string WrongTool = "sfx.cleaning.wrong_tool";
        public const string RoomCompleted = "sfx.cleaning.room_completed";
        public const string JobCompleted = "sfx.job.completed";
        public const string Purchase = "sfx.ui.purchase";
        public const string Click = "sfx.ui.click";

        public static string TownMusic(DayPhase phase) => "music.town." + phase.ToString().ToLowerInvariant();
    }

    public sealed class NullAudioService : IAudioService, IMusicService, IAudioVolume
    {
        public void PlaySfx(string cueId)
        {
        }

        public void PlayMusic(string cueId)
        {
        }

        public void StopMusic()
        {
        }

        public void SetVolumes(float master, float music, float sfx)
        {
        }
    }

    /// <summary>SettingsChanged -> volumes.</summary>
    internal sealed class AudioSettingsProjection : IDisposable
    {
        private readonly IDisposable _subscription;

        public AudioSettingsProjection(IEventBus events, IAudioVolume volume)
        {
            _subscription = events.Subscribe<SettingsChangedEvent>(
                e => volume.SetVolumes(e.Settings.Audio.Master, e.Settings.Audio.Music, e.Settings.Audio.Sfx));
        }

        public void Dispose() => _subscription.Dispose();
    }

    public sealed class AudioModule : FeatureModule
    {
        public override string Name => "Audio";

        public override void Register(IServiceRegistry services)
        {
            // Silent defaults; Audio.Infrastructure registers the Unity implementation after this module.
            services.RegisterSingleton(_ => new NullAudioService());
            services.Forward<IAudioService, NullAudioService>();
            services.Forward<IMusicService, NullAudioService>();
            services.Forward<IAudioVolume, NullAudioService>();

            services.RegisterSingleton(r => new AudioSettingsProjection(r.Resolve<IEventBus>(), r.Resolve<IAudioVolume>()));
        }

        public override void Initialize(IServiceResolver services)
        {
            services.Resolve<AudioSettingsProjection>();
        }
    }
}
