using System;
using System.Collections.Generic;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Explicit finite state machine with a whitelist of allowed transitions.</summary>
    public sealed class StateMachine<TState>
        where TState : struct, Enum
    {
        private readonly Dictionary<TState, HashSet<TState>> _transitions = new Dictionary<TState, HashSet<TState>>();

        public StateMachine(TState initial)
        {
            Current = initial;
        }

        public TState Current { get; private set; }

        /// <summary>Raised after a transition with (previous, current).</summary>
        public event Action<TState, TState> Changed;

        public StateMachine<TState> Allow(TState from, params TState[] to)
        {
            if (!_transitions.TryGetValue(from, out var set))
            {
                set = new HashSet<TState>();
                _transitions[from] = set;
            }

            foreach (var state in to)
                set.Add(state);

            return this;
        }

        public bool CanTransitionTo(TState next) =>
            _transitions.TryGetValue(Current, out var set) && set.Contains(next);

        public Result TransitionTo(TState next)
        {
            if (!CanTransitionTo(next))
                return Result.Failure(
                    "state.invalid_transition",
                    $"{typeof(TState).Name}: {Current} -> {next} is not allowed.");

            var previous = Current;
            Current = next;
            Changed?.Invoke(previous, next);
            return Result.Success();
        }

        /// <summary>Unconditional reset, e.g. when a session is torn down.</summary>
        public void ForceSet(TState state)
        {
            var previous = Current;
            Current = state;
            if (!EqualityComparer<TState>.Default.Equals(previous, state))
                Changed?.Invoke(previous, state);
        }
    }
}
