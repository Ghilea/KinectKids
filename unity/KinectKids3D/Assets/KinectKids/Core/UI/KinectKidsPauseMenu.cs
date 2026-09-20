using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D.Platform
{
    public sealed class KinectKidsPauseMenu : MonoBehaviour
    {
        private bool open;
        private int selected;

        private void Update()
        {
            if (SceneManager.GetActiveScene().name == KinectKidsSceneLoader.MainMenuScene) { open = false; return; }
            if (Input.GetKeyDown(KeyCode.Escape)) SetOpen(!open);
            if (!open) return;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) selected = (selected + 2) % 3;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) selected = (selected + 1) % 3;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Activate(selected);
        }

        private void SetOpen(bool value)
        {
            open = value;
            Time.timeScale = open ? 0f : 1f;
            AudioListener.pause = open;
        }

        private void Activate(int item)
        {
            if (item == 0) SetOpen(false);
            else if (item == 1) { SetOpen(false); KinectKidsPlatformRoot.Instance.Scenes.Restart(); }
            else { SetOpen(false); KinectKidsPlatformRoot.Instance.Scenes.LoadMenu(); }
        }

        private void OnGUI()
        {
            if (!open) return;
            GUI.color = new Color(0.06f, 0.035f, 0.08f, 0.94f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.color = Color.white;
            float w = Mathf.Min(620f, Screen.width * 0.7f), x = (Screen.width - w) * 0.5f;
            GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(x, Screen.height * 0.2f, w, 80), "PAUS", title);
            string[] labels = { "FORTSATT", "STARTA OM", "TILL SPELMENYN" };
            for (int i = 0; i < labels.Length; i++)
            {
                Color old = GUI.color;
                if (i == selected) GUI.color = new Color(1f, 0.78f, 0.22f);
                if (GUI.Button(new Rect(x, Screen.height * 0.36f + i * 78f, w, 58f), labels[i])) Activate(i);
                GUI.color = old;
            }
        }
    }
}
