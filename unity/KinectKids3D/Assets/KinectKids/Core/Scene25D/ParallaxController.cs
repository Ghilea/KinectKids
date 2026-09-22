using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// Central driver for a set of <see cref="ParallaxLayer"/> instances.
    ///
    /// Gameplay code only pushes a single scalar "speed" (how fast the world is
    /// travelling toward the camera) plus an optional lateral offset. The
    /// controller integrates that into a shared travel value and distributes it
    /// to every registered layer, so depth illusion stays consistent regardless
    /// of which art is loaded.
    /// </summary>
    public sealed class ParallaxController : MonoBehaviour
    {
        private readonly List<ParallaxLayer> layers = new List<ParallaxLayer>();

        [Tooltip("Current forward travel speed of the world, in world units per second.")]
        public float forwardSpeed = 0f;

        [Tooltip("Target lateral offset (e.g. player weaving left/right). Smoothed toward.")]
        public float lateralTarget = 0f;

        [Tooltip("How quickly the lateral offset eases toward its target.")]
        public float lateralResponse = 6f;

        private float horizontalTravel;
        private float lateralOffset;

        public float ForwardTravel { get; private set; }

        /// <summary>Register a layer (auto-collected from children on Awake too).</summary>
        public void Register(ParallaxLayer layer)
        {
            if (layer != null && !layers.Contains(layer)) layers.Add(layer);
        }

        public void Clear() => layers.Clear();

        private void Awake()
        {
            GetComponentsInChildren(true, layers);
        }

        /// <summary>Re-scan children for layers (after runtime assembly).</summary>
        public void CollectLayers()
        {
            layers.Clear();
            GetComponentsInChildren(true, layers);
            for (int i = 0; i < layers.Count; i++) layers[i].CaptureBasePosition();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;

            ForwardTravel += forwardSpeed * dt;
            lateralOffset = Mathf.Lerp(lateralOffset, lateralTarget,
                1f - Mathf.Exp(-lateralResponse * dt));

            // Lateral weaving reads as a small horizontal shift of the world.
            horizontalTravel = lateralOffset;

            for (int i = 0; i < layers.Count; i++)
            {
                ParallaxLayer layer = layers[i];
                if (layer == null) continue;
                // Forward travel feeds the vertical scroll term so layers can
                // stream past; lateral weaving feeds the horizontal term.
                layer.ApplyTravel(horizontalTravel, ForwardTravel, dt);
            }
        }
    }
}
