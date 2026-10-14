using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Stable business identifier for: Inventory item. Never a scene or instance id.</summary>
    public readonly struct ItemId : IEquatable<ItemId>
    {
        public string Value { get; }

        public ItemId(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Value = value;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public bool Equals(ItemId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ItemId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(ItemId left, ItemId right) => left.Equals(right);

        public static bool operator !=(ItemId left, ItemId right) => !left.Equals(right);
    }
}
