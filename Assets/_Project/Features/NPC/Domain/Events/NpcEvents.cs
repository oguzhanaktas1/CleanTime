using CleanTime.Shared.Kernel;

namespace CleanTime.NPC.Domain
{
    /// <summary>An NPC's schedule moved them to another abstract node (e.g. "home" -> "shop").</summary>
    public sealed record NpcScheduleNodeChangedEvent(NpcId NpcId, string NodeId) : IDomainEvent;
}
