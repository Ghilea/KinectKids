using UnityEngine;
using KinectKids.Games.GhostHunt;

namespace KinectKids3D.Platform
{
    /// <summary>
    /// Scene entry for Spökjakten. Boots the new 2.5D wagon dark-ride
    /// (<see cref="GhostRide25D"/>). The legacy full-3D <see cref="SpokjaktenGame"/>
    /// stays available behind <see cref="useLegacy3D"/> as a fallback until the
    /// 2.5D ride is fully art-complete.
    /// </summary>
    public sealed class SpokjaktenSceneEntry : MonoBehaviour
    {
        [Tooltip("Use the old full-3D dark-ride instead of the new 2.5D ride.")]
        public bool useLegacy3D = false;

        private void Awake()
        {
            if (useLegacy3D)
            {
                if (FindFirstObjectByType<SpokjaktenGame>() == null)
                {
                    GameObject game = new GameObject("Spökjakten");
                    game.AddComponent<SpokjaktenGame>();
                }
                return;
            }

            if (FindFirstObjectByType<GhostRide25D>() == null)
            {
                GameObject game = new GameObject("Spökjakten - 2.5D Ride");
                game.AddComponent<GhostRide25D>();
            }
        }
    }
}
