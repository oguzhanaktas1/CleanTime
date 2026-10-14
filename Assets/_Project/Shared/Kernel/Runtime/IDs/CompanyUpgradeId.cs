using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Stable business identifier for: Company upgrade. Never a scene or instance id.</summary>
    public readonly struct CompanyUpgradeId : IEquatable<CompanyUpgradeId>
    {
        public string Value { get; }

        public CompanyUpgradeId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool Equals(CompanyUpgradeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is CompanyUpgradeId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(CompanyUpgradeId left, CompanyUpgradeId right) => left.Equals(right);

        public static bool operator !=(CompanyUpgradeId left, CompanyUpgradeId right) => !left.Equals(right);
    }
}
