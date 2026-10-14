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
    public sealed class DayPhaseView : MonoBehaviour, IDayPhaseView
    {
        [SerializeField] private TMP_Text label;

        public void SetDate(GameDate date)
        {
            if (label != null)
                label.text = $"Day {date.Day} - {date.Phase}";
        }
    }
}
