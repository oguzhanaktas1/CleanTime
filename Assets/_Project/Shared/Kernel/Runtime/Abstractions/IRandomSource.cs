using System;

namespace CleanTime.Shared.Kernel
{
    /// <summary>Injected randomness so important outcomes are reproducible from a stored seed.</summary>
    public interface IRandomSource
    {
        int Next(int minInclusive, int maxExclusive);

        float Next01();
    }

    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SeededRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

        public float Next01() => (float)_random.NextDouble();
    }
}
