using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using KinectKids3D.Platform;

namespace KinectKids3D.Editor
{
    internal static class KinectKidsPlatformBuilder
    {
        private const string BootstrapPath = "Assets/KinectKids/Core/Bootstrap/00_Bootstrap.unity";
        private const string MenuPath = "Assets/KinectKids/Menu/Scenes/01_MainMenu.unity";
        private const string GrevePath = "Assets/KinectKids/Games/GreveGast/Scenes/GreveGast.unity";
        private const string DefinitionPath = "Assets/KinectKids/Menu/GreveGast.asset";
        private const string RegistryPath = "Assets/KinectKids/Menu/GameRegistry.asset";

        [MenuItem("KinectKids/Plattform/Skapa scener och register")]
        public static void CreatePlatformAssets()
        {
            GameDefinition definition = AssetDatabase.LoadAssetAtPath<GameDefinition>(DefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<GameDefinition>();
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }
            definition.displayName = "Greve Gast";
            definition.description = "Spring, hoppa, ducka och byt väg i takt med Greve Gasts sång.";
            definition.sceneName = "GreveGast";
            definition.cardColor = new Color(0.72f, 0.22f, 0.72f);
            definition.isAvailable = true;
            EditorUtility.SetDirty(definition);

            GameRegistry registry = AssetDatabase.LoadAssetAtPath<GameRegistry>(RegistryPath);
            if (registry == null)
            {
                AssetDatabase.DeleteAsset(RegistryPath);
                registry = ScriptableObject.CreateInstance<GameRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }
            registry.games = new[] { definition };
            EditorUtility.SetDirty(registry);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("KinectKids Platform").AddComponent<Platform.KinectKidsPlatformRoot>();
            EditorSceneManager.SaveScene(scene, BootstrapPath);

            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MainMenuController menu = new GameObject("Gemensam spelmeny").AddComponent<MainMenuController>();
            menu.SetRegistry(registry);
            EditorUtility.SetDirty(menu);
            EditorSceneManager.SaveScene(scene, MenuPath);

            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Greve Gast Scene Entry").AddComponent<Platform.GreveGastSceneEntry>();
            EditorSceneManager.SaveScene(scene, GrevePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapPath, true),
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(GrevePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("KinectKids-plattformens scener och spelregister är skapade.");
        }

        [MenuItem("KinectKids/Bygg gemensam plattform for Windows")]
        public static void BuildPlatform()
        {
            CreatePlatformAssets();
            string root = KinectKidsWindowsBuild.FindProjectRoot();
            if (root == null) throw new InvalidOperationException("KinectKids.sln hittades inte ovanför Unity-projektet.");
            GreveGast2DBuilder.BuildAll();
            KinectKidsWindowsBuild.EnsureGreveGastDrawnRuntimePrefab();
            KinectKidsWindowsBuild.ConfigureGreveGastMusic();
            KinectKidsWindowsBuild.BuildBridge(root);
            KinectKidsWindowsBuild.CopyBridgeIntoProject(root);
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsFullScreen = true;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;

            string folder = Path.Combine(root, "unity", "KinectKids3D", "Build", "KinectKids");
            Directory.CreateDirectory(folder);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { BootstrapPath, MenuPath, GrevePath },
                locationPathName = Path.Combine(folder, "KinectKids.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("KinectKids-plattformen kunde inte byggas: " + report.summary.result);
            File.WriteAllText(Path.Combine(folder, "STARTA-HAR.txt"),
                "KINECT KIDS\r\n\r\nStarta KinectKids.exe. Unity Hub behövs inte.\r\n" +
                "Kinect 360 kräver Kinect SDK 1.8 och nätadapter. Tangentbord fungerar som reserv.\r\n" +
                "Escape öppnar pausmenyn i ett spel. Därifrån går det alltid att återvända till spelmenyn.\r\n");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(folder);
            Debug.Log("Gemensam KinectKids-version klar: " + folder);
        }
    }
}
