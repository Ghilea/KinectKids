using UnityEngine;
using KinectKids.Scene25D;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// Builds the layered 2.5D chase environment as a set of REUSABLE MODULES
    /// (see src/problems/issues.md):
    ///
    ///   * BackgroundCorridor  — the deep hall the count comes from (env_19)
    ///   * FloorLane           — a wide, continuous run lane running from the
    ///                           background toward the camera (env_0 / env_1)
    ///   * WallModule          — left/right wall sections with a built-in torch
    ///                           (env_2 / env_3), placed with variation
    ///   * Pillar              — free-standing framing pillars (env_13)
    ///   * Torch               — occasional extra torch accent (env_10)
    ///   * FogLayer            — several thin functional fog bands, not one block
    ///
    /// Each module is its own method so the scene reads as a build pipeline and
    /// pieces can be reused / re-tuned independently. When a sprite is missing a
    /// layer falls back to a placeholder quad so the scene always builds.
    /// </summary>
    public static class ChaseEnvironmentBuilder
    {
        // Named environment cuts (see docs/asset-sources/environment_sheet.png).
        private const string Corridor = "env_19";     // torch-lit hall (deep background)
        private const string CastleRuins = "env_18";  // castle + moon (alt background)
        private const string FloorClean = "env_0";    // stone path (perspective tile)
        private const string FloorCracked = "env_1";  // cracked stone path (perspective tile)
        private const string WallLeft = "env_2";      // wall + banner + built-in torch
        private const string WallRight = "env_3";     // wall + built-in torch
        private const string Pillar = "env_13";       // stone pillar
        private const string Torch = "env_10";        // wall torch (accent only)
        private const string FogA = "env_16";         // thin fog bank
        private const string FogB = "env_17";         // thick fog bank

        // Sprites import at this PPU with a bottom-centre pivot, so a placed
        // sprite's localPos.y is its FLOOR line and it grows upward.
        // Camera: orthographic size 5 → visible y ≈ [-5, +5], x ≈ [-8.9, +8.9] @16:9.
        private const float FloorLine = -4.6f;   // where the lane meets the bottom
        private const float ScreenEdgeX = 8.6f;  // just inside the 16:9 ortho edge

        public static ParallaxController Build(Transform parent)
        {
            GameObject worldGo = new GameObject("ChaseWorld");
            worldGo.transform.SetParent(parent, false);
            var parallax = worldGo.AddComponent<ParallaxController>();

            bool art = GreveChaseSprites.Environment(Corridor) != null;

            BuildBackgroundCorridor(worldGo.transform, art);
            BuildFloorLane(worldGo.transform, art);
            BuildSideWalls(worldGo.transform, art);
            BuildFramingPillars(worldGo.transform, art);
            BuildFogLayers(worldGo.transform, art);

            parallax.CollectLayers();
            return parallax;
        }

        // -------------------------------------------------------------------
        // MODULE: deep background corridor (issue 7 — active depth layer).
        // -------------------------------------------------------------------
        private static void BuildBackgroundCorridor(Transform world, bool art)
        {
            var far = NewLayer(world, "FarBackground", SceneBand.FarBackground, 0.08f);
            if (art)
            {
                // Wide corridor filling the upper half; its vanishing point sits
                // where the lane and the count converge, reading as "deep hall".
                PlaceSpriteByHeight(far, GreveChaseSprites.Environment(Corridor),
                    new Vector3(0f, -0.4f, 0f), 12.5f, SceneBand.FarBackground, 0.2f);
                // A darker vignette band behind the lane grounds the foreground.
                Quad(far, PlaceholderArt.SolidBlock(), new Color(0.05f, 0.05f, 0.12f, 0.55f),
                    new Vector3(0f, -6f, 0.2f), new Vector3(40f, 8f, 1f),
                    SceneBand.FarBackground, 0.25f);
            }
            else
            {
                Quad(far, PlaceholderArt.SolidBlock(), PlaceholderArt.NightBlue,
                    new Vector3(0f, 2f, 0f), new Vector3(40f, 24f, 1f),
                    SceneBand.FarBackground, 0.1f);
            }
        }

        // -------------------------------------------------------------------
        // MODULE: the run lane (issues 1 & 2 — wide, continuous, readable path
        // leading from the background toward the camera).
        // -------------------------------------------------------------------
        private static void BuildFloorLane(Transform world, bool art)
        {
            var floor = NewLayer(world, "FloorLane", SceneBand.Floor, 0.5f);
            if (art)
            {
                // Overlapping perspective tiles: each nearer tile is bigger, sits
                // lower and slightly wider, so they merge into one continuous lane
                // instead of a thin floating strip. depth01 rises toward the camera.
                // (segment 0 = deepest/back, last = closest/front)
                var segments = new[]
                {
                    // height, y (floor line),  widthScale, tile
                    new LaneSeg(2.6f, -1.2f, 0.60f, FloorClean),
                    new LaneSeg(3.4f, -2.2f, 0.78f, FloorCracked),
                    new LaneSeg(4.3f, -3.4f, 1.00f, FloorClean),
                    new LaneSeg(5.2f, -4.9f, 1.25f, FloorCracked),
                };
                for (int i = 0; i < segments.Length; i++)
                {
                    float depth01 = 0.4f + i * 0.15f; // nearer draws in front
                    PlaceLaneSegment(floor, segments[i], depth01);
                }
            }
            else
            {
                // Trapezoid-ish placeholder lane (still wide and centred).
                Quad(floor, PlaceholderArt.SolidBlock(), new Color(0.18f, 0.15f, 0.24f),
                    new Vector3(0f, -2.6f, 0f), new Vector3(7f, 5f, 1f), SceneBand.Floor, 0.5f);
                Quad(floor, PlaceholderArt.SolidBlock(), new Color(0.12f, 0.10f, 0.18f),
                    new Vector3(0f, -4.4f, 0f), new Vector3(10f, 3f, 1f), SceneBand.Floor, 0.6f);
            }
        }

        private readonly struct LaneSeg
        {
            public readonly float height;
            public readonly float y;
            public readonly float widthScale;
            public readonly string tile;
            public LaneSeg(float height, float y, float widthScale, string tile)
            {
                this.height = height; this.y = y; this.widthScale = widthScale; this.tile = tile;
            }
        }

        private static void PlaceLaneSegment(Component parent, LaneSeg seg, float depth01)
        {
            Sprite sprite = GreveChaseSprites.Environment(seg.tile);
            SpriteRenderer sr = PlaceSpriteByHeight(parent, sprite,
                new Vector3(0f, seg.y, 0f), seg.height, SceneBand.Floor, depth01);
            if (sr != null)
            {
                // Stretch horizontally for the perspective widen toward camera.
                Vector3 s = sr.transform.localScale;
                s.x *= seg.widthScale * 1.6f;
                sr.transform.localScale = s;
            }
        }

        // -------------------------------------------------------------------
        // MODULE: side walls (issues 2 & 3 — modular, varied, low symmetry;
        // walls already contain their own torch so we don't stack extra ones).
        // -------------------------------------------------------------------
        private static void BuildSideWalls(Transform world, bool art)
        {
            var leftEnv = NewLayer(world, "LeftEnvironment", SceneBand.LeftEnvironment, 0.6f);
            var rightEnv = NewLayer(world, "RightEnvironment", SceneBand.RightEnvironment, 0.6f);

            if (art)
            {
                // Left side: two wall modules at different depths/heights.
                WallModule(leftEnv, WallLeft, SceneBand.LeftEnvironment,
                    x: -ScreenEdgeX, y: FloorLine, height: 10f, flip: false, depth01: 0.5f);
                WallModule(leftEnv, WallLeft, SceneBand.LeftEnvironment,
                    x: -ScreenEdgeX - 0.6f, y: FloorLine + 1.2f, height: 7.2f, flip: false, depth01: 0.4f);

                // Right side: deliberately NOT a mirror — different heights, one
                // module nudged inward, breaking the "stage set" symmetry.
                WallModule(rightEnv, WallRight, SceneBand.RightEnvironment,
                    x: ScreenEdgeX, y: FloorLine, height: 9.2f, flip: false, depth01: 0.5f);
                WallModule(rightEnv, WallRight, SceneBand.RightEnvironment,
                    x: ScreenEdgeX + 0.4f, y: FloorLine + 1.8f, height: 6.4f, flip: false, depth01: 0.4f);
            }
            else
            {
                Quad(leftEnv, PlaceholderArt.SolidBlock(), PlaceholderArt.DeepPurple,
                    new Vector3(-7.5f, 0f, 0), new Vector3(4f, 16f, 1f), SceneBand.LeftEnvironment, 0.5f);
                Quad(rightEnv, PlaceholderArt.SolidBlock(), PlaceholderArt.DeepPurple,
                    new Vector3(7.5f, 0f, 0), new Vector3(4f, 16f, 1f), SceneBand.RightEnvironment, 0.5f);
                // A couple of glow accents so placeholders still read as torch-lit.
                Quad(leftEnv, PlaceholderArt.Glow(), PlaceholderArt.LanternOrange,
                    new Vector3(-6f, 1.5f, -0.1f), new Vector3(1.4f, 1.4f, 1f), SceneBand.LeftEnvironment, 0.6f);
                Quad(rightEnv, PlaceholderArt.Glow(), PlaceholderArt.LanternOrange,
                    new Vector3(6f, 0.5f, -0.1f), new Vector3(1.4f, 1.4f, 1f), SceneBand.RightEnvironment, 0.6f);
            }
        }

        /// <summary>Reusable wall section (bottom-pivot, torch built into art).</summary>
        private static void WallModule(Component parent, string sprite, SceneBand band,
            float x, float y, float height, bool flip, float depth01)
        {
            SpriteRenderer sr = PlaceSpriteByHeight(parent, GreveChaseSprites.Environment(sprite),
                new Vector3(x, y, 0f), height, band, depth01);
            if (sr != null && flip)
            {
                Vector3 s = sr.transform.localScale;
                s.x = -s.x;
                sr.transform.localScale = s;
            }
        }

        // -------------------------------------------------------------------
        // MODULE: framing pillars + a single extra torch accent (issue 4 —
        // frame the scene, don't crowd the middle or mirror perfectly).
        // -------------------------------------------------------------------
        private static void BuildFramingPillars(Transform world, bool art)
        {
            var frame = NewLayer(world, "Foreground", SceneBand.Foreground, 0.75f);
            if (!art)
            {
                for (int i = -1; i <= 1; i += 2)
                    Quad(frame, PlaceholderArt.SolidBlock(), PlaceholderArt.Shadow,
                        new Vector3(i * 8.2f, -2f, 0f), new Vector3(1.4f, 8f, 1f),
                        SceneBand.Foreground, 0.7f);
                return;
            }

            // Asymmetric framing: one tall pillar close on the left, one shorter
            // pillar further back on the right. Kept near the edges so the run
            // lane in the middle stays clear.
            PlaceSpriteByHeight(frame, GreveChaseSprites.Environment(Pillar),
                new Vector3(-8.9f, FloorLine, 0f), 9.5f, SceneBand.Foreground, 0.85f);
            PlaceSpriteByHeight(frame, GreveChaseSprites.Environment(Pillar),
                new Vector3(9.1f, FloorLine + 0.8f, 0f), 7.5f, SceneBand.Foreground, 0.6f);

            // One lone extra torch accent, off-centre (not a mirrored pair).
            PlaceSpriteByHeight(frame, GreveChaseSprites.Environment(Torch),
                new Vector3(-6.9f, 1.4f, -0.1f), 1.7f, SceneBand.Foreground, 0.9f);
        }

        // -------------------------------------------------------------------
        // MODULE: fog as several thin functional bands (issue 5 — low floor fog
        // + thin background fog + slow drift, never one big central block).
        // -------------------------------------------------------------------
        private static void BuildFogLayers(Transform world, bool art)
        {
            // Low floor fog: a thin band hugging the bottom, drifting sideways.
            var lowFog = NewLayer(world, "FogLow", SceneBand.Fog, 0.9f);
            lowFog.driftPerSecond = new Vector2(0.12f, 0f);
            // Thin background fog: sits deep, very faint, slow opposite drift.
            var backFog = NewLayer(world, "FogBack", SceneBand.Background, 0.2f);
            backFog.driftPerSecond = new Vector2(-0.05f, 0f);

            if (art)
            {
                var low = PlaceSpriteByHeight(lowFog, GreveChaseSprites.Environment(FogA),
                    new Vector3(-1.5f, -4.8f, 0f), 2.4f, SceneBand.Fog, 0.5f);
                if (low != null)
                {
                    low.color = new Color(1f, 1f, 1f, 0.42f);
                    var s = low.transform.localScale; s.x *= 3.2f; low.transform.localScale = s;
                }
                var low2 = PlaceSpriteByHeight(lowFog, GreveChaseSprites.Environment(FogA),
                    new Vector3(2.6f, -5.0f, 0.05f), 2.0f, SceneBand.Fog, 0.55f);
                if (low2 != null)
                {
                    low2.color = new Color(1f, 1f, 1f, 0.35f);
                    var s = low2.transform.localScale; s.x *= 3.0f; low2.transform.localScale = s;
                }

                var back = PlaceSpriteByHeight(backFog, GreveChaseSprites.Environment(FogB),
                    new Vector3(0f, -1.5f, 0f), 4.5f, SceneBand.Background, 0.4f);
                if (back != null)
                {
                    back.color = new Color(0.8f, 0.85f, 1f, 0.22f);
                    var s = back.transform.localScale; s.x *= 2.4f; back.transform.localScale = s;
                }
            }
            else
            {
                var low = Quad(lowFog, PlaceholderArt.Glow(), new Color(0.55f, 0.55f, 0.7f, 0.18f),
                    new Vector3(0f, -4.4f, 0f), new Vector3(24f, 3f, 1f), SceneBand.Fog, 0.5f);
                low.name = "FogLowSheet";
                Quad(backFog, PlaceholderArt.Glow(), new Color(0.5f, 0.55f, 0.7f, 0.10f),
                    new Vector3(0f, -1f, 0f), new Vector3(28f, 6f, 1f), SceneBand.Background, 0.4f);
            }
        }

        // -------------------------------------------------------------------
        // Shared helpers.
        // -------------------------------------------------------------------
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
        /// <paramref name="worldHeight"/> units (sprites import bottom-centre, so
        /// localPos.y is the floor line). Keeps every cut asset a consistent size.
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
