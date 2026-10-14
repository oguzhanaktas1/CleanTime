using System;

namespace CleanTime.Shared.Kernel
{
    public readonly struct SaveId : IEquatable<SaveId>
    {
        public Guid Value { get; }

        public SaveId(Guid value)
        {
            Value = value;
        }

        public static SaveId New() => new SaveId(Guid.NewGuid());

        public bool IsEmpty => Value == Guid.Empty;

        public bool Equals(SaveId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is SaveId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString("D");

        public static SaveId Parse(string text) => new SaveId(Guid.Parse(text));

        public static bool operator ==(SaveId left, SaveId right) => left.Equals(right);

        public static bool operator !=(SaveId left, SaveId right) => !left.Equals(right);
    }
}
