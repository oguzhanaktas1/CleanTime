using CleanTime.Shared.Kernel;

namespace CleanTime.Player.Domain
{
    public enum PlayerMovementState
    {
        Idle,
        Move,
        Interact,
        Clean,
        Dialogue,
        Disabled,
    }

    public sealed class PlayerProfile
    {
        public PlayerProfile(PlayerId id, string name)
        {
            Id = id;
            Name = Guard.NotEmpty(name, nameof(name));
        }

        public PlayerId Id { get; }

        public string Name { get; }
    }

    /// <summary>Identity plus what the player is currently doing (gameplay state, not physics).</summary>
    public sealed class PlayerState
    {
        private readonly StateMachine<PlayerMovementState> _machine;

        public PlayerState(PlayerProfile profile)
        {
            Profile = Guard.NotNull(profile, nameof(profile));
            _machine = new StateMachine<PlayerMovementState>(PlayerMovementState.Idle)
                .Allow(PlayerMovementState.Idle, PlayerMovementState.Move, PlayerMovementState.Interact, PlayerMovementState.Clean, PlayerMovementState.Dialogue, PlayerMovementState.Disabled)
                .Allow(PlayerMovementState.Move, PlayerMovementState.Idle, PlayerMovementState.Interact, PlayerMovementState.Clean, PlayerMovementState.Dialogue, PlayerMovementState.Disabled)
                .Allow(PlayerMovementState.Interact, PlayerMovementState.Idle, PlayerMovementState.Dialogue, PlayerMovementState.Disabled)
                .Allow(PlayerMovementState.Clean, PlayerMovementState.Idle, PlayerMovementState.Move, PlayerMovementState.Disabled)
                .Allow(PlayerMovementState.Dialogue, PlayerMovementState.Idle, PlayerMovementState.Disabled)
                .Allow(PlayerMovementState.Disabled, PlayerMovementState.Idle);
        }

        public PlayerProfile Profile { get; }

        public PlayerMovementState Movement => _machine.Current;

        /// <summary>The player may walk in Idle, Move and Clean (cleaning does not root the player).</summary>
        public bool CanMove => Movement == PlayerMovementState.Idle || Movement == PlayerMovementState.Move || Movement == PlayerMovementState.Clean;

        public Result TransitionTo(PlayerMovementState next) => _machine.TransitionTo(next);
    }
}
