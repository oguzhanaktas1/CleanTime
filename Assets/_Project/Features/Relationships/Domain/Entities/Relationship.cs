using System.Collections.Generic;
using System.Linq;
using CleanTime.Shared.Kernel;

namespace CleanTime.Relationships.Domain
{
    /// <summary>Points needed to reach a level, with an optional story milestone fired on arrival.</summary>
    public sealed class RelationshipThreshold
    {
        public RelationshipThreshold(int level, int requiredPoints, string milestoneId = "")
        {
            Level = Guard.NotNegative(level, nameof(level));
            RequiredPoints = Guard.NotNegative(requiredPoints, nameof(requiredPoints));
            MilestoneId = milestoneId ?? string.Empty;
        }

        public int Level { get; }

        public int RequiredPoints { get; }

        /// <summary>Non-empty when reaching this level is a story beat (e.g. the mother's final milestone).</summary>
        public string MilestoneId { get; }
    }

    public sealed class Relationship
    {
        public Relationship(NpcId npcId, int points = 0, int level = 0, int? lastChangedDay = null)
        {
            NpcId = npcId;
            Points = Guard.NotNegative(points, nameof(points));
            Level = Guard.NotNegative(level, nameof(level));
            LastChangedDay = lastChangedDay;
        }

        public NpcId NpcId { get; }

        public int Points { get; private set; }

        public int Level { get; private set; }

        public int? LastChangedDay { get; private set; }

        public void AddPoints(int amount, int gameDay)
        {
            Points = System.Math.Max(0, Points + amount);
            LastChangedDay = gameDay;
        }

        /// <summary>Applies a new level; returns true when it changed.</summary>
        public bool SetLevel(int level)
        {
            if (level == Level)
                return false;

            Level = level;
            return true;
        }
    }

    /// <summary>
    /// Per-NPC level thresholds. NPCs without their own table use the default one: nothing about
    /// the mother is hard-coded, she is simply an NPC with a configured table.
    /// </summary>
    public sealed class RelationshipThresholdSet
    {
        private readonly IReadOnlyDictionary<NpcId, IReadOnlyList<RelationshipThreshold>> _perNpc;
        private readonly IReadOnlyList<RelationshipThreshold> _default;

        public RelationshipThresholdSet(
            IReadOnlyList<RelationshipThreshold> defaultThresholds,
            IReadOnlyDictionary<NpcId, IReadOnlyList<RelationshipThreshold>> perNpc = null)
        {
            _default = defaultThresholds.OrderBy(t => t.Level).ToList();
            _perNpc = perNpc ?? new Dictionary<NpcId, IReadOnlyList<RelationshipThreshold>>();
        }

        public static RelationshipThresholdSet Default => new RelationshipThresholdSet(new[]
        {
            new RelationshipThreshold(1, 20),
            new RelationshipThreshold(2, 60),
            new RelationshipThreshold(3, 120),
            new RelationshipThreshold(4, 200),
            new RelationshipThreshold(5, 300),
        });

        public IReadOnlyList<RelationshipThreshold> For(NpcId npcId) =>
            _perNpc.TryGetValue(npcId, out var specific) ? specific.OrderBy(t => t.Level).ToList() : _default;
    }
}
