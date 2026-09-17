using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Följer färden utan att ärva kamerans duckning, lutning eller skakning.
    /// Kameran motsvarar spelarens huvud; vagnen ska ligga kvar på rälsen.
    /// </summary>
    public sealed class RideCarFollower : MonoBehaviour
    {
        private Transform head;
        private const float StableHeight = 1.72f;

        public void Configure(Transform cameraTransform)
        {
            head = cameraTransform;
            if (head != null)
            {
                SnapToRide();
            }
        }

        private void LateUpdate()
        {
            if (head != null) SnapToRide();
        }

        private void SnapToRide()
        {
            transform.position = new Vector3(head.position.x, StableHeight, head.position.z);
            Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (flatForward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        }
    }
}
