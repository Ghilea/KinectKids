using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Diagnostics;

namespace KinectKids3D
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneReload()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == "00_Bootstrap" || activeScene == "01_MainMenu" || activeScene == "GreveGast"
                || global::KinectKids3D.Platform.KinectKidsPlatformRoot.IsActive) return;
            if (UnityEngine.Object.FindFirstObjectByType<HauntedRideGame>() != null
                || UnityEngine.Object.FindFirstObjectByType<ChaseGame>() != null
                || UnityEngine.Object.FindFirstObjectByType<global::GreveGast2D.GreveGastDrawnTestPanel>() != null
                || SceneManager.GetActiveScene().name == "GreveGastDrawnTest") return;
            bool greveGast = IsGreveGastBuild();
            GameObject root = new GameObject(greveGast
                ? "KinectKids 3D - Greve Gasts teckningsjakt"
                : "KinectKids 3D - Spokjakten");
            if (greveGast) root.AddComponent<ChaseGame>();
            else root.AddComponent<HauntedRideGame>();
            // Spelet hör till scenen. En riktig scenomladdning ska ta bort hela
            // runtime-världen så dörrar, monster och fällor byggs om från noll.
        }

        private static bool IsGreveGastBuild()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
                if (string.Equals(argument, "--greve-gast", StringComparison.OrdinalIgnoreCase)) return true;
            try
            {
                string executable = Process.GetCurrentProcess().ProcessName;
                return executable.IndexOf("GreveGast", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartGame();
        }
    }
}
