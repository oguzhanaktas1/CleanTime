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
    /// <summary>Base class for scene objects the player can interact with. Subclasses add the behaviour.</summary>
    public abstract class InteractableBehaviour : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionId = "";
        [SerializeField] private string promptKey = "interaction.default";

        public string InteractionId => interactionId;

        public string PromptKey => promptKey;

        public virtual bool CanInteract(InteractionContext context) => true;

        public abstract Result Interact(InteractionContext context);
    }
}
