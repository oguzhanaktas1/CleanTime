using UnityEngine;
using CleanTime.Employees.Domain;
using CleanTime.Shared.Kernel;

namespace CleanTime.Content
{
    [CreateAssetMenu(menuName = "Clean Time/Employee Definition", fileName = "SO_Employee_")]
    public sealed class EmployeeDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string id = "";
        [SerializeField] private string displayNameKey = "";
        [SerializeField] private long hireCostCents;
        [Min(0)] [SerializeField] private int cleaningSkill = 20;
        [Min(0)] [SerializeField] private int speedSkill = 20;
        [Min(0)] [SerializeField] private int reliabilitySkill = 20;

        public string Id => id;

        public EmployeeDefinitionSnapshot ToSnapshot() => new EmployeeDefinitionSnapshot(
            new EmployeeDefinitionId(id),
            displayNameKey,
            new Money(hireCostCents),
            new EmployeeStats(cleaningSkill, speedSkill, reliabilitySkill));
    }
}
