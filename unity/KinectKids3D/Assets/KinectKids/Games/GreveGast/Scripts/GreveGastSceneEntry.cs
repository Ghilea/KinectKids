using UnityEngine;
using KinectKids.Games.GreveGast;

namespace KinectKids3D.Platform
{
    /// <summary>
    /// Scene entry for the Greve Gast game.
    ///
    /// It now boots the new 2.5D layered chase (<see cref="ChaseSceneDirector"/>)
    /// per new-way.md. The earlier immediate-mode Style C+ slice
    /// (<see cref="GreveGastStyleCGame"/>) is kept as a fallback and can be
    /// re-enabled by setting <see cref="useLegacySlice"/> to true.
    /// </summary>
    public sealed class GreveGastSceneEntry : MonoBehaviour
    {
        [Tooltip("Use the old immediate-mode Style C+ slice instead of the new 2.5D chase.")]
        public bool useLegacySlice = false;

        private void Awake()
        {
            if (useLegacySlice)
            {
                if (FindFirstObjectByType<GreveGastStyleCGame>() == null)
                {
                    GameObject game = new GameObject("Greve Gast - Style C+ vertical slice");
                    game.AddComponent<GreveGastStyleCGame>();
                }
                return;
            }

            if (FindFirstObjectByType<ChaseSceneDirector>() == null)
            {
                GameObject game = new GameObject("Greve Gast - 2.5D Chase");
                game.AddComponent<ChaseSceneDirector>();
            }
        }
    }
}
