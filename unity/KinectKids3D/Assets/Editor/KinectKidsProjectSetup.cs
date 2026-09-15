using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace KinectKids3D.Editor
{
    [InitializeOnLoad]
    internal static class KinectKidsProjectSetup
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string ScenePath = SceneFolder + "/Spokjakten3D.unity";

        static KinectKidsProjectSetup()
        {
            EditorApplication.delayCall += EnsurePlayableScene;
        }

        private static void EnsurePlayableScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath)) return;
            if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }
}
