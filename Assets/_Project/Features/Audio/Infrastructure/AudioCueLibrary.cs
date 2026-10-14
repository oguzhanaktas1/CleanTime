using System.Collections.Generic;
using UnityEngine;
using CleanTime.Audio.Application;
using CleanTime.Shared.Kernel;
using KernelLogger = CleanTime.Shared.Kernel.ILogger;

namespace CleanTime.Audio.Infrastructure
{
    /// <summary>Maps stable cue ids to clips. Authored later, when audio assets exist.</summary>
    [CreateAssetMenu(menuName = "Clean Time/Audio Cue Library", fileName = "AudioCueLibrary")]
    public sealed class AudioCueLibrary : ScriptableObject
    {
        [System.Serializable]
        public struct Cue
        {
            public string id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private Cue[] cues = new Cue[0];

        public bool TryGet(string cueId, out Cue cue)
        {
            foreach (var candidate in cues)
            {
                if (candidate.id == cueId)
                {
                    cue = candidate;
                    return true;
                }
            }

            cue = default;
            return false;
        }
    }
}
