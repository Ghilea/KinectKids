using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KinectKids3D.Editor
{
    internal static class KinectKidsWindowsBuild
    {
        [MenuItem("KinectKids/Bygg skolversion för Windows")]
        public static void BuildSchoolVersion()
        {
            string root = FindProjectRoot();
            if (root == null) throw new InvalidOperationException("KinectKids.sln hittades inte ovanför Unity-projektet.");
            KinectKidsMusicSetup.EnsureMusicAsset();
            BuildBridge(root);
            CopyBridgeIntoProject(root);

            string buildFolder = Path.Combine(root, "unity", "KinectKids3D", "Build", "Spokjakten3D");
            Directory.CreateDirectory(buildFolder);
            string[] scenes = { "Assets/Scenes/Spokjakten3D.unity" };
            if (scenes.Length == 0) throw new InvalidOperationException("Ingen aktiv Unity-scen finns i Build Settings.");

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(buildFolder, "Spokjakten3D.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows-bygget misslyckades: " + report.summary.result);

            File.WriteAllText(Path.Combine(buildFolder, "STARTA-HÄR.txt"),
                "SPÖKJAKTEN\r\n\r\nStarta Spokjakten3D.exe.\r\n" +
                "Unity behöver inte vara installerat. Datorn behöver Kinect SDK 1.8 och Kinectens nätadapter.\r\n" +
                "Stäng Kinect Explorer och Kinect Studio innan spelet startas.\r\n");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(buildFolder);
            UnityEngine.Debug.Log("Skolversionen är klar: " + buildFolder);
        }

        [MenuItem("KinectKids/Bygg Greve Gasts Jakt for Windows")]
        public static void BuildGreveGastVersion()
        {
            string root = FindProjectRoot();
            if (root == null) throw new InvalidOperationException("KinectKids.sln hittades inte ovanfor Unity-projektet.");
            GreveGast2DBuilder.BuildAll();
            EnsureGreveGastDrawnRuntimePrefab();
            // Behall den gamla 3D-figuren som saker fallback tills hela den nya
            // 2.5D-riktningen ar verifierad i spelet.
            global::GreveGast.Editor.GreveGastBuilder.BuildCharacter();
            EnsureGreveGastRuntimePrefab();
            ConfigureGreveGastMusic();
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsFullScreen = true;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            BuildBridge(root);
            CopyBridgeIntoProject(root);

            string buildFolder = Path.Combine(root, "unity", "KinectKids3D", "Build", "GreveGastJakt");
            Directory.CreateDirectory(buildFolder);
            string[] scenes = { "Assets/Scenes/Spokjakten3D.unity" };
            if (scenes.Length == 0) throw new InvalidOperationException("Ingen aktiv Unity-scen finns i Build Settings.");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(buildFolder, "GreveGastJakt.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Greve Gast-bygget misslyckades: " + report.summary.result);

            File.WriteAllText(Path.Combine(buildFolder, "STARTA-HAR.txt"),
                "GREVE GASTS TECKNINGSJAKT - TEKNISK PROTOTYP\r\n\r\nStarta GreveGastJakt.exe.\r\n" +
                "Greve Gast har tagit over en levande teckning och jagar spelaren genom pappersvarlden.\r\n" +
                "F2 visar tidslinjeverktyget. F3 hoppar till forsta refrangen.\r\n" +
                "Tangentbordstest: W spring, mellanslag hoppa, S ducka, A/D sidsteg.\r\n");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(buildFolder);
            UnityEngine.Debug.Log("Greve Gasts Jakt ar klar: " + buildFolder);
        }

        [MenuItem("KinectKids/Greve Gast/Bygg tecknat animationstest for Windows")]
        public static void BuildGreveGastDrawnTestVersion()
        {
            GreveGast2DBuilder.BuildAll();
            string root = FindProjectRoot();
            if (root == null) throw new InvalidOperationException("KinectKids.sln hittades inte ovanfor Unity-projektet.");
            string buildFolder = Path.Combine(root, "unity", "KinectKids3D", "Build", "GreveGastDrawnTest");
            Directory.CreateDirectory(buildFolder);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/GreveGast2D/GreveGastDrawnTest.unity" },
                locationPathName = Path.Combine(buildFolder, "GreveGastDrawnTest.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Greve Gast-animationstestet misslyckades: " + report.summary.result);
            UnityEngine.Debug.Log("Greve Gast-animationstestet ar klart: " + buildFolder);
        }

        internal static void EnsureGreveGastRuntimePrefab()
        {
            const string source = "Assets/GreveGast/Generated/Prefabs/GreveGast.prefab";
            const string folder = "Assets/Resources/GreveGastCharacter";
            const string target = folder + "/GreveGast.prefab";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Resources", "GreveGastCharacter");
            AssetDatabase.DeleteAsset(target);
            if (!AssetDatabase.CopyAsset(source, target))
                throw new InvalidOperationException("Greve Gast-prefaben kunde inte kopieras till Resources.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        internal static void EnsureGreveGastDrawnRuntimePrefab()
        {
            const string source = "Assets/GreveGast2D/Prefabs/GreveGastDrawn.prefab";
            const string folder = "Assets/Resources/GreveGast2D";
            const string target = folder + "/GreveGastDrawn.prefab";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Resources", "GreveGast2D");
            AssetDatabase.DeleteAsset(target);
            if (!AssetDatabase.CopyAsset(source, target))
                throw new InvalidOperationException("Greve Gasts tecknade prefab kunde inte kopieras till Resources.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        internal static void ConfigureGreveGastMusic()
        {
            const string assetPath = "Assets/Resources/Audio/Music/GreveGastsJakt.wav";
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Greve Gasts musikfil saknas: " + assetPath);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.82f;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        internal static void BuildBridge(string root)
        {
            string script = Path.Combine(root, "scripts", "Build-KinectBridge.ps1");
            using (Process process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"",
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                if (process == null || !process.WaitForExit(120000) || process.ExitCode != 0)
                    throw new InvalidOperationException("Kinect-bryggan kunde inte byggas. Kör scripts\\Build-KinectBridge.ps1 i PowerShell.");
            }
        }

        internal static void CopyBridgeIntoProject(string root)
        {
            string source = Path.Combine(root, "src", "KinectBridge", "bin", "Release", "KinectBridge.exe");
            string targetFolder = Path.Combine(Application.dataPath, "StreamingAssets", "KinectBridge");
            Directory.CreateDirectory(targetFolder);
            File.Copy(source, Path.Combine(targetFolder, "KinectBridge.exe"), true);
            string config = source + ".config";
            if (File.Exists(config)) File.Copy(config, Path.Combine(targetFolder, "KinectBridge.exe.config"), true);
            AssetDatabase.Refresh();
        }

        internal static string FindProjectRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(Application.dataPath);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "KinectKids.sln"))) return directory.FullName;
                directory = directory.Parent;
            }
            return null;
        }
    }
}
