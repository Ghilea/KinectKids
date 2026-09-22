using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// Scales and positions a sprite/actor along a virtual depth axis to fake
    /// perspective in an orthographic 2.5D scene (new-way.md: "scaling mot
    /// kameran"). An object with <see cref="depth01"/> near 0 sits far away and
    /// small; near 1 it is close to the camera and large.
    ///
    /// Purely visual. Gameplay just sets <see cref="depth01"/>; the mapping from
    /// depth to on-screen scale/position lives here so it can be tuned per scene
    /// without touching game rules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraDepthScaler : MonoBehaviour
    {
        [Range(0f, 1f)]
        [Tooltip("0 = far/back (small), 1 = near/front of camera (large).")]
        public float depth01 = 0.5f;

        [Tooltip("Local scale when the object is at the far plane (depth01 = 0).")]
        public float farScale = 0.35f;

        [Tooltip("Local scale when the object is at the near plane (depth01 = 1).")]
        public float nearScale = 1.6f;

        [Tooltip("Local Y position at the far plane (objects sit higher near the horizon).")]
        public float farY = 1.4f;

        [Tooltip("Local Y position at the near plane (objects sit lower, closer to bottom).")]
        public float nearY = -2.2f;

        [Tooltip("Optional band this object lives in; depth also fine-tunes sorting.")]
        public SceneBand band = SceneBand.Actors;

        [Tooltip("Use a curved falloff so near objects grow faster than linear (more depth feel).")]
        public bool usePerspectiveCurve = true;

        private Renderer[] renderers;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void LateUpdate()
        {
            Apply(depth01);
        }

        /// <summary>Immediately position and scale for a given depth.</summary>
        public void Apply(float depth)
        {
            depth01 = Mathf.Clamp01(depth);

            float t = usePerspectiveCurve
                ? depth01 * depth01 * (3f - 2f * depth01) // smoothstep-ish perspective
                : depth01;

            float scale = Mathf.Lerp(farScale, nearScale, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            Vector3 p = transform.localPosition;
            p.y = Mathf.Lerp(farY, nearY, t);
            transform.localPosition = p;

            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].sortingOrder = LayerSorting.OrderInBand(band, depth01);
            }
        }
    }
}
