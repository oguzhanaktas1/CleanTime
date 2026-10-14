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
    /// <summary>Forwards the Interact intent to the selected interactable.</summary>
    public sealed class InteractionController : MonoBehaviour, ISceneService
    {
        [SerializeField] private InteractionTargetSelector selector;

        private IGameplayInput _input;
        private ISelectedEquipmentQuery _equipment;
        private PlayerState _player;

        public void Construct(IServiceResolver services)
        {
            _input = services.Resolve<IGameplayInput>();
            _equipment = services.Resolve<ISelectedEquipmentQuery>();
            _player = services.Resolve<PlayerState>();
            _input.IntentPerformed += OnIntent;
        }

        private void OnDestroy()
        {
            if (_input != null)
                _input.IntentPerformed -= OnIntent;
        }

        private void OnIntent(GameplayIntent intent)
        {
            if (intent != GameplayIntent.Interact || selector == null || selector.Current == null)
                return;

            var context = new InteractionContext(_player.Profile.Id, _equipment.GetSelected());

            if (selector.Current.CanInteract(context))
                selector.Current.Interact(context);
        }
    }
}
