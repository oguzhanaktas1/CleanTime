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
    /// <summary>Shows / hides the prompt as the selected interactable changes.</summary>
    public sealed class InteractionPromptPresenter : IDisposable
    {
        private readonly InteractionTargetSelector _selector;
        private readonly IInteractionPromptView _view;

        public InteractionPromptPresenter(InteractionTargetSelector selector, IInteractionPromptView view)
        {
            _selector = selector;
            _view = view;
            _selector.CurrentChanged += OnChanged;
            OnChanged(selector.Current);
        }

        public void Dispose() => _selector.CurrentChanged -= OnChanged;

        private void OnChanged(IInteractable interactable)
        {
            if (interactable == null)
                _view.Hide();
            else
                _view.Show(interactable.PromptKey);
        }
    }
}
