using UnityEngine;
using KinectKids.Games.Movement;

namespace KinectKids3D.Platform
{
    public sealed class BalloonGameEntry : MonoBehaviour
    {
        [Tooltip("Use the old flat OnGUI balloon game instead of the 2.5D version.")]
        public bool useLegacyFlat = false;

        private void Awake()
        {
            if (useLegacyFlat) { gameObject.AddComponent<BalloonGameUnity>(); return; }
            gameObject.AddComponent<Balloon25DGame>();
        }
    }
}
