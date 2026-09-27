using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace KinectKids.Editor
{
    /// <summary>Keeps editor component icons out of the playable Game view.</summary>
    [InitializeOnLoad]
    internal static class HideGameViewGizmos
    {
        private static readonly Type GameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        private static readonly FieldInfo DrawGizmosField = GameViewType?.GetField(
            "m_Gizmos", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ShowGizmosField = GameViewType?.BaseType?.GetField(
            "m_ShowGizmos", BindingFlags.Instance | BindingFlags.NonPublic);
        private static double nextCheck;

        static HideGameViewGizmos()
        {
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!EditorApplication.isPlaying || GameViewType == null) return;
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 0.5;

            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null || window.GetType() != GameViewType) continue;
                bool changed = Disable(DrawGizmosField, window) | Disable(ShowGizmosField, window);
                if (changed) window.Repaint();
            }
        }

        private static bool Disable(FieldInfo field, EditorWindow window)
        {
            if (field == null || !(bool)field.GetValue(window)) return false;
            field.SetValue(window, false);
            return true;
        }
    }
}
