using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CleanTime.Content
{
    /// <summary>
    /// The single list of authored content shipped with the game. Boot builds the runtime registry from
    /// it, and the editor validators check it. (Addressables labels replace direct lists later.)
    /// </summary>
    [CreateAssetMenu(menuName = "Clean Time/Game Content Catalog", fileName = "GameContentCatalog")]
    public sealed class GameContentCatalog : ScriptableObject
    {
        [SerializeField] private DirtDefinition[] dirt = Array.Empty<DirtDefinition>();
        [SerializeField] private EquipmentDefinition[] equipment = Array.Empty<EquipmentDefinition>();
        [SerializeField] private LocationDefinitionAsset[] locations = Array.Empty<LocationDefinitionAsset>();
        [SerializeField] private JobDefinitionAsset[] jobs = Array.Empty<JobDefinitionAsset>();
        [SerializeField] private EmployeeDefinitionAsset[] employees = Array.Empty<EmployeeDefinitionAsset>();
        [SerializeField] private GiftDefinitionAsset[] gifts = Array.Empty<GiftDefinitionAsset>();
        [SerializeField] private QuestDefinitionAsset[] quests = Array.Empty<QuestDefinitionAsset>();
        [SerializeField] private NpcDefinitionAsset[] npcs = Array.Empty<NpcDefinitionAsset>();
        [SerializeField] private CompanyUpgradeDefinitionAsset[] companyUpgrades = Array.Empty<CompanyUpgradeDefinitionAsset>();

        public IReadOnlyList<DirtDefinition> Dirt => dirt;

        public IReadOnlyList<EquipmentDefinition> Equipment => equipment;

        public IReadOnlyList<LocationDefinitionAsset> Locations => locations;

        public IReadOnlyList<JobDefinitionAsset> Jobs => jobs;

        public IReadOnlyList<EmployeeDefinitionAsset> Employees => employees;

        public IReadOnlyList<GiftDefinitionAsset> Gifts => gifts;

        public IReadOnlyList<QuestDefinitionAsset> Quests => quests;

        public IReadOnlyList<NpcDefinitionAsset> Npcs => npcs;

        public IReadOnlyList<CompanyUpgradeDefinitionAsset> CompanyUpgrades => companyUpgrades;
    }

    /// <summary>Consistency checks shared by the boot sequence and the editor validators.</summary>
    public static class ContentValidation
    {
        public static IReadOnlyList<string> Validate(GameContentCatalog catalog)
        {
            var issues = new List<string>();

            CheckIds("dirt", catalog.Dirt.Select(x => (x as UnityEngine.Object, x != null ? x.Id : null)), issues);
            CheckIds("equipment", catalog.Equipment.Select(x => (x as UnityEngine.Object, x != null ? x.Id : null)), issues);
            CheckIds("location", catalog.Locations.Select(x => (x as UnityEngine.Object, x != null ? x.Id : null)), issues);
            CheckIds("job", catalog.Jobs.Select(x => (x as UnityEngine.Object, x != null ? x.Id : null)), issues);
            CheckIds("employee", catalog.Employees.Select(x => (x as UnityEngine.Object, x != null ? x.Id : null)), issues);
            CheckIds("gift", catalog.Gifts.Select(x => (x as UnityEngine.Object, x != null ? x.Id : null)), issues);
            CheckIds("quest", catalog.Quests.Select(x => (x as UnityEngine.Object, x != null ? x.Id : null)), issues);
            CheckIds("npc", catalog.Npcs.Select(x => (x as UnityEngine.Object, x != null ? x.Id : null)), issues);
            CheckIds("company upgrade", catalog.CompanyUpgrades.Select(x => (x as UnityEngine.Object, x != null ? x.Id : null)), issues);

            var dirtIds = new HashSet<string>(catalog.Dirt.Where(d => d != null).Select(d => d.Id));
            var locationIds = new HashSet<string>(catalog.Locations.Where(l => l != null).Select(l => l.Id));

            foreach (var tool in catalog.Equipment.Where(e => e != null))
            {
                foreach (var target in tool.SupportedTargetIds.Where(t => !dirtIds.Contains(t)))
                    issues.Add($"Equipment '{tool.Id}' supports unknown target '{target}'.");
            }

            foreach (var job in catalog.Jobs.Where(j => j != null && !locationIds.Contains(j.LocationId)))
                issues.Add($"Job '{job.Id}' references unknown location '{job.LocationId}'.");

            foreach (var location in catalog.Locations.Where(l => l != null))
            {
                foreach (var (_, required) in location.RequirementReferences.Where(r => !locationIds.Contains(r.LocationId)))
                    issues.Add($"Location '{location.Id}' requires unknown location '{required}'.");
            }

            issues.AddRange(FindUnlockCycles(catalog));
            return issues;
        }

        private static void CheckIds(string kind, IEnumerable<(UnityEngine.Object Asset, string Id)> items, List<string> issues)
        {
            var seen = new HashSet<string>();

            foreach (var (asset, id) in items)
            {
                if (asset == null)
                {
                    issues.Add($"A {kind} catalog entry is empty (missing reference).");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(id))
                    issues.Add($"{kind} '{asset.name}' has no id.");
                else if (!seen.Add(id))
                    issues.Add($"Duplicate {kind} id '{id}' ('{asset.name}').");
            }
        }

        private static IEnumerable<string> FindUnlockCycles(GameContentCatalog catalog)
        {
            var requires = new Dictionary<string, List<string>>();

            foreach (var location in catalog.Locations.Where(l => l != null))
                requires[location.Id ?? string.Empty] = location.RequirementReferences.Select(r => r.LocationId).ToList();

            var state = new Dictionary<string, int>(); // 1 = visiting, 2 = done

            foreach (var start in requires.Keys.ToList())
            {
                var cycle = Visit(start, requires, state, new Stack<string>());
                if (cycle != null)
                    yield return "Location unlock requirements form a cycle: " + cycle;
            }
        }

        private static string Visit(string node, Dictionary<string, List<string>> graph, Dictionary<string, int> state, Stack<string> path)
        {
            if (state.TryGetValue(node, out var s))
                return s == 1 ? string.Join(" -> ", path.Reverse().Concat(new[] { node })) : null;

            if (!graph.TryGetValue(node, out var next))
                return null;

            state[node] = 1;
            path.Push(node);

            foreach (var other in next)
            {
                var cycle = Visit(other, graph, state, path);
                if (cycle != null)
                    return cycle;
            }

            path.Pop();
            state[node] = 2;
            return null;
        }
    }
}
