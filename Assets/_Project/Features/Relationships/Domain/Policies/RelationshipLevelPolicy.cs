using System.Linq;
using CleanTime.Shared.Kernel;

namespace CleanTime.Relationships.Domain
{
    public interface IRelationshipLevelPolicy
    {
        int LevelFor(NpcId npcId, int points);

        /// <summary>The threshold at <paramref name="level"/> for the NPC, or null when none is defined.</summary>
        RelationshipThreshold ThresholdAt(NpcId npcId, int level);
    }

    public sealed class ThresholdRelationshipLevelPolicy : IRelationshipLevelPolicy
    {
        private readonly RelationshipThresholdSet _thresholds;

        public ThresholdRelationshipLevelPolicy(RelationshipThresholdSet thresholds)
        {
            _thresholds = thresholds;
        }

        public int LevelFor(NpcId npcId, int points)
        {
            var reached = _thresholds.For(npcId).Where(t => points >= t.RequiredPoints).ToList();
            return reached.Count == 0 ? 0 : reached.Max(t => t.Level);
        }

        public RelationshipThreshold ThresholdAt(NpcId npcId, int level) =>
            _thresholds.For(npcId).FirstOrDefault(t => t.Level == level);
    }
}
