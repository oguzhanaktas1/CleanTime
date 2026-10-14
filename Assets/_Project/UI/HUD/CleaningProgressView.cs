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
    /// <summary>Shows the cleaning progress as a bar + label. Switch <see cref="showRemaining"/> for the GDD's "Dirt Remaining" wording.</summary>
    public sealed class CleaningProgressView : MonoBehaviour, ICleaningProgressView
    {
        [SerializeField] private Slider bar;
        [SerializeField] private TMP_Text label;
        [SerializeField] private bool showRemaining = true;

        public void SetProgress(float completed)
        {
            var progress = new CleanTime.Cleaning.Domain.CleaningProgress(completed);

            if (bar != null)
                bar.value = progress.Completed;

            if (label != null)
                label.text = showRemaining ? $"Dirt Remaining: {progress.RemainingPercent}%" : $"Clean: {progress.CompletedPercent}%";
        }
    }
}
