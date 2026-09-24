using System.Collections.Generic;
using UnityEngine;
using KinectKids.Scene25D;
using KinectKids.Scene25D.Sections;
using KinectKids3D;
using KinectKids3D.Platform;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// A ground-up rebuild of the Greve Gast chase view as a true 2.5D
    /// endless-runner corridor, following <c>src/problems/follow.md</c>.
    ///
    /// WHY A REWRITE: the old <see cref="SectionStreamer"/> laid a handful of
    /// static floor tiles ONCE and relied on a <see cref="ParallaxLayer"/> that
    /// loops the whole layer's position. The world therefore read as a static,
    /// narrow ramp climbing up the screen (follow.md's "lång smal ramp") instead
    /// of a corridor rushing past the camera.
    ///
    /// THE FIX (the core idea in follow.md): the ENVIRONMENT moves, not the
    /// player. Every visible piece — floor rows, left/right wall posts, framing
    /// pillars, side props (torches, banners, barrels) — is a "segment" that
    /// carries a normalised depth d in [0..1] (1 = far at the vanishing point,
    /// 0 = right at the camera). Each frame we advance d toward 0 by the world
    /// speed, then re-place and re-scale the segment through the shared
    /// <see cref="PerspectiveModel"/>. When a segment passes the camera it wraps
    /// back to the far plane and is reused. Nothing is static: remove the player
    /// and Greve Gast and the corridor alone still reads as fast forward travel
    /// (follow.md's acceptance question).
    ///
    /// The player runs TOWARD the camera (front-facing run loop) in the CENTER of
    /// three lanes and side-steps to LEFT/RIGHT; Greve Gast floats further back
    /// and grows/approaches as the chase meter rises; hazards spawn far and scale
    /// up with the perspective as they approach. A room "palette" (CastleCorridor
    /// or Kitchen) is just a set of sprite keys + tints, so the SAME streaming
    /// system swaps assets mid-run without a reload.
    /// </summary>
    public sealed class CorridorRunnerDirector : MonoBehaviour, IDodgeSource
    {
        // ---- Tuning ------------------------------------------------------
        [Header("World motion")]
        [Tooltip("Base corridor travel speed (normalised depth units per second at full run).")]
        public float baseWorldSpeed = 0.55f;
        [Tooltip("Idle drift speed when the player is not actively running.")]
        public float idleWorldSpeed = 0.32f;

        [Header("Chase")]
        public float catchOnHit = 0.14f;
        public float recoverPerSecond = 0.05f;
        public float runDrainPerSecond = 0.05f;

        [Header("Lanes")]
        [Tooltip("Half the near-plane lane spacing (world units the player shifts to reach LEFT/RIGHT).")]
        public float laneNearOffset = 2.4f;

        // ---- Perspective / corridor -------------------------------------
        private PerspectiveModel model = PerspectiveModel.Default;

        // The near/far Z the whole corridor lives in (kept in front of the
        // orthographic camera at z = -15). Larger index bands sit further back.
        private const float CameraZ = -15f;

        // Number of streamed pieces per rail. More = denser corridor.
        private const int FloorRows = 14;
        private const int WallPosts = 9;
        private const int SideProps = 7;

        // ---- Runtime ----------------------------------------------------
        private Camera cam;
        private ScriptedCamera scriptedCamera;
        private Transform worldRoot;
        private Transform backdropRoot;

        private readonly List<Segment> floor = new List<Segment>();
        private readonly List<Segment> leftWall = new List<Segment>();
        private readonly List<Segment> rightWall = new List<Segment>();
        private readonly List<Segment> leftProps = new List<Segment>();
        private readonly List<Segment> rightProps = new List<Segment>();
        private readonly List<RunHazard> hazards = new List<RunHazard>();

        private PuppetRig playerRig;
        private PuppetRig grevePuppet;
        private Transform playerTransform;
        private Transform greveTransform;

        private GreveGastBodyInput body;
        private AudioSource music;

        private RoomPalette palette;
        private RoomPalette targetPalette;   // during an asset swap
        private float paletteBlend = 1f;      // 1 = fully in `palette`

        private float chase = 0.35f;
        private float lateral;                // smoothed -1..1
        private int laneIndex = 1;            // 0=left,1=center,2=right
        private float worldSpeed;
        private float hazardTimer = 2.2f;
        private int hazardCounter;
        private DodgeAction currentDodge = DodgeAction.None;

        private float elapsed;
        private bool swapTriggered;

        // IDodgeSource ----------------------------------------------------
        public DodgeAction CurrentDodge => currentDodge;
        public float LateralPosition => Mathf.Clamp(lateral, -1f, 1f);

        // =================================================================
        private void Start()
        {
            EnsureInputManager();
            body = new GreveGastBodyInput();
            body.Start();

            palette = RoomPalette.CastleCorridor();
            targetPalette = palette;

            BuildCamera();
            BuildBackdrop();
            BuildCorridor();
            BuildCharacters();
            StartMusic();

            worldSpeed = idleWorldSpeed;
        }

        // ---- Camera: chest height, looking down the corridor ------------
        private void BuildCamera()
        {
            var camGo = new GameObject("CorridorCamera");
            camGo.transform.SetParent(transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;               // visible y in [-5, +5]
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PlaceholderArt.Shadow;
            cam.transform.position = new Vector3(0f, 0f, CameraZ);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            scriptedCamera = camGo.AddComponent<ScriptedCamera>();
            scriptedCamera.shots.Clear();
            scriptedCamera.shots.Add(new ScriptedCamera.Shot
            {
                name = "Run",
                position = new Vector3(0f, 0f, CameraZ),
                eulerAngles = Vector3.zero,
                orthographicSize = 5f,
                blendSeconds = 0.6f,
            });
        }

        // ---- Deep background: a still corridor wash for depth only ------
        private void BuildBackdrop()
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(transform, false);
            backdropRoot = go.transform;

            // Far vault / moon wash: a large soft glow sunk at the vanishing
            // point so the mouth of the corridor is never empty black.
            SpriteRenderer vault = PlaceholderArt.NewSpriteObject("FarVault",
                PlaceholderArt.Glow(), new Color(0.30f, 0.32f, 0.55f, 0.85f),
                backdropRoot, SceneBand.FarBackground, 0f);
            vault.transform.localPosition = new Vector3(0f, model.horizon + 0.4f, LayerSorting.BandZ(SceneBand.FarBackground));
            vault.transform.localScale = new Vector3(6f, 4.5f, 1f);

            // A dim back wall panel filling behind the vanishing point.
            SpriteRenderer backWall = PlaceholderArt.NewSpriteObject("BackWall",
                PlaceholderArt.SolidBlock(), new Color(0.10f, 0.10f, 0.20f, 1f),
                backdropRoot, SceneBand.Sky, 0f);
            backWall.transform.localPosition = new Vector3(0f, 2.2f, LayerSorting.BandZ(SceneBand.Sky));
            backWall.transform.localScale = new Vector3(40f, 12f, 1f);
        }

        // ---- Corridor: pools of depth-carrying segments -----------------
        private void BuildCorridor()
        {
            var go = new GameObject("Corridor");
            go.transform.SetParent(transform, false);
            worldRoot = go.transform;

            // Floor rows: full-width trapezoid tiles stacked from far to near.
            for (int i = 0; i < FloorRows; i++)
            {
                float d = (float)i / FloorRows;            // 0..~1 spread
                Segment seg = MakeSegment("FloorRow", SegmentKind.Floor, SceneBand.Floor);
                seg.depth = d;
                floor.Add(seg);
            }

            // Wall posts marching down each side toward the vanishing point.
            for (int i = 0; i < WallPosts; i++)
            {
                float d = (float)i / WallPosts;
                leftWall.Add(MakeWall("WallL", -1f, d));
                rightWall.Add(MakeWall("WallR", 1f, d));
            }

            // Side props (torches / banners / barrels) alternate down the rails.
            for (int i = 0; i < SideProps; i++)
            {
                float d = (i + 0.5f) / SideProps;
                leftProps.Add(MakeProp("PropL", -1f, d, i));
                rightProps.Add(MakeProp("PropR", 1f, d, i));
            }

            // Framing pillars very close to the camera on each side (foreground).
            leftProps.Add(MakePillar("PillarL", -1f, 0.06f));
            rightProps.Add(MakePillar("PillarR", 1f, 0.10f));

            LayoutAll();
        }

        private Segment MakeSegment(string name, SegmentKind kind, SceneBand band)
        {
            SpriteRenderer sr = PlaceholderArt.NewSpriteObject(name,
                PlaceholderArt.SolidBlock(), Color.white, worldRoot, band, 0.5f);
            return new Segment { kind = kind, band = band, sr = sr, t = sr.transform };
        }

        private Segment MakeWall(string name, float side, float depth)
        {
            Segment seg = MakeSegment(name, SegmentKind.Wall,
                side < 0 ? SceneBand.LeftEnvironment : SceneBand.RightEnvironment);
            seg.side = side;
            seg.depth = depth;
            return seg;
        }

        private Segment MakeProp(string name, float side, float depth, int index)
        {
            SegmentKind kind = (index % 2 == 0) ? SegmentKind.Torch : SegmentKind.Banner;
            Segment seg = MakeSegment(name, kind,
                side < 0 ? SceneBand.LeftEnvironment : SceneBand.RightEnvironment);
            seg.side = side;
            seg.depth = depth;
            seg.variant = index;
            return seg;
        }

        private Segment MakePillar(string name, float side, float depth)
        {
            Segment seg = MakeSegment(name, SegmentKind.Pillar, SceneBand.Foreground);
            seg.side = side;
            seg.depth = depth;
            return seg;
        }

        // ---- Characters -------------------------------------------------
        private void BuildCharacters()
        {
            // Player: front-facing, in the CENTER lane, large and ~65% down the
            // screen (world y around -1.6 for an ortho size-5 camera).
            playerRig = PuppetBuilder.BuildPlayer(transform);
            playerTransform = playerRig.transform;
            playerTransform.localPosition = new Vector3(0f, -1.7f,
                LayerSorting.BandZ(SceneBand.Actors));
            playerTransform.localScale = Vector3.one * 1.5f;
            playerRig.SetPose("run", true);

            // Greve Gast: further back (higher on screen, smaller), floating.
            grevePuppet = PuppetBuilder.BuildGreveGast(transform);
            greveTransform = grevePuppet.transform;
            greveTransform.localPosition = new Vector3(0f, 0.6f,
                LayerSorting.BandZ(SceneBand.Actors, 0.15f));
            grevePuppet.SetPose("chase", true);
        }

        private void StartMusic()
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/Music/GreveGastsJakt");
            if (clip == null) clip = Resources.Load<AudioClip>("Audio/CustomRideMusic");
            if (clip == null) return;
            music = gameObject.AddComponent<AudioSource>();
            music.clip = clip;
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0.8f;
            music.spatialBlend = 0f;
            music.Play();
        }

        private void EnsureInputManager()
        {
            if (KinectKidsInputManager.Instance == null)
                new GameObject("KinectKidsInputManager").AddComponent<KinectKidsInputManager>();
        }

        // =================================================================
        private void Update()
        {
            if (body == null) return;
            float dt = Time.deltaTime;
            elapsed += dt;

            body.Update();
            ReadInput();
            DriveWorld(dt);
            StreamSegments(dt);
            DriveCharacters(dt);
            TickHazards(dt);
            TickPaletteSwap(dt);
        }

        private void ReadInput()
        {
            currentDodge = DodgeAction.None;
            switch (body.Current)
            {
                case GreveGastAction.Jump: currentDodge = DodgeAction.Jump; break;
                case GreveGastAction.Duck: currentDodge = DodgeAction.Duck; break;
                case GreveGastAction.Left: currentDodge = DodgeAction.Left; break;
                case GreveGastAction.Right: currentDodge = DodgeAction.Right; break;
            }

            // Hold-to-lane: left => lane 0 (LEFT), right => lane 2 (RIGHT),
            // otherwise settle back to lane 1 (CENTER).
            int wantLane = 1;
            if (body.Current == GreveGastAction.Left) wantLane = 0;
            else if (body.Current == GreveGastAction.Right) wantLane = 2;
            laneIndex = wantLane;

            float laneTarget = (laneIndex - 1); // -1, 0, +1
            // Kinect analogue nudges within the lane.
            laneTarget += Mathf.Clamp(body.HorizontalDelta * 2.5f, -0.5f, 0.5f);
            lateral = Mathf.Lerp(lateral, Mathf.Clamp(laneTarget, -1f, 1f),
                1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        private void DriveWorld(float dt)
        {
            bool running = body.Current == GreveGastAction.Run
                || body.RunEnergy > 0.4f
                || Input.GetKey(KeyCode.W)
                || Input.GetKey(KeyCode.LeftShift);

            float target = running ? baseWorldSpeed : idleWorldSpeed;
            worldSpeed = Mathf.Lerp(worldSpeed, target, 1f - Mathf.Exp(-4f * dt));

            chase += (running ? -runDrainPerSecond : recoverPerSecond) * dt;
            chase = Mathf.Clamp01(chase);
        }

        // ---- The heart of the effect: advance depth, re-place, recycle --
        private void StreamSegments(float dt)
        {
            float step = worldSpeed * dt;
            AdvanceList(floor, step);
            AdvanceList(leftWall, step);
            AdvanceList(rightWall, step);
            AdvanceList(leftProps, step);
            AdvanceList(rightProps, step);
            LayoutAll();
        }

        private void AdvanceList(List<Segment> list, float step)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Segment s = list[i];
                s.depth -= step;
                // Recycle: once past the camera, send it back to the far plane.
                while (s.depth < 0f) s.depth += 1f;
            }
        }

        private void LayoutAll()
        {
            for (int i = 0; i < floor.Count; i++) LayoutFloor(floor[i]);
            for (int i = 0; i < leftWall.Count; i++) LayoutWall(leftWall[i]);
            for (int i = 0; i < rightWall.Count; i++) LayoutWall(rightWall[i]);
            for (int i = 0; i < leftProps.Count; i++) LayoutProp(leftProps[i]);
            for (int i = 0; i < rightProps.Count; i++) LayoutProp(rightProps[i]);
        }

        // Depth 1 = far/tiny/high; depth 0 = near/huge/low. `weaveX` slides the
        // whole world sideways as the player weaves, stronger near the camera.
        private float WeaveX(float depth) => -lateral * laneNearOffset * (1f - PerspectiveModel.Curve(depth));

        private void LayoutFloor(Segment s)
        {
            float d = s.depth;
            float y = model.Y(d);
            float halfW = model.HalfWidth(d);

            // A floor row spans the full corridor width at its depth; its height
            // shrinks with depth so rows compress toward the horizon.
            float rowHeight = Mathf.Lerp(1.35f, 0.12f, PerspectiveModel.Curve(d));
            s.t.localPosition = new Vector3(WeaveX(d) * 0.4f, y, LayerSorting.BandZ(s.band));
            s.t.localScale = new Vector3(halfW * 2f, rowHeight, 1f);

            Color baseColor = FloorTint(s.depth);
            s.sr.color = model.Shade(baseColor, d, 0.5f);
            s.sr.sortingOrder = LayerSorting.OrderInBand(SceneBand.Floor, 1f - d);
        }

        private void LayoutWall(Segment s)
        {
            float d = s.depth;
            float scale = model.Scale(d);
            float wallX = model.HalfWidth(d) + 0.9f * scale;
            float wallHeight = Mathf.Lerp(11f, 1.4f, PerspectiveModel.Curve(d));
            float wallWidth = Mathf.Lerp(1.7f, 0.25f, PerspectiveModel.Curve(d));

            float y = model.Y(d) + wallHeight * 0.5f - 0.6f; // stand on the floor line
            s.t.localPosition = new Vector3(s.side * wallX + WeaveX(d), y, LayerSorting.BandZ(s.band));
            s.t.localScale = new Vector3(wallWidth, wallHeight, 1f);

            s.sr.color = model.Shade(WallTint(), d, 0.42f);
            s.sr.sortingOrder = LayerSorting.OrderInBand(s.band, 1f - d);
        }

        private void LayoutProp(Segment s)
        {
            float d = s.depth;
            float wallX = model.HalfWidth(d) + 0.5f * model.Scale(d);

            float baseHeight;
            Sprite sprite;
            Color color;
            switch (s.kind)
            {
                case SegmentKind.Torch:
                    baseHeight = 1.1f;
                    sprite = PlaceholderArt.Glow();
                    color = TorchTint();
                    break;
                case SegmentKind.Banner:
                    baseHeight = 3.0f;
                    sprite = PlaceholderArt.SolidBlock();
                    color = BannerTint();
                    break;
                case SegmentKind.Pillar:
                default:
                    baseHeight = 10.5f;
                    sprite = PlaceholderArt.SolidBlock();
                    color = WallTint();
                    break;
            }

            // Swap in real art if the palette maps this prop.
            Sprite real = palette.PropSprite(s.kind, s.variant);
            if (real != null) sprite = real;
            if (s.sr.sprite != sprite) s.sr.sprite = sprite;

            float h = baseHeight * Mathf.Lerp(1f, 0.12f, PerspectiveModel.Curve(d));
            float w = (s.kind == SegmentKind.Torch)
                ? h                       // glow is round
                : h * (s.kind == SegmentKind.Pillar ? 0.28f : 0.5f);

            float y = model.Y(d) + h * 0.5f - 0.4f;
            if (s.kind == SegmentKind.Torch)
                y = model.Y(d) + Mathf.Lerp(3.2f, 0.4f, PerspectiveModel.Curve(d)); // torches mounted high on the wall

            s.t.localPosition = new Vector3(s.side * wallX + WeaveX(d), y, LayerSorting.BandZ(s.band));
            s.t.localScale = new Vector3(w, h, 1f);

            SceneBand sortBand = s.kind == SegmentKind.Pillar ? SceneBand.Foreground : s.band;
            s.sr.color = s.kind == SegmentKind.Torch ? color : model.Shade(color, d, 0.5f);
            s.sr.sortingOrder = LayerSorting.OrderInBand(sortBand, 1f - d);
        }

        private void DriveCharacters(float dt)
        {
            // Player: default run loop, side/jump/duck poses layered on top.
            if (playerTransform != null)
            {
                string pose = "run";
                switch (currentDodge)
                {
                    case DodgeAction.Jump: pose = "jump"; break;
                    case DodgeAction.Duck: pose = "duck"; break;
                    case DodgeAction.Left: pose = "left"; break;
                    case DodgeAction.Right: pose = "right"; break;
                }
                playerRig.SetPose(pose);

                Vector3 p = playerTransform.localPosition;
                p.x = Mathf.Lerp(p.x, lateral * laneNearOffset, 1f - Mathf.Exp(-12f * dt));
                playerTransform.localPosition = p;
            }

            // Greve Gast: chase meter drives size + how close (lower & bigger).
            if (greveTransform != null)
            {
                float t = PerspectiveModel.Curve(chase);
                float scale = Mathf.Lerp(0.45f, 1.35f, t);
                greveTransform.localScale = Vector3.one * scale;

                Vector3 gp = greveTransform.localPosition;
                gp.y = Mathf.Lerp(1.7f, -0.6f, t);                 // floats down toward the player
                gp.x = Mathf.Lerp(gp.x, lateral * laneNearOffset * 0.7f,
                    1f - Mathf.Exp(-4f * dt));
                greveTransform.localPosition = gp;

                grevePuppet.SetPose(chase > 0.72f ? "reach" : "chase");
            }
        }

        // ---- Hazards: spawn far, scale up with perspective as they near --
        private void TickHazards(float dt)
        {
            // Update + cull existing hazards.
            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                RunHazard h = hazards[i];
                h.depth -= worldSpeed * dt;
                LayoutHazard(h);

                if (!h.resolved && h.depth <= h.resolveDepth)
                {
                    h.resolved = true;
                    bool avoided = EvaluateHazard(h);
                    OnHazardResolved(h, avoided);
                }
                if (h.depth <= -0.05f)
                {
                    if (h.sr != null) Destroy(h.sr.gameObject);
                    hazards.RemoveAt(i);
                }
            }

            hazardTimer -= dt;
            if (hazardTimer > 0f) return;
            hazardTimer = Mathf.Lerp(2.4f, 1.3f, chase);

            SpawnHazard(hazardCounter++);
        }

        private void SpawnHazard(int index)
        {
            // Alternate a JUMP obstacle (a hole across the lane) and a SIDESTEP
            // obstacle (an object filling one lane) — the two follow.md requires.
            HazardType type = (index % 2 == 0) ? HazardType.JumpHole : HazardType.SideBlock;

            var h = new RunHazard { type = type, depth = 1f, resolveDepth = 0.10f };
            SpriteRenderer sr = PlaceholderArt.NewSpriteObject("Hazard_" + type,
                PlaceholderArt.SolidBlock(), Color.white, worldRoot, SceneBand.Hazards, 0.5f);
            h.sr = sr;

            if (type == HazardType.JumpHole)
            {
                h.requiredAction = DodgeAction.Jump;
                h.side = 0f;
                sr.color = new Color(0.02f, 0.02f, 0.05f, 1f);       // a dark hole
                Sprite real = palette.HazardSprite(type);
                if (real != null) sr.sprite = real;
            }
            else
            {
                h.requiredAction = (Random.value < 0.5f) ? DodgeAction.Left : DodgeAction.Right;
                // The block sits in the lane the player must LEAVE.
                h.side = h.requiredAction == DodgeAction.Left ? 1f : -1f;
                sr.color = new Color(0.35f, 0.22f, 0.12f, 1f);       // a barrel/crate
                Sprite real = palette.HazardSprite(type);
                if (real != null) sr.sprite = real;
            }

            hazards.Add(h);
            LayoutHazard(h);
        }

        private void LayoutHazard(RunHazard h)
        {
            float d = Mathf.Clamp01(h.depth);
            float y = model.Y(d);

            if (h.type == HazardType.JumpHole)
            {
                float w = model.HalfWidth(d) * 1.6f;
                float ht = Mathf.Lerp(1.1f, 0.12f, PerspectiveModel.Curve(d));
                h.sr.transform.localPosition = new Vector3(WeaveX(d) * 0.4f, y - ht * 0.1f, LayerSorting.BandZ(SceneBand.Hazards));
                h.sr.transform.localScale = new Vector3(w, ht, 1f);
            }
            else
            {
                float laneX = h.side * model.HalfWidth(d) * 0.6f;
                float ht = Mathf.Lerp(2.0f, 0.2f, PerspectiveModel.Curve(d));
                float w = ht * 0.8f;
                h.sr.transform.localPosition = new Vector3(laneX + WeaveX(d), y + ht * 0.4f, LayerSorting.BandZ(SceneBand.Hazards));
                h.sr.transform.localScale = new Vector3(w, ht, 1f);
            }
            h.sr.sortingOrder = LayerSorting.OrderInBand(SceneBand.Hazards, 1f - d);
        }

        private bool EvaluateHazard(RunHazard h)
        {
            if (h.type == HazardType.JumpHole)
                return currentDodge == DodgeAction.Jump;

            // Side block: avoided if the player is clear of the blocked side.
            if (currentDodge == h.requiredAction) return true;
            return h.requiredAction == DodgeAction.Left
                ? lateral < -0.3f
                : lateral > 0.3f;
        }

        private void OnHazardResolved(RunHazard h, bool avoided)
        {
            if (avoided)
            {
                chase = Mathf.Clamp01(chase - 0.05f);
                grevePuppet?.SetPose("stunned");
            }
            else
            {
                chase = Mathf.Clamp01(chase + catchOnHit);
                scriptedCamera?.Shake(0.35f);
                grevePuppet?.SetPose("reach");
                if (h.sr != null) h.sr.color = new Color(0.9f, 0.2f, 0.2f, 1f);
            }
        }

        // ---- Asset-set swap: CastleCorridor -> Kitchen mid-run ----------
        private void TickPaletteSwap(float dt)
        {
            // Trigger the swap once, partway through the prototype, to prove the
            // corridor can morph into the kitchen without a scene reload.
            if (!swapTriggered && elapsed > 9f)
            {
                swapTriggered = true;
                targetPalette = RoomPalette.Kitchen();
                paletteBlend = 0f;
                model = PerspectiveModel.Tight; // the kitchen is a tighter space
            }

            if (paletteBlend < 1f)
            {
                paletteBlend = Mathf.Clamp01(paletteBlend + dt / 2.0f);
                if (paletteBlend >= 1f) palette = targetPalette;
            }
        }

        // Tints crossfade from the current palette to the incoming one while a
        // swap is in progress; SwapT is 0 when no swap is active.
        private float SwapT => (targetPalette == palette) ? 0f : paletteBlend;

        private Color FloorTint(float depth)
        {
            int idx = Mathf.FloorToInt(depth * 6f);
            return Color.Lerp(palette.FloorTint(idx), targetPalette.FloorTint(idx), SwapT);
        }
        private Color WallTint() => Color.Lerp(palette.wall, targetPalette.wall, SwapT);
        private Color TorchTint() => Color.Lerp(palette.torch, targetPalette.torch, SwapT);
        private Color BannerTint() => Color.Lerp(palette.banner, targetPalette.banner, SwapT);

        // ---- HUD --------------------------------------------------------
        private GUIStyle hud;
        private void OnGUI()
        {
            if (hud == null)
            {
                hud = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Clamp(Screen.height / 40, 16, 30),
                    fontStyle = FontStyle.Bold,
                };
                hud.normal.textColor = Color.white;
            }
            string room = paletteBlend < 1f ? (palette.name + " -> " + targetPalette.name) : palette.name;
            GUI.Label(new Rect(30, 24, 900, 34),
                "GREVE GAST NÄRMAR SIG  |  RUM: " + room +
                "  |  Spring (W), sidsteg (A/D), hoppa (Space)", hud);
        }

        private void OnDestroy()
        {
            if (music != null) music.Stop();
            body?.Dispose();
        }

        // =================================================================
        // Nested data types
        // =================================================================
        private enum SegmentKind { Floor, Wall, Torch, Banner, Pillar }

        private sealed class Segment
        {
            public SegmentKind kind;
            public SceneBand band;
            public SpriteRenderer sr;
            public Transform t;
            public float depth;
            public float side;   // -1 left, +1 right, 0 center
            public int variant;
        }

        private enum HazardType { JumpHole, SideBlock }

        private sealed class RunHazard
        {
            public HazardType type;
            public SpriteRenderer sr;
            public float depth;
            public float resolveDepth;
            public bool resolved;
            public DodgeAction requiredAction;
            public float side;
        }

        /// <summary>
        /// A swappable ROOM PALETTE: just sprite keys + tints. The streaming
        /// system is identical for every room, so switching palette mid-run
        /// morphs CastleCorridor into the Kitchen with no reload (follow.md
        /// "Nästa rum"). Real art fills in through <see cref="GreveChaseSprites"/>
        /// when the keyed sprites exist; otherwise tinted placeholders render.
        /// </summary>
        private sealed class RoomPalette
        {
            public string name;
            public Color wall;
            public Color torch;
            public Color banner;
            public Color[] floor;

            public string floorKey;
            public string wallKey;
            public string torchKey;
            public string bannerKey;
            public string pillarKey;
            public string holeKey;
            public string blockKey;

            public Color FloorTint(int idx)
            {
                if (floor == null || floor.Length == 0) return Color.gray;
                return floor[((idx % floor.Length) + floor.Length) % floor.Length];
            }

            public Sprite PropSprite(SegmentKind kind, int variant)
            {
                string key = kind switch
                {
                    SegmentKind.Torch => torchKey,
                    SegmentKind.Banner => bannerKey,
                    SegmentKind.Pillar => pillarKey,
                    _ => null,
                };
                return string.IsNullOrEmpty(key) ? null : GreveChaseSprites.Environment(key);
            }

            public Sprite HazardSprite(HazardType type)
            {
                string key = type == HazardType.JumpHole ? holeKey : blockKey;
                return string.IsNullOrEmpty(key) ? null : GreveChaseSprites.Hazard(key);
            }

            public static RoomPalette CastleCorridor() => new RoomPalette
            {
                name = "CastleCorridor",
                wall = new Color(0.24f, 0.24f, 0.34f),
                torch = PlaceholderArt.LanternOrange,
                banner = new Color(0.55f, 0.12f, 0.12f),
                floor = new[]
                {
                    new Color(0.42f, 0.42f, 0.50f),
                    new Color(0.34f, 0.34f, 0.42f),
                },
                floorKey = "env_0",
                wallKey = "env_2",
                torchKey = "env_10",
                bannerKey = "env_11",
                pillarKey = "env_13",
                holeKey = "haz_0",
                blockKey = "haz_4",
            };

            public static RoomPalette Kitchen() => new RoomPalette
            {
                name = "Kitchen",
                wall = new Color(0.40f, 0.28f, 0.18f),
                torch = new Color(1f, 0.82f, 0.5f),
                banner = new Color(0.30f, 0.40f, 0.24f),
                floor = new[]
                {
                    new Color(0.52f, 0.42f, 0.32f),
                    new Color(0.46f, 0.36f, 0.28f),
                },
                floorKey = "env_1",
                wallKey = "env_3",
                torchKey = "env_10",
                bannerKey = "env_11",
                pillarKey = "env_13",
                holeKey = "haz_0",
                blockKey = "haz_4",
            };
        }
    }
}
