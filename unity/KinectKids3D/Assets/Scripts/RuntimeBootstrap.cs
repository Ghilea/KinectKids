using UnityEngine;
using UnityEngine.SceneManagement;

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
            if (Object.FindFirstObjectByType<SpokjaktenGame>() != null) return;
            GameObject root = new GameObject("KinectKids 3D - Spokjakten");
            root.AddComponent<SpokjaktenGame>();
            // Spelet hör till scenen. En riktig scenomladdning ska ta bort hela
            // runtime-världen så dörrar, monster och fällor byggs om från noll.
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartGame();
        }
    }
}
