using UnityEngine;
using KinectKids.Games.GreveGast;

namespace KinectKids3D.Platform
{
    /// <summary>
    /// Scene entry for the Greve Gast game.
    ///
    /// By default it boots <see cref="CorridorRunnerDirector"/>: a perspective
    /// camera looking through reusable physical 3D corridor segments. Player,
    /// Greve Gast and decorations remain illustrated 2D/2.5D sprites in that
    /// world; their Z positions provide real perspective without manual scaling.
    ///
    /// Two alternative directors are available behind toggles:
    ///   * <see cref="useRunner"/> — the modular, section-based runner
    ///     (<see cref="RunnerSceneDirector"/> + <c>SectionStreamer</c>) meant to
    ///     stream a SEQUENCE of rooms (corridor → great hall → kitchen …) by song
    ///     structure. This is the long-term architecture, but its perspective
    ///     LAYOUT is still WIP: it currently clumps modules near screen-centre
    ///     with no deep-background anchor ("gröt av assets"), so it is OFF until
    ///     its old layout math is retained only as a legacy fallback.
    ///   * <see cref="useLegacySlice"/> — the earlier immediate-mode Style C+
    ///     slice (<see cref="GreveGastStyleCGame"/>), kept as a fallback.
    /// </summary>
    public sealed class GreveGastSceneEntry : MonoBehaviour
    {
        [Tooltip("Boot the OLD modular section-based room streamer (SectionStreamer). " +
                 "Its perspective layout is unfinished (assets clump center-screen). " +
                 "Superseded by the rebuilt CorridorRunnerDirector; leave OFF.")]
        public bool useRunner = false;

        [Tooltip("Use the old immediate-mode Style C+ slice instead of the 2.5D corridor chase.")]
        public bool useLegacySlice = false;

        [Tooltip("Boot the old hard-coded ChaseSceneDirector corridor instead of the rebuilt runner.")]
        public bool useLegacyChase = false;

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
                    GameObject game = new GameObject("Greve Gast - 2.5D Runner (legacy WIP)");
                    game.AddComponent<RunnerSceneDirector>();
                }
                return;
            }

            if (useLegacyChase)
            {
                if (FindFirstObjectByType<ChaseSceneDirector>() == null)
                {
                    GameObject game = new GameObject("Greve Gast - 2.5D Corridor Chase (legacy)");
                    game.AddComponent<ChaseSceneDirector>();
                }
                return;
            }

            // Default: physical 3D corridor skeleton with 2.5D actors.
            if (FindFirstObjectByType<CorridorRunnerDirector>() == null)
            {
                GameObject game = new GameObject("Greve Gast - 3D Corridor Runner");
                game.AddComponent<CorridorRunnerDirector>();
            }
        }
    }
}
