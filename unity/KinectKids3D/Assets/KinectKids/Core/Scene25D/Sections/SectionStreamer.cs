using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Scene25D.Sections
{
    /// <summary>
    /// Streams <see cref="SectionDefinition"/> rooms past a locked 2.5D camera.
    ///
    /// Responsibilities (all environment, NO gameplay rules):
    ///   * Build a room's reusable modules (floor lane, receding walls, props,
    ///     overheads, framing, fog) in perspective via <see cref="PerspectiveModel"/>.
    ///   * Drive the shared treadmill by pushing forward speed into a
    ///     <see cref="ParallaxController"/>, so the world moves toward the player.
    ///   * Cross-fade from one room to the next when asked (a
    ///     <c>SongStructureDriver</c> decides WHEN; the streamer decides HOW).
    ///
    /// It owns two room "stages" (current + incoming) so a transition can overlap
    /// the two rooms and blend their tint/alpha, giving a smooth doorway-style
    /// change instead of a hard cut. Gameplay code only calls
    /// <see cref="EnterRoom"/> / reads <see cref="ActiveRoom"/>; hazards are
    /// spawned by the director using <see cref="ActiveDefinition"/>.
    /// </summary>
    public sealed class SectionStreamer : MonoBehaviour
    {
        /// <summary>One live room instance: a root object holding all its layers.</summary>
        private sealed class Stage
        {
            public SectionDefinition Definition;
            public GameObject Root;
            public readonly List<SpriteRenderer> Renderers = new List<SpriteRenderer>();
            public float Alpha = 1f;
        }

        [Tooltip("The treadmill that moves the world toward the camera.")]
        public ParallaxController parallax;

        [Tooltip("How fast the world streams toward the player, world units/sec. " +
                 "Gameplay drives this each frame (running faster = larger).")]
        public float forwardSpeed = 6f;

        [Tooltip("Smoothed lateral offset for player weaving, -1..1.")]
        public float lateralTarget = 0f;

        private ModuleLibrary library;
        private PerspectiveModel perspective = PerspectiveModel.Default;

        private Stage current;
        private Stage incoming;
        private float transitionT;       // 0..1 progress of an active transition
        private float transitionDuration; // seconds
        private bool transitioning;

        /// <summary>The room currently in control of gameplay (the one the player is in).</summary>
        public RoomKind ActiveRoom => current?.Definition?.room ?? RoomKind.Corridor;

        /// <summary>The definition currently in control (used by the director for hazards/theme).</summary>
        public SectionDefinition ActiveDefinition => current?.Definition;

        /// <summary>True while a room-to-room cross-fade is in progress.</summary>
        public bool IsTransitioning => transitioning;

        /// <summary>Wire up the streamer. Call once before entering the first room.</summary>
        public void Init(ParallaxController controller, ModuleLibrary moduleLibrary, PerspectiveModel model)
        {
            parallax = controller;
            library = moduleLibrary ?? new ModuleLibrary();
            perspective = model;
        }

        /// <summary>
        /// Enter a room. If one is already active, this begins a cross-fade over
        /// <see cref="SectionDefinition.transitionSeconds"/>; otherwise it snaps in.
        /// </summary>
        public void EnterRoom(SectionDefinition definition, PerspectiveModel? modelOverride = null)
        {
            if (definition == null) return;
            if (modelOverride.HasValue) perspective = modelOverride.Value;

            Stage stage = BuildStage(definition);

            if (current == null)
            {
                current = stage;
                current.Alpha = 1f;
                ApplyAlpha(current);
                return;
            }

            // A previous incoming that never finished is discarded to avoid stacking.
            if (incoming != null) DestroyStage(incoming);

            incoming = stage;
            incoming.Alpha = 0f;
            ApplyAlpha(incoming);
            transitioning = true;
            transitionT = 0f;
            transitionDuration = Mathf.Max(0.05f, definition.transitionSeconds);
        }

        private void Update()
        {
            if (parallax != null)
            {
                parallax.forwardSpeed = forwardSpeed;
                parallax.lateralTarget = lateralTarget;
            }

            if (!transitioning) return;

            transitionT += Time.deltaTime / transitionDuration;
            float e = Mathf.Clamp01(transitionT);
            float smooth = e * e * (3f - 2f * e);

            if (incoming != null)
            {
                incoming.Alpha = smooth;
                ApplyAlpha(incoming);
            }
            if (current != null)
            {
                current.Alpha = 1f - smooth;
                ApplyAlpha(current);
            }

            if (e >= 1f)
            {
                // Incoming becomes current; old room is torn down.
                if (current != null) DestroyStage(current);
                current = incoming;
                incoming = null;
                if (current != null) { current.Alpha = 1f; ApplyAlpha(current); }
                transitioning = false;
            }
        }

        // ---------------------------------------------------------------
        // Room construction: turn a definition into layered perspective art.
        // ---------------------------------------------------------------
        private Stage BuildStage(SectionDefinition def)
        {
            var stage = new Stage { Definition = def };
            var root = new GameObject("Room_" + def.displayName);
            root.transform.SetParent(transform, false);
            stage.Root = root;

            Scene25DBackdrop.Theme theme = Scene25DBackdrop.Theme.Tinted(def.signature);

            BuildFloorLane(stage, def, theme);
            BuildWalls(stage, def, theme);
            BuildProps(stage, def, theme);
            BuildEffects(stage, def, theme);

            // New layers must be picked up by the parallax treadmill.
            if (parallax != null) parallax.CollectLayers();
            return stage;
        }

        private void BuildFloorLane(Stage stage, SectionDefinition def, in Scene25DBackdrop.Theme theme)
        {
            if (def.floorTiles.Count == 0) return;
            ParallaxLayer floor = NewLayer(stage.Root.transform, "FloorLane", SceneBand.Floor, 0.5f);
            floor.forwardFactor = 0.5f;
            floor.loopHeight = 1.2f;

            const int tileCount = 9;
            for (int i = tileCount - 1; i >= 0; i--)
            {
                float t = (float)i / (tileCount - 1);
                EnvironmentModule tile = def.floorTiles[i % def.floorTiles.Count];
                Sprite sprite = library.Resolve(tile);
                float depth01 = 0.6f - t * 0.5f;
                SpriteRenderer sr = Spawn(stage, floor.transform, "floorTile", sprite,
                    ResolveTint(tile, theme), SceneBand.Floor, depth01);

                float spriteW = SafeWidth(sprite);
                float targetHalfWidth = perspective.HalfWidth(t);
                float scaleX = spriteW > 0.0001f ? (targetHalfWidth * 2f) / spriteW : perspective.Scale(t);
                float scaleY = scaleX * Mathf.Lerp(1.0f, 0.7f, t);
                sr.transform.localPosition = new Vector3(0f, perspective.Y(t), 0f);
                sr.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                sr.color = perspective.Shade(sr.color, t, 0.72f);
            }
        }

        private void BuildWalls(Stage stage, SectionDefinition def, in Scene25DBackdrop.Theme theme)
        {
            ParallaxLayer leftLayer = NewLayer(stage.Root.transform, "LeftWalls", SceneBand.LeftEnvironment, 0.5f);
            ParallaxLayer rightLayer = NewLayer(stage.Root.transform, "RightWalls", SceneBand.RightEnvironment, 0.5f);

            float[] depths = { 0.05f, 0.30f, 0.52f, 0.70f, 0.84f };
            for (int i = 0; i < depths.Length; i++)
            {
                float t = depths[i];
                float wallX = perspective.HalfWidth(t) + 1.1f * perspective.Scale(t) + 0.6f;
                float wallHeight = Mathf.Lerp(11f, 2.2f, PerspectiveModel.Curve(t));
                float depth01 = 0.7f - t * 0.5f;

                if (def.leftWalls.Count > 0)
                {
                    EnvironmentModule m = def.leftWalls[i % def.leftWalls.Count];
                    PlaceByHeight(stage, leftLayer.transform, "wallL", library.Resolve(m),
                        new Vector3(-wallX, perspective.Y(t), 0f),
                        wallHeight * (m.worldHeight > 0 ? m.worldHeight / 11f : 1f),
                        SceneBand.LeftEnvironment, depth01, perspective.Shade(ResolveTint(m, theme), t));
                }
                if (def.rightWalls.Count > 0)
                {
                    EnvironmentModule m = def.rightWalls[i % def.rightWalls.Count];
                    PlaceByHeight(stage, rightLayer.transform, "wallR", library.Resolve(m),
                        new Vector3(wallX, perspective.Y(t), 0f),
                        wallHeight * (m.worldHeight > 0 ? m.worldHeight / 11f : 1f),
                        SceneBand.RightEnvironment, depth01, perspective.Shade(ResolveTint(m, theme), t));
                }
            }
        }

        private void BuildProps(Stage stage, SectionDefinition def, in Scene25DBackdrop.Theme theme)
        {
            if (def.props.Count == 0) return;
            ParallaxLayer decor = NewLayer(stage.Root.transform, "Props", SceneBand.Background, 0.18f);
            decor.forwardFactor = 0.3f;

            foreach (PropPlacement p in def.props)
            {
                float t = p.depth01;
                EnvironmentModule m = p.module;
                SceneBand band = m.band;
                float side = m.side == ModuleSide.Right ? 1f : (m.side == ModuleSide.Left ? -1f : 0f);
                float x = side * perspective.HalfWidth(t) * 1.35f + m.lateralOffset;
                float y = m.kind == ModuleKind.Overhead
                    ? Mathf.Lerp(3.6f, perspective.horizon + 0.4f, PerspectiveModel.Curve(t))
                    : perspective.Y(t);
                float height = (m.worldHeight > 0 ? m.worldHeight : 3f) * perspective.Scale(t);
                PlaceByHeight(stage, decor.transform, "prop", library.Resolve(m),
                    new Vector3(x, y, 0.05f), height, band, 0.5f,
                    perspective.Shade(ResolveTint(m, theme), t, 0.7f));
            }
        }

        private void BuildEffects(Stage stage, SectionDefinition def, in Scene25DBackdrop.Theme theme)
        {
            if (def.effects.Count == 0) return;
            ParallaxLayer fx = NewLayer(stage.Root.transform, "Fx", SceneBand.Fog, 0.9f);
            fx.driftPerSecond = new Vector2(0.12f, 0f);

            for (int i = 0; i < def.effects.Count; i++)
            {
                EnvironmentModule m = def.effects[i];
                float x = (i % 2 == 0) ? -1.5f : 2.6f;
                float y = -4.9f + i * 0.15f;
                SpriteRenderer sr = Spawn(stage, fx.transform, "fx", library.Resolve(m),
                    ResolveTint(m, theme), m.band == SceneBand.Actors ? SceneBand.Fog : m.band, 0.5f);
                float h = m.worldHeight > 0 ? m.worldHeight : 2.2f;
                float scale = SafeHeight(sr.sprite) > 0.0001f ? h / SafeHeight(sr.sprite) : h;
                sr.transform.localPosition = new Vector3(x + m.lateralOffset, y, 0f);
                sr.transform.localScale = new Vector3(scale * 3.2f, scale, 1f);
            }
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private ParallaxLayer NewLayer(Transform parent, string name, SceneBand band, float hFactor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0, LayerSorting.BandZ(band));
            var layer = go.AddComponent<ParallaxLayer>();
            layer.depth = (int)band;
            layer.horizontalFactor = hFactor;
            layer.verticalFactor = 0f;
            return layer;
        }

        private SpriteRenderer Spawn(Stage stage, Transform parent, string name, Sprite sprite,
            Color color, SceneBand band, float depth01)
        {
            SpriteRenderer sr = PlaceholderArt.NewSpriteObject(name, sprite, color, parent, band, depth01);
            stage.Renderers.Add(sr);
            return sr;
        }

        private void PlaceByHeight(Stage stage, Transform parent, string name, Sprite sprite,
            Vector3 localPos, float worldHeight, SceneBand band, float depth01, Color color)
        {
            if (sprite == null) return;
            SpriteRenderer sr = Spawn(stage, parent, name, sprite, color, band, depth01);
            float spriteHeight = SafeHeight(sprite);
            float scale = spriteHeight > 0.0001f ? worldHeight / spriteHeight : 1f;
            sr.transform.localPosition = localPos;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static float SafeWidth(Sprite s) => s != null ? s.bounds.size.x : 1f;
        private static float SafeHeight(Sprite s) => s != null ? s.bounds.size.y : 1f;

        /// <summary>A module's own tint if set, else a themed placeholder tint for its kind.</summary>
        private static Color ResolveTint(in EnvironmentModule m, in Scene25DBackdrop.Theme theme)
        {
            bool isWhite = m.tint.r >= 0.999f && m.tint.g >= 0.999f && m.tint.b >= 0.999f && m.tint.a >= 0.999f;
            return isWhite ? ModuleLibrary.PlaceholderTint(m.kind, theme) : m.tint;
        }

        private void ApplyAlpha(Stage stage)
        {
            if (stage == null) return;
            for (int i = 0; i < stage.Renderers.Count; i++)
            {
                SpriteRenderer sr = stage.Renderers[i];
                if (sr == null) continue;
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, stage.Alpha);
            }
        }

        private void DestroyStage(Stage stage)
        {
            if (stage?.Root != null) Object.Destroy(stage.Root);
        }
    }
}
