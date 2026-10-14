using System;
using UnityEngine;
using CleanTime.Audio.Infrastructure;
using CleanTime.Content;
using CleanTime.Shared.Kernel;

namespace CleanTime.Bootstrap
{
    /// <summary>Everything the bootstrapper needs that is authored in the editor. All of it is optional until assets exist.</summary>
    [Serializable]
    public sealed class BootstrapSettings
    {
        [Tooltip("Authored game content. When empty the game boots with no content (useful for the empty project).")]
        public GameContentCatalog contentCatalog;

        [Tooltip("Cue id -> clip map. Optional until audio assets exist.")]
        public AudioCueLibrary audioCues;

        [Tooltip("Load the MainMenu scene when boot finishes. Disable in test scenes.")]
        public bool loadMainMenuOnBoot = true;

        public LogLevel logLevel = LogLevel.Debug;

        [Tooltip("Money the player starts a new game with, in cents.")]
        public long startingMoneyCents;

        [Tooltip("Persist settings to a JSON file in the persistent data path.")]
        public bool persistSettings = true;

        public string settingsFileName = "settings.json";

        public string mainMenuSceneName = "MainMenu";

        public string townSceneName = "Town";
    }
}
