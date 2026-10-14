using UnityEngine;
using CleanTime.Cleaning.Application;
using CleanTime.Equipment.Application;
using CleanTime.GameInput.Application;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;

namespace CleanTime.Cleaning.Presentation
{
    /// <summary>
    /// Turns the primary-action intent into an ApplyCleaningAction command against the nearest target.
    /// Taps and (optionally) holding both map to the same domain command; no rules live here.
    /// </summary>
    public sealed class CleaningInteractionController : MonoBehaviour, ISceneService
    {
        [SerializeField] private CleaningTargetSelector selector;
        [SerializeField] private bool holdToClean;
        [SerializeField] private float holdInterval = 0.25f;

        private ICleaningCommandService _cleaning;
        private ISelectedEquipmentQuery _equipment;
        private IGameplayInput _input;
        private float _nextHoldTime;

        public void Construct(IServiceResolver services)
        {
            _cleaning = services.Resolve<ICleaningCommandService>();
            _equipment = services.Resolve<ISelectedEquipmentQuery>();
            _input = services.Resolve<IGameplayInput>();
            _input.IntentPerformed += OnIntent;
        }

        private void OnDestroy()
        {
            if (_input != null)
                _input.IntentPerformed -= OnIntent;
        }

        private void Update()
        {
            if (!holdToClean || _input == null || !_input.IsPrimaryHeld || Time.time < _nextHoldTime)
                return;

            _nextHoldTime = Time.time + holdInterval;
            OnPrimaryAction();
        }

        private void OnIntent(GameplayIntent intent)
        {
            if (intent == GameplayIntent.PrimaryAction)
                OnPrimaryAction();
        }

        public void OnPrimaryAction()
        {
            if (_cleaning == null || selector == null)
                return;

            var target = selector.Current;

            if (target is null)
                return;

            var equipmentId = _equipment.GetSelected();

            if (equipmentId.IsEmpty)
                return;

            _cleaning.ApplyAction(
                target.TargetInstanceId,
                equipmentId);
        }
    }
}
