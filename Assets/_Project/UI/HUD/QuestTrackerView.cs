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
    public sealed class QuestTrackerView : MonoBehaviour, IQuestTrackerView
    {
        [SerializeField] private TMP_Text label;

        public void SetQuests(IReadOnlyList<QuestInstance> activeQuests)
        {
            if (label == null)
                return;

            var text = new StringBuilder();

            foreach (var quest in activeQuests)
            {
                text.Append(quest.Definition.DisplayNameKey).Append(": ");
                for (var i = 0; i < quest.Progress.Count; i++)
                    text.Append(quest.Progress[i]).Append('/').Append(quest.Definition.Objectives[i].RequiredCount).Append(' ');
                text.AppendLine();
            }

            label.text = text.ToString();
        }
    }
}
