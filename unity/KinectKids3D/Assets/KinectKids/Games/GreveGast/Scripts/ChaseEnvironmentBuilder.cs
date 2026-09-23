using UnityEngine;
using KinectKids.Scene25D;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// Builds the layered 2.5D chase environment as a set of REUSABLE MODULES,
    /// laid out as a TRUE PERSPECTIVE CORRIDOR so the scene reads with depth
    /// (a run lane receding to a vanishing point, framed by receding walls and
    /// pillars) instead of a flat stack of same-size images.
    ///
    ///   * BackgroundCorridor  — the deep hall the count comes from (env_19),
    ///                           sized as a SMALL far element around the
    ///                           vanishing point, never a full-screen picture.
    ///   * FloorLane           — a perspective run lane: tiles get shorter and
    ///                           narrower as they recede toward the vanishing
    ///                           point (env_0 / env_1).
    ///   * SideWalls           — left/right wall sections that recede toward the
    ///                           vanishing point (env_2 / env_3), plus banners
    ///                           and statues that shrink with distance.
    ///   * Pillars             — free-standing framing pillars near the camera
    ///                           (env_13), large in front, small further back.
    ///   * FogLayers           — thin functional fog bands, not one block.
    ///
    /// PERSPECTIVE MODEL (orthographic camera, so we fake depth by geometry):
    /// a normalised depth t in [0..1] where 0 = at the camera (near, bottom of
    /// screen, full size) and 1 = the vanishing point (far, up near the horizon,
    /// tiny). <see cref="PerspectiveY"/> / <see cref="PerspectiveScale"/> /
    /// <see cref="PerspectiveHalfWidth"/> map that depth to screen geometry so
    /// every module recedes to the SAME point and the corridor holds together.
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
        private const string Window = "env_7";        // window with moon + castle (bg accent)
        private const string Banner = "env_11";       // hanging banner / flag
        private const string Statue = "env_12";       // knight statue with axe
        private const string Rocks = "env_9";         // rubble / stone pile (foreground)
        private const string Portrait = "env_8";      // framed Greve Gast portrait
        private const string Arch = "env_4";          // stone archway (deep bg portal)
        private const string DoorSprite = "env_5";    // haunted door (deep bg portal)

        // Camera: orthographic size 5 → visible y ≈ [-5, +5], x ≈ [-8.9, +8.9] @16:9.
        // The corridor recedes to a vanishing point set slightly above centre.
        private const float FloorLine = -4.7f;    // bottom of screen: lane meets camera
        private const float Horizon = 1.6f;       // y of the vanishing point
        private const float NearHalfWidth = 4.2f; // half-width of the lane at the camera
        private const float FarHalfWidth = 0.55f; // half-width of the lane at the horizon
        private const float NearScale = 1.0f;      // tile scale at the camera
        private const float FarScale = 0.14f;      // tile scale at the horizon
        private const float ScreenEdgeX = 8.6f;    // just inside the 16:9 ortho edge

        // ------------------------------------------------------------------
        // Perspective mapping helpers: depth t in [0..1], 0 = near, 1 = far.
        // A curved falloff makes near tiles grow faster than linear, which is
        // what actually sells the depth (matches CameraDepthScaler's curve).
        // ------------------------------------------------------------------
        private static float PerspectiveCurve(float t)
        {
            t = Mathf.Clamp01(t);
            // Ease so distance compresses toward the horizon (real perspective).
            return t * t * (3f - 2f * t);
        }

        private static float PerspectiveY(float t)
        {
            return Mathf.Lerp(FloorLine, Horizon, PerspectiveCurve(t));
        }

        private static float PerspectiveScale(float t)
        {
            return Mathf.Lerp(NearScale, FarScale, PerspectiveCurve(t));
        }

        private static float PerspectiveHalfWidth(float t)
        {
            return Mathf.Lerp(NearHalfWidth, FarHalfWidth, PerspectiveCurve(t));
        }

        public static ParallaxController Build(Transform parent)
        {
            GameObject worldGo = new GameObject("ChaseWorld");
            worldGo.transform.SetParent(parent, false);
            var parallax = worldGo.AddComponent<ParallaxController>();

            bool art = GreveChaseSprites.Environment(Corridor) != null;

            BuildBackgroundCorridor(worldGo.transform, art);
            BuildBackgroundDecor(worldGo.transform, art);
            BuildFloorLane(worldGo.transform, art);
            BuildSideWalls(worldGo.transform, art);
            BuildFramingPillars(worldGo.transform, art);
            BuildFogLayers(worldGo.transform, art);

            parallax.CollectLayers();
            return parallax;
        }

        // -------------------------------------------------------------------
        // MODULE: deep background corridor. Sized SMALL and centred on the
        // vanishing point so it reads as "the hall far away" — the lane and the
        // walls all converge onto it. This is the single change that stops the
        // scene looking like one flat full-screen picture.
        // -------------------------------------------------------------------
        private static void BuildBackgroundCorridor(Transform world, bool art)
        {
            var far = NewLayer(world, "FarBackground", SceneBand.FarBackground, 0.06f);
            if (art)
            {
                // A dark night wash fills the whole frame behind everything so
                // there are no empty white gaps around the receding corridor.
                Quad(far, PlaceholderArt.SolidBlock(), new Color(0.06f, 0.06f, 0.13f, 1f),
                    new Vector3(0f, 0f, 0.3f), new Vector3(60f, 40f, 1f),
                    SceneBand.FarBackground, 0.02f);

                // The corridor cut is the vanishing point itself: a modest patch
                // sitting AT the horizon that the whole lane converges onto.
                var corr = PlaceSpriteByHeight(far, GreveChaseSprites.Environment(Corridor),
                    new Vector3(0f, Horizon - 1.7f, 0.2f), 3.4f, SceneBand.FarBackground, 0.15f);
                if (corr != null) corr.color = new Color(0.85f, 0.88f, 1f, 1f);

                // A moonlit window (env_7) set high into the back wall, off to one
                // side, so the moon/castle reads deep behind the hall.
                PlaceSpriteByHeight(far, GreveChaseSprites.Environment(Window),
                    new Vector3(-4.6f, 1.9f, 0.1f), 3.0f, SceneBand.FarBackground, 0.2f);
            }
            else
            {
                Quad(far, PlaceholderArt.SolidBlock(), PlaceholderArt.NightBlue,
                    new Vector3(0f, 2f, 0f), new Vector3(40f, 24f, 1f),
                    SceneBand.FarBackground, 0.1f);
            }
        }

        // -------------------------------------------------------------------
        // MODULE: recurring background decor (arches, doors, portraits) that
        // recede along the corridor toward the vanishing point and STREAM toward
        // the camera as the player runs. Each piece is placed at a depth t and
        // sized by the perspective curve so it shrinks correctly with distance.
        // -------------------------------------------------------------------
        private static void BuildBackgroundDecor(Transform world, bool art)
        {
            if (!art) return;

            var decor = NewLayer(world, "BackgroundDecor", SceneBand.Background, 0.18f);

            // Depths along the corridor (near→far). Alternate type + side so no
            // two neighbours match and it never looks mirrored.
            var pieces = new[] { Arch, Portrait, DoorSprite, Portrait, Arch };
            float[] depths = { 0.42f, 0.55f, 0.66f, 0.78f, 0.88f };
            for (int i = 0; i < pieces.Length; i++)
            {
                float t = depths[i];
                float side = (i % 2 == 0) ? -1f : 1f;
                // Sit just inside the wall line at this depth.
                float x = side * PerspectiveHalfWidth(t) * 1.35f;
                float baseHeight = 4.2f;
                var sr = PlaceSpriteByHeight(decor, GreveChaseSprites.Environment(pieces[i]),
                    new Vector3(x, PerspectiveY(t), 0.05f),
                    baseHeight * PerspectiveScale(t), SceneBand.Background, 0.2f - i * 0.02f);
                // Sink deeper pieces into the cool background haze.
                if (sr != null) sr.color = new Color(0.78f, 0.82f, 0.98f, 0.92f);
            }

            // Slow forward stream so the decor drifts toward the camera; loops on
            // the piece spacing so it repeats. Slower than the floor so it reads
            // as "further away" (parallax by speed).
            decor.forwardFactor = 0.3f;
            decor.loopHeight = 3.0f;
        }

        // -------------------------------------------------------------------
        // MODULE: the run lane, built as a TRUE PERSPECTIVE FLOOR. Tiles are
        // placed from the camera (t=0, big + wide) up to the vanishing point
        // (t=1, tiny + narrow). Nearer tiles overlap farther ones, so the eye
        // reads a continuous stone path receding into the hall — the core of the
        // "how it should look" reference.
        // -------------------------------------------------------------------
        private static void BuildFloorLane(Transform world, bool art)
        {
            var floor = NewLayer(world, "FloorLane", SceneBand.Floor, 0.5f);
            if (art)
            {
                const int tileCount = 9; // enough steps to reach the horizon smoothly
                // Draw far→near so nearer (larger) tiles paint OVER farther ones.
                for (int i = tileCount - 1; i >= 0; i--)
                {
                    float t = (float)i / (tileCount - 1); // 0 near … 1 far
                    string tileName = (i % 2 == 0) ? FloorClean : FloorCracked;
                    Sprite tile = GreveChaseSprites.Environment(tileName);
                    if (tile == null) continue;

                    // depth01 for sorting: near tiles in front within the band.
                    float depth01 = 0.6f - t * 0.5f;
                    SpriteRenderer sr = PlaceholderArt.NewSpriteObject("floorTile", tile,
                        Color.white, floor.transform, SceneBand.Floor, depth01);

                    // Scale the tile so its lane width matches the corridor width
                    // at this depth (x), keeping its own aspect for the length (y).
                    float spriteW = tile.bounds.size.x;
                    float spriteH = tile.bounds.size.y;
                    float targetHalfWidth = PerspectiveHalfWidth(t);
                    float scaleX = spriteW > 0.0001f ? (targetHalfWidth * 2f) / spriteW : 1f;
                    // Length scales with the general perspective so far tiles are
                    // short slivers near the horizon.
                    float scaleY = scaleX * Mathf.Lerp(1.0f, 0.7f, t);

                    sr.transform.localPosition = new Vector3(0f, PerspectiveY(t), 0f);
                    sr.transform.localScale = new Vector3(scaleX, scaleY, 1f);

                    // Darken tiles slightly with distance so depth reads even more.
                    float shade = Mathf.Lerp(1f, 0.72f, t);
                    sr.color = new Color(shade, shade, Mathf.Lerp(1f, 0.85f, t), 1f);
                }

                // Treadmill: scroll the whole lane toward the camera as the player
                // runs. Kept subtle because the perspective already sells motion.
                floor.forwardFactor = 0.5f;
                floor.loopHeight = 1.2f;
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

        // -------------------------------------------------------------------
        // MODULE: side walls that RECEDE toward the vanishing point. Instead of
        // one tall flat module at each screen edge, several wall sections are
        // placed at increasing depth, each shrunk + moved inward by the same
        // perspective curve as the floor, so the corridor walls converge on the
        // hall exactly where the lane does.
        // -------------------------------------------------------------------
        private static void BuildSideWalls(Transform world, bool art)
        {
            var leftEnv = NewLayer(world, "LeftEnvironment", SceneBand.LeftEnvironment, 0.5f);
            var rightEnv = NewLayer(world, "RightEnvironment", SceneBand.RightEnvironment, 0.5f);

            if (art)
            {
                // Wall sections at increasing depth (near→far). env_2 is authored
                // as the LEFT wall (torch faces inward), env_3 as the RIGHT wall.
                float[] depths = { 0.05f, 0.30f, 0.52f, 0.70f, 0.84f };
                for (int i = 0; i < depths.Length; i++)
                {
                    float t = depths[i];
                    // Wall stands just OUTSIDE the lane edge at this depth.
                    float wallX = PerspectiveHalfWidth(t) + 1.1f * PerspectiveScale(t) + 0.6f;
                    float wallHeight = Mathf.Lerp(11f, 2.2f, PerspectiveCurve(t));
                    float depth01 = 0.7f - t * 0.5f;

                    WallSection(leftEnv, WallLeft, SceneBand.LeftEnvironment,
                        -wallX, PerspectiveY(t), wallHeight, depth01, t);
                    WallSection(rightEnv, WallRight, SceneBand.RightEnvironment,
                        wallX, PerspectiveY(t), wallHeight, depth01, t);
                }

                // A hanging banner high on the near-left wall + a knight statue
                // on the floor in front of it (reference layer 3 / 8). Deliberately
                // NOT mirrored — different depths/heights to avoid stage symmetry.
                PlaceSpriteByHeight(leftEnv, GreveChaseSprites.Environment(Banner),
                    new Vector3(-5.9f, 1.6f, -0.05f), 3.0f, SceneBand.LeftEnvironment, 0.75f);
                PlaceSpriteByHeight(leftEnv, GreveChaseSprites.Environment(Statue),
                    new Vector3(-5.4f, FloorLine + 0.2f, -0.1f), 4.0f, SceneBand.LeftEnvironment, 0.8f);

                PlaceSpriteByHeight(rightEnv, GreveChaseSprites.Environment(Banner),
                    new Vector3(6.2f, 1.0f, -0.05f), 2.6f, SceneBand.RightEnvironment, 0.75f);
                PlaceSpriteByHeight(rightEnv, GreveChaseSprites.Environment(Statue),
                    new Vector3(5.7f, FloorLine + 0.5f, -0.1f), 3.5f, SceneBand.RightEnvironment, 0.8f);
            }
            else
            {
                Quad(leftEnv, PlaceholderArt.SolidBlock(), PlaceholderArt.DeepPurple,
                    new Vector3(-7.5f, 0f, 0), new Vector3(4f, 16f, 1f), SceneBand.LeftEnvironment, 0.5f);
                Quad(rightEnv, PlaceholderArt.SolidBlock(), PlaceholderArt.DeepPurple,
                    new Vector3(7.5f, 0f, 0), new Vector3(4f, 16f, 1f), SceneBand.RightEnvironment, 0.5f);
                Quad(leftEnv, PlaceholderArt.Glow(), PlaceholderArt.LanternOrange,
                    new Vector3(-6f, 1.5f, -0.1f), new Vector3(1.4f, 1.4f, 1f), SceneBand.LeftEnvironment, 0.6f);
                Quad(rightEnv, PlaceholderArt.Glow(), PlaceholderArt.LanternOrange,
                    new Vector3(6f, 0.5f, -0.1f), new Vector3(1.4f, 1.4f, 1f), SceneBand.RightEnvironment, 0.6f);
            }
        }

        /// <summary>
        /// Reusable receding wall section (bottom-pivot; torch built into art).
        /// Faces inward toward the lane and is shaded darker with distance.
        /// </summary>
        private static void WallSection(Component parent, string sprite, SceneBand band,
            float x, float y, float height, float depth01, float t)
        {
            SpriteRenderer sr = PlaceSpriteByHeight(parent, GreveChaseSprites.Environment(sprite),
                new Vector3(x, y, 0f), height, band, depth01);
            if (sr == null) return;
            // Right wall art faces left already; left wall faces right already.
            // Shade with distance so the far end sinks into the hall.
            float shade = Mathf.Lerp(1f, 0.6f, t);
            sr.color = new Color(shade, shade, Mathf.Lerp(1f, 0.8f, t), 1f);
        }

        // -------------------------------------------------------------------
        // MODULE: framing pillars near the camera + rubble at their base. Big in
        // front, framing the mouth of the corridor without crowding the lane.
        // -------------------------------------------------------------------
        private static void BuildFramingPillars(Transform world, bool art)
        {
            var frame = NewLayer(world, "Foreground", SceneBand.Foreground, 0.85f);
            if (!art)
            {
                for (int i = -1; i <= 1; i += 2)
                    Quad(frame, PlaceholderArt.SolidBlock(), PlaceholderArt.Shadow,
                        new Vector3(i * 8.2f, -2f, 0f), new Vector3(1.4f, 8f, 1f),
                        SceneBand.Foreground, 0.7f);
                return;
            }

            // Asymmetric framing: one tall pillar close on the left, one shorter
            // pillar a touch further back on the right. Right at the screen edges
            // so the receding lane in the middle stays clear.
            PlaceSpriteByHeight(frame, GreveChaseSprites.Environment(Pillar),
                new Vector3(-8.9f, FloorLine, 0f), 10.5f, SceneBand.Foreground, 0.9f);
            PlaceSpriteByHeight(frame, GreveChaseSprites.Environment(Pillar),
                new Vector3(9.1f, FloorLine + 0.6f, 0f), 8.5f, SceneBand.Foreground, 0.8f);

            // Rubble / stone piles at the base of the pillars, framing the
            // foreground without crowding the run lane.
            PlaceSpriteByHeight(frame, GreveChaseSprites.Environment(Rocks),
                new Vector3(-7.2f, FloorLine, -0.1f), 2.2f, SceneBand.Foreground, 0.95f);
            PlaceSpriteByHeight(frame, GreveChaseSprites.Environment(Rocks),
                new Vector3(7.6f, FloorLine, -0.1f), 1.7f, SceneBand.Foreground, 0.75f);
        }

        // -------------------------------------------------------------------
        // MODULE: fog as several thin functional bands (low floor fog + thin
        // background fog at the horizon + slow drift), never one big block. The
        // horizon fog also softens the vanishing point so the corridor "breathes".
        // -------------------------------------------------------------------
        private static void BuildFogLayers(Transform world, bool art)
        {
            var lowFog = NewLayer(world, "FogLow", SceneBand.Fog, 0.9f);
            lowFog.driftPerSecond = new Vector2(0.12f, 0f);
            var horizonFog = NewLayer(world, "FogHorizon", SceneBand.Background, 0.12f);
            horizonFog.driftPerSecond = new Vector2(-0.04f, 0f);

            if (art)
            {
                // Low floor fog hugging the bottom, drifting sideways.
                var low = PlaceSpriteByHeight(lowFog, GreveChaseSprites.Environment(FogA),
                    new Vector3(-1.5f, -4.9f, 0f), 2.2f, SceneBand.Fog, 0.5f);
                if (low != null)
                {
                    low.color = new Color(1f, 1f, 1f, 0.4f);
                    var s = low.transform.localScale; s.x *= 3.4f; low.transform.localScale = s;
                }
                var low2 = PlaceSpriteByHeight(lowFog, GreveChaseSprites.Environment(FogA),
                    new Vector3(2.6f, -5.1f, 0.05f), 1.9f, SceneBand.Fog, 0.55f);
                if (low2 != null)
                {
                    low2.color = new Color(1f, 1f, 1f, 0.33f);
                    var s = low2.transform.localScale; s.x *= 3.1f; low2.transform.localScale = s;
                }

                // Soft fog pooled AT the vanishing point so the hall glows/recedes.
                var horizon = PlaceSpriteByHeight(horizonFog, GreveChaseSprites.Environment(FogB),
                    new Vector3(0f, Horizon - 0.9f, 0f), 2.6f, SceneBand.Background, 0.25f);
                if (horizon != null)
                {
                    horizon.color = new Color(0.82f, 0.87f, 1f, 0.32f);
                    var s = horizon.transform.localScale; s.x *= 2.2f; horizon.transform.localScale = s;
                }
            }
            else
            {
                var low = Quad(lowFog, PlaceholderArt.Glow(), new Color(0.55f, 0.55f, 0.7f, 0.18f),
                    new Vector3(0f, -4.4f, 0f), new Vector3(24f, 3f, 1f), SceneBand.Fog, 0.5f);
                low.name = "FogLowSheet";
                Quad(horizonFog, PlaceholderArt.Glow(), new Color(0.5f, 0.55f, 0.7f, 0.12f),
                    new Vector3(0f, Horizon - 0.5f, 0f), new Vector3(10f, 4f, 1f), SceneBand.Background, 0.4f);
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
