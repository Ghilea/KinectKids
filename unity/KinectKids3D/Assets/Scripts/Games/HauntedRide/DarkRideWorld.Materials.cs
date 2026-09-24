using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class DarkRideWorld
    {
        // Materialfabriken bor numera i den delade MaterialFactory. Dessa
        // metoder behålls som tunna alias eftersom båda spelen och alla
        // prop-/mål-komponenter redan anropar DarkRideWorld.* . Beteendet är
        // identiskt – anropen vidarebefordras rakt av.
        public static Material MaterialOf(Color color, float metallic = 0f)
            => MaterialFactory.Solid(color, metallic);

        public static Material TexturedMaterial(Color tint, Texture2D texture, float metallic)
            => MaterialFactory.Textured(tint, texture, metallic);

        public static Material GlowMaterial(Color color, float strength)
            => MaterialFactory.Glow(color, strength);
    }
}
