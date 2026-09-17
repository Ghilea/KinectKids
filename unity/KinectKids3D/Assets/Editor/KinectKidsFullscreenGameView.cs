using System;
using UnityEditor;

namespace KinectKids3D.Editor
{
    public static class KinectKidsFullscreenGameView
    {
        [MenuItem("KinectKids/Växla maximerad Game-vy _F11")]
        public static void ToggleMaximizedGameView()
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return;
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            gameView.maximized = !gameView.maximized;
            gameView.Focus();
        }
    }
}
