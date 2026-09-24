using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Ser till att ett fristående spel alltid lämnar tillbaka kontrollen till
    /// KinectKids-menyn, även om användaren stänger spelfönstret med Alt+F4.
    /// </summary>
    public sealed class LauncherReturnService : MonoBehaviour
    {
        private static string launcherPath;
        private static bool launcherFullscreen;
        private static bool launcherStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], "--launcher", StringComparison.OrdinalIgnoreCase))
                    launcherPath = arguments[index + 1];
                else if (string.Equals(arguments[index], "--launcher-fullscreen", StringComparison.OrdinalIgnoreCase))
                    launcherFullscreen = arguments[index + 1] == "1";
            }

            var host = new GameObject("KinectKids Launcher Return");
            DontDestroyOnLoad(host);
            host.AddComponent<LauncherReturnService>();
        }

        public static void ReturnToLauncher()
        {
            if (global::KinectKids3D.Platform.KinectKidsPlatformRoot.IsActive)
            {
                global::KinectKids3D.Platform.KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
                return;
            }
            StartLauncher();
            Application.Quit();
        }

        private void OnApplicationQuit() => StartLauncher();

        private static void StartLauncher()
        {
            if (launcherStarted || string.IsNullOrWhiteSpace(launcherPath) || !File.Exists(launcherPath)) return;
            launcherStarted = true;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = launcherPath,
                    WorkingDirectory = Path.GetDirectoryName(launcherPath),
                    Arguments = launcherFullscreen ? "--fullscreen" : string.Empty,
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError("Kunde inte återgå till KinectKids-menyn: " + exception.Message);
            }
        }
    }
}
