using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Application
{
    /// <summary>Tunable job evaluation settings; supplied at composition time (later: authored content).</summary>
    public sealed class JobEvaluationSettings
    {
        public JobEvaluationSettings(int mistakeTolerance = 10)
        {
            MistakeTolerance = Guard.NotNegative(mistakeTolerance, nameof(mistakeTolerance));
        }

        /// <summary>Wrong-tool attempts that reduce the mistakes component to zero.</summary>
        public int MistakeTolerance { get; }
    }
}
