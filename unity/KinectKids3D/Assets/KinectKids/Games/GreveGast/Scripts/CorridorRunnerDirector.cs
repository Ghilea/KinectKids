using System.Collections.Generic;
using UnityEngine;
using KinectKids.Scene25D;
using KinectKids3D;
using KinectKids3D.Platform;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// Endless runner built on a small real 3D corridor. Perspective, apparent
    /// scale and depth come exclusively from the perspective camera and world Z.
    /// The illustrated characters and decorations remain flat sprites.
    /// </summary>
    public sealed class CorridorRunnerDirector : MonoBehaviour, IDodgeSource
    {
        public enum EnvironmentTheme { CastleCorridor, GreatHall, Kitchen, Passage }
        public enum Lane { Left, Center, Right }
        private enum PlayerMotion { Running, Jumping, Ducking, ChangingLane, Hit, Recovering }

        [Header("3D corridor")]
        [Range(4, 6)] public int segmentCount = 6;
        public float segmentLength = 16f;
        public float corridorWidth = 22f;
        public float corridorHeight = 18f;
        public float runningSpeed = 14f;
        public float idleSpeed = 8f;

        [Header("Perspective camera")]
        [Range(40f, 80f)] public float fieldOfView = 70f;
        public Vector3 cameraPosition = new Vector3(0f, 4.2f, -10f);

        [Header("Actors and lanes")]
        public float laneSpacing = 4.2f;
        public float laneChangeSharpness = 6f;
        [Tooltip("Seconds for one complete lane-change animation and movement.")]
        public float laneChangeDuration = 0.42f;
        public float playerZ = 2.5f;
        public float playerStartZ = 5.5f;
        public float playerHeight = 3.2f;
        [Tooltip("Extra world-space height at the top of the jump arc.")]
        public float playerJumpHeight = 1.35f;
        public float playerJumpDuration = 0.68f;
        public float playerDuckDuration = 0.82f;
        public float playerHitDuration = 0.72f;
        public float playerRecoverDuration = 0.86f;
        public float greveFarZ = 18f;
        public float greveNearZ = 6f;
        public float greveHeight = 3.4f;

        [Header("Chase")]
        public float catchOnHit = 0.14f;
        public float recoverPerSecond = 0.05f;
        public float runDrainPerSecond = 0.05f;
        [Tooltip("Extra distance Greve Gast gains while the player ducks.")]
        public float duckPressurePerSecond = 0.11f;
        public float hitPressurePerSecond = 0.14f;

        [Header("Theme demonstration")]
        [Tooltip("Streams Kitchen segments in behind CastleCorridor after this many seconds.")]
        public float kitchenTransitionAt = 10f;
        public bool cycleAllThemes = true;
        public bool showIllustratedVista = true;

        private Camera worldCamera;
        private Transform corridorRoot;
        private Transform hazardRoot;
        private readonly List<EnvironmentSegment3D> segments = new List<EnvironmentSegment3D>();
        private readonly List<RunHazard> hazards = new List<RunHazard>();
        private readonly Dictionary<EnvironmentTheme, ThemeStyle> themeStyles = new Dictionary<EnvironmentTheme, ThemeStyle>();

        private SpriteRenderer player;
        private SpriteRenderer greve;
        private SpriteRenderer illustratedVista;
        private Sprite[] playerRunFrames;
        private Sprite[] playerJumpFrames;
        private Sprite[] playerSideFrames;
        private Sprite[] playerDuckFrames;
        private Sprite[] playerHitFrames;
        private Sprite[] playerRecoverFrames;
        private Sprite[] greveSingFrames;
        private Sprite[] greveFlyFrames;
        private Sprite[] greveDuckReactFrames;
        private GreveGastBodyInput body;
        private AudioSource music;
        private EnvironmentTheme requestedTheme = EnvironmentTheme.CastleCorridor;
        private Lane lane = Lane.Center;
        private DodgeAction currentDodge = DodgeAction.None;
        private float lateral;
        private float worldSpeed;
        private float chase = 0.35f;
        private float elapsed;
        private float hazardTimer = 2.2f;
        private int hazardCounter;
        private float playerRunCycle;
        private float laneAnimationTime;
        private float laneAnimationStartX;
        private int laneMoveDirection;
        private PlayerMotion playerMotion = PlayerMotion.Running;
        private float playerMotionTime;
        private GreveGastAction previousRawAction = GreveGastAction.None;

        public DodgeAction CurrentDodge => currentDodge;
        public float LateralPosition => Mathf.Clamp(lateral, -1f, 1f);
        public EnvironmentTheme CurrentTheme => requestedTheme;
        private bool WorldPaused => playerMotion == PlayerMotion.Ducking ||
            playerMotion == PlayerMotion.Hit || playerMotion == PlayerMotion.Recovering;

        private void Start()
        {
            EnsureInputManager();
            body = new GreveGastBodyInput();
            body.Start();
            BuildCamera();
            BuildThemeStyles();
            BuildLighting();
            BuildCorridor();
            BuildIllustratedVista();
            BuildCharacters();
            StartMusic();
            worldSpeed = idleSpeed;
        }

        private void BuildCamera()
        {
            GameObject cameraGo = new GameObject("Locked Perspective Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(transform, false);
            cameraGo.transform.localPosition = cameraPosition;
            cameraGo.transform.localRotation = Quaternion.identity;

            worldCamera = cameraGo.AddComponent<Camera>();
            worldCamera.orthographic = false;
            worldCamera.fieldOfView = fieldOfView;
            worldCamera.nearClipPlane = 0.1f;
            worldCamera.farClipPlane = 150f;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = new Color(0.035f, 0.045f, 0.07f, 1f);

            if (FindFirstObjectByType<AudioListener>() == null)
                cameraGo.AddComponent<AudioListener>();
        }

        private void BuildThemeStyles()
        {
            Texture2D floorTexture = Resources.Load<Texture2D>("Textures/CastleRoad");
            Texture2D wallTexture = Resources.Load<Texture2D>("Textures/CastleStone");
            if (floorTexture != null)
            {
                floorTexture.wrapMode = TextureWrapMode.Repeat;
                floorTexture.anisoLevel = 4;
            }
            if (wallTexture != null)
            {
                wallTexture.wrapMode = TextureWrapMode.Repeat;
                wallTexture.anisoLevel = 4;
            }

            // Cube primitives keep their normal UVs. Repeating the textures per
            // segment gives the floor and walls a stable stone size instead of
            // stretching a single photograph over twelve world metres.
            Vector2 floorTiling = new Vector2(corridorWidth / 3f, segmentLength / 3f);
            Vector2 wallTiling = new Vector2(segmentLength / 3f, corridorHeight / 2f);
            Vector2 accentTiling = new Vector2(2f, 2f);

            themeStyles[EnvironmentTheme.CastleCorridor] = new ThemeStyle(EnvironmentTheme.CastleCorridor,
                CreateTiledMaterial(new Color(0.72f, 0.76f, 0.86f), floorTexture, floorTiling),
                CreateTiledMaterial(new Color(0.68f, 0.72f, 0.84f), wallTexture, wallTiling),
                CreateTiledMaterial(new Color(0.48f, 0.52f, 0.64f), wallTexture, floorTiling),
                CreateTiledMaterial(new Color(0.82f, 0.84f, 0.94f), wallTexture, accentTiling),
                new Color(1f, 0.55f, 0.24f), "new_wall_left", "new_window");
            themeStyles[EnvironmentTheme.GreatHall] = new ThemeStyle(EnvironmentTheme.GreatHall,
                CreateTiledMaterial(new Color(0.84f, 0.70f, 0.58f), floorTexture, floorTiling),
                CreateTiledMaterial(new Color(0.78f, 0.62f, 0.58f), wallTexture, wallTiling),
                CreateTiledMaterial(new Color(0.48f, 0.36f, 0.40f), wallTexture, floorTiling),
                CreateTiledMaterial(new Color(0.86f, 0.68f, 0.34f), wallTexture, accentTiling, 0.18f),
                new Color(1f, 0.72f, 0.35f), "new_banner", "new_armour");
            themeStyles[EnvironmentTheme.Kitchen] = new ThemeStyle(EnvironmentTheme.Kitchen,
                CreateTiledMaterial(new Color(0.82f, 0.68f, 0.54f), floorTexture, floorTiling),
                CreateTiledMaterial(new Color(0.90f, 0.78f, 0.62f), wallTexture, wallTiling),
                CreateTiledMaterial(new Color(0.56f, 0.46f, 0.38f), wallTexture, floorTiling),
                CreateTiledMaterial(new Color(0.66f, 0.48f, 0.30f), wallTexture, accentTiling),
                new Color(1f, 0.64f, 0.30f), "new_barrel", "new_crate");
            themeStyles[EnvironmentTheme.Passage] = new ThemeStyle(EnvironmentTheme.Passage,
                CreateTiledMaterial(new Color(0.60f, 0.72f, 0.74f), floorTexture, floorTiling),
                CreateTiledMaterial(new Color(0.54f, 0.68f, 0.72f), wallTexture, wallTiling),
                CreateTiledMaterial(new Color(0.34f, 0.46f, 0.50f), wallTexture, floorTiling),
                CreateTiledMaterial(new Color(0.58f, 0.74f, 0.76f), wallTexture, accentTiling),
                new Color(0.40f, 0.72f, 0.78f), "new_door", "new_pillar");
        }

        private static Material CreateTiledMaterial(Color tint, Texture2D texture, Vector2 tiling, float metallic = 0f)
        {
            if (texture == null) return MaterialFactory.Solid(tint, metallic);
            Material material = MaterialFactory.Textured(tint, texture, metallic);
            if (material.HasProperty("_MainTex")) material.SetTextureScale("_MainTex", tiling);
            if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", tiling);
            return material;
        }

        private void BuildLighting()
        {
            RenderSettings.ambientLight = new Color(0.20f, 0.21f, 0.27f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = worldCamera.backgroundColor;
            RenderSettings.fogStartDistance = 65f;
            RenderSettings.fogEndDistance = 115f;

            GameObject lightGo = new GameObject("Corridor Key Light");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localRotation = Quaternion.Euler(42f, -28f, 0f);
            Light key = lightGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.78f, 0.82f, 1f);
            key.intensity = 0.75f;
            key.shadows = LightShadows.Soft;
        }

        private void BuildCorridor()
        {
            corridorRoot = new GameObject("3D Environment Segments").transform;
            corridorRoot.SetParent(transform, false);
            hazardRoot = new GameObject("3D Hazards").transform;
            hazardRoot.SetParent(transform, false);

            segmentCount = Mathf.Clamp(segmentCount, 4, 6);
            for (int i = 0; i < segmentCount; i++)
            {
                GameObject go = new GameObject("EnvironmentSegment_" + i);
                go.transform.SetParent(corridorRoot, false);
                // One segment already sits behind the lens. As all geometry moves
                // along +Z, it enters the view from the camera and continues away.
                float firstCenterZ = cameraPosition.z - segmentLength * 0.5f;
                go.transform.localPosition = new Vector3(0f, 0f, firstCenterZ + i * segmentLength);
                EnvironmentSegment3D segment = go.AddComponent<EnvironmentSegment3D>();
                segment.Build(segmentLength, corridorWidth, corridorHeight, i);
                segment.ApplyTheme(themeStyles[EnvironmentTheme.CastleCorridor]);
                segments.Add(segment);
            }
        }

        private void BuildIllustratedVista()
        {
            if (!showIllustratedVista) return;
            // env_18 is the open castle/moon illustration without a painted
            // corridor floor or two complete corridor walls. It is a distant
            // billboard skin only; the 3D primitives still define perspective.
            Sprite sprite = GreveChaseSprites.Environment("new_kitchen_vista");
            if (sprite == null) sprite = GreveChaseSprites.Environment("env_18");
            if (sprite == null) return;

            GameObject go = new GameObject("Illustrated castle kitchen vista");
            go.transform.SetParent(corridorRoot, false);
            go.transform.localPosition = new Vector3(0f, 0f, 38f);
            go.transform.localRotation = worldCamera.transform.localRotation;
            illustratedVista = go.AddComponent<SpriteRenderer>();
            illustratedVista.sprite = sprite;
            illustratedVista.sortingOrder = -10;
            SizeSpriteToHeight(illustratedVista, corridorHeight * 0.95f);
        }

        private void BuildCharacters()
        {
            playerRunFrames = GreveChaseSprites.PlayerAnimation("run");
            playerJumpFrames = GreveChaseSprites.PlayerAnimation("jump");
            playerSideFrames = GreveChaseSprites.PlayerAnimation("side");
            playerDuckFrames = GreveChaseSprites.PlayerAnimation("duck");
            playerHitFrames = GreveChaseSprites.PlayerAnimation("hit");
            playerRecoverFrames = GreveChaseSprites.PlayerAnimation("recover");
            greveSingFrames = GreveChaseSprites.GreveAnimation("sing");
            greveFlyFrames = GreveChaseSprites.GreveAnimation("fly");
            greveDuckReactFrames = GreveChaseSprites.GreveAnimation("duck_react");

            Sprite playerStart = playerRunFrames.Length > 0 ? playerRunFrames[0] : ResolvePlayer("run_near");
            Sprite greveStart = greveSingFrames.Length > 0 ? greveSingFrames[0] : ResolveGreve("chase");
            player = BuildActor("Player", playerStart, playerHeight,
                new Vector3(0f, 0f, Mathf.Max(playerZ, playerStartZ)));
            greve = BuildActor("Greve Gast (singing)", greveStart, greveHeight,
                new Vector3(0f, 0.25f, greveFarZ));
        }

        private SpriteRenderer BuildActor(string actorName, Sprite sprite, float height, Vector3 position)
        {
            GameObject go = new GameObject(actorName + " 2.5D Billboard");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            go.transform.localRotation = worldCamera.transform.localRotation;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 20;
            SizeSpriteToHeight(renderer, height);
            return renderer;
        }

        private void Update()
        {
            if (body == null) return;
            float dt = Time.deltaTime;
            elapsed += dt;
            body.Update();
            ReadInput(dt);
            DriveWorld(dt);
            StreamSegments(dt);
            MoveIllustratedVista(dt);
            DriveCharacters(dt);
            TickHazards(dt);
            TickThemeSequence();
        }

        private void ReadInput(float dt)
        {
            currentDodge = DodgeAction.None;

            // A gesture starts a complete motion. Holding the pose does not
            // extend or repeat it; Kinect and keyboard both re-arm only after
            // the raw action changes away and is performed again.
            if (playerMotion != PlayerMotion.Running && playerMotionTime >= MotionDuration(playerMotion))
            {
                if (playerMotion == PlayerMotion.Hit)
                {
                    playerMotion = PlayerMotion.Recovering;
                    playerMotionTime = 0f;
                }
                else
                {
                    playerMotion = PlayerMotion.Running;
                    playerMotionTime = 0f;
                    laneMoveDirection = 0;
                }
            }

            GreveGastAction raw = body.Current;
            bool freshAction = raw != previousRawAction;
            if (playerMotion == PlayerMotion.Running && freshAction)
            {
                if (raw == GreveGastAction.Jump) BeginMotion(PlayerMotion.Jumping);
                else if (raw == GreveGastAction.Duck) BeginMotion(PlayerMotion.Ducking);
                else if (raw == GreveGastAction.Left || raw == GreveGastAction.Right)
                {
                    int direction = raw == GreveGastAction.Left ? -1 : 1;
                    int oldLane = (int)lane;
                    int nextLane = Mathf.Clamp(oldLane + direction, (int)Lane.Left, (int)Lane.Right);
                    if (nextLane != oldLane)
                    {
                        lane = (Lane)nextLane;
                        laneMoveDirection = direction;
                        laneAnimationStartX = player != null ? player.transform.localPosition.x : LaneX((Lane)oldLane);
                        BeginMotion(PlayerMotion.ChangingLane);
                    }
                }
            }

            switch (playerMotion)
            {
                case PlayerMotion.Jumping: currentDodge = DodgeAction.Jump; break;
                case PlayerMotion.Ducking: currentDodge = DodgeAction.Duck; break;
                case PlayerMotion.ChangingLane:
                    currentDodge = laneMoveDirection < 0 ? DodgeAction.Left : DodgeAction.Right;
                    break;
            }

            if (playerMotion != PlayerMotion.Running) playerMotionTime += dt;
            laneAnimationTime = playerMotion == PlayerMotion.ChangingLane ? playerMotionTime : 0f;
            previousRawAction = raw;

            float target = LaneX(lane);
            lateral = Mathf.Lerp(lateral, target / laneSpacing,
                1f - Mathf.Exp(-laneChangeSharpness * Time.deltaTime));
        }

        private void BeginMotion(PlayerMotion motion)
        {
            playerMotion = motion;
            playerMotionTime = 0f;
        }

        private float MotionDuration(PlayerMotion motion)
        {
            switch (motion)
            {
                case PlayerMotion.Jumping: return playerJumpDuration;
                case PlayerMotion.Ducking: return playerDuckDuration;
                case PlayerMotion.ChangingLane: return laneChangeDuration;
                case PlayerMotion.Hit: return playerHitDuration;
                case PlayerMotion.Recovering: return playerRecoverDuration;
                default: return 0f;
            }
        }

        private void DriveWorld(float dt)
        {
            if (WorldPaused)
            {
                // The room, obstacles and vista stop. Greve Gast does not: this
                // is his opportunity to close the distance during a duck/fall.
                worldSpeed = 0f;
                float pressure = playerMotion == PlayerMotion.Ducking
                    ? duckPressurePerSecond : hitPressurePerSecond;
                chase = Mathf.Clamp01(chase + pressure * dt);
                return;
            }

            bool running = body.Current == GreveGastAction.Run || body.RunEnergy > 0.4f ||
                           Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.LeftShift);
            worldSpeed = Mathf.Lerp(worldSpeed, running ? runningSpeed : idleSpeed, 1f - Mathf.Exp(-4f * dt));
            float chaseRate = running ? -runDrainPerSecond : recoverPerSecond;
            chase = Mathf.Clamp01(chase + chaseRate * dt);
        }

        private void StreamSegments(float dt)
        {
            float move = worldSpeed * dt;
            for (int i = 0; i < segments.Count; i++)
                segments[i].transform.localPosition += Vector3.forward * move;

            // The segment behind the camera feeds new geometry into the view.
            // Once its camera-side edge has passed the lens, recycle the farthest
            // segment behind the queue. Nothing visibly travels toward the camera.
            for (int recycle = 0; recycle < segmentCount; recycle++)
            {
                int nearestIndex = 0;
                int farthestIndex = 0;
                for (int i = 1; i < segments.Count; i++)
                {
                    if (segments[i].transform.localPosition.z < segments[nearestIndex].transform.localPosition.z) nearestIndex = i;
                    if (segments[i].transform.localPosition.z > segments[farthestIndex].transform.localPosition.z) farthestIndex = i;
                }

                float nearestZ = segments[nearestIndex].transform.localPosition.z;
                if (nearestZ - segmentLength * 0.5f < cameraPosition.z) break;

                EnvironmentSegment3D recycled = segments[farthestIndex];
                Vector3 p = recycled.transform.localPosition;
                p.z = nearestZ - segmentLength;
                recycled.transform.localPosition = p;
                recycled.ApplyTheme(themeStyles[requestedTheme]);
            }
        }

        private void MoveIllustratedVista(float dt)
        {
            if (illustratedVista == null) return;
            Vector3 p = illustratedVista.transform.localPosition;
            p.z += worldSpeed * 0.25f * dt;
            // Reset while hidden in the distance; its visible motion is always +Z.
            if (p.z > 72f) p.z = 38f;
            illustratedVista.transform.localPosition = p;
        }

        private void DriveCharacters(float dt)
        {
            if (player != null)
            {
                playerRunCycle += dt * 7.5f;
                bool regularRun = playerMotion == PlayerMotion.Running;
                Sprite sprite;
                if (playerMotion == PlayerMotion.Hit && playerHitFrames.Length > 0)
                    sprite = FrameProgress(playerHitFrames,
                        playerMotionTime / Mathf.Max(0.1f, playerHitDuration));
                else if (playerMotion == PlayerMotion.Recovering && playerRecoverFrames.Length > 0)
                    sprite = FrameProgress(playerRecoverFrames,
                        playerMotionTime / Mathf.Max(0.1f, playerRecoverDuration));
                else if (playerMotion == PlayerMotion.Jumping && playerJumpFrames.Length > 0)
                {
                    float jumpProgress = Mathf.Clamp01(playerMotionTime / Mathf.Max(0.1f, playerJumpDuration));
                    sprite = FrameProgress(playerJumpFrames, Mathf.Sin(jumpProgress * Mathf.PI));
                }
                else if (playerMotion == PlayerMotion.Ducking && playerDuckFrames.Length > 0)
                    sprite = FrameProgress(playerDuckFrames,
                        playerMotionTime / Mathf.Max(0.1f, playerDuckDuration * 0.45f));
                else if (playerMotion == PlayerMotion.Ducking) sprite = ResolvePlayer("duck");
                else if (playerMotion == PlayerMotion.ChangingLane && playerSideFrames.Length > 0)
                    sprite = FrameProgress(playerSideFrames,
                        laneAnimationTime / Mathf.Max(0.05f, laneChangeDuration));
                else if (regularRun && playerRunFrames.Length > 0)
                    sprite = Frame(playerRunFrames, playerRunCycle, 1f, true);
                else
                    sprite = ResolvePlayer("run_near");
                if (sprite != null && player.sprite != sprite) player.sprite = sprite;
                float stride = Mathf.Sin(playerRunCycle * Mathf.PI);
                // Authored frames now provide the foot/arm changes. The small
                // movement below only plants the animation in the moving world.
                // The authored sheet contains one side-change direction. Mirror
                // that same clean sequence for the opposite lane.
                player.flipX = playerMotion == PlayerMotion.ChangingLane &&
                    laneMoveDirection < 0 && playerSideFrames.Length > 0;
                player.transform.localRotation = regularRun
                    ? Quaternion.Euler(0f, 0f, stride * 4f)
                    : Quaternion.identity;
                Vector3 p = player.transform.localPosition;
                float finalLaneX = LaneX(lane);
                if (playerMotion == PlayerMotion.ChangingLane)
                {
                    float progress = Mathf.Clamp01(laneAnimationTime / Mathf.Max(0.05f, laneChangeDuration));
                    float eased = progress * progress * (3f - 2f * progress);
                    p.x = Mathf.Lerp(laneAnimationStartX, finalLaneX, eased);
                    if (progress >= 1f) p.x = finalLaneX;
                }
                else p.x = finalLaneX;
                // Chase sprites use a bottom-centre pivot. The authored jump
                // frames are combined with a real world-space arc so the jump
                // clears obstacles instead of only changing the drawing.
                if (playerMotion == PlayerMotion.Jumping)
                {
                    float jumpProgress = Mathf.Clamp01(playerMotionTime / Mathf.Max(0.1f, playerJumpDuration));
                    p.y = Mathf.Sin(jumpProgress * Mathf.PI) * playerJumpHeight;
                }
                else p.y = regularRun ? Mathf.Abs(stride) * 0.13f : 0f;
                // The player is the only non-chaser object allowed to advance
                // toward the camera. It settles at a stable gameplay depth.
                p.z = Mathf.MoveTowards(p.z, playerZ, 1.2f * dt);
                player.transform.localPosition = p;
            }
            if (greve != null)
            {
                // New-sheet performance states: sing at distance, fly toward the
                // player in the middle of the chase, and clown around when the
                // player ducks. Old-sheet near/reach poses still finish the attack.
                Sprite sprite;
                bool sillyDuck = false;
                if (chase >= 0.84f) sprite = ResolveGreve("reach");
                else if (playerMotion == PlayerMotion.Ducking && greveDuckReactFrames.Length > 0)
                {
                    sprite = Frame(greveDuckReactFrames, playerMotionTime, 6f, true);
                    sillyDuck = true;
                }
                else if (chase >= 0.72f) sprite = ResolveGreve("chase_near");
                else if (chase >= 0.56f && greveFlyFrames.Length > 0)
                    sprite = FrameProgress(greveFlyFrames, Mathf.InverseLerp(0.56f, 0.72f, chase));
                else if (greveSingFrames.Length > 0) sprite = Frame(greveSingFrames, elapsed, 7f, true);
                else sprite = ResolveGreve("chase");
                if (sprite != null && greve.sprite != sprite)
                    greve.sprite = sprite;

                // Perspective already contributes to apparent size, but the
                // authored chase also calls for an unmistakable far/near read.
                // Recalculate from the current frame every tick (no accumulated
                // scaling), small in the distance and imposing near the player.
                float chaseEase = chase * chase * (3f - 2f * chase);
                float distanceScale = Mathf.Lerp(0.78f, 1.32f, chaseEase);
                SizeSpriteToHeight(greve, greveHeight * distanceScale * (sillyDuck ? 1.05f : 1f));
                Vector3 p = greve.transform.localPosition;
                p.x = Mathf.Lerp(p.x, lateral * laneSpacing * 0.65f, 1f - Mathf.Exp(-4f * dt));
                p.y = 0.25f + Mathf.Sin(elapsed * (sillyDuck ? 8f : 2f)) * (sillyDuck ? 0.18f : 0.08f);
                p.z = Mathf.Lerp(greveFarZ, greveNearZ, chase);
                greve.transform.localPosition = p;
                greve.transform.localRotation = sillyDuck
                    ? Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 9f) * 7f)
                    : Quaternion.identity;
            }
        }

        private static Sprite FrameProgress(Sprite[] frames, float progress)
        {
            if (frames == null || frames.Length == 0) return null;
            int index = Mathf.Min(frames.Length - 1,
                Mathf.FloorToInt(Mathf.Clamp01(progress) * frames.Length));
            return frames[index];
        }

        private static Sprite Frame(Sprite[] frames, float time, float framesPerSecond, bool pingPong)
        {
            if (frames == null || frames.Length == 0) return null;
            if (frames.Length == 1) return frames[0];
            int step = Mathf.FloorToInt(Mathf.Max(0f, time) * framesPerSecond);
            if (!pingPong) return frames[step % frames.Length];
            int span = frames.Length * 2 - 2;
            int index = step % span;
            if (index >= frames.Length) index = span - index;
            return frames[index];
        }

        private void TickHazards(float dt)
        {
            if (WorldPaused) return;
            for (int i = hazards.Count - 1; i >= 0; i--)
            {
                RunHazard hazard = hazards[i];
                hazard.root.localPosition += Vector3.forward * (worldSpeed * dt);
                if (hazard.warning != null)
                {
                    hazard.warningTime += dt;
                    float pulse = 0.82f + Mathf.Abs(Mathf.Sin(hazard.warningTime * 7f)) * 0.18f;
                    hazard.warning.transform.localScale = hazard.warningBaseScale * pulse;
                    Color warningColor = hazard.warning.color;
                    warningColor.a = 0.62f + Mathf.Abs(Mathf.Sin(hazard.warningTime * 7f)) * 0.38f;
                    hazard.warning.color = warningColor;

                    // The marker warns first, then disappears once the physical
                    // obstacle itself has travelled clearly into the corridor.
                    if (hazard.root.localPosition.z > cameraPosition.z + 6f)
                        hazard.warning.gameObject.SetActive(false);
                }
                if (!hazard.resolved && hazard.root.localPosition.z >= player.transform.localPosition.z - 0.35f)
                {
                    hazard.resolved = true;
                    ResolveHazard(hazard, Avoided(hazard));
                }
                if (hazard.root.localPosition.z > 82f)
                {
                    if (hazard.root != null) Destroy(hazard.root.gameObject);
                    if (hazard.warning != null) Destroy(hazard.warning.gameObject);
                    hazards.RemoveAt(i);
                }
            }
            hazardTimer -= dt;
            if (hazardTimer > 0f) return;
            hazardTimer = Mathf.Lerp(2.8f, 1.5f, chase);
            SpawnHazard(hazardCounter++);
        }

        private void SpawnHazard(int index)
        {
            bool jump = index % 2 == 0;
            Lane obstacleLane = (Lane)(index % 3);
            GameObject root = new GameObject(jump ? "Jump obstacle" : "Lane obstacle");
            root.transform.SetParent(hazardRoot, false);
            // Obstacles belong to the environment: they enter from just behind
            // the camera and move away from it on the same +Z axis as the track.
            root.transform.localPosition = new Vector3(LaneX(obstacleLane), 0f, cameraPosition.z - 1.5f);
            RunHazard hazard = new RunHazard { root = root.transform, lane = obstacleLane, jump = jump };
            BuildHazardWarning(hazard);
            if (jump)
            {
                GameObject obstacle = CreatePrimitive("Low obstacle", PrimitiveType.Cube, root.transform,
                    new Vector3(0f, 0.55f, 0f), new Vector3(2.2f, 1.1f, 0.9f), themeStyles[requestedTheme].accentMaterial);
                obstacle.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            }
            else
            {
                CreatePrimitive("Tall obstacle", PrimitiveType.Cube, root.transform,
                    new Vector3(0f, 1.25f, 0f), new Vector3(2.0f, 2.5f, 1.2f), themeStyles[requestedTheme].accentMaterial);
            }
            hazards.Add(hazard);
        }

        private void BuildHazardWarning(RunHazard hazard)
        {
            Sprite warningSprite = GreveChaseSprites.Hazard(hazard.jump ? "haz_20" : "haz_10");
            if (warningSprite == null) return;

            GameObject go = new GameObject(hazard.jump ? "Jump warning" : "Lane warning");
            go.transform.SetParent(hazardRoot, false);
            go.transform.localPosition = new Vector3(LaneX(hazard.lane), 0.18f, playerZ + 3.2f);
            go.transform.localRotation = worldCamera.transform.localRotation;
            SpriteRenderer warning = go.AddComponent<SpriteRenderer>();
            warning.sprite = warningSprite;
            warning.sortingOrder = 40;
            SizeSpriteToHeight(warning, 1.35f);
            hazard.warning = warning;
            hazard.warningBaseScale = go.transform.localScale;
        }

        private bool Avoided(RunHazard hazard)
        {
            bool sameLane = Mathf.Abs(lateral * laneSpacing - LaneX(hazard.lane)) < laneSpacing * 0.45f;
            if (!sameLane) return true;
            return hazard.jump && currentDodge == DodgeAction.Jump;
        }

        private void ResolveHazard(RunHazard hazard, bool avoided)
        {
            if (hazard.warning != null) hazard.warning.gameObject.SetActive(false);
            if (avoided) { chase = Mathf.Clamp01(chase - 0.05f); return; }
            chase = Mathf.Clamp01(chase + catchOnHit);
            BeginMotion(PlayerMotion.Hit);
            currentDodge = DodgeAction.None;
            laneMoveDirection = 0;
            worldSpeed = 0f;
            Renderer[] renderers = hazard.root.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++) renderers[i].material.color = new Color(0.9f, 0.15f, 0.12f);
        }

        /// <summary>Streams a new theme in on recycled far segments without interrupting play.</summary>
        public void RequestTheme(EnvironmentTheme theme) { requestedTheme = theme; }

        private void TickThemeSequence()
        {
            if (elapsed >= kitchenTransitionAt && requestedTheme == EnvironmentTheme.CastleCorridor)
                RequestTheme(EnvironmentTheme.Kitchen);
            if (!cycleAllThemes) return;
            if (elapsed >= kitchenTransitionAt + 34f) RequestTheme(EnvironmentTheme.Passage);
            else if (elapsed >= kitchenTransitionAt + 17f) RequestTheme(EnvironmentTheme.GreatHall);
        }

        private float LaneX(Lane value) { return ((int)value - 1) * laneSpacing; }

        private void StartMusic()
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/Music/GreveGastsJakt");
            if (clip == null) clip = Resources.Load<AudioClip>("Audio/CustomRideMusic");
            if (clip == null) return;
            music = gameObject.AddComponent<AudioSource>();
            music.clip = clip; music.loop = true; music.playOnAwake = false; music.volume = 0.8f; music.spatialBlend = 0f; music.Play();
        }

        private static void EnsureInputManager()
        {
            if (KinectKidsInputManager.Instance == null)
                new GameObject("KinectKidsInputManager").AddComponent<KinectKidsInputManager>();
        }

        private Sprite ResolvePlayer(string pose)
        {
            Sprite sprite = GreveChaseSprites.Player(pose);
            if (sprite == null) sprite = GreveChaseSprites.Player("run_near");
            if (sprite == null) sprite = GreveChaseSprites.Player("idle");
            return sprite;
        }

        private Sprite ResolveGreve(string pose)
        {
            Sprite sprite = GreveChaseSprites.Greve(pose);
            if (sprite == null) sprite = GreveChaseSprites.Greve("chase");
            if (sprite == null) sprite = GreveChaseSprites.Greve("idle");
            return sprite;
        }

        private static void SizeSpriteToHeight(SpriteRenderer renderer, float height)
        {
            if (renderer == null || renderer.sprite == null) return;
            float spriteHeight = renderer.sprite.bounds.size.y;
            float scale = spriteHeight > 0.001f ? height / spriteHeight : 1f;
            renderer.transform.localScale = Vector3.one * scale;
        }

        internal static GameObject CreatePrimitive(string objectName, PrimitiveType type, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return go;
        }

        private GUIStyle hud;
        private void OnGUI()
        {
            if (hud == null)
            {
                hud = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 40, 16, 30), fontStyle = FontStyle.Bold };
                hud.normal.textColor = Color.white;
            }
            GUI.Label(new Rect(30, 24, 1050, 34), "RUM: " + requestedTheme + "  |  BANA: " + lane +
                "  |  Spring (W), byt bana (A/D), center (S), hoppa (Space)", hud);
        }

        private void OnDestroy()
        {
            if (music != null) music.Stop();
            body?.Dispose();
        }

        private sealed class RunHazard
        {
            public Transform root;
            public Lane lane;
            public bool jump;
            public bool resolved;
            public SpriteRenderer warning;
            public Vector3 warningBaseScale;
            public float warningTime;
        }

        internal sealed class ThemeStyle
        {
            public readonly EnvironmentTheme theme;
            public readonly Material floorMaterial, wallMaterial, ceilingMaterial, accentMaterial;
            public readonly Color lightColor;
            public readonly string decorationA, decorationB;
            public ThemeStyle(EnvironmentTheme theme, Material floor, Material wall, Material ceiling, Material accent,
                Color lightColor, string decorationA, string decorationB)
            {
                this.theme = theme; floorMaterial = floor; wallMaterial = wall; ceilingMaterial = ceiling;
                accentMaterial = accent; this.lightColor = lightColor; this.decorationA = decorationA; this.decorationB = decorationB;
            }
        }
    }

    /// <summary>A reusable physical corridor segment that only translates on the Z axis.</summary>
    internal sealed class EnvironmentSegment3D : MonoBehaviour
    {
        private readonly List<Renderer> floorRenderers = new List<Renderer>();
        private readonly List<Renderer> wallRenderers = new List<Renderer>();
        private readonly List<Renderer> ceilingRenderers = new List<Renderer>();
        private readonly List<Renderer> accentRenderers = new List<Renderer>();
        private readonly List<SpriteRenderer> decorations = new List<SpriteRenderer>();
        private Light practicalLight;
        private int variant;
        private float halfWidth;
        public CorridorRunnerDirector.EnvironmentTheme Theme { get; private set; }

        public void Build(float length, float width, float height, int segmentVariant)
        {
            variant = segmentVariant;
            halfWidth = width * 0.5f;
            Material placeholder = MaterialFactory.Solid(Color.gray);
            floorRenderers.Add(AddBox("Floor", new Vector3(0f, -0.12f, 0f), new Vector3(width, 0.24f, length + 0.08f), placeholder));
            wallRenderers.Add(AddBox("Left wall", new Vector3(-width * 0.5f, height * 0.5f, 0f), new Vector3(0.24f, height, length + 0.08f), placeholder));
            wallRenderers.Add(AddBox("Right wall", new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(0.24f, height, length + 0.08f), placeholder));
            ceilingRenderers.Add(AddBox("Ceiling", new Vector3(0f, height + 0.12f, 0f), new Vector3(width, 0.24f, length + 0.08f), placeholder));

            float boundaryZ = -length * 0.5f + 0.24f;
            float pillarX = halfWidth - 0.55f;
            accentRenderers.Add(AddBox("Monumental arch left pillar", new Vector3(-pillarX, height * 0.5f, boundaryZ),
                new Vector3(0.82f, height, 0.72f), placeholder));
            accentRenderers.Add(AddBox("Monumental arch right pillar", new Vector3(pillarX, height * 0.5f, boundaryZ),
                new Vector3(0.82f, height, 0.72f), placeholder));
            accentRenderers.Add(AddBox("Arch left shoulder", new Vector3(-halfWidth + 2.0f, height - 0.95f, boundaryZ),
                new Vector3(3.2f, 1.9f, 0.72f), placeholder));
            accentRenderers.Add(AddBox("Arch right shoulder", new Vector3(halfWidth - 2.0f, height - 0.95f, boundaryZ),
                new Vector3(3.2f, 1.9f, 0.72f), placeholder));
            accentRenderers.Add(AddBox("Arch crown", new Vector3(0f, height - 0.28f, boundaryZ),
                new Vector3(width, 0.62f, 0.72f), placeholder));

            GameObject lightGo = new GameObject("Theme practical light");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, height - 0.8f, 0f);
            practicalLight = lightGo.AddComponent<Light>();
            practicalLight.type = LightType.Point; practicalLight.range = length * 0.9f; practicalLight.intensity = 1.1f;
            practicalLight.shadows = LightShadows.None;
        }

        public void ApplyTheme(CorridorRunnerDirector.ThemeStyle style)
        {
            Theme = style.theme;
            SetMaterials(floorRenderers, style.floorMaterial); SetMaterials(wallRenderers, style.wallMaterial);
            SetMaterials(ceilingRenderers, style.ceilingMaterial); SetMaterials(accentRenderers, style.accentMaterial);
            practicalLight.color = style.lightColor;
            ClearDecorations();
            float side = variant % 2 == 0 ? -1f : 1f;
            AddWallDecoration(style.decorationA, side, -3.2f, 4.2f, 2.7f);
            AddWallDecoration(style.decorationB, -side, 3.8f, 5.4f, 3.1f);
            AddFloorFog(variant % 2 == 0 ? "new_fog_back" : "new_fog_front", variant % 2 == 0 ? -1.5f : 2.0f);
            AddThemeProp(style.theme, side);
        }

        private Renderer AddBox(string objectName, Vector3 position, Vector3 scale, Material material)
        {
            return CorridorRunnerDirector.CreatePrimitive(objectName, PrimitiveType.Cube, transform, position, scale, material).GetComponent<Renderer>();
        }

        private void AddWallDecoration(string spriteKey, float side, float z, float y, float height)
        {
            Sprite sprite = spriteKey.StartsWith("haz_") ? GreveChaseSprites.Hazard(spriteKey) : GreveChaseSprites.Environment(spriteKey);
            if (sprite == null) return;
            GameObject go = new GameObject("Wall plane " + spriteKey);
            go.transform.SetParent(transform, false);
            // The sprite plane lies in the wall's YZ plane instead of turning to
            // face the camera. A small inset prevents z-fighting with the wall.
            go.transform.localPosition = new Vector3(side * (halfWidth - 0.16f), y, z);
            go.transform.localRotation = Quaternion.Euler(0f, side < 0f ? 90f : -90f, 0f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = 10;
            float spriteHeight = sprite.bounds.size.y;
            go.transform.localScale = Vector3.one * (spriteHeight > 0.001f ? height / spriteHeight : 1f);
            decorations.Add(renderer);
        }

        private void AddFloorFog(string spriteKey, float z)
        {
            Sprite sprite = GreveChaseSprites.Environment(spriteKey);
            if (sprite == null) return;
            GameObject go = new GameObject("Low floor fog " + spriteKey);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.06f, z);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 6;
            float width = sprite.bounds.size.x;
            float scale = width > 0.001f ? (halfWidth * 1.75f) / width : 1f;
            go.transform.localScale = Vector3.one * scale;
            renderer.color = new Color(0.72f, 0.78f, 1f, variant % 2 == 0 ? 0.28f : 0.38f);
            decorations.Add(renderer);
        }

        private void AddThemeProp(CorridorRunnerDirector.EnvironmentTheme theme, float side)
        {
            string key;
            float height;
            switch (theme)
            {
                case CorridorRunnerDirector.EnvironmentTheme.GreatHall:
                    key = variant % 2 == 0 ? "new_armour" : "new_banner";
                    height = variant % 2 == 0 ? 4.8f : 5.8f;
                    break;
                case CorridorRunnerDirector.EnvironmentTheme.Kitchen:
                    key = variant % 2 == 0 ? "new_barrel" : "new_crate";
                    height = variant % 2 == 0 ? 2.2f : 2.0f;
                    break;
                case CorridorRunnerDirector.EnvironmentTheme.Passage:
                    key = variant % 2 == 0 ? "new_rubble" : "new_rubble_pillar";
                    height = variant % 2 == 0 ? 1.8f : 2.6f;
                    break;
                default:
                    key = variant % 3 == 0 ? "new_armour" : "new_pillar";
                    height = variant % 3 == 0 ? 4.5f : 4.2f;
                    break;
            }

            Sprite sprite = GreveChaseSprites.Environment(key);
            if (sprite == null) return;
            GameObject go = new GameObject("Floor prop " + key);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(side * (halfWidth - 1.15f), 0f, 0.4f);
            go.transform.localRotation = Quaternion.Euler(0f, side < 0f ? 8f : -8f, 0f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 8;
            float spriteHeight = sprite.bounds.size.y;
            float scale = spriteHeight > 0.001f ? height / spriteHeight : 1f;
            go.transform.localScale = Vector3.one * scale;
            decorations.Add(renderer);
        }

        private void ClearDecorations()
        {
            for (int i = 0; i < decorations.Count; i++) if (decorations[i] != null) Destroy(decorations[i].gameObject);
            decorations.Clear();
        }

        private static void SetMaterials(List<Renderer> renderers, Material material)
        {
            for (int i = 0; i < renderers.Count; i++) renderers[i].sharedMaterial = material;
        }
    }
}
