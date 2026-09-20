using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace KinectKids3D.Platform
{
    public sealed class KinectKidsPlatformRoot : MonoBehaviour
    {
        public static KinectKidsPlatformRoot Instance { get; private set; }
        public KinectKidsSceneLoader Scenes { get; private set; }
        public KinectKidsInputManager Input { get; private set; }
        public static bool IsActive => Instance != null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Scenes = new KinectKidsSceneLoader();
            Input = gameObject.AddComponent<KinectKidsInputManager>();
            gameObject.AddComponent<KinectKidsPauseMenu>();
            foreach (string argument in Environment.GetCommandLineArgs())
                if (string.Equals(argument, "--platform-smoke-test", StringComparison.OrdinalIgnoreCase))
                    gameObject.AddComponent<KinectKidsPlatformSmokeTest>();
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == KinectKidsSceneLoader.BootstrapScene)
                Scenes.LoadMenu();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
