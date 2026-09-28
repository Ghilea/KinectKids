using UnityEngine;

namespace KinectKids.Games.GreveGast
{
    public sealed class WindowParallaxScroll : MonoBehaviour
    {
        private Material scenery;
        private Transform viewer;

        public void Initialize(Material sceneryMaterial)
        {
            scenery = sceneryMaterial;
            viewer = Camera.main != null ? Camera.main.transform : null;
            UpdateOffsets();
        }

        private void LateUpdate()
        {
            UpdateOffsets();
        }

        private void UpdateOffsets()
        {
            if (scenery == null) return;
            // Adjacent windows reveal different parts of the same panorama.
            // Read the relative position after corridor and camera movement.
            float relativeZ = transform.position.z - (viewer != null ? viewer.position.z : 0f);
            scenery.SetFloat("_FarOffset", Mathf.Repeat(0.28f + relativeZ * 0.003f, 1f));
            scenery.SetFloat("_NearOffset", Mathf.Repeat(0.5f + relativeZ * 0.008f +
                Time.time * 0.004f, 1f));
        }

        private void OnDestroy()
        {
            if (scenery != null) Destroy(scenery);
        }
    }
}
