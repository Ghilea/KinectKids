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
            if (FindFirstObjectByType<GreveGastGame>() == null)
                Debug.LogError("PLATFORM_SMOKE: GreveGastGame missing");
            else Debug.Log("PLATFORM_SMOKE: Greve Gast loaded");
            yield return new WaitForSecondsRealtime(2f);
            KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
            yield return WaitForScene(KinectKidsSceneLoader.MainMenuScene);
            Debug.Log("PLATFORM_SMOKE: return to MainMenu succeeded");
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
