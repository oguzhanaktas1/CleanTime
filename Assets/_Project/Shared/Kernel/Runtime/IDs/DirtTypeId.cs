using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Stable business identifier for: Dirt / repair target type. Never a scene or instance id.</summary>
    public readonly struct DirtTypeId : IEquatable<DirtTypeId>
    {
        public string Value { get; }

        public DirtTypeId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool Equals(DirtTypeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is DirtTypeId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(DirtTypeId left, DirtTypeId right) => left.Equals(right);

        public static bool operator !=(DirtTypeId left, DirtTypeId right) => !left.Equals(right);
    }
}
