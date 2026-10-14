using System.Collections.Generic;
using CleanTime.Equipment.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Cleaning.Domain
{
    public interface ICleaningTargetRepository
    {
        Result<CleaningTarget> Get(string instanceId);

        Result Save(CleaningTarget target);

        IReadOnlyCollection<CleaningTarget> GetAll();

        void ReplaceAll(IEnumerable<CleaningTarget> targets);

        void Clear();
    }

    public interface ICleaningSessionRepository
    {
        Result<CleaningSession> GetCurrent();

        Result Save(CleaningSession session);

        void Clear();
    }

    public interface ICleaningProgressCalculator
    {
        /// <summary>Weighted progress of the given targets, 0..1.</summary>
        float Calculate(IReadOnlyCollection<CleaningTarget> targets);
    }

    public interface IEquipmentCompatibilityPolicy
    {
        bool CanHandle(EquipmentDefinitionSnapshot equipment, DirtTypeId dirtType);
    }
}
