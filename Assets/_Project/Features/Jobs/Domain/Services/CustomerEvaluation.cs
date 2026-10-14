using System;
using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Domain
{
    /// <summary>Weights are configuration, not magic numbers. They must sum to 1.</summary>
    public sealed class CustomerEvaluationWeights
    {
        public CustomerEvaluationWeights(float cleaning, float optional, float efficiency, float mistakes)
        {
            if (cleaning < 0f || optional < 0f || efficiency < 0f || mistakes < 0f)
                throw new ArgumentOutOfRangeException(nameof(cleaning), "Weights must not be negative.");

            var sum = cleaning + optional + efficiency + mistakes;
            if (Math.Abs(sum - 1f) > 0.001f)
                throw new ArgumentException($"Weights must sum to 1 (was {sum}).");

            Cleaning = cleaning;
            Optional = optional;
            Efficiency = efficiency;
            Mistakes = mistakes;
        }

        public float Cleaning { get; }

        public float Optional { get; }

        public float Efficiency { get; }

        public float Mistakes { get; }

        public static CustomerEvaluationWeights Default => new CustomerEvaluationWeights(0.60f, 0.20f, 0.10f, 0.10f);
    }

    /// <summary>Normalised (0..1) components of a customer evaluation.</summary>
    public readonly struct CustomerEvaluationInput
    {
        public CustomerEvaluationInput(float cleaning, float optional, float efficiency, float mistakes)
        {
            Cleaning = Clamp01(cleaning);
            Optional = Clamp01(optional);
            Efficiency = Clamp01(efficiency);
            Mistakes = Clamp01(mistakes);
        }

        public float Cleaning { get; }

        public float Optional { get; }

        public float Efficiency { get; }

        public float Mistakes { get; }

        /// <param name="mistakeTolerance">Wrong-tool attempts that bring the mistakes component to zero.</param>
        public static CustomerEvaluationInput From(
            float cleaningProgress,
            float optionalCompletion,
            float durationSeconds,
            float parDurationSeconds,
            int wrongToolAttempts,
            int mistakeTolerance)
        {
            var efficiency = parDurationSeconds <= 0f || durationSeconds <= parDurationSeconds
                ? 1f
                : parDurationSeconds / durationSeconds;

            var mistakes = mistakeTolerance <= 0
                ? (wrongToolAttempts == 0 ? 1f : 0f)
                : 1f - (float)wrongToolAttempts / mistakeTolerance;

            return new CustomerEvaluationInput(cleaningProgress, optionalCompletion, efficiency, mistakes);
        }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }

    public interface ICustomerRatingCalculator
    {
        CustomerEvaluation Calculate(CustomerEvaluationInput input);
    }

    public sealed class CustomerRatingCalculator : ICustomerRatingCalculator
    {
        private readonly CustomerEvaluationWeights _weights;

        public CustomerRatingCalculator(CustomerEvaluationWeights weights)
        {
            _weights = Guard.NotNull(weights, nameof(weights));
        }

        public CustomerEvaluation Calculate(CustomerEvaluationInput input)
        {
            var score =
                input.Cleaning * _weights.Cleaning +
                input.Optional * _weights.Optional +
                input.Efficiency * _weights.Efficiency +
                input.Mistakes * _weights.Mistakes;

            return new CustomerEvaluation(
                input.Cleaning,
                input.Optional,
                input.Efficiency,
                input.Mistakes,
                Math.Max(0f, Math.Min(1f, score)));
        }
    }

    public interface IJobRewardCalculator
    {
        Money Calculate(JobDefinitionSnapshot job, CustomerEvaluation evaluation);
    }

    /// <summary>Reward scales linearly between a minimum and maximum multiplier of the base reward.</summary>
    public sealed class QualityScaledRewardCalculator : IJobRewardCalculator
    {
        private readonly decimal _minMultiplier;
        private readonly decimal _maxMultiplier;

        public QualityScaledRewardCalculator(decimal minMultiplier = 0.5m, decimal maxMultiplier = 1.25m)
        {
            if (minMultiplier < 0m || maxMultiplier < minMultiplier)
                throw new ArgumentException("Invalid reward multiplier range.");

            _minMultiplier = minMultiplier;
            _maxMultiplier = maxMultiplier;
        }

        public Money Calculate(JobDefinitionSnapshot job, CustomerEvaluation evaluation)
        {
            var multiplier = _minMultiplier + (_maxMultiplier - _minMultiplier) * (decimal)evaluation.Score;

            return job.BaseReward.Scale(multiplier);
        }
    }
}
