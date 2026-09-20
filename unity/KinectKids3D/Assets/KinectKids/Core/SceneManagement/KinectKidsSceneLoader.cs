using UnityEngine.SceneManagement;

namespace KinectKids3D.Platform
{
    public sealed class KinectKidsSceneLoader
    {
        public const string BootstrapScene = "00_Bootstrap";
        public const string MainMenuScene = "01_MainMenu";

        public void LoadMenu() => SceneManager.LoadScene(MainMenuScene);
        public void LoadGame(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName)) SceneManager.LoadScene(sceneName);
        }
        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
