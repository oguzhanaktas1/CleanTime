using System;

namespace CleanTime.GameInput.Application
{
    /// <summary>What the player wants to do, independent of keyboard keys, gamepad buttons or touch controls.</summary>
    public enum GameplayIntent
    {
        Interact,
        PrimaryAction,
        PreviousEquipment,
        NextEquipment,
        Pause,
        OpenInventory,
        OpenMap,
    }

    /// <summary>Normalised 2D movement input (each axis -1..1).</summary>
    public readonly struct MoveInput
    {
        public MoveInput(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }

        public float Y { get; }

        public bool IsZero => X == 0f && Y == 0f;

        public static MoveInput None => new MoveInput(0f, 0f);
    }

    /// <summary>
    /// Gameplay code depends on this port only. Keyboard F, a gamepad south button and a mobile action
    /// button all surface as <see cref="GameplayIntent.Interact"/>.
    /// </summary>
    public interface IGameplayInput
    {
        MoveInput Move { get; }

        /// <summary>True while the primary action is held (for hold-to-clean accessibility).</summary>
        bool IsPrimaryHeld { get; }

        event Action<GameplayIntent> IntentPerformed;
    }
}
