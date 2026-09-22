using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// Central definition of the 2.5D layer stack described in new-way.md.
    /// Gameplay and asset code reference these named bands instead of magic
    /// numbers, so the whole scene keeps a consistent back-to-front order and
    /// art can be reassigned without touching sorting logic.
    /// </summary>
    public enum SceneBand
    {
        Sky = 0,
        FarBackground = 1,
        Background = 2,
        MidBackground = 3,
        LeftEnvironment = 4,
        RightEnvironment = 5,
        Floor = 6,
        Actors = 7,
        Hazards = 8,
        Foreground = 9,
        Fog = 10,
        Fx = 11,
        Ui = 12,
    }

    /// <summary>
    /// Helper for deterministic sprite sorting in a 2.5D scene.
    ///
    /// Each <see cref="SceneBand"/> reserves a block of sorting orders; inside a
    /// band an object's own depth (usually its Y or chase distance) fine-tunes
    /// order so nearer objects draw in front. This keeps the whole illusion
    /// stable no matter how many sprites a placeholder or final asset spawns.
    /// </summary>
    public static class LayerSorting
    {
        public const int BandStride = 1000;

        /// <summary>Base sorting order for a band.</summary>
        public static int BaseOrder(SceneBand band) => (int)band * BandStride;

        /// <summary>
        /// Compute a sorting order inside a band. <paramref name="depth01"/> is a
        /// 0..1 value where 0 = far (drawn behind) and 1 = near (drawn in front).
        /// </summary>
        public static int OrderInBand(SceneBand band, float depth01)
        {
            int span = BandStride - 2;
            int offset = Mathf.Clamp(Mathf.RoundToInt(depth01 * span), 0, span);
            return BaseOrder(band) + offset;
        }

        /// <summary>Assign band + depth ordering to a renderer (and its children).</summary>
        public static void Apply(Renderer renderer, SceneBand band, float depth01 = 0.5f)
        {
            if (renderer == null) return;
            renderer.sortingOrder = OrderInBand(band, depth01);
        }

        /// <summary>Assign a Z world depth that matches a band for orthographic layering.</summary>
        public static float BandZ(SceneBand band, float within01 = 0.5f)
        {
            // Further bands sit at larger positive Z (further from an orthographic
            // camera looking down -Z). Kept small so an orthographic size still frames all.
            float bandZ = (int)band * 1.0f;
            return bandZ + (1f - within01) * 0.5f;
        }
    }
}
