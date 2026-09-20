using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D.Platform
{
    public sealed class KinectKidsPlatformSmokeTest : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return WaitForScene(KinectKidsSceneLoader.MainMenuScene);
            Debug.Log("PLATFORM_SMOKE: MainMenu loaded");
            yield return new WaitForSecondsRealtime(1f);
            KinectKidsPlatformRoot.Instance.Scenes.LoadGame("GreveGast");
            yield return WaitForScene("GreveGast");
            yield return null;
            if (FindFirstObjectByType<GreveGastStyleCGame>() == null)
                Debug.LogError("PLATFORM_SMOKE: GreveGastStyleCGame missing");
            else Debug.Log("PLATFORM_SMOKE: Greve Gast loaded");
            yield return new WaitForSecondsRealtime(2f);
            KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
            yield return WaitForScene(KinectKidsSceneLoader.MainMenuScene);
            Debug.Log("PLATFORM_SMOKE: return to MainMenu succeeded");
            yield return new WaitForSecondsRealtime(0.5f);
            KinectKidsPlatformRoot.Instance.Scenes.LoadGame("Spokjakten");
            yield return WaitForScene("Spokjakten");
            yield return null;
            if (FindFirstObjectByType<SpokjaktenGame>() == null)
                Debug.LogError("PLATFORM_SMOKE: SpokjaktenGame missing");
            else Debug.Log("PLATFORM_SMOKE: Spokjakten loaded");
            yield return new WaitForSecondsRealtime(2f);
            KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
            yield return WaitForScene(KinectKidsSceneLoader.MainMenuScene);
            Debug.Log("PLATFORM_SMOKE: Spokjakten return to MainMenu succeeded");
            string[] learningScenes = { "Matematikbanan", "Bokstavsjakten", "Formverkstan", "Monsterjakten", "SimonSager", "Ballongjakten" };
            foreach (string learningScene in learningScenes)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                KinectKidsPlatformRoot.Instance.Scenes.LoadGame(learningScene);
                yield return WaitForScene(learningScene);
                yield return null;
                bool gameFound = learningScene == "SimonSager"
                    ? FindFirstObjectByType<SimonGameUnity>() != null
                    : learningScene == "Ballongjakten"
                        ? FindFirstObjectByType<BalloonGameUnity>() != null
                        : FindFirstObjectByType<LearningChoiceGameUnity>() != null;
                if (!gameFound)
                    Debug.LogError("PLATFORM_SMOKE: learning game missing in " + learningScene);
                else Debug.Log("PLATFORM_SMOKE: " + learningScene + " loaded");
                yield return new WaitForSecondsRealtime(0.7f);
                KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
                yield return WaitForScene(KinectKidsSceneLoader.MainMenuScene);
                Debug.Log("PLATFORM_SMOKE: " + learningScene + " return succeeded");
            }
            Application.Quit(0);
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (SceneManager.GetActiveScene().name != sceneName)
                Debug.LogError("PLATFORM_SMOKE: timeout waiting for " + sceneName);
        }
    }
}
