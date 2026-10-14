using UnityEngine;
using CleanTime.GameInput.Application;
using CleanTime.Player.Domain;
using CleanTime.Shared.Kernel;
using CleanTime.Shared.Unity;

namespace CleanTime.Player.Presentation
{
    /// <summary>Top-down 2D movement. Reads intents from the input port; owns no gameplay rules.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor2D : MonoBehaviour, ISceneService
    {
        [SerializeField] private float moveSpeed = 4.5f;

        private Rigidbody2D _body;
        private IGameplayInput _input;
        private PlayerState _state;
        private Vector2 _facing = Vector2.down;

        public Vector2 Facing => _facing;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
        }

        public void Construct(IServiceResolver services)
        {
            _input = services.Resolve<IGameplayInput>();
            _state = services.Resolve<PlayerState>();
        }

        private void FixedUpdate()
        {
            if (_input == null || _state == null)
                return;

            var move = _state.CanMove ? new Vector2(_input.Move.X, _input.Move.Y) : Vector2.zero;
            if (move.sqrMagnitude > 1f)
                move.Normalize();

            _body.linearVelocity = move * moveSpeed;

            if (move.sqrMagnitude > 0.0001f)
                _facing = move.normalized;

            if (_state.Movement == PlayerMovementState.Idle && !move.Equals(Vector2.zero))
                _state.TransitionTo(PlayerMovementState.Move);
            else if (_state.Movement == PlayerMovementState.Move && move.Equals(Vector2.zero))
                _state.TransitionTo(PlayerMovementState.Idle);
        }
    }
}
