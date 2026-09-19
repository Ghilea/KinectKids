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
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
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
            ConfigureGreveGastMusic();
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsFullScreen = true;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            BuildBridge(root);
            CopyBridgeIntoProject(root);

            string buildFolder = Path.Combine(root, "unity", "KinectKids3D", "Build", "GreveGastJakt");
            Directory.CreateDirectory(buildFolder);
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
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
                "GREVE GASTS JAKT\r\n\r\nStarta GreveGastJakt.exe.\r\n" +
                "F2 visar tidslinjeverktyget. F3 hoppar till forsta refrangen.\r\n" +
                "Tangentbordstest: W spring, mellanslag hoppa, S ducka, A/D sidsteg.\r\n");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(buildFolder);
            UnityEngine.Debug.Log("Greve Gasts Jakt ar klar: " + buildFolder);
        }

        private static void ConfigureGreveGastMusic()
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

        private static void BuildBridge(string root)
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

        private static void CopyBridgeIntoProject(string root)
        {
            string source = Path.Combine(root, "src", "KinectBridge", "bin", "Release", "KinectBridge.exe");
            string targetFolder = Path.Combine(Application.dataPath, "StreamingAssets", "KinectBridge");
            Directory.CreateDirectory(targetFolder);
            File.Copy(source, Path.Combine(targetFolder, "KinectBridge.exe"), true);
            string config = source + ".config";
            if (File.Exists(config)) File.Copy(config, Path.Combine(targetFolder, "KinectBridge.exe.config"), true);
            AssetDatabase.Refresh();
        }

        private static string FindProjectRoot()
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
