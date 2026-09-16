using UnityEngine;

namespace KinectKids3D
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            if (Object.FindFirstObjectByType<SpokjaktenGame>() != null) return;
            GameObject root = new GameObject("KinectKids 3D - Spokjakten");
            root.AddComponent<SpokjaktenGame>();
            Object.DontDestroyOnLoad(root);
        }
    }
}
