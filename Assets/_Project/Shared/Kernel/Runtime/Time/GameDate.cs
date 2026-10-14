using System;

namespace CleanTime.Shared.Kernel
{
    public enum DayPhase
    {
        Morning = 0,
        Afternoon = 1,
        Evening = 2,
        Night = 3,
    }

    public readonly struct GameDate : IEquatable<GameDate>, IComparable<GameDate>
    {
        public GameDate(int day, DayPhase phase)
        {
            if (day < 1)
                throw new ArgumentOutOfRangeException(nameof(day), day, "Days are 1-based.");

            Day = day;
            Phase = phase;
        }

        public int Day { get; }

        public DayPhase Phase { get; }

        public static GameDate Start => new GameDate(1, DayPhase.Morning);

        public int CompareTo(GameDate other)
        {
            var byDay = Day.CompareTo(other.Day);
            return byDay != 0 ? byDay : Phase.CompareTo(other.Phase);
        }

        public bool Equals(GameDate other) => Day == other.Day && Phase == other.Phase;

        public override bool Equals(object obj) => obj is GameDate other && Equals(other);

        public override int GetHashCode() => (Day, Phase).GetHashCode();

        public override string ToString() => $"Day {Day} ({Phase})";

        public static bool operator ==(GameDate left, GameDate right) => left.Equals(right);

        public static bool operator !=(GameDate left, GameDate right) => !left.Equals(right);

        public static bool operator <(GameDate left, GameDate right) => left.CompareTo(right) < 0;

        public static bool operator >(GameDate left, GameDate right) => left.CompareTo(right) > 0;

        public static bool operator <=(GameDate left, GameDate right) => left.CompareTo(right) <= 0;

        public static bool operator >=(GameDate left, GameDate right) => left.CompareTo(right) >= 0;
    }
}
