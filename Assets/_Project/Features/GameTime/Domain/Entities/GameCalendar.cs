using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.GameTime.Domain
{
    /// <summary>Kinds of player activity that can consume time.</summary>
    public enum GameActionKind
    {
        JobCompleted,
        TravelToLocation,
        Interaction,
        Custom,
    }

    /// <summary>What happened as a result of consuming actions.</summary>
    public sealed class CalendarAdvance
    {
        private readonly List<(DayPhase From, DayPhase To)> _phaseChanges = new List<(DayPhase, DayPhase)>();

        public IReadOnlyList<(DayPhase From, DayPhase To)> PhaseChanges => _phaseChanges;

        /// <summary>The final phase ran out of actions; the caller should end the day.</summary>
        public bool DayEnded { get; internal set; }

        internal void AddPhaseChange(DayPhase from, DayPhase to) => _phaseChanges.Add((from, to));
    }

    /// <summary>
    /// Action-count based calendar (the GDD has no real-time clock). Phases advance when enough
    /// actions were consumed; the day ends when the last phase is exhausted.
    /// </summary>
    public sealed class GameCalendar
    {
        public GameCalendar()
            : this(GameDate.Start, 0)
        {
        }

        public GameCalendar(GameDate now, int actionsInPhase)
        {
            Now = now;
            ActionsInPhase = Guard.NotNegative(actionsInPhase, nameof(actionsInPhase));
        }

        public GameDate Now { get; private set; }

        public int ActionsInPhase { get; private set; }

        public CalendarAdvance ConsumeActions(int cost, IDayPhasePolicy policy)
        {
            var advance = new CalendarAdvance();

            if (cost <= 0)
                return advance;

            ActionsInPhase += cost;

            while (ActionsInPhase >= policy.ActionsRequired(Now.Phase))
            {
                ActionsInPhase -= policy.ActionsRequired(Now.Phase);
                var next = policy.Next(Now.Phase);

                if (!next.HasValue)
                {
                    advance.DayEnded = true;
                    ActionsInPhase = 0;
                    break;
                }

                advance.AddPhaseChange(Now.Phase, next.Value);
                Now = new GameDate(Now.Day, next.Value);
            }

            return advance;
        }

        public void StartNextDay()
        {
            Now = new GameDate(Now.Day + 1, DayPhase.Morning);
            ActionsInPhase = 0;
        }
    }

    /// <summary>Presentation-oriented aggregate of what happened during a game day.</summary>
    public sealed class DaySummary
    {
        public DaySummary(
            int day,
            int jobsCompleted,
            Money moneyEarned,
            Money employeeRevenue,
            Money moneySpent,
            int relationshipsChanged,
            int questsCompleted,
            int locationsUnlocked)
        {
            Day = day;
            JobsCompleted = jobsCompleted;
            MoneyEarned = moneyEarned;
            EmployeeRevenue = employeeRevenue;
            MoneySpent = moneySpent;
            RelationshipsChanged = relationshipsChanged;
            QuestsCompleted = questsCompleted;
            LocationsUnlocked = locationsUnlocked;
        }

        public int Day { get; }

        public int JobsCompleted { get; }

        public Money MoneyEarned { get; }

        public Money EmployeeRevenue { get; }

        public Money MoneySpent { get; }

        public int RelationshipsChanged { get; }

        public int QuestsCompleted { get; }

        public int LocationsUnlocked { get; }
    }
}
