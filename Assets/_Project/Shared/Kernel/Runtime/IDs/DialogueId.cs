using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Stable business identifier for: Dialogue graph. Never a scene or instance id.</summary>
    public readonly struct DialogueId : IEquatable<DialogueId>
    {
        public string Value { get; }

        public DialogueId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool Equals(DialogueId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is DialogueId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(DialogueId left, DialogueId right) => left.Equals(right);

        public static bool operator !=(DialogueId left, DialogueId right) => !left.Equals(right);
    }
}
