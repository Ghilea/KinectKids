using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// A single 2.5D depth layer. Placed at a fixed Z depth, it scrolls and
    /// scales in response to a shared parallax "travel" value driven by
    /// <see cref="ParallaxController"/>.
    ///
    /// The component is purely visual bookkeeping: it owns no gameplay rules and
    /// no baked-in art. Any sprite or child hierarchy can be parented under it,
    /// so final assets can replace placeholders without code changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("Depth of this layer. 0 = at the camera focal plane, higher = further back.")]
        public float depth = 1f;

        [Tooltip("How strongly this layer scrolls horizontally with camera/player travel. " +
                 "Background layers use small values, foreground layers larger values.")]
        public float horizontalFactor = 1f;

        [Tooltip("How strongly this layer scrolls vertically with travel. Usually 0 for a chase.")]
        public float verticalFactor = 0f;

        [Tooltip("How strongly this layer scrolls TOWARD the camera with forward travel. " +
                 "Drives the 'running forward' treadmill: the floor uses a positive value so " +
                 "it streams downward past the camera. Combine with loopHeight to make it seamless.")]
        public float forwardFactor = 0f;

        [Tooltip("Optional looping width. When > 0 the layer wraps its horizontal offset " +
                 "so a tiled backdrop repeats seamlessly.")]
        public float loopWidth = 0f;

        [Tooltip("Optional looping height. When > 0 the forward scroll wraps every loopHeight " +
                 "units so a tiled floor/wall streams past the camera without gaps.")]
        public float loopHeight = 0f;

        [Tooltip("Extra idle drift, e.g. slow fog motion, added on top of parallax scroll.")]
        public Vector2 driftPerSecond = Vector2.zero;

        private Vector3 basePosition;
        private float driftTime;

        public float Depth => depth;

        private void Awake()
        {
            basePosition = transform.localPosition;
        }

        /// <summary>Re-cache the authored base position (call after runtime placement).</summary>
        public void CaptureBasePosition()
        {
            basePosition = transform.localPosition;
        }

        /// <summary>
        /// Apply a global travel offset. Called every frame by the controller so
        /// all layers stay perfectly in sync.
        /// </summary>
        public void ApplyTravel(float horizontalTravel, float verticalTravel, float deltaTime)
        {
            driftTime += deltaTime;

            float x = -horizontalTravel * horizontalFactor;
            if (loopWidth > 0.0001f)
            {
                x = Mathf.Repeat(x, loopWidth);
                if (x > loopWidth * 0.5f) x -= loopWidth;
            }

            // Vertical scroll: legacy verticalFactor plus the "run forward"
            // forwardFactor. A positive forwardFactor streams the layer DOWNWARD
            // (toward the camera). loopHeight wraps it so a tiled floor repeats
            // seamlessly, giving a treadmill "running forward" effect.
            float y = -verticalTravel * verticalFactor;
            if (forwardFactor > 0.0001f)
            {
                float forward = verticalTravel * forwardFactor;
                if (loopHeight > 0.0001f) forward = Mathf.Repeat(forward, loopHeight);
                y -= forward; // subtract so the layer moves down/toward camera
            }

            Vector3 drift = new Vector3(driftPerSecond.x, driftPerSecond.y, 0f) * driftTime;
            transform.localPosition = basePosition + new Vector3(x, y, 0f) + drift;
        }
    }
}
