using UnityEngine;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// Boots the new 2.5D chase (<see cref="ChaseSceneDirector"/>) in the active
    /// platform scene. Drop this component on any object, or let
    /// <c>GreveGastSceneEntry</c> add it, and the layered chase assembles itself
    /// with placeholder art.
    /// </summary>
    public sealed class ChaseSceneEntry : MonoBehaviour
    {
        private void Awake()
        {
            if (FindFirstObjectByType<ChaseSceneDirector>() == null)
            {
                var go = new GameObject("Greve Gast - 2.5D Chase");
                go.AddComponent<ChaseSceneDirector>();
            }
        }
    }
}
