using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CleanTime.GameInput.Application;

namespace CleanTime.GameInput.Presentation
{
    /// <summary>
    /// Input System adapter. Uses the assigned <see cref="InputActionAsset"/> when there is one (actions
    /// are looked up by name), otherwise builds a default keyboard + gamepad map in code, so input works
    /// before any asset is authored.
    /// </summary>
    public sealed class UnityGameplayInput : MonoBehaviour, IGameplayInput
    {
        [SerializeField] private InputActionAsset actionsAsset;

        private InputActionMap _map;
        private InputAction _move;
        private InputAction _primary;

        public event Action<GameplayIntent> IntentPerformed;

        public MoveInput Move
        {
            get
            {
                var value = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
                return new MoveInput(value.x, value.y);
            }
        }

        public bool IsPrimaryHeld => _primary != null && _primary.IsPressed();

        private void Awake()
        {
            _map = actionsAsset != null ? actionsAsset.FindActionMap("Gameplay", throwIfNotFound: true) : DefaultBindings.Create();
            _move = _map.FindAction(ActionNames.Move, throwIfNotFound: true);
            _primary = _map.FindAction(ActionNames.PrimaryAction, throwIfNotFound: true);

            Bind(ActionNames.Interact, GameplayIntent.Interact);
            Bind(ActionNames.PrimaryAction, GameplayIntent.PrimaryAction);
            Bind(ActionNames.PreviousEquipment, GameplayIntent.PreviousEquipment);
            Bind(ActionNames.NextEquipment, GameplayIntent.NextEquipment);
            Bind(ActionNames.Pause, GameplayIntent.Pause);
            Bind(ActionNames.OpenInventory, GameplayIntent.OpenInventory);
            Bind(ActionNames.OpenMap, GameplayIntent.OpenMap);
        }

        private void OnEnable() => _map?.Enable();

        private void OnDisable() => _map?.Disable();

        private void Bind(string actionName, GameplayIntent intent)
        {
            var action = _map.FindAction(actionName, throwIfNotFound: false);
            if (action != null)
                action.performed += _ => IntentPerformed?.Invoke(intent);
        }
    }

    internal static class ActionNames
    {
        public const string Move = "Move";
        public const string Interact = "Interact";
        public const string PrimaryAction = "PrimaryAction";
        public const string PreviousEquipment = "PreviousEquipment";
        public const string NextEquipment = "NextEquipment";
        public const string Pause = "Pause";
        public const string OpenInventory = "OpenInventory";
        public const string OpenMap = "OpenMap";
    }

    internal static class DefaultBindings
    {
        public static InputActionMap Create()
        {
            var map = new InputActionMap("Gameplay");

            var move = map.AddAction(ActionNames.Move, InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            var interact = map.AddAction(ActionNames.Interact, InputActionType.Button, "<Keyboard>/f");
            interact.AddBinding("<Gamepad>/buttonSouth");

            var primary = map.AddAction(ActionNames.PrimaryAction, InputActionType.Button, "<Mouse>/leftButton");
            primary.AddBinding("<Keyboard>/space");
            primary.AddBinding("<Gamepad>/rightTrigger");

            var previous = map.AddAction(ActionNames.PreviousEquipment, InputActionType.Button, "<Keyboard>/q");
            previous.AddBinding("<Gamepad>/leftShoulder");

            var next = map.AddAction(ActionNames.NextEquipment, InputActionType.Button, "<Keyboard>/e");
            next.AddBinding("<Gamepad>/rightShoulder");

            var pause = map.AddAction(ActionNames.Pause, InputActionType.Button, "<Keyboard>/escape");
            pause.AddBinding("<Gamepad>/start");

            map.AddAction(ActionNames.OpenInventory, InputActionType.Button, "<Keyboard>/i");
            map.AddAction(ActionNames.OpenMap, InputActionType.Button, "<Keyboard>/m");

            return map;
        }
    }
}
