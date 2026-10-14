using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CleanTime.Audio.Presentation;
using CleanTime.Bootstrap;
using CleanTime.Cleaning.Presentation;
using CleanTime.DebugTools.Presentation;
using CleanTime.Equipment.Presentation;
using CleanTime.Interaction.Presentation;
using CleanTime.NPC.Presentation;
using CleanTime.Player.Presentation;
using CleanTime.Save.Presentation;
using CleanTime.UI.HUD;

namespace CleanTime.Editor.ContentTools
{
    /// <summary>
    /// Creates the structural scenes (architecture §46) with their scene services wired, no art required:
    /// Bootstrap (persistent), MainMenu, Town, PropertyShell and the CleaningVerticalSlice test scene.
    /// Safe to re-run: existing scenes are overwritten, build settings are rewritten.
    /// </summary>
    public static class SceneSetup
    {
        private const string Root = "Assets/_Project/Scenes";

        public const string Bootstrap = Root + "/Bootstrap.unity";
        public const string MainMenu = Root + "/MainMenu.unity";
        public const string Town = Root + "/Town/Town.unity";
        public const string PropertyShell = Root + "/Properties/PropertyShell.unity";
        public const string VerticalSlice = Root + "/TestScenes/CleaningVerticalSlice.unity";

        [MenuItem("Clean Time/Setup/Create Base Scenes")]
        public static void CreateBaseScenes()
        {
            CreateBootstrap(Bootstrap, loadMainMenu: true);
            CreateMainMenu();
            CreateTown();
            CreatePropertyShell(PropertyShell);
            CreateVerticalSlice();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Bootstrap, true),
                new EditorBuildSettingsScene(MainMenu, true),
                new EditorBuildSettingsScene(Town, true),
                new EditorBuildSettingsScene(PropertyShell, true),
            };

            AssetDatabase.SaveAssets();
            Debug.Log("[CleanTime] Base scenes created and added to the build settings.");
        }

        public static void CreateBaseScenesFromCommandLine()
        {
            CreateBaseScenes();
            EditorApplication.Exit(0);
        }

        private static Scene NewScene(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void Save(Scene scene, string path) => EditorSceneManager.SaveScene(scene, path);

        private static GameObject CreateCamera(string name = "Main Camera")
        {
            var camera = new GameObject(name) { tag = "MainCamera" };
            var component = camera.AddComponent<Camera>();
            component.orthographic = true;
            component.orthographicSize = 6f;
            component.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            component.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static void CreateBootstrap(string path, bool loadMainMenu)
        {
            var scene = NewScene(path);

            var root = new GameObject("Bootstrap");
            var bootstrapper = root.AddComponent<GameBootstrapper>();
            root.AddComponent<SceneLifetimeScope>();
            root.AddComponent<AutosaveLifecycleHooks>();
            root.AddComponent<DebugPanel>();

            var settings = new SerializedObject(bootstrapper).FindProperty("settings");
            settings.FindPropertyRelative("loadMainMenuOnBoot").boolValue = loadMainMenu;
            settings.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Save(scene, path);
        }

        private static void CreateMainMenu()
        {
            var scene = NewScene(MainMenu);
            CreateCamera();
            new GameObject("SceneScope").AddComponent<SceneLifetimeScope>();
            Save(scene, MainMenu);
        }

        private static GameObject CreatePlayer(bool withCleaning)
        {
            var player = new GameObject("Player");
            player.AddComponent<SpriteRenderer>();
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            player.AddComponent<CircleCollider2D>().radius = 0.35f;
            player.AddComponent<PlayerMotor2D>();

            var interaction = new GameObject("Interaction");
            interaction.transform.SetParent(player.transform, false);
            var selector = interaction.AddComponent<InteractionTargetSelector>();
            var controller = interaction.AddComponent<InteractionController>();
            SetReference(controller, "selector", selector);
            interaction.AddComponent<EquipmentSwitchController>();

            if (withCleaning)
            {
                var cleaning = new GameObject("Cleaning");
                cleaning.transform.SetParent(player.transform, false);
                var cleaningSelector = cleaning.AddComponent<CleaningTargetSelector>();
                var cleaningController = cleaning.AddComponent<CleaningInteractionController>();
                SetReference(cleaningController, "selector", cleaningSelector);
            }

            return player;
        }

        private static void CreateTown()
        {
            var scene = NewScene(Town);
            CreateCamera();
            new GameObject("SceneScope").AddComponent<SceneLifetimeScope>();
            CreatePlayer(withCleaning: false);

            var town = new GameObject("Town");
            town.AddComponent<TownMusicController>();
            town.AddComponent<NpcScheduleController>();
            town.AddComponent<HudBinder>();

            Save(scene, Town);
        }

        private static void CreatePropertyShell(string path)
        {
            var scene = NewScene(path);
            CreateCamera();
            new GameObject("SceneScope").AddComponent<SceneLifetimeScope>();
            CreatePlayer(withCleaning: true);

            var property = new GameObject("Property");
            var root = property.AddComponent<CleaningSceneRoot>();
            var binder = property.AddComponent<PropertySceneBinder>();
            SetReference(binder, "cleaningRoot", root);
            var hud = property.AddComponent<HudBinder>();
            SetReference(hud, "cleaningRoot", root);

            new GameObject("Targets"); // place CleaningTargetAuthoring objects under here

            Save(scene, path);
        }

        private static void CreateVerticalSlice()
        {
            // Standalone: boots the game itself and skips the main menu, so the scene can be played directly.
            CreateBootstrap(VerticalSlice, loadMainMenu: false);

            var scene = EditorSceneManager.GetActiveScene();
            CreateCamera();
            CreatePlayer(withCleaning: true);

            var property = new GameObject("Property");
            var root = property.AddComponent<CleaningSceneRoot>();
            var binder = property.AddComponent<PropertySceneBinder>();
            SetReference(binder, "cleaningRoot", root);
            property.AddComponent<HudBinder>();
            new GameObject("Targets");

            Save(scene, VerticalSlice);
        }

        private static void SetReference(Object owner, string field, Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
