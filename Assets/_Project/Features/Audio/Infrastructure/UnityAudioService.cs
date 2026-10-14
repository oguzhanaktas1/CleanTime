using System.Collections.Generic;
using UnityEngine;
using CleanTime.Audio.Application;
using CleanTime.Shared.Kernel;
using KernelLogger = CleanTime.Shared.Kernel.ILogger;

namespace CleanTime.Audio.Infrastructure
{
    /// <summary>
    /// AudioSource based playback. Missing cues are skipped quietly (debug log) so the game runs
    /// before any audio exists.
    /// </summary>
    public sealed class UnityAudioService : IAudioService, IMusicService, IAudioVolume
    {
        private readonly AudioCueLibrary _library;
        private readonly KernelLogger _logger;
        private readonly AudioSource _sfx;
        private readonly AudioSource _music;
        private readonly HashSet<string> _reportedMissing = new HashSet<string>();
        private float _master = 1f;
        private float _musicVolume = 0.8f;
        private float _sfxVolume = 1f;

        public UnityAudioService(AudioCueLibrary library, KernelLogger logger, Transform parent)
        {
            _library = library;
            _logger = logger;

            var root = new GameObject("Audio");
            root.transform.SetParent(parent, false);

            _sfx = root.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;

            _music = root.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
        }

        public void PlaySfx(string cueId)
        {
            if (!TryResolve(cueId, out var cue))
                return;

            _sfx.PlayOneShot(cue.clips[Random.Range(0, cue.clips.Length)], _master * _sfxVolume * (cue.volume <= 0f ? 1f : cue.volume));
        }

        public void PlayMusic(string cueId)
        {
            if (!TryResolve(cueId, out var cue))
                return;

            var clip = cue.clips[0];
            if (_music.clip == clip && _music.isPlaying)
                return;

            _music.clip = clip;
            _music.volume = _master * _musicVolume;
            _music.Play();
        }

        public void StopMusic() => _music.Stop();

        public void SetVolumes(float master, float music, float sfx)
        {
            _master = master;
            _musicVolume = music;
            _sfxVolume = sfx;
            _music.volume = _master * _musicVolume;
        }

        private bool TryResolve(string cueId, out AudioCueLibrary.Cue cue)
        {
            cue = default;

            if (_library != null && _library.TryGet(cueId, out cue) && cue.clips != null && cue.clips.Length > 0)
                return true;

            if (_reportedMissing.Add(cueId))
                _logger.Debug($"Audio cue '{cueId}' has no clip assigned yet.");

            return false;
        }
    }
}
