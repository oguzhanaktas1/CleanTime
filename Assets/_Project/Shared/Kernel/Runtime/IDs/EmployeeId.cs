using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Stable business identifier for: Hired employee. Never a scene or instance id.</summary>
    public readonly struct EmployeeId : IEquatable<EmployeeId>
    {
        public string Value { get; }

        public EmployeeId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool Equals(EmployeeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is EmployeeId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(EmployeeId left, EmployeeId right) => left.Equals(right);

        public static bool operator !=(EmployeeId left, EmployeeId right) => !left.Equals(right);
    }
}
