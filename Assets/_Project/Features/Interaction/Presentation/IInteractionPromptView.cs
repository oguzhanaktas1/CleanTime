using System;
using UnityEngine;
using CleanTime.GameInput.Application;
using CleanTime.Interaction.Domain;
using CleanTime.Player.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;
using CleanTime.Equipment.Application;

namespace CleanTime.Interaction.Presentation
{
    public interface IInteractionPromptView
    {
        void Show(string promptKey);

        void Hide();
    }
}
