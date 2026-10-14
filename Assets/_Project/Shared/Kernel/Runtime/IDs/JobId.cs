using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Stable business identifier for: Job instance. Never a scene or instance id.</summary>
    public readonly struct JobId : IEquatable<JobId>
    {
        public string Value { get; }

        public JobId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool Equals(JobId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is JobId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(JobId left, JobId right) => left.Equals(right);

        public static bool operator !=(JobId left, JobId right) => !left.Equals(right);
    }
}
