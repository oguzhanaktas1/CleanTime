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
    // Views are intentionally dumb: they show what the presenter hands them. Text is a placeholder
    // until the localization package is installed (then labels resolve their keys).

    public sealed class WalletView : MonoBehaviour, IWalletView
    {
        [SerializeField] private TMP_Text label;

        public void SetBalance(Money balance)
        {
            if (label != null)
                label.text = balance.ToString();
        }
    }
}
