using UnityEngine;

namespace KinectKids3D.Platform
{
    public sealed class BalloonGameEntry : MonoBehaviour
    {
        private void Awake() => gameObject.AddComponent<BalloonGameUnity>();
    }
}
