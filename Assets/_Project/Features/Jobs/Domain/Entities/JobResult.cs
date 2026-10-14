using CleanTime.Shared.Kernel;

namespace CleanTime.Jobs.Domain
{
    public sealed class CustomerEvaluation
    {
        public CustomerEvaluation(float cleaning, float optional, float efficiency, float mistakes, float score)
        {
            Cleaning = cleaning;
            Optional = optional;
            Efficiency = efficiency;
            Mistakes = mistakes;
            Score = score;
        }

        public float Cleaning { get; }

        public float Optional { get; }

        public float Efficiency { get; }

        public float Mistakes { get; }

        /// <summary>Weighted overall score, 0..1.</summary>
        public float Score { get; }

        /// <summary>Customer-facing rating, 1..5.</summary>
        public int Stars
        {
            get
            {
                var stars = (int)System.Math.Round(Score * 5f);
                return stars < 1 ? 1 : (stars > 5 ? 5 : stars);
            }
        }
    }

    public sealed class JobResult
    {
        public JobResult(
            JobId jobId,
            JobDefinitionId definitionId,
            LocationId locationId,
            float cleaningProgress,
            CustomerEvaluation evaluation,
            Money reward,
            float durationSeconds,
            int completedDay)
        {
            JobId = jobId;
            DefinitionId = definitionId;
            LocationId = locationId;
            CleaningProgress = cleaningProgress;
            Evaluation = evaluation;
            Reward = reward;
            DurationSeconds = durationSeconds;
            CompletedDay = completedDay;
        }

        public JobId JobId { get; }

        public JobDefinitionId DefinitionId { get; }

        public LocationId LocationId { get; }

        public float CleaningProgress { get; }

        public CustomerEvaluation Evaluation { get; }

        public Money Reward { get; }

        public float DurationSeconds { get; }

        public int CompletedDay { get; }
    }
}
