using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>In-game (action based) calendar. Never derived from UnityEngine.Time.</summary>
    public interface IGameClock
    {
        GameDate Now { get; }
    }

    /// <summary>Wall clock, used for timestamps and durations only.</summary>
    public interface IUtcClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemUtcClock : IUtcClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
