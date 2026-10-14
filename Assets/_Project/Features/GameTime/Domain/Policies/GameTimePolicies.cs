using System;
using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.GameTime.Domain
{
    public interface IDayPhasePolicy
    {
        int ActionsRequired(DayPhase phase);

        /// <summary>The phase after <paramref name="phase"/>, or null when the day ends.</summary>
        DayPhase? Next(DayPhase phase);
    }

    public interface IGameActionPolicy
    {
        /// <summary>How many actions of time the given activity consumes.</summary>
        int GetCost(GameActionKind kind);
    }

    /// <summary>Morning -> Afternoon -> Evening -> Night -> end of day, each phase lasting N actions.</summary>
    public sealed class ActionsPerPhaseDayPhasePolicy : IDayPhasePolicy
    {
        private readonly int[] _actionsPerPhase;

        public ActionsPerPhaseDayPhasePolicy(int actionsPerPhase = 1)
            : this(new[] { actionsPerPhase, actionsPerPhase, actionsPerPhase, actionsPerPhase })
        {
        }

        public ActionsPerPhaseDayPhasePolicy(int[] actionsPerPhase)
        {
            if (actionsPerPhase == null || actionsPerPhase.Length != 4)
                throw new ArgumentException("Exactly four phase lengths are required.", nameof(actionsPerPhase));

            foreach (var value in actionsPerPhase)
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(actionsPerPhase), "Every phase needs at least one action.");
            }

            _actionsPerPhase = (int[])actionsPerPhase.Clone();
        }

        public int ActionsRequired(DayPhase phase) => _actionsPerPhase[(int)phase];

        public DayPhase? Next(DayPhase phase) => phase == DayPhase.Night ? (DayPhase?)null : phase + 1;
    }

    public sealed class DefaultGameActionPolicy : IGameActionPolicy
    {
        private readonly IReadOnlyDictionary<GameActionKind, int> _costs;

        public DefaultGameActionPolicy(IReadOnlyDictionary<GameActionKind, int> costs = null)
        {
            _costs = costs ?? new Dictionary<GameActionKind, int> { { GameActionKind.JobCompleted, 1 } };
        }

        public int GetCost(GameActionKind kind) => _costs.TryGetValue(kind, out var cost) ? cost : 0;
    }
}
