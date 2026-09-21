using UnityEngine;

namespace KinectKids3D.Platform
{
    public sealed class SimonGameEntry : MonoBehaviour
    {
        private void Awake() => gameObject.AddComponent<SimonGameUnity>();
    }
}
