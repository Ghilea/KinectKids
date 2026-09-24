using System.Collections.Generic;
using UnityEngine;
using KinectKids.Scene25D;
using KinectKids.Scene25D.Sections;
using KinectKids3D;
using KinectKids3D.Platform;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// A ground-up 2.5D endless-runner corridor for Greve Gast, following
    /// <c>src/problems/follow.md</c>.
    ///
    /// KEY INSIGHT (from inspecting the runtime video + the real art): the
    /// project already ships DRAWN, perspective-correct art — the floor tiles
    /// (env_0/env_1) are receding trapezoid stone strips, the wall module (env_2)
    /// is a full corridor wall with pillar + banner + torch drawn at an angle,
    /// and the player/Greve are finished character illustrations. The earlier
    /// attempt built the corridor out of dark tinted boxes, which read as a black
    /// ladder. This version instead STREAMS THE REAL SPRITES at full brightness
    /// and lets their built-in perspective do the work.
    ///
    /// THE MOTION: the environment moves, not the player. Every piece (floor
    /// rows, left/right wall modules) carries a normalised depth d in [0..1]
    /// (1 = far at the vanishing point, 0 = at the camera). Each frame d advances
    /// toward 0 by the world speed; the piece is re-placed and re-scaled through
    /// <see cref="PerspectiveModel"/> and recycled to the far plane when it passes
    /// the camera. Remove the characters and the corridor alone still reads as
    /// fast forward travel.
    ///
    /// The player runs TOWARD the camera (front-facing run art) in the CENTER of
    /// three lanes and side-steps LEFT/RIGHT; Greve Gast floats further back and
    /// grows/approaches as the chase meter rises; a jump-hole and a sidestep-block
    /// hazard scale up with perspective. A swappable room palette morphs
    /// CastleCorridor into the Kitchen mid-run without a reload.
    /// </summary>
    public sealed class CorridorRunnerDirector : MonoBehaviour, IDodgeSource
    {
        // ---- Tuning ------------------------------------------------------
        [Header("World motion")]
        public float baseWorldSpeed = 0.55f;
        public float idleWorldSpeed = 0.34f;

        [Header("Chase")]
        public float catchOnHit = 0.14f;
        public float recoverPerSecond = 0.05f;
        public float runDrainPerSecond = 0.05f;

        [Header("Lanes")]
        public float laneNearOffset = 3.6f;

        [Header("Motion")]
        [Tooltip("If true, decor recedes AWAY from the camera (0->1). If false, it " +
             "streams from the vanishing point toward the lens (1->0). " +
             "The environment should recede while hazards approach the player.")]
        public bool environmentRecedes = true;

        // ---- Perspective / corridor -------------------------------------
        // A WIDER corridor than the default so the floor fills the bottom of the
        // screen (follow.md: "MYCKET BRETT längst ner") and three lanes read
        // clearly near the camera. Vanishing point stays in the upper third.
        private PerspectiveModel model = new PerspectiveModel
        {
            floorLine = -4.9f,
            horizon = 1.7f,
            nearHalfWidth = 6.6f,   // near edge close to the screen sides (±~8.9)
            farHalfWidth = 0.7f,
            nearScale = 1.0f,
            farScale = 0.14f,
        };
        private const float CameraZ = -15f;

        private const int FloorRows = 10;   // overlapping stone strips -> solid floor
        private const int WallModules = 14; // dense overlapping wall sections down each side

        // ---- Runtime ----------------------------------------------------
        private Camera cam;
        private ScriptedCamera scriptedCamera;
        private Transform worldRoot;

        private readonly List<Segment> floor = new List<Segment>();
        private readonly List<Segment> leftWall = new List<Segment>();
        private readonly List<Segment> rightWall = new List<Segment>();
        private readonly List<RunHazard> hazards = new List<RunHazard>();

        private SpriteRenderer player;
        private SpriteRenderer greve;
        private float playerRunCycle;

        private GreveGastBodyInput body;
        private AudioSource music;

        private RoomPalette palette;
        private RoomPalette targetPalette;
        private float paletteBlend = 1f;

        private float chase = 0.35f;
        private float lateral;
        private int laneIndex = 1;
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
            // A muted blue-grey so the corridor never sits on pure black.
            cam.backgroundColor = new Color(0.18f, 0.19f, 0.28f);
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

        // ---- Deep background: a lit far wall so the corridor mouth glows -
        private void BuildBackdrop()
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(transform, false);

            // Full back panel (lighter than the clear colour) behind everything.
            SpriteRenderer backWall = PlaceholderArt.NewSpriteObject("BackWall",
                PlaceholderArt.SolidBlock(), new Color(0.26f, 0.28f, 0.42f, 1f),
                go.transform, SceneBand.Sky, 0f);
            backWall.transform.localPosition = new Vector3(0f, 2.5f, LayerSorting.BandZ(SceneBand.Sky));
            backWall.transform.localScale = new Vector3(40f, 14f, 1f);

            // A soft warm glow at the vanishing point (a distant light / moon).
            SpriteRenderer vault = PlaceholderArt.NewSpriteObject("FarGlow",
                PlaceholderArt.Glow(), new Color(0.6f, 0.6f, 0.85f, 0.9f),
                go.transform, SceneBand.FarBackground, 0f);
            vault.transform.localPosition = new Vector3(0f, model.horizon, LayerSorting.BandZ(SceneBand.FarBackground));
            vault.transform.localScale = new Vector3(5f, 4f, 1f);
        }

        // ---- Corridor: pools of depth-carrying segments -----------------
        private void BuildCorridor()
        {
            var go = new GameObject("Corridor");
            go.transform.SetParent(transform, false);
            worldRoot = go.transform;

            for (int i = 0; i < FloorRows; i++)
            {
                float d = (float)i / FloorRows;
                Segment seg = MakeSegment("FloorRow", SegmentKind.Floor, SceneBand.Floor);
                seg.depth = d;
                seg.variant = i;
                floor.Add(seg);
            }

            for (int i = 0; i < WallModules; i++)
            {
                float d = (float)i / WallModules;
                leftWall.Add(MakeWall("WallL", -1f, d, i));
                rightWall.Add(MakeWall("WallR", 1f, d, i));
            }

            LayoutAll();
        }

        private Segment MakeSegment(string name, SegmentKind kind, SceneBand band)
        {
            SpriteRenderer sr = PlaceholderArt.NewSpriteObject(name,
                PlaceholderArt.SolidBlock(), Color.white, worldRoot, band, 0.5f);
            return new Segment { kind = kind, band = band, sr = sr, t = sr.transform };
        }

        private Segment MakeWall(string name, float side, float depth, int index)
        {
            Segment seg = MakeSegment(name, SegmentKind.Wall,
                side < 0 ? SceneBand.LeftEnvironment : SceneBand.RightEnvironment);
            seg.side = side;
            seg.depth = depth;
            seg.variant = index;
            return seg;
        }

        // ---- Characters: the finished drawn sprites at proper size ------
        private void BuildCharacters()
        {
            // Player: front-facing run art, CENTER lane, large, ~62% down screen.
            var pGo = new GameObject("Player");
            pGo.transform.SetParent(transform, false);
            player = pGo.AddComponent<SpriteRenderer>();
            player.sprite = ResolvePlayer("run_near");
            LayerSorting.Apply(player, SceneBand.Actors, 0.9f);
            SizeSpriteToHeight(player, 3.4f); // world units tall
            player.transform.localPosition = new Vector3(0f, -1.6f, LayerSorting.BandZ(SceneBand.Actors));

            // Greve Gast: chase art, further back (higher, smaller), floating.
            var gGo = new GameObject("GreveGast");
            gGo.transform.SetParent(transform, false);
            greve = gGo.AddComponent<SpriteRenderer>();
            greve.sprite = ResolveGreve("chase");
            LayerSorting.Apply(greve, SceneBand.Actors, 0.4f);
            SizeSpriteToHeight(greve, 3.0f);
            greve.transform.localPosition = new Vector3(0f, 1.1f, LayerSorting.BandZ(SceneBand.Actors, 0.2f));
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

            int wantLane = 1;
            if (body.Current == GreveGastAction.Left) wantLane = 0;
            else if (body.Current == GreveGastAction.Right) wantLane = 2;
            laneIndex = wantLane;

            float laneTarget = (laneIndex - 1);
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

        // ---- Streaming: advance depth, re-place, recycle ----------------
        private void StreamSegments(float dt)
        {
            float step = worldSpeed * dt;
            AdvanceList(floor, step);
            AdvanceList(leftWall, step);
            AdvanceList(rightWall, step);
            LayoutAll();
        }

        private void AdvanceList(List<Segment> list, float step)
        {
            // Direction of the environment stream.
            //
            // The player runs TOWARD the camera (toward the bottom of the frame).
            // Relative to the player, the world he has passed must fall BEHIND
            // him — i.e. environment should RECEDE away from the camera: born
            // large/near at the bottom, then shrink and rise toward the vanishing
            // point, then recycle. If the world instead came from the vanishing
            // point toward the lens, it would look like the player is running
            // BACKWARD (the world overtaking him toward the camera).
            //
            // depth: 1 = far (vanishing point, small), 0 = near (camera, large).
            // Receding away therefore means depth INCREASES (0 -> 1).
            float dir = environmentRecedes ? +1f : -1f;
            for (int i = 0; i < list.Count; i++)
            {
                Segment s = list[i];
                s.depth += step * dir;
                while (s.depth < 0f) s.depth += 1f;
                while (s.depth >= 1f) s.depth -= 1f;
            }
        }

        private void LayoutAll()
        {
            for (int i = 0; i < floor.Count; i++) LayoutFloor(floor[i]);
            for (int i = 0; i < leftWall.Count; i++) LayoutWall(leftWall[i]);
            for (int i = 0; i < rightWall.Count; i++) LayoutWall(rightWall[i]);
        }

        private float WeaveX(float depth) => -lateral * laneNearOffset * (1f - PerspectiveModel.Curve(depth));

        private void LayoutFloor(Segment s)
        {
            float d = s.depth;

            // Ensure the real floor sprite (env_0/env_1) is applied.
            Sprite spr = palette.FloorSprite(s.variant);
            if (spr == null) spr = PlaceholderArt.SolidBlock();
            if (s.sr.sprite != spr) s.sr.sprite = spr;

            // Width spans the full corridor at this depth, plus a margin so the
            // drawn stone reaches all the way out to the wall bases (no gap).
            float halfW = model.HalfWidth(d);
            float w = halfW * 2.35f;
            float rowH = Mathf.Lerp(2.8f, 0.25f, PerspectiveModel.Curve(d)); // tall + overlapping

            // NEAR-CAMERA SWEEP: the smoothstep perspective compresses motion at
            // the camera, which makes the ground look slow / like it's floating
            // toward a fixed lens. To sell RUNNING FORWARD, the closest rows must
            // rush down and OFF the bottom of the screen. We push low-depth rows
            // further down (below the floor line) so the nearest stone sweeps
            // past the camera instead of stalling at the bottom edge.
            float y = model.Y(d);
            float nearBoost = Mathf.Clamp01((0.28f - d) / 0.28f); // 0 above d=0.28, ->1 at d=0
            y -= nearBoost * nearBoost * 3.2f;                    // accelerate off the bottom
            w *= 1f + nearBoost * 0.6f;                           // and grow as it passes

            float sw = SafeW(s.sr.sprite);
            float sh = SafeH(s.sr.sprite);
            float scaleX = sw > 0.001f ? w / sw : 1f;
            float scaleY = sh > 0.001f ? rowH / sh : 1f;

            s.t.localPosition = new Vector3(WeaveX(d) * 0.35f, y, LayerSorting.BandZ(SceneBand.Floor));
            s.t.localScale = new Vector3(scaleX, scaleY, 1f);

            // Brighten toward the camera, only mild darkening far away.
            s.sr.color = FloorColor(d);
            s.sr.sortingOrder = LayerSorting.OrderInBand(SceneBand.Floor, 1f - d);
        }

        private void LayoutWall(Segment s)
        {
            float d = s.depth;

            Sprite spr = palette.WallSprite(s.side, s.variant);
            if (spr == null) spr = PlaceholderArt.SolidBlock();
            if (s.sr.sprite != spr) s.sr.sprite = spr;

            // Wall height fills from the floor line up; sits just outside the
            // floor edge so the drawn walls line the corridor and converge.
            float wallHeight = Mathf.Lerp(9.5f, 1.1f, PerspectiveModel.Curve(d)) * 1.2f;
            float sw = SafeW(s.sr.sprite);
            float sh = SafeH(s.sr.sprite);
            float scaleY = sh > 0.001f ? wallHeight / sh : 1f;
            float scaleX = scaleY; // keep the drawn wall's aspect ratio
            float wallW = sw * scaleX;

            // Anchor the wall so its inner edge sits AT the floor edge (a slight
            // inward overlap) so walls rise straight off the floor with no gap.
            float floorEdge = model.HalfWidth(d);
            float x = s.side * (floorEdge + wallW * 0.5f - 0.9f) + WeaveX(d);
            float y = model.Y(d) + wallHeight * 0.5f - 1.4f;

            // NEAR-CAMERA SWEEP: closest wall sections rush OUTWARD and down past
            // the lens, matching the floor, so the corridor visibly streams by.
            float nearBoost = Mathf.Clamp01((0.28f - d) / 0.28f);
            x += s.side * nearBoost * nearBoost * 3.0f;
            y -= nearBoost * nearBoost * 1.6f;

            // Both walls must face into the corridor instead of exposing the
            // outside edge of the authored wall art.
            s.t.localScale = new Vector3(-scaleX, scaleY, 1f);
            s.t.localPosition = new Vector3(x, y, LayerSorting.BandZ(s.band));

            s.sr.color = WallColor(d);
            s.sr.sortingOrder = LayerSorting.OrderInBand(s.band, 1f - d);
        }

        // ---- Characters -------------------------------------------------
        private void DriveCharacters(float dt)
        {
            if (player != null)
            {
                // Choose the front-run art by default; swap to a pose sprite for
                // jump/duck/sidestep. A subtle vertical bob sells the run.
                string pose = "run_near";
                switch (currentDodge)
                {
                    case DodgeAction.Jump: pose = "jump"; break;
                    case DodgeAction.Duck: pose = "duck"; break;
                    case DodgeAction.Left: pose = "sidestep_left"; break;
                    case DodgeAction.Right: pose = "sidestep_right"; break;
                }
                Sprite spr = ResolvePlayer(pose);
                if (spr != null && player.sprite != spr) player.sprite = spr;

                playerRunCycle += dt * 9f;
                float bob = (currentDodge == DodgeAction.None || currentDodge == DodgeAction.Left || currentDodge == DodgeAction.Right)
                    ? Mathf.Abs(Mathf.Sin(playerRunCycle)) * 0.12f : 0f;

                Vector3 p = player.transform.localPosition;
                p.x = Mathf.Lerp(p.x, lateral * laneNearOffset, 1f - Mathf.Exp(-12f * dt));
                p.y = -1.6f + bob;
                player.transform.localPosition = p;
            }

            if (greve != null)
            {
                float t = PerspectiveModel.Curve(chase);
                // Swap Greve art by distance band + reach when very close.
                string gp = chase > 0.72f ? "reach" : (chase > 0.4f ? "chase_near" : "chase");
                Sprite gs = ResolveGreve(gp);
                if (gs != null && greve.sprite != gs) greve.sprite = gs;

                SizeSpriteToHeight(greve, Mathf.Lerp(1.6f, 4.2f, t));

                Vector3 g = greve.transform.localPosition;
                g.y = Mathf.Lerp(1.8f, -0.4f, t);
                g.x = Mathf.Lerp(g.x, lateral * laneNearOffset * 0.6f, 1f - Mathf.Exp(-4f * dt));
                greve.transform.localPosition = g;

                // A gentle float bob.
                g = greve.transform.localPosition;
                g.y += Mathf.Sin(elapsed * 2f) * 0.08f;
                greve.transform.localPosition = g;
            }
        }

        // ---- Hazards ----------------------------------------------------
        private void TickHazards(float dt)
        {
            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                RunHazard h = hazards[i];
                h.depth -= worldSpeed * dt;
                LayoutHazard(h);

                if (!h.resolved && h.depth <= h.resolveDepth)
                {
                    h.resolved = true;
                    OnHazardResolved(h, EvaluateHazard(h));
                }
                if (h.depth <= -0.05f)
                {
                    if (h.sr != null) Destroy(h.sr.gameObject);
                    hazards.RemoveAt(i);
                }
            }

            hazardTimer -= dt;
            if (hazardTimer > 0f) return;
            hazardTimer = Mathf.Lerp(2.6f, 1.4f, chase);
            SpawnHazard(hazardCounter++);
        }

        private void SpawnHazard(int index)
        {
            HazardType type = (index % 2 == 0) ? HazardType.JumpHole : HazardType.SideBlock;
            var h = new RunHazard { type = type, depth = 1f, resolveDepth = 0.12f };
            SpriteRenderer sr = PlaceholderArt.NewSpriteObject("Hazard_" + type,
                PlaceholderArt.SolidBlock(), Color.white, worldRoot, SceneBand.Hazards, 0.5f);
            h.sr = sr;

            if (type == HazardType.JumpHole)
            {
                h.requiredAction = DodgeAction.Jump;
                h.side = 0f;
                Sprite real = palette.HazardSprite(type);
                if (real != null) sr.sprite = real; else sr.color = new Color(0.02f, 0.02f, 0.05f, 1f);
            }
            else
            {
                h.requiredAction = (Random.value < 0.5f) ? DodgeAction.Left : DodgeAction.Right;
                h.side = h.requiredAction == DodgeAction.Left ? 1f : -1f;
                Sprite real = palette.HazardSprite(type);
                if (real != null) sr.sprite = real; else sr.color = new Color(0.5f, 0.33f, 0.18f, 1f);
            }

            hazards.Add(h);
            LayoutHazard(h);
        }

        private void LayoutHazard(RunHazard h)
        {
            float d = Mathf.Clamp01(h.depth);
            float y = model.Y(d);
            float sw = SafeW(h.sr.sprite);
            float sh = SafeH(h.sr.sprite);

            if (h.type == HazardType.JumpHole)
            {
                float w = model.HalfWidth(d) * 1.5f;
                float ht = Mathf.Lerp(1.6f, 0.15f, PerspectiveModel.Curve(d));
                float scaleX = sw > 0.001f ? w / sw : w;
                float scaleY = sh > 0.001f ? ht / sh : ht;
                h.sr.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                h.sr.transform.localPosition = new Vector3(WeaveX(d) * 0.35f, y, LayerSorting.BandZ(SceneBand.Hazards));
            }
            else
            {
                float ht = Mathf.Lerp(2.4f, 0.25f, PerspectiveModel.Curve(d));
                float scaleY = sh > 0.001f ? ht / sh : ht;
                float scaleX = scaleY;
                float laneX = h.side * model.HalfWidth(d) * 0.55f;
                h.sr.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                h.sr.transform.localPosition = new Vector3(laneX + WeaveX(d), y + ht * 0.4f, LayerSorting.BandZ(SceneBand.Hazards));
            }
            h.sr.sortingOrder = LayerSorting.OrderInBand(SceneBand.Hazards, 1f - d);
        }

        private bool EvaluateHazard(RunHazard h)
        {
            if (h.type == HazardType.JumpHole) return currentDodge == DodgeAction.Jump;
            if (currentDodge == h.requiredAction) return true;
            return h.requiredAction == DodgeAction.Left ? lateral < -0.3f : lateral > 0.3f;
        }

        private void OnHazardResolved(RunHazard h, bool avoided)
        {
            if (avoided) chase = Mathf.Clamp01(chase - 0.05f);
            else
            {
                chase = Mathf.Clamp01(chase + catchOnHit);
                scriptedCamera?.Shake(0.35f);
                if (h.sr != null && palette.HazardSprite(h.type) == null)
                    h.sr.color = new Color(0.9f, 0.2f, 0.2f, 1f);
            }
        }

        // ---- Palette swap: CastleCorridor -> Kitchen mid-run ------------
        private void TickPaletteSwap(float dt)
        {
            if (!swapTriggered && elapsed > 9f)
            {
                swapTriggered = true;
                targetPalette = RoomPalette.Kitchen();
                paletteBlend = 0f;
            }
            if (paletteBlend < 1f)
            {
                paletteBlend = Mathf.Clamp01(paletteBlend + dt / 2.0f);
                if (paletteBlend >= 1f) palette = targetPalette;
            }
        }

        // ---- Colour helpers: keep art bright, only mild depth shading ---
        private Color FloorColor(float d)
        {
            // Near = full bright; far = ~70% so it recedes into a little haze.
            float shade = Mathf.Lerp(1f, 0.72f, PerspectiveModel.Curve(d));
            Color tint = Color.Lerp(palette.floorTint, targetPalette.floorTint, SwapT);
            return new Color(tint.r * shade, tint.g * shade, tint.b * shade, 1f);
        }

        private Color WallColor(float d)
        {
            float shade = Mathf.Lerp(1f, 0.68f, PerspectiveModel.Curve(d));
            Color tint = Color.Lerp(palette.wallTint, targetPalette.wallTint, SwapT);
            return new Color(tint.r * shade, tint.g * shade, tint.b * shade, 1f);
        }

        private float SwapT => (targetPalette == palette) ? 0f : paletteBlend;

        // ---- Sprite utilities -------------------------------------------
        private static float SafeW(Sprite s) => s != null && s.bounds.size.x > 0.0001f ? s.bounds.size.x : 1f;
        private static float SafeH(Sprite s) => s != null && s.bounds.size.y > 0.0001f ? s.bounds.size.y : 1f;

        private static void SizeSpriteToHeight(SpriteRenderer sr, float worldHeight)
        {
            if (sr == null || sr.sprite == null) return;
            float h = SafeH(sr.sprite);
            float scale = h > 0.0001f ? worldHeight / h : 1f;
            float sign = Mathf.Sign(sr.transform.localScale.x == 0 ? 1 : sr.transform.localScale.x);
            sr.transform.localScale = new Vector3(scale * (sign == 0 ? 1 : sign), scale, 1f);
        }

        // Real art with a clearly-visible bright fallback if a sprite is missing.
        private Sprite ResolvePlayer(string pose)
        {
            Sprite s = GreveChaseSprites.Player(pose);
            if (s == null) s = GreveChaseSprites.Player("run_near");
            if (s == null) s = GreveChaseSprites.Player("idle");
            return s; // may be null -> handled by caller keeping previous sprite
        }

        private Sprite ResolveGreve(string pose)
        {
            Sprite s = GreveChaseSprites.Greve(pose);
            if (s == null) s = GreveChaseSprites.Greve("chase");
            if (s == null) s = GreveChaseSprites.Greve("idle");
            return s;
        }

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
        private enum SegmentKind { Floor, Wall }

        private sealed class Segment
        {
            public SegmentKind kind;
            public SceneBand band;
            public SpriteRenderer sr;
            public Transform t;
            public float depth;
            public float side;
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
        /// A swappable ROOM PALETTE: sprite keys + tints. The streaming system is
        /// identical for every room, so switching palette mid-run morphs the
        /// corridor into the kitchen with no reload. Real art fills in through
        /// <see cref="GreveChaseSprites"/>; unmapped keys fall back to tinted
        /// placeholders.
        /// </summary>
        private sealed class RoomPalette
        {
            public string name;
            public Color floorTint;
            public Color wallTint;
            public string[] floorKeys;
            public string[] wallKeys;
            public string holeKey;
            public string blockKey;

            public Sprite FloorSprite(int variant)
            {
                if (floorKeys == null || floorKeys.Length == 0) return null;
                return GreveChaseSprites.Environment(floorKeys[((variant % floorKeys.Length) + floorKeys.Length) % floorKeys.Length]);
            }

            public Sprite WallSprite(float side, int variant)
            {
                if (wallKeys == null || wallKeys.Length == 0) return null;
                int wallIndex = side < 0f ? 0 : 1;
                string key = wallKeys[Mathf.Min(wallIndex, wallKeys.Length - 1)];
                return GreveChaseSprites.Environment(key);
            }

            public Sprite HazardSprite(HazardType type)
            {
                string key = type == HazardType.JumpHole ? holeKey : blockKey;
                return string.IsNullOrEmpty(key) ? null : GreveChaseSprites.Hazard(key);
            }

            public static RoomPalette CastleCorridor() => new RoomPalette
            {
                name = "CastleCorridor",
                floorTint = new Color(1f, 0.98f, 0.92f),   // near-white: show the art's own colour
                wallTint = new Color(1f, 0.98f, 0.92f),
                floorKeys = new[] { "env_0", "env_1" },     // drawn stone floor strips
                wallKeys = new[] { "env_2", "env_3" },      // authored left and right corridor walls
                holeKey = "haz_0",
                blockKey = "haz_4",
            };

            public static RoomPalette Kitchen() => new RoomPalette
            {
                name = "Kitchen",
                floorTint = new Color(1f, 0.9f, 0.78f),     // warmer wash for the kitchen
                wallTint = new Color(1f, 0.88f, 0.74f),
                floorKeys = new[] { "env_1", "env_0" },
                wallKeys = new[] { "env_2", "env_3" },
                holeKey = "haz_0",
                blockKey = "haz_4",
            };
        }
    }
}
