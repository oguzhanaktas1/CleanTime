using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Stable business identifier for: Npc. Never a scene or instance id.</summary>
    public readonly struct NpcId : IEquatable<NpcId>
    {
        public string Value { get; }

        public NpcId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool Equals(NpcId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is NpcId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(NpcId left, NpcId right) => left.Equals(right);

        public static bool operator !=(NpcId left, NpcId right) => !left.Equals(right);
    }
}
