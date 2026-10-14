using System.Collections.Generic;
using UnityEngine;
using CleanTime.Audio.Application;
using CleanTime.Shared.Kernel;
using KernelLogger = CleanTime.Shared.Kernel.ILogger;

namespace CleanTime.Audio.Infrastructure
{
    public sealed class AudioInfrastructureModule : FeatureModule
    {
        private readonly AudioCueLibrary _library;
        private readonly Transform _parent;

        /// <param name="library">May be null until audio assets are authored.</param>
        /// <param name="parent">Persistent object the audio sources are parented to.</param>
        public AudioInfrastructureModule(AudioCueLibrary library, Transform parent)
        {
            _library = library;
            _parent = parent;
        }

        public override string Name => "Audio.Infrastructure";

        public override void Register(IServiceRegistry services)
        {
            services.RegisterSingleton(r => new UnityAudioService(_library, r.Resolve<KernelLogger>(), _parent));
            services.Forward<IAudioService, UnityAudioService>();
            services.Forward<IMusicService, UnityAudioService>();
            services.Forward<IAudioVolume, UnityAudioService>();
        }
    }
}
