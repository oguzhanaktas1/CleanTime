using UnityEngine;
using CleanTime.Equipment.Application;
using CleanTime.Equipment.Domain;
using CleanTime.GameInput.Application;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;

namespace CleanTime.Equipment.Presentation
{
    /// <summary>Maps the previous / next equipment intents onto the SwitchEquipment command.</summary>
    public sealed class EquipmentSwitchController : MonoBehaviour, ISceneService
    {
        private IGameplayInput _input;
        private ICommandHandler<SwitchEquipmentCommand> _switch;

        public void Construct(IServiceResolver services)
        {
            _input = services.Resolve<IGameplayInput>();
            _switch = services.Resolve<ICommandHandler<SwitchEquipmentCommand>>();
            _input.IntentPerformed += OnIntent;
        }

        private void OnDestroy()
        {
            if (_input != null)
                _input.IntentPerformed -= OnIntent;
        }

        private void OnIntent(GameplayIntent intent)
        {
            if (intent == GameplayIntent.PreviousEquipment)
                _switch.Handle(new SwitchEquipmentCommand(SwitchDirection.Previous));
            else if (intent == GameplayIntent.NextEquipment)
                _switch.Handle(new SwitchEquipmentCommand(SwitchDirection.Next));
        }
    }
}
