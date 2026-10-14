using System;
using CleanTime.Jobs.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Employees.Domain
{
    public interface IEmployeeJobSimulator
    {
        EmployeeJobOutcome Simulate(Employee employee, JobDefinitionSnapshot job, int deterministicSeed);
    }

    public interface IEmployeeRevenueCalculator
    {
        Money Calculate(Employee employee, EmployeeJobOutcome outcome);
    }

    /// <summary>
    /// Employees do not play the job; the outcome is resolved mathematically from their skill, the job
    /// difficulty and a stored seed, so the same inputs always yield the same result.
    /// </summary>
    public sealed class EmployeeJobSimulator : IEmployeeJobSimulator
    {
        public EmployeeJobOutcome Simulate(
            Employee employee,
            JobDefinitionSnapshot job,
            int deterministicSeed)
        {
            IRandomSource random = new SeededRandomSource(deterministicSeed);

            float skillFactor =
                Math.Max(0.1f, Math.Min(1.5f, employee.Stats.CleaningSkill / 100f));

            float variance =
                0.9f + random.Next01() * 0.2f;

            float quality =
                Math.Max(0f, Math.Min(1f, skillFactor * variance / job.Difficulty));

            long reward =
                (long)(job.BaseReward.Cents * (0.6f + quality * 0.4f));

            return new EmployeeJobOutcome(
                quality,
                new Money(reward));
        }
    }

    /// <summary>The company keeps <c>share</c> of the reward the employee earned (default: all of it).</summary>
    public sealed class ShareEmployeeRevenueCalculator : IEmployeeRevenueCalculator
    {
        private readonly decimal _share;

        public ShareEmployeeRevenueCalculator(decimal share = 1m)
        {
            if (share < 0m || share > 1m)
                throw new ArgumentOutOfRangeException(nameof(share));

            _share = share;
        }

        public Money Calculate(Employee employee, EmployeeJobOutcome outcome) => outcome.Reward.Scale(_share);
    }
}
