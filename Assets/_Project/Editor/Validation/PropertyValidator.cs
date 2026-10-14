using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CleanTime.Cleaning.Presentation;

namespace CleanTime.Editor.Validation
{
    /// <summary>
    /// Property authoring checks (architecture §125): every cleaning target needs a stable, unique id and a
    /// dirt definition, and the scene needs exactly one CleaningSceneRoot with a location id.
    /// </summary>
    public static class PropertyValidator
    {
        [MenuItem("Clean Time/Properties/Validate Open Scene")]
        public static void ValidateOpenScene()
        {
            var problems = Validate();

            if (problems.Count == 0)
                Debug.Log("[CleanTime] Property validation passed.");
            else
                foreach (var problem in problems)
                    Debug.LogError("[CleanTime] " + problem);
        }

        [MenuItem("Clean Time/Properties/Assign Missing Target Ids")]
        public static void AssignMissingIds()
        {
            var changed = 0;

            foreach (var target in Targets())
            {
                if (!string.IsNullOrWhiteSpace(target.StableInstanceId))
                    continue;

                Undo.RecordObject(target, "Assign cleaning target id");
                target.AssignNewStableId();
                EditorUtility.SetDirty(target);
                changed++;
            }

            if (changed > 0)
                EditorSceneManager.MarkAllScenesDirty();

            Debug.Log($"[CleanTime] Assigned {changed} missing target id(s).");
        }

        /// <summary>Gives duplicates (typically from copy / paste) fresh ids. Keeps the first holder of each id.</summary>
        [MenuItem("Clean Time/Properties/Fix Duplicate Target Ids")]
        public static void FixDuplicateIds()
        {
            var seen = new HashSet<string>();
            var changed = 0;

            foreach (var target in Targets())
            {
                if (seen.Add(target.StableInstanceId))
                    continue;

                Undo.RecordObject(target, "Regenerate duplicate cleaning target id");
                target.AssignNewStableId();
                EditorUtility.SetDirty(target);
                seen.Add(target.StableInstanceId);
                changed++;
            }

            if (changed > 0)
                EditorSceneManager.MarkAllScenesDirty();

            Debug.Log($"[CleanTime] Regenerated {changed} duplicate target id(s). Saves made with the old ids will not match them.");
        }

        public static IReadOnlyList<string> Validate()
        {
            var problems = new List<string>();
            var targets = Targets().ToList();
            var roots = Object.FindObjectsByType<CleaningSceneRoot>(FindObjectsInactive.Include);

            if (roots.Length != 1)
                problems.Add($"Expected exactly one CleaningSceneRoot in the open scene(s), found {roots.Length}.");

            foreach (var root in roots.Where(r => string.IsNullOrWhiteSpace(r.LocationId.Value)))
                problems.Add($"CleaningSceneRoot '{root.name}' has no location id.");

            foreach (var target in targets)
            {
                if (!target.IsValid(out var problem))
                    problems.Add($"{target.name}: {problem}.");
            }

            foreach (var group in targets.Where(t => !string.IsNullOrWhiteSpace(t.StableInstanceId)).GroupBy(t => t.StableInstanceId).Where(g => g.Count() > 1))
                problems.Add($"Duplicate target id '{group.Key}' on: {string.Join(", ", group.Select(t => t.name))}.");

            return problems;
        }

        private static IEnumerable<CleaningTargetAuthoring> Targets() =>
            Object.FindObjectsByType<CleaningTargetAuthoring>(FindObjectsInactive.Include)
                .OrderBy(t => t.transform.GetSiblingIndex())
                .ThenBy(t => t.name);
    }
}
