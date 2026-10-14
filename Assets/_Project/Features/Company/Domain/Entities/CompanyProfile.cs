using System;
using System.Collections.Generic;
using CleanTime.Shared.Kernel;

namespace CleanTime.Company.Domain
{
    public static class CompanyErrors
    {
        public const string NotUnlocked = "company.not_unlocked";
        public const string AlreadyExists = "company.already_exists";
        public const string InvalidName = "company.invalid_name";
        public const string NotFound = "company.not_found";
        public const string UnknownUpgrade = "company.unknown_upgrade";
        public const string UpgradeMaxed = "company.upgrade_maxed";
    }

    public sealed class CompanyUpgradeDefinition
    {
        public CompanyUpgradeDefinition(CompanyUpgradeId id, string displayNameKey, int maxLevel, Money baseCost, decimal costGrowth = 1.5m)
        {
            Id = id;
            DisplayNameKey = displayNameKey ?? string.Empty;
            MaxLevel = Guard.NotNegative(maxLevel, nameof(maxLevel));
            BaseCost = baseCost;
            CostGrowth = costGrowth < 1m ? 1m : costGrowth;
        }

        public CompanyUpgradeId Id { get; }

        public string DisplayNameKey { get; }

        public int MaxLevel { get; }

        public Money BaseCost { get; }

        public decimal CostGrowth { get; }
    }

    /// <summary>The player's cleaning company. Created once the company feature is unlocked.</summary>
    public sealed class CompanyProfile
    {
        private readonly Dictionary<CompanyUpgradeId, int> _upgrades;

        public CompanyProfile(
            string name,
            int level = 1,
            int reputation = 0,
            Money? lifetimeRevenue = null,
            IReadOnlyDictionary<CompanyUpgradeId, int> upgrades = null)
        {
            Name = Guard.NotEmpty(name, nameof(name)).Trim();
            Level = level < 1 ? 1 : level;
            Reputation = Guard.NotNegative(reputation, nameof(reputation));
            LifetimeRevenue = lifetimeRevenue ?? Money.Zero;
            _upgrades = upgrades != null
                ? new Dictionary<CompanyUpgradeId, int>(upgrades)
                : new Dictionary<CompanyUpgradeId, int>();
        }

        public string Name { get; }

        public int Level { get; private set; }

        public int Reputation { get; private set; }

        public Money LifetimeRevenue { get; private set; }

        public IReadOnlyDictionary<CompanyUpgradeId, int> Upgrades => _upgrades;

        /// <summary>How many employees the company can have at once.</summary>
        public int EmployeeCapacity => 2 + Level;

        public int GetUpgradeLevel(CompanyUpgradeId id) => _upgrades.TryGetValue(id, out var level) ? level : 0;

        public void AddRevenue(Money amount)
        {
            if (amount.Cents > 0)
                LifetimeRevenue += amount;
        }

        public void AddReputation(int amount)
        {
            Reputation = Math.Max(0, Reputation + amount);
        }

        /// <summary>Returns true when the level actually changed.</summary>
        public bool SetLevel(int level)
        {
            level = level < 1 ? 1 : level;
            if (level == Level)
                return false;

            Level = level;
            return true;
        }

        public Result PurchaseUpgrade(CompanyUpgradeDefinition definition)
        {
            var current = GetUpgradeLevel(definition.Id);
            if (current >= definition.MaxLevel)
                return Result.Failure(CompanyErrors.UpgradeMaxed, $"'{definition.Id}' is fully upgraded.");

            _upgrades[definition.Id] = current + 1;
            return Result.Success();
        }
    }
}
