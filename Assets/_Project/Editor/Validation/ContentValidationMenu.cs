using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CleanTime.Content;

namespace CleanTime.Editor.Validation
{
    /// <summary>
    /// Content validation (architecture §76-78): duplicate / missing ids, broken references, impossible
    /// or cyclic unlock chains. Run from the menu while authoring, or from CI with
    /// <c>-executeMethod CleanTime.Editor.Validation.ContentValidationMenu.ValidateFromCommandLine</c>.
    /// </summary>
    public static class ContentValidationMenu
    {
        [MenuItem("Clean Time/Validate Content")]
        public static void ValidateFromMenu()
        {
            var issues = ValidateAll();

            if (issues.Count == 0)
                Debug.Log("[CleanTime] Content validation passed.");
            else
                EditorUtility.DisplayDialog("Content validation", $"{issues.Count} problem(s) found. See the console.", "OK");
        }

        /// <summary>Exits the editor with code 1 when any problem is found, so CI fails the build.</summary>
        public static void ValidateFromCommandLine()
        {
            EditorApplication.Exit(ValidateAll().Count == 0 ? 0 : 1);
        }

        public static IReadOnlyList<string> ValidateAll()
        {
            var issues = new List<string>();
            var catalogs = AssetDatabase.FindAssets("t:GameContentCatalog")
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameContentCatalog>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(catalog => catalog != null)
                .ToList();

            if (catalogs.Count == 0)
                Debug.LogWarning("[CleanTime] No GameContentCatalog found; nothing to validate yet.");

            if (catalogs.Count > 1)
                issues.Add($"Found {catalogs.Count} content catalogs; the game expects exactly one.");

            foreach (var catalog in catalogs)
            {
                issues.AddRange(ContentValidation.Validate(catalog).Select(issue => $"{catalog.name}: {issue}"));
                issues.AddRange(FindOrphans(catalog));
            }

            foreach (var issue in issues)
                Debug.LogError("[CleanTime] " + issue);

            return issues;
        }

        /// <summary>Definitions that exist as assets but are not listed in the catalog never reach the game.</summary>
        private static IEnumerable<string> FindOrphans(GameContentCatalog catalog)
        {
            foreach (var orphan in Orphans<DirtDefinition>(catalog.Dirt)
                         .Concat(Orphans<EquipmentDefinition>(catalog.Equipment))
                         .Concat(Orphans<LocationDefinitionAsset>(catalog.Locations))
                         .Concat(Orphans<JobDefinitionAsset>(catalog.Jobs))
                         .Concat(Orphans<EmployeeDefinitionAsset>(catalog.Employees))
                         .Concat(Orphans<GiftDefinitionAsset>(catalog.Gifts))
                         .Concat(Orphans<QuestDefinitionAsset>(catalog.Quests))
                         .Concat(Orphans<NpcDefinitionAsset>(catalog.Npcs))
                         .Concat(Orphans<CompanyUpgradeDefinitionAsset>(catalog.CompanyUpgrades)))
            {
                yield return $"'{orphan}' is not referenced by the content catalog.";
            }
        }

        private static IEnumerable<string> Orphans<T>(IReadOnlyList<T> listed)
            where T : ScriptableObject
        {
            var inCatalog = new HashSet<T>(listed.Where(item => item != null));

            return AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null && !inCatalog.Contains(asset))
                .Select(asset => AssetDatabase.GetAssetPath(asset));
        }
    }
}
