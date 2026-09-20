using UnityEngine;

namespace GreveGast2D
{
    /// <summary>Keeps a flat character facing the active game camera in a 3D world.</summary>
    public sealed class BillboardToCamera : MonoBehaviour
    {
        [SerializeField] private bool yAxisOnly = true;
        [SerializeField] private Camera targetCamera;

        public bool YAxisOnly
        {
            get => yAxisOnly;
            set => yAxisOnly = value;
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null && Camera.allCamerasCount > 0)
                    targetCamera = Camera.allCameras[0];
            }
            if (targetCamera == null) return;

            Vector3 direction = targetCamera.transform.position - transform.position;
            if (yAxisOnly) direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
