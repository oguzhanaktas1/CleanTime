using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Stable business identifier for: Employee archetype. Never a scene or instance id.</summary>
    public readonly struct EmployeeDefinitionId : IEquatable<EmployeeDefinitionId>
    {
        public string Value { get; }

        public EmployeeDefinitionId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool Equals(EmployeeDefinitionId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is EmployeeDefinitionId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(EmployeeDefinitionId left, EmployeeDefinitionId right) => left.Equals(right);

        public static bool operator !=(EmployeeDefinitionId left, EmployeeDefinitionId right) => !left.Equals(right);
    }
}
