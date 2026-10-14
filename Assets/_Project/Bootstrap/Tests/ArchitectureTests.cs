using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CleanTime.Bootstrap.Tests
{
    /// <summary>
    /// Guards the rules from the architecture document so they cannot erode silently
    /// (dependency direction, Unity-free domain, one MonoBehaviour per correctly named file).
    /// </summary>
    public sealed class ArchitectureTests
    {
        private static readonly string[] Layers = { "Domain", "Application", "Infrastructure", "Presentation" };

        private static IEnumerable<Assembly> ProjectAssemblies() =>
            UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies()
                .Where(a => a.GetName().Name.StartsWith("CleanTime.", StringComparison.Ordinal))
                .Where(a => !a.GetName().Name.EndsWith(".Tests", StringComparison.Ordinal) &&
                            !a.GetName().Name.EndsWith(".PlayModeTests", StringComparison.Ordinal) &&
                            !a.GetName().Name.EndsWith(".TestUtilities", StringComparison.Ordinal));

        private static string LayerOf(string assemblyName)
        {
            var last = assemblyName.Substring(assemblyName.LastIndexOf('.') + 1);
            return Layers.Contains(last) ? last : null;
        }

        private static string FeatureOf(string assemblyName)
        {
            var parts = assemblyName.Split('.');
            return parts.Length == 3 && Layers.Contains(parts[2]) ? parts[1] : null;
        }

        [Test]
        public void TheProjectDefinesAllExpectedLayerAssemblies()
        {
            var names = ProjectAssemblies().Select(a => a.GetName().Name).ToList();

            foreach (var feature in new[] { "Cleaning", "Jobs", "Economy", "Save", "Progression", "Employees", "Quests" })
            {
                foreach (var layer in new[] { "Domain", "Application", "Infrastructure" })
                    CollectionAssert.Contains(names, $"CleanTime.{feature}.{layer}");
            }
        }

        [Test]
        public void DomainAndApplication_NeverReferenceUnity()
        {
            foreach (var assembly in ProjectAssemblies())
            {
                var layer = LayerOf(assembly.GetName().Name);
                var isKernel = assembly.GetName().Name == "CleanTime.Shared.Kernel";
                if (layer != "Domain" && layer != "Application" && !isKernel)
                    continue;

                var unity = assembly.GetReferencedAssemblies()
                    .Select(r => r.Name)
                    .Where(n => n.StartsWith("UnityEngine", StringComparison.Ordinal) || n.StartsWith("UnityEditor", StringComparison.Ordinal) || n.StartsWith("Unity."))
                    .ToList();

                Assert.IsEmpty(unity, $"{assembly.GetName().Name} must stay free of Unity but references: {string.Join(", ", unity)}");
            }
        }

        [Test]
        public void DependenciesPointInwards()
        {
            var violations = new List<string>();

            foreach (var assembly in ProjectAssemblies())
            {
                var name = assembly.GetName().Name;
                var layer = LayerOf(name);

                if (layer == null)
                    continue;

                foreach (var reference in assembly.GetReferencedAssemblies().Select(r => r.Name))
                {
                    var referencedLayer = LayerOf(reference);
                    if (referencedLayer == null || !reference.StartsWith("CleanTime.", StringComparison.Ordinal))
                        continue;

                    var sameFeature = FeatureOf(name) == FeatureOf(reference);

                    // Domain: only other domains. Application: domains and other applications (public contracts).
                    // Infrastructure: its own domain / application. Nothing may reference another feature's presentation or infrastructure.
                    var allowed =
                        (layer == "Domain" && referencedLayer == "Domain") ||
                        (layer == "Application" && (referencedLayer == "Domain" || referencedLayer == "Application")) ||
                        (layer == "Infrastructure" && (referencedLayer == "Domain" || (referencedLayer == "Application" && sameFeature))) ||
                        (layer == "Presentation" && (referencedLayer == "Domain" || referencedLayer == "Application" || referencedLayer == "Presentation"));

                    if (!allowed)
                        violations.Add($"{name} -> {reference}");
                }
            }

            Assert.IsEmpty(violations, string.Join("\n", violations));
        }

        [Test]
        public void NoFeatureDependsOnABootstrapOrContent_FromBelow()
        {
            var forbidden = new[] { "CleanTime.Bootstrap", "CleanTime.Editor" };

            foreach (var assembly in ProjectAssemblies().Where(a => LayerOf(a.GetName().Name) != null))
            {
                var bad = assembly.GetReferencedAssemblies().Select(r => r.Name).Intersect(forbidden).ToList();
                Assert.IsEmpty(bad, $"{assembly.GetName().Name} references {string.Join(", ", bad)}");
            }
        }

        [Test]
        public void EveryMonoBehaviourAndScriptableObject_LivesInAFileNamedAfterIt()
        {
            var problems = new List<string>();

            foreach (var assembly in ProjectAssemblies())
            {
                foreach (var type in SafeTypes(assembly))
                {
                    var isUnityBehaviour = typeof(MonoBehaviour).IsAssignableFrom(type) || typeof(ScriptableObject).IsAssignableFrom(type);
                    if (!isUnityBehaviour || type.IsGenericType || type.IsNested)
                        continue;

                    var scripts = AssetDatabase.FindAssets(type.Name + " t:MonoScript")
                        .Select(guid => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)))
                        .Where(script => script != null);

                    if (!scripts.Any(script => script.GetClass() == type))
                        problems.Add($"{type.FullName}: put it in a file called {type.Name}.cs (Unity cannot serialise it otherwise).");
                }
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void StableIds_AreValueTypes_NotRawStrings()
        {
            var ids = typeof(CleanTime.Shared.Kernel.JobId).Assembly.GetTypes()
                .Where(t => t.Namespace == "CleanTime.Shared.Kernel" && t.Name.EndsWith("Id") && t.IsValueType)
                .ToList();

            Assert.GreaterOrEqual(ids.Count, 10);
        }

        private static IEnumerable<Type> SafeTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
    }
}
