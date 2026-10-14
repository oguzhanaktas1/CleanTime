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
    public sealed class InteractionPromptView : MonoBehaviour, IInteractionPromptView
    {
        [SerializeField] private TMP_Text label;

        public void Show(string promptKey)
        {
            gameObject.SetActive(true);
            if (label != null)
                label.text = promptKey;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
