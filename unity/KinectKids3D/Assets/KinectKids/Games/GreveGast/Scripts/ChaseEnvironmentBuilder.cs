using UnityEngine;
using KinectKids.Scene25D;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// Builds the layered 2.5D chase environment. When the real cut environment
    /// sprites are present (Resources/GreveChase/Environment) they are used for
    /// each parallax band — a haunted corridor deep background, stone walls with
    /// torches on the sides, a tiled stone floor, pillars in the mid layer and
    /// drifting fog in front. If a sprite is missing the layer falls back to a
    /// placeholder quad, so the scene always builds.
    /// </summary>
    public static class ChaseEnvironmentBuilder
    {
        // Named environment cuts (see docs/asset-sources/environment_sheet.png).
        private const string Corridor = "env_19";     // torch-lit hall (deep background)
        private const string CastleRuins = "env_18";  // castle + moon (alt background)
        private const string FloorClean = "env_0";    // stone path
        private const string FloorCracked = "env_1";  // cracked stone path
        private const string WallLeft = "env_2";      // wall + banner + torch
        private const string WallRight = "env_3";     // wall + torch
        private const string Pillar = "env_13";       // stone pillar
        private const string Torch = "env_10";        // wall torch
        private const string FogA = "env_16";         // thin fog bank
        private const string FogB = "env_17";         // thick fog bank

        public static ParallaxController Build(Transform parent)
        {
            GameObject worldGo = new GameObject("ChaseWorld");
            worldGo.transform.SetParent(parent, false);
            var parallax = worldGo.AddComponent<ParallaxController>();

            bool art = GreveChaseSprites.Environment(Corridor) != null;

            // --- Deep background: the haunted corridor stretching toward the camera. ---
            var far = NewLayer(worldGo.transform, "FarBackground", SceneBand.FarBackground, 0.10f);
            if (art)
            {
                PlaceSpriteByHeight(far, GreveChaseSprites.Environment(Corridor),
                    new Vector3(0, 0.5f, 0), 12f, SceneBand.FarBackground, 0.2f);
            }
            else
            {
                Quad(far, PlaceholderArt.SolidBlock(), PlaceholderArt.NightBlue,
                    new Vector3(0, 2f, 0), new Vector3(40f, 24f, 1f), SceneBand.FarBackground, 0.1f);
            }

            // --- Mid background: pillars marching past. ---
            var mid = NewLayer(worldGo.transform, "MidBackground", SceneBand.MidBackground, 0.35f);
            for (int i = -3; i <= 3; i++)
            {
                if (art && (i % 2 == 0))
                    PlaceSpriteByHeight(mid, GreveChaseSprites.Environment(Pillar),
                        new Vector3(i * 3.2f, -0.5f, 0), 7f, SceneBand.MidBackground, 0.4f);
                else if (!art)
                    Quad(mid, PlaceholderArt.SolidBlock(), PlaceholderArt.Shadow,
                        new Vector3(i * 3f, 0.5f, 0), new Vector3(1.0f, 6f, 1f), SceneBand.MidBackground, 0.4f);
            }

            // --- Side walls with torches (strong parallax, framing the run). ---
            var leftEnv = NewLayer(worldGo.transform, "LeftEnvironment", SceneBand.LeftEnvironment, 0.6f);
            var rightEnv = NewLayer(worldGo.transform, "RightEnvironment", SceneBand.RightEnvironment, 0.6f);
            if (art)
            {
                for (int i = 0; i < 3; i++)
                {
                    float x = -7f - i * 0.2f;
                    PlaceSpriteByHeight(leftEnv, GreveChaseSprites.Environment(WallLeft),
                        new Vector3(x, 0f, 0), 12f, SceneBand.LeftEnvironment, 0.5f);
                    PlaceSpriteByHeight(rightEnv, GreveChaseSprites.Environment(WallRight),
                        new Vector3(-x, 0f, 0), 12f, SceneBand.RightEnvironment, 0.5f);
                }
                // Warm torches as accents.
                for (int i = 0; i < 3; i++)
                {
                    float y = 2.5f - i * 1.8f;
                    PlaceSpriteByHeight(leftEnv, GreveChaseSprites.Environment(Torch),
                        new Vector3(-4.6f, y, -0.1f), 2.2f, SceneBand.LeftEnvironment, 0.7f);
                    PlaceSpriteByHeight(rightEnv, GreveChaseSprites.Environment(Torch),
                        new Vector3(4.6f, y, -0.1f), 2.2f, SceneBand.RightEnvironment, 0.7f);
                }
            }
            else
            {
                Quad(leftEnv, PlaceholderArt.SolidBlock(), PlaceholderArt.DeepPurple,
                    new Vector3(-6.5f, 0f, 0), new Vector3(6f, 16f, 1f), SceneBand.LeftEnvironment, 0.5f);
                Quad(rightEnv, PlaceholderArt.SolidBlock(), PlaceholderArt.DeepPurple,
                    new Vector3(6.5f, 0f, 0), new Vector3(6f, 16f, 1f), SceneBand.RightEnvironment, 0.5f);
                for (int i = 0; i < 4; i++)
                {
                    float y = 3f - i * 1.6f;
                    Quad(leftEnv, PlaceholderArt.Glow(), PlaceholderArt.LanternOrange,
                        new Vector3(-3.8f, y, -0.1f), new Vector3(1.4f, 1.4f, 1f), SceneBand.LeftEnvironment, 0.6f);
                    Quad(rightEnv, PlaceholderArt.Glow(), PlaceholderArt.LanternOrange,
                        new Vector3(3.8f, y, -0.1f), new Vector3(1.4f, 1.4f, 1f), SceneBand.RightEnvironment, 0.6f);
                }
            }

            // --- Floor: tiled stone path scrolling toward the camera. ---
            var floor = NewLayer(worldGo.transform, "Floor", SceneBand.Floor, 0.5f);
            if (art)
            {
                for (int i = -2; i <= 4; i++)
                {
                    string tile = (i % 2 == 0) ? FloorClean : FloorCracked;
                    PlaceSpriteByHeight(floor, GreveChaseSprites.Environment(tile),
                        new Vector3(0, -3.6f - i * 0.01f, i * 0.01f), 4.5f, SceneBand.Floor, 0.5f);
                }
            }
            else
            {
                Quad(floor, PlaceholderArt.SolidBlock(), new Color(0.15f, 0.12f, 0.20f),
                    new Vector3(0, -4.5f, 0), new Vector3(40f, 6f, 1f), SceneBand.Floor, 0.5f);
            }

            // --- Fog banks drifting in front. ---
            var fog = NewLayer(worldGo.transform, "Fog", SceneBand.Fog, 0.9f);
            fog.driftPerSecond = new Vector2(0.15f, 0f);
            if (art)
            {
                var fogSr = PlaceSpriteByHeight(fog, GreveChaseSprites.Environment(FogB),
                    new Vector3(0, -2.8f, 0), 4f, SceneBand.Fog, 0.5f);
                if (fogSr != null) fogSr.color = new Color(1f, 1f, 1f, 0.7f);
                var fogSr2 = PlaceSpriteByHeight(fog, GreveChaseSprites.Environment(FogA),
                    new Vector3(-3f, -3.2f, 0), 3f, SceneBand.Fog, 0.55f);
                if (fogSr2 != null) fogSr2.color = new Color(1f, 1f, 1f, 0.55f);
            }
            else
            {
                var fogSr = Quad(fog, PlaceholderArt.Glow(), new Color(0.5f, 0.5f, 0.65f, 0.18f),
                    new Vector3(0, -2.5f, 0), new Vector3(24f, 8f, 1f), SceneBand.Fog, 0.5f);
                fogSr.name = "FogSheet";
            }

            parallax.CollectLayers();
            return parallax;
        }

        private static ParallaxLayer NewLayer(Transform parent, string name, SceneBand band, float hFactor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0, LayerSorting.BandZ(band));
            var layer = go.AddComponent<ParallaxLayer>();
            layer.depth = (int)band;
            layer.horizontalFactor = hFactor;
            layer.verticalFactor = 0f;
            return layer;
        }

        /// <summary>
        /// Place a real sprite scaled so its rendered height equals
        /// <paramref name="worldHeight"/> units. Sprites are imported at 280 px
        /// per unit; this keeps every cut asset at a consistent on-screen size
        /// regardless of its source resolution.
        /// </summary>
        private static SpriteRenderer PlaceSpriteByHeight(Component parent, Sprite sprite,
            Vector3 localPos, float worldHeight, SceneBand band, float depth01)
        {
            if (sprite == null) return null;
            SpriteRenderer sr = PlaceholderArt.NewSpriteObject("env", sprite, Color.white,
                parent.transform, band, depth01);
            float spriteHeight = sprite.bounds.size.y; // world units at import PPU
            float scale = spriteHeight > 0.0001f ? worldHeight / spriteHeight : 1f;
            sr.transform.localPosition = localPos;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            return sr;
        }

        private static SpriteRenderer Quad(Component parent, Sprite sprite, Color color,
            Vector3 localPos, Vector3 localScale, SceneBand band, float depth01)
        {
            SpriteRenderer sr = PlaceholderArt.NewSpriteObject("piece", sprite, color,
                parent.transform, band, depth01);
            sr.transform.localPosition = localPos;
            sr.transform.localScale = localScale;
            return sr;
        }
    }
}
