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
        private const string SpokjaktenPath = "Assets/KinectKids/Games/GhostHunt/Spokjakten.unity";
        private const string LearningScenes = "Assets/KinectKids/Games/Learning/Scenes/";
        private const string DefinitionPath = "Assets/KinectKids/Menu/GreveGast.asset";
        private const string SpokjaktenDefinitionPath = "Assets/KinectKids/Menu/Spokjakten.asset";
        private const string RegistryPath = "Assets/KinectKids/Menu/GameRegistry.asset";

        [MenuItem("KinectKids/Plattform/Skapa scener och register")]
        public static void CreatePlatformAssets()
        {
            EnsureRuntimeMaterial();
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

            GameDefinition spokjakten = AssetDatabase.LoadAssetAtPath<GameDefinition>(SpokjaktenDefinitionPath);
            if (spokjakten == null)
            {
                spokjakten = ScriptableObject.CreateInstance<GameDefinition>();
                AssetDatabase.CreateAsset(spokjakten, SpokjaktenDefinitionPath);
            }
            spokjakten.displayName = "Spökjakten";
            spokjakten.description = "Åk genom den hemsökta borgen och kasta magi på spöken och monster.";
            spokjakten.sceneName = "Spokjakten";
            spokjakten.cardColor = new Color(0.10f, 0.58f, 0.78f);
            spokjakten.isAvailable = true;
            EditorUtility.SetDirty(spokjakten);

            GameDefinition math = EnsureGame("Math", "Matematikbanan", "Fånga rätt svar på addition och subtraktion.", "Matematikbanan", new Color(0.12f, 0.50f, 0.88f));
            GameDefinition swedish = EnsureGame("Swedish", "Bokstavsjakten", "Hitta första eller sista bokstaven i svenska ord.", "Bokstavsjakten", new Color(0.94f, 0.34f, 0.20f));
            GameDefinition shapes = EnsureGame("Shapes", "Formverkstan", "Känn igen cirklar, trianglar, kvadrater och stjärnor.", "Formverkstan", new Color(0.12f, 0.66f, 0.38f));
            GameDefinition patterns = EnsureGame("Patterns", "Mönsterjakten", "Fortsätt mönster med former, tal och bokstäver.", "Monsterjakten", new Color(0.60f, 0.24f, 0.78f));

            GameDefinition simon = EnsureGame("Simon", "Simon sÃ¤ger", "HÃ¤rma rÃ¶relserna med hela kroppen.", "SimonSager", new Color(0.86f, 0.40f, 0.75f));
            GameDefinition balloons = EnsureGame("Balloons", "Ballongjakten", "SmÃ¤ll flygande ballonger med hÃ¤nderna.", "Ballongjakten", new Color(0.10f, 0.70f, 0.76f));
            simon.displayName = "Simon s\u00e4ger";
            simon.description = "H\u00e4rma r\u00f6relserna med hela kroppen.";
            balloons.description = "Sm\u00e4ll flygande ballonger med h\u00e4nderna.";

            GameRegistry registry = AssetDatabase.LoadAssetAtPath<GameRegistry>(RegistryPath);
            if (registry == null)
            {
                AssetDatabase.DeleteAsset(RegistryPath);
                registry = ScriptableObject.CreateInstance<GameRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }
            registry.games = new[] { definition, spokjakten, math, swedish, shapes, patterns, simon, balloons };
            EditorUtility.SetDirty(registry);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("KinectKids Platform").AddComponent<Platform.KinectKidsPlatformRoot>();
            EditorSceneManager.SaveScene(scene, BootstrapPath);

            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MainMenuController menu = new GameObject("Gemensam spelmeny").AddComponent<MainMenuController>();
            menu.SetRegistry(registry);
            menu.SetVisualAssets(
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/menu/ChatGPT Image 20 sep. 2026 16_54_25 (1).png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/menu/ChatGPT Image 20 sep. 2026 16_54_26 (2).png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/menu/ChatGPT Image 20 sep. 2026 16_54_26 (4).png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/menu/ChatGPT Image 20 sep. 2026 16_54_27 (5).png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/menu/ChatGPT Image 20 sep. 2026 16_54_28 (7).png"));
            EditorUtility.SetDirty(menu);
            EditorSceneManager.SaveScene(scene, MenuPath);

            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Greve Gast Scene Entry").AddComponent<Platform.GreveGastSceneEntry>();
            EditorSceneManager.SaveScene(scene, GrevePath);

            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Spökjakten Scene Entry").AddComponent<Platform.SpokjaktenSceneEntry>();
            EditorSceneManager.SaveScene(scene, SpokjaktenPath);

            CreateLearningScene("Matematikbanan", LearningGameMode.Math);
            CreateLearningScene("Bokstavsjakten", LearningGameMode.Swedish);
            CreateLearningScene("Formverkstan", LearningGameMode.Shapes);
            CreateLearningScene("Monsterjakten", LearningGameMode.Patterns);
            CreateMovementScene<SimonGameEntry>("SimonSager");
            CreateMovementScene<BalloonGameEntry>("Ballongjakten");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapPath, true),
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(GrevePath, true),
                new EditorBuildSettingsScene(SpokjaktenPath, true),
                new EditorBuildSettingsScene(LearningScenes + "Matematikbanan.unity", true),
                new EditorBuildSettingsScene(LearningScenes + "Bokstavsjakten.unity", true),
                new EditorBuildSettingsScene(LearningScenes + "Formverkstan.unity", true),
                new EditorBuildSettingsScene(LearningScenes + "Monsterjakten.unity", true),
                new EditorBuildSettingsScene(LearningScenes + "SimonSager.unity", true),
                new EditorBuildSettingsScene(LearningScenes + "Ballongjakten.unity", true)
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

            string buildRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KinectKids", "Build");
            string folder = Path.Combine(buildRoot, "KinectKids");
            string stagingFolder = Path.Combine(buildRoot, "KinectKids-staging-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
            Directory.CreateDirectory(stagingFolder);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = Path.Combine(stagingFolder, "KinectKids.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("KinectKids-plattformen kunde inte byggas: " + report.summary.result);
            File.WriteAllText(Path.Combine(stagingFolder, "STARTA-HAR.txt"),
                "KINECT KIDS\r\n\r\nStarta KinectKids.exe. Unity Hub behövs inte.\r\n" +
                "Kinect 360 kräver Kinect SDK 1.8 och nätadapter. Tangentbord fungerar som reserv.\r\n" +
                "Escape öppnar pausmenyn i ett spel. Därifrån går det alltid att återvända till spelmenyn.\r\n");
            InstallFreshBuild(folder, stagingFolder);
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(folder);
            Debug.Log("Gemensam KinectKids-version klar: " + folder);
        }

        private static void InstallFreshBuild(string finalFolder, string stagingFolder)
        {
            string backupFolder = finalFolder + "-old-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            bool previousMoved = false;
            try
            {
                if (Directory.Exists(finalFolder))
                {
                    Directory.Move(finalFolder, backupFolder);
                    previousMoved = true;
                }
                Directory.Move(stagingFolder, finalFolder);
            }
            catch
            {
                if (!Directory.Exists(finalFolder) && previousMoved && Directory.Exists(backupFolder))
                    Directory.Move(backupFolder, finalFolder);
                throw;
            }

            if (!previousMoved) return;
            try { Directory.Delete(backupFolder, true); }
            catch (Exception exception) { Debug.LogWarning("Den tidigare byggmappen kunde inte rensas: " + exception.Message); }
        }

        private static GameDefinition EnsureGame(string assetName, string displayName, string description,
            string sceneName, Color color)
        {
            string path = "Assets/KinectKids/Menu/" + assetName + ".asset";
            GameDefinition game = AssetDatabase.LoadAssetAtPath<GameDefinition>(path);
            if (game == null)
            {
                game = ScriptableObject.CreateInstance<GameDefinition>();
                AssetDatabase.CreateAsset(game, path);
            }
            game.displayName = displayName;
            game.description = description;
            game.sceneName = sceneName;
            game.cardColor = color;
            game.isAvailable = true;
            EditorUtility.SetDirty(game);
            return game;
        }

        private static void EnsureRuntimeMaterial()
        {
            const string path = "Assets/Resources/KinectKidsRuntimeStandard.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(shader) { name = "KinectKids Runtime Standard" };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
        }

        private static void CreateLearningScene(string sceneName, LearningGameMode mode)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LearningGameEntry entry = new GameObject(sceneName + " Scene Entry").AddComponent<LearningGameEntry>();
            entry.Configure(mode);
            EditorUtility.SetDirty(entry);
            EditorSceneManager.SaveScene(scene, LearningScenes + sceneName + ".unity");
        }

        private static void CreateMovementScene<T>(string sceneName) where T : Component
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(sceneName + " Scene Entry").AddComponent<T>();
            EditorSceneManager.SaveScene(scene, LearningScenes + sceneName + ".unity");
        }
    }
}
