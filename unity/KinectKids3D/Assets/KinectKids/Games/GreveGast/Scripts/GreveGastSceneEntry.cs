using UnityEngine;
using KinectKids.Games.GreveGast;

namespace KinectKids3D.Platform
{
    /// <summary>
    /// Scene entry for the Greve Gast game.
    ///
    /// It now boots the new 2.5D layered chase (<see cref="ChaseSceneDirector"/>)
    /// per new-way.md. Two alternative directors are available behind toggles:
    ///   * <see cref="useRunner"/> — the modular, section-based runner
    ///     (<see cref="RunnerSceneDirector"/>) that streams a sequence of rooms
    ///     (corridor → great hall → kitchen → cellar → attic) driven by the
    ///     song's structure. This is the current development target.
    ///   * <see cref="useLegacySlice"/> — the earlier immediate-mode Style C+
    ///     slice (<see cref="GreveGastStyleCGame"/>), kept as a fallback.
    /// </summary>
    public sealed class GreveGastSceneEntry : MonoBehaviour
    {
        [Tooltip("Boot the modular section-based runner (streams rooms by song structure).")]
        public bool useRunner = true;

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

            if (useRunner)
            {
                if (FindFirstObjectByType<RunnerSceneDirector>() == null)
                {
                    GameObject game = new GameObject("Greve Gast - 2.5D Runner");
                    game.AddComponent<RunnerSceneDirector>();
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
