using System.Collections.Generic;
using System.Linq;
using CleanTime.Shared.Kernel;

namespace CleanTime.NPC.Domain
{
    public enum NpcRole
    {
        Family,
        Vendor,
        Customer,
        Townsperson,
    }

    public sealed class NpcProfile
    {
        public NpcProfile(NpcId id, string displayNameKey, NpcRole role)
        {
            Id = id;
            DisplayNameKey = displayNameKey ?? string.Empty;
            Role = role;
        }

        public NpcId Id { get; }

        public string DisplayNameKey { get; }

        public NpcRole Role { get; }
    }

    /// <summary>Where an NPC should be during a day phase (an abstract node such as "home" or "shop").</summary>
    public sealed class NpcScheduleEntry
    {
        public NpcScheduleEntry(DayPhase phase, string nodeId)
        {
            Phase = phase;
            NodeId = Guard.NotEmpty(nodeId, nameof(nodeId));
        }

        public DayPhase Phase { get; }

        public string NodeId { get; }
    }

    public sealed class NpcSchedule
    {
        private readonly IReadOnlyList<NpcScheduleEntry> _entries;

        public NpcSchedule(IReadOnlyList<NpcScheduleEntry> entries, string defaultNodeId = "home")
        {
            _entries = entries ?? Empty<NpcScheduleEntry>.List;
            DefaultNodeId = Guard.NotEmpty(defaultNodeId, nameof(defaultNodeId));
        }

        public string DefaultNodeId { get; }

        public IReadOnlyList<NpcScheduleEntry> Entries => _entries;

        public string NodeFor(DayPhase phase) => _entries.FirstOrDefault(e => e.Phase == phase)?.NodeId ?? DefaultNodeId;
    }

    /// <summary>
    /// NPC identity and abstract state. Far-away NPCs are simulated only as a schedule node;
    /// the presentation layer materialises them when the player is near.
    /// </summary>
    public sealed class Npc
    {
        public Npc(NpcProfile profile, NpcSchedule schedule)
        {
            Profile = Guard.NotNull(profile, nameof(profile));
            Schedule = Guard.NotNull(schedule, nameof(schedule));
            CurrentNodeId = schedule.DefaultNodeId;
        }

        public NpcProfile Profile { get; }

        public NpcId Id => Profile.Id;

        public NpcSchedule Schedule { get; }

        public string CurrentNodeId { get; private set; }

        /// <summary>Returns true when the NPC moved to a different node.</summary>
        public bool MoveToPhase(DayPhase phase)
        {
            var node = Schedule.NodeFor(phase);
            if (node == CurrentNodeId)
                return false;

            CurrentNodeId = node;
            return true;
        }
    }
}
