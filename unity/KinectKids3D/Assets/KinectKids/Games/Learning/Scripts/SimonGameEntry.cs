using UnityEngine;
using KinectKids.Games.Movement;

namespace KinectKids3D.Platform
{
    public sealed class SimonGameEntry : MonoBehaviour
    {
        [Tooltip("Use the old flat OnGUI Simon game instead of the 2.5D version.")]
        public bool useLegacyFlat = false;

        private void Awake()
        {
            if (useLegacyFlat) { gameObject.AddComponent<SimonGameUnity>(); return; }
            gameObject.AddComponent<Simon25DGame>();
        }
    }
}
