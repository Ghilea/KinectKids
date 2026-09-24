using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using KinectKids.Games.GreveGast;
using KinectKids.Games.GhostHunt;
using KinectKids.Games.Learning;
using KinectKids.Games.Movement;

namespace KinectKids3D.Platform
{
    public sealed class KinectKidsPlatformSmokeTest : MonoBehaviour
    {
        private int failures;

        private IEnumerator Start()
        {
            yield return WaitForScene(KinectKidsSceneLoader.MainMenuScene);
            Debug.Log("PLATFORM_SMOKE: MainMenu loaded");
            yield return new WaitForSecondsRealtime(1f);
            KinectKidsPlatformRoot.Instance.Scenes.LoadGame("GreveGast");
            yield return WaitForScene("GreveGast");
            yield return null;
            if (FindFirstObjectByType<RunnerSceneDirector>() == null)
                Fail("Greve Gast runner missing");
            else Debug.Log("PLATFORM_SMOKE: Greve Gast loaded");
            yield return new WaitForSecondsRealtime(2f);
            KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
            yield return WaitForScene(KinectKidsSceneLoader.MainMenuScene);
            Debug.Log("PLATFORM_SMOKE: return to MainMenu succeeded");
            yield return new WaitForSecondsRealtime(0.5f);
            KinectKidsPlatformRoot.Instance.Scenes.LoadGame("Spokjakten");
            yield return WaitForScene("Spokjakten");
            yield return null;
            if (FindFirstObjectByType<GhostRide25D>() == null)
                Fail("Spokjakten ride missing");
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
                    ? FindFirstObjectByType<Simon25DGame>() != null
                    : learningScene == "Ballongjakten"
                        ? FindFirstObjectByType<Balloon25DGame>() != null
                        : FindFirstObjectByType<Learning25DGame>() != null;
                if (!gameFound)
                    Fail("learning game missing in " + learningScene);
                else Debug.Log("PLATFORM_SMOKE: " + learningScene + " loaded");
                yield return new WaitForSecondsRealtime(0.7f);
                KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
                yield return WaitForScene(KinectKidsSceneLoader.MainMenuScene);
                Debug.Log("PLATFORM_SMOKE: " + learningScene + " return succeeded");
            }
            if (failures == 0) Debug.Log("PLATFORM_SMOKE: all scenes passed");
            Application.Quit(failures == 0 ? 0 : 1);
        }

        private void Fail(string message)
        {
            failures++;
            Debug.LogError("PLATFORM_SMOKE: " + message);
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
