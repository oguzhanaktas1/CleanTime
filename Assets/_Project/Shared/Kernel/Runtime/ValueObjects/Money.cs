using System;
using System.Globalization;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Currency amount in integer minor units (cents) to avoid floating-point money bugs.</summary>
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public Money(long cents)
        {
            Cents = cents;
        }

        public long Cents { get; }

        public static Money Zero => new Money(0);

        public bool IsNegative => Cents < 0;

        public bool IsZero => Cents == 0;

        public static Money FromUnits(decimal units) => new Money((long)Math.Round(units * 100m));

        /// <summary>Multiplies by a factor, rounding to the nearest cent.</summary>
        public Money Scale(decimal factor) => new Money((long)Math.Round(Cents * factor, MidpointRounding.AwayFromZero));

        public decimal ToUnits() => Cents / 100m;

        public int CompareTo(Money other) => Cents.CompareTo(other.Cents);

        public bool Equals(Money other) => Cents == other.Cents;

        public override bool Equals(object obj) => obj is Money other && Equals(other);

        public override int GetHashCode() => Cents.GetHashCode();

        public override string ToString() => ToUnits().ToString("F2", CultureInfo.InvariantCulture);

        public static Money operator +(Money a, Money b) => new Money(a.Cents + b.Cents);

        public static Money operator -(Money a, Money b) => new Money(a.Cents - b.Cents);

        public static bool operator ==(Money a, Money b) => a.Equals(b);

        public static bool operator !=(Money a, Money b) => !a.Equals(b);

        public static bool operator <(Money a, Money b) => a.Cents < b.Cents;

        public static bool operator >(Money a, Money b) => a.Cents > b.Cents;

        public static bool operator <=(Money a, Money b) => a.Cents <= b.Cents;

        public static bool operator >=(Money a, Money b) => a.Cents >= b.Cents;
    }
}
