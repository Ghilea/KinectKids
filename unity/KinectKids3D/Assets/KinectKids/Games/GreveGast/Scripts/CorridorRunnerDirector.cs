using System.Collections.Generic;
using UnityEngine;
using KinectKids.Scene25D;
using KinectKids3D;
using KinectKids3D.Platform;
using KinectKids.Games.GreveGast.LivingFog;

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
        [Tooltip("Speed multiplier while running in place or holding Shift.")]
        public float sprintSpeedMultiplier = 1.4f;

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
        public float greveFarZ = 16f;
        public float greveNearZ = 4.8f;
        public float greveHeight = 5.8f;

        [Header("Chase")]
        public float catchOnHit = 0.14f;
        [Tooltip("How quickly Greve falls behind while the player avoids damage.")]
        public float recoverPerSecond = 0.018f;
        [Tooltip("Additional distance gained by running in place or holding Shift.")]
        public float runDrainPerSecond = 0.05f;
        [Tooltip("Extra distance Greve Gast gains while the player ducks.")]
        public float duckPressurePerSecond = 0.11f;
        public float hitPressurePerSecond = 0.14f;

        [Header("Theme demonstration")]
        [Tooltip("Streams Kitchen segments in behind CastleCorridor after this many seconds.")]
        public float kitchenTransitionAt = 10f;
        public bool cycleAllThemes = true;
        public bool showIllustratedVista = false;

        private Camera worldCamera;
        private Transform corridorRoot;
        private Transform hazardRoot;
        private readonly List<EnvironmentSegment3D> segments = new List<EnvironmentSegment3D>();
        private readonly List<RunHazard> hazards = new List<RunHazard>();
        private readonly Dictionary<EnvironmentTheme, ThemeStyle> themeStyles = new Dictionary<EnvironmentTheme, ThemeStyle>();

        private SpriteRenderer player;
        private SpriteRenderer greve;
        private Transform darknessRoot;
        private GastLivingFog livingFogMass;
        private GastFogController livingFogController;
        private Material floorFogVolumeMaterial;
        private Material foregroundFloorFogMaterial;
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
        private bool greveFinalReachPose;
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
        private float hitFlashUntil;
        private const float IntroCrawlEnd = 3.2f;
        private const float IntroLookEnd = 6.2f;
        private const float IntroWalkEnd = 10.2f;
        private const float IntroGreveReveal = 17.5f;
        private const float IntroChaseStart = 22f;
        private const float IntroWallZ = 14.5f;
        private Transform introWall;
        private Light introPoofLight;
        private bool chaseStarted;
        private bool introMusicStarted;
        private float introElapsed;
        private const float IntroFadeDuration = 2.2f;
        private const float FinalStageWarningDelay = 2.5f;
        private const float FinalStageCaptureDelay = 15f;
        private const float CaptureFadeDuration = 2.5f;
        private float finalStageElapsed;
        private float captureFadeElapsed;
        private bool captureStarted;
        private float captureMusicVolume;

        public DodgeAction CurrentDodge => currentDodge;
        public float LateralPosition => Mathf.Clamp(lateral, -1f, 1f);
        public EnvironmentTheme CurrentTheme => requestedTheme;
        private bool WorldPaused => false;

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
            BuildIntroWall();
            BeginIntro();
            StartMusic();
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
            worldCamera.depthTextureMode |= DepthTextureMode.Depth;
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
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 88f;

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
            BuildDarknessFront();
        }

        private void BuildIntroWall()
        {
            introWall = new GameObject("Castle wall with crawl opening").transform;
            introWall.SetParent(transform, false);
            introWall.localPosition = new Vector3(0f, 0f, IntroWallZ);

            ThemeStyle stone = themeStyles[EnvironmentTheme.CastleCorridor];
            float openingWidth = 4.6f;
            float openingHeight = 3.7f;
            float sideWidth = (corridorWidth - openingWidth) * 0.5f;
            float sideX = (corridorWidth + openingWidth) * 0.25f;
            CreatePrimitive("Left stone wall", PrimitiveType.Cube, introWall,
                new Vector3(-sideX, corridorHeight * 0.5f, 0f),
                new Vector3(sideWidth, corridorHeight, 0.9f), stone.wallMaterial);
            CreatePrimitive("Right stone wall", PrimitiveType.Cube, introWall,
                new Vector3(sideX, corridorHeight * 0.5f, 0f),
                new Vector3(sideWidth, corridorHeight, 0.9f), stone.wallMaterial);
            CreatePrimitive("Stone above opening", PrimitiveType.Cube, introWall,
                new Vector3(0f, (corridorHeight + openingHeight) * 0.5f, 0f),
                new Vector3(openingWidth, corridorHeight - openingHeight, 0.9f), stone.wallMaterial);

            // The projecting stones make the opening legible from the fixed camera.
            CreatePrimitive("Left opening jamb", PrimitiveType.Cube, introWall,
                new Vector3(-openingWidth * 0.5f - 0.16f, openingHeight * 0.5f, -0.55f),
                new Vector3(0.38f, openingHeight, 0.42f), stone.accentMaterial);
            CreatePrimitive("Right opening jamb", PrimitiveType.Cube, introWall,
                new Vector3(openingWidth * 0.5f + 0.16f, openingHeight * 0.5f, -0.55f),
                new Vector3(0.38f, openingHeight, 0.42f), stone.accentMaterial);
            CreatePrimitive("Opening lintel", PrimitiveType.Cube, introWall,
                new Vector3(0f, openingHeight + 0.19f, -0.55f),
                new Vector3(openingWidth + 0.7f, 0.38f, 0.42f), stone.accentMaterial);
        }

        private void BeginIntro()
        {
            worldSpeed = 0f;
            if (darknessRoot != null) darknessRoot.gameObject.SetActive(false);
            if (greve != null) greve.gameObject.SetActive(false);
            if (player != null)
            {
                player.transform.localPosition = new Vector3(0f, 0f, IntroWallZ + 0.7f);
                player.sprite = ResolvePlayer("duck");
                SizeSpriteToHeight(player, playerHeight * 0.72f);
            }

            GameObject flash = new GameObject("Greve entrance flash placeholder");
            flash.transform.SetParent(transform, false);
            flash.transform.localPosition = new Vector3(0f, 3.2f, Mathf.Lerp(greveFarZ, greveNearZ, chase));
            introPoofLight = flash.AddComponent<Light>();
            introPoofLight.type = LightType.Point;
            introPoofLight.color = new Color(0.35f, 0.12f, 0.65f);
            introPoofLight.range = 12f;
            introPoofLight.intensity = 0f;
            introPoofLight.shadows = LightShadows.None;
        }

        private void DriveIntro(float dt)
        {
            if (player == null || greve == null) return;
            if (introElapsed >= IntroCrawlEnd && !introMusicStarted)
            {
                introMusicStarted = true;
                if (music != null) music.volume = 0.8f;
            }
            Vector3 position = player.transform.localPosition;
            if (introElapsed < IntroCrawlEnd)
            {
                float progress = Mathf.SmoothStep(0f, 1f, introElapsed / IntroCrawlEnd);
                position.z = Mathf.Lerp(IntroWallZ + 0.7f, IntroWallZ - 1.6f, progress);
                position.y = 0.04f * Mathf.Sin(introElapsed * 9f);
                player.sprite = ResolvePlayer("duck");
                player.flipX = false;
                SizeSpriteToHeight(player, playerHeight * 0.72f);
            }
            else if (introElapsed < IntroLookEnd)
            {
                position.z = IntroWallZ - 1.6f;
                position.y = 0f;
                player.sprite = ResolvePlayer("listen");
                player.flipX = Mathf.Repeat(introElapsed - IntroCrawlEnd, 1.8f) < 0.9f;
                SizeSpriteToHeight(player, playerHeight);
            }
            else if (introElapsed < IntroWalkEnd)
            {
                float progress = Mathf.SmoothStep(0f, 1f,
                    (introElapsed - IntroLookEnd) / (IntroWalkEnd - IntroLookEnd));
                position.z = Mathf.Lerp(IntroWallZ - 1.6f, playerStartZ, progress);
                playerRunCycle += dt * 3.8f;
                player.sprite = playerRunFrames.Length > 0
                    ? Frame(playerRunFrames, playerRunCycle, 1f, true) : ResolvePlayer("idle");
                player.flipX = false;
                SizeSpriteToHeight(player, playerHeight);
            }
            else
            {
                position.z = playerStartZ;
                position.y = 0f;
                float lookTime = introElapsed - IntroWalkEnd;
                bool lookLeft = Mathf.Repeat(lookTime, 5.2f) < 2.6f;
                player.sprite = introElapsed >= IntroGreveReveal + 0.5f
                    ? ResolvePlayer("look_back") : ResolvePlayer("listen");
                player.flipX = lookLeft;
                SizeSpriteToHeight(player, playerHeight);
            }
            player.transform.localPosition = position;

            if (introElapsed >= IntroGreveReveal)
            {
                if (!greve.gameObject.activeSelf) greve.gameObject.SetActive(true);
                float appear = Mathf.SmoothStep(0f, 1f,
                    (introElapsed - IntroGreveReveal) / 0.75f);
                greve.sprite = ResolveGreve("threaten");
                greve.color = new Color(1f, 1f, 1f, appear);
                SizeSpriteToHeight(greve, greveHeight * Mathf.Lerp(0.35f, 1.2f, appear));
                greve.transform.localPosition = new Vector3(0f, 0.38f,
                    Mathf.Lerp(greveFarZ, greveNearZ, chase));
                if (introPoofLight != null)
                    introPoofLight.intensity = 2.5f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(
                        (introElapsed - IntroGreveReveal) / 1.2f));
            }
        }

        private void BeginChase()
        {
            chaseStarted = true;
            elapsed = 0f;
            worldSpeed = idleSpeed;
            playerRunCycle = 0f;
            if (introPoofLight != null) introPoofLight.intensity = 0f;
            if (greve != null) greve.color = Color.white;
            if (darknessRoot != null)
            {
                darknessRoot.gameObject.SetActive(true);
                darknessRoot.localScale = Vector3.one * 0.55f;
                if (livingFogMass != null) livingFogMass.SetVisibility(0f);
            }
        }

        private void BuildDarknessFront()
        {
            darknessRoot = new GameObject("Greve Gasts levande mörkerfront").transform;
            darknessRoot.SetParent(transform, false);
            BuildLivingMass();
            BuildVolumetricFloorFog();
            BuildForegroundFloorFog();
            BuildFloorParticleFog();
        }

        private void BuildLivingMass()
        {
            Shader shader = Resources.Load<Shader>("GastLivingMass");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogError("Greve Gasts living mass shader is missing or unsupported.");
                return;
            }

            GameObject body = new GameObject("GastLivingFog");
            body.transform.SetParent(darknessRoot, false);
            livingFogMass = body.AddComponent<GastLivingFog>();
            livingFogMass.Initialize(shader, corridorWidth, corridorHeight, true);
            livingFogController = body.AddComponent<GastFogController>();
            livingFogController.showTestControls = false;
            livingFogController.aggression = chase;
            livingFogMass.SetAggression(chase);
        }

        private void BuildVolumetricFloorFog()
        {
            // Loading through Resources prevents Unity's player build shader
            // stripping from removing this runtime-created volume material.
            Shader volumeShader = Resources.Load<Shader>("GreveVolumetricFogReference");
            if (volumeShader == null || !volumeShader.isSupported)
                volumeShader = Shader.Find("KinectKids/Greve Volumetric Fog");
            if (volumeShader == null || !volumeShader.isSupported) return;

            floorFogVolumeMaterial = BuildFogVolume("Raymarchad blåvit 3D-golvdimma", transform,
                new Vector3(0f, 1.55f, -3.0f),
                new Vector3(corridorWidth * 1.16f, 4.5f, 14f),
                new Color(0.52f, 0.70f, 1f, 0.48f),
                2.2f,
                0.30f,
                1f,
                2989,
                volumeShader);
        }

        private void BuildForegroundFloorFog()
        {
            Shader volumeShader = Resources.Load<Shader>("GreveVolumetricFogReference");
            if (volumeShader == null || !volumeShader.isSupported)
                volumeShader = Shader.Find("KinectKids/Greve Volumetric Fog");
            if (volumeShader == null || !volumeShader.isSupported) return;

            // This short volume spans the player and the space between the
            // player and camera. It is drawn over the rear mass, then below the
            // character sprites, so the visible bank lives in the foreground.
            foregroundFloorFogMaterial = BuildFogVolume("Blåvit 3D-golvdimma vid spelaren", transform,
                new Vector3(0f, 0.75f, -2.5f),
                new Vector3(corridorWidth * 1.06f, 2.6f, 9f),
                new Color(0.50f, 0.68f, 0.98f, 0.55f),
                1.9f,
                0.22f,
                1f,
                3001,
                volumeShader);
        }

        private static Material BuildFogVolume(string volumeName, Transform parent,
            Vector3 localPosition, Vector3 localScale, Color color, float density,
            float edgeSoftness, float floorMode, int renderQueue, Shader shader)
        {
            GameObject volume = GameObject.CreatePrimitive(PrimitiveType.Cube);
            volume.name = volumeName;
            volume.transform.SetParent(parent, false);
            volume.transform.localPosition = localPosition;
            volume.transform.localRotation = Quaternion.identity;
            volume.transform.localScale = localScale;
            Collider collider = volume.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            Material material = new Material(shader);
            material.name = volumeName + " material";
            material.renderQueue = renderQueue;
            material.SetColor("_FogColor", color);
            material.SetFloat("_Density", density);
            material.SetFloat("_NoiseScale", floorMode > 0.5f ? 0.42f : 0.28f);
            material.SetVector("_NoiseSpeed", floorMode > 0.5f
    ? new Vector4(0.055f, 0.016f, -0.030f, 0f)
    : new Vector4(-0.034f, 0.022f, 0.028f, 0f));
            material.SetFloat("_EdgeSoftness", edgeSoftness);
            material.SetFloat("_FloorMode", floorMode);
            MeshRenderer renderer = volume.GetComponent<MeshRenderer>();
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return material;
        }

        private void BuildDarknessParticleFog()
        {
            if (TryBuildGpuFogLibrary(true)) return;
            GameObject fogObject = new GameObject("Mjuk svart partikelrök");
            fogObject.SetActive(false);
            fogObject.transform.SetParent(darknessRoot, false);
            fogObject.transform.localPosition = new Vector3(0f, corridorHeight * 0.5f, 1.25f);
            fogObject.transform.localScale = new Vector3(
                corridorWidth * 0.55f, corridorHeight * 0.52f, 3.0f);

            ParticleSystem fog = fogObject.AddComponent<ParticleSystem>();
            fog.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = fog.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 7f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5.5f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.32f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.3f, 2.6f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.001f, 0f, 0.004f, 0.48f),
                new Color(0.008f, 0.002f, 0.014f, 0.76f));
            main.maxParticles = 200;

            ParticleSystem.EmissionModule emission = fog.emission;
            emission.rateOverTime = 25f;
            ParticleSystem.ShapeModule shape = fog.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            shape.radiusThickness = 1f;

            ParticleSystem.VelocityOverLifetimeModule velocity = fog.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.18f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.10f);

            ParticleSystem.NoiseModule noise = fog.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.High;
            noise.strength = new ParticleSystem.MinMaxCurve(0.28f, 0.72f);
            noise.frequency = 0.16f;
            noise.scrollSpeed = 0.18f;
            noise.octaveCount = 2;

            ParticleSystem.ColorOverLifetimeModule colorOverLife = fog.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.92f, 0.16f),
                    new GradientAlphaKey(0.72f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLife.color = fade;

            ParticleSystem.SizeOverLifetimeModule sizeOverLife = fog.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(0.45f, 1f), new Keyframe(1f, 1.18f)));

            ParticleSystemRenderer fogRenderer = fog.GetComponent<ParticleSystemRenderer>();
            fogRenderer.material = CreateParticleFogMaterial(fogRenderer, "Greve soft black fog");
            fogRenderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal,
                ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV,
                ParticleSystemVertexStream.StableRandomX
            });
            fogRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            fogRenderer.alignment = ParticleSystemRenderSpace.View;
            fogRenderer.sortingOrder = 19;
            fogObject.SetActive(true);
            fog.Play();
        }

        private void BuildFloorParticleFog()
        {
            if (TryBuildGpuFogLibrary(false)) return;
            GameObject floorFogObject = new GameObject("Kontinuerlig partikelgolvdimma");
            floorFogObject.SetActive(false);
            floorFogObject.transform.SetParent(transform, false);
            floorFogObject.transform.localPosition = new Vector3(0f, 0.24f, -2f);

            ParticleSystem floorFog = floorFogObject.AddComponent<ParticleSystem>();
            floorFog.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = floorFog.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 8f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.14f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.4f, 4.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.62f, 0.72f, 0.96f, 0.12f),
                new Color(0.92f, 0.96f, 1f, 0.23f));
            main.maxParticles = 170;

            ParticleSystem.EmissionModule emission = floorFog.emission;
            emission.rateOverTime = 17f;
            ParticleSystem.ShapeModule shape = floorFog.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(corridorWidth * 0.98f, 0.42f, 9f);
            ParticleSystem.VelocityOverLifetimeModule velocity = floorFog.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.02f);
            ParticleSystem.NoiseModule noise = floorFog.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.High;
            noise.strength = new ParticleSystem.MinMaxCurve(0.06f, 0.20f);
            noise.frequency = 0.12f;
            noise.scrollSpeed = 0.07f;

            ParticleSystem.ColorOverLifetimeModule colorOverLife = floorFog.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.18f),
                    new GradientAlphaKey(0.65f, 0.78f), new GradientAlphaKey(0f, 1f)
                });
            colorOverLife.color = fade;

            ParticleSystemRenderer renderer = floorFog.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleFogMaterial(renderer, "Soft floor particle fog");
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Normal,
                ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV,
                ParticleSystemVertexStream.StableRandomX
            });
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 12;
            floorFogObject.SetActive(true);
            floorFog.Play();
        }

        private bool TryBuildGpuFogLibrary(bool blackFog)
        {
            GameObject prefab = Resources.Load<GameObject>("GPUFog/Ground Fog");
            if (prefab == null) return false;
            if (!blackFog)
            {
                // The raymarched volume supplies real perspective depth.  The
                // library particles now only add rising animated wisps, avoiding
                // the flat horizontal band produced by a dense billboard layer.
                BuildRisingFloorWisps(prefab);
                return true;
            }
            Transform parent = blackFog ? darknessRoot : transform;
            GameObject fogObject = Instantiate(prefab, parent, false);
            fogObject.name = blackFog
                ? "GPU Fog Particles - Grevens svarta mörker"
                : "GPU Fog Particles - blåvit golvdimma";
            fogObject.SetActive(false);
            fogObject.transform.localPosition = blackFog
                ? new Vector3(0f, corridorHeight * 0.50f, 0.72f)
                : new Vector3(0f, 0.20f, 5f);
            fogObject.transform.localRotation = Quaternion.identity;
            fogObject.transform.localScale = Vector3.one;

            ParticleSystem fog = fogObject.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = fogObject.GetComponent<ParticleSystemRenderer>();
            if (fog == null || renderer == null)
            {
                Destroy(fogObject);
                return false;
            }
            fog.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = fog.main;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = blackFog
                ? new ParticleSystem.MinMaxCurve(5.5f, 9f)
                : new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = blackFog
                ? new ParticleSystem.MinMaxCurve(0.04f, 0.20f)
                : new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = blackFog
                ? new ParticleSystem.MinMaxCurve(2.8f, 4.8f)
                : new ParticleSystem.MinMaxCurve(3.0f, 6.2f);
            main.startColor = blackFog
                ? new ParticleSystem.MinMaxGradient(
                    new Color(0f, 0f, 0f, 0.90f),
                    new Color(0.004f, 0.001f, 0.008f, 1f))
                : new ParticleSystem.MinMaxGradient(
                    new Color(0.48f, 0.66f, 1f, 0.22f),
                    new Color(0.88f, 0.96f, 1f, 0.48f));
            main.maxParticles = blackFog ? 180 : 340;
            ParticleSystem.ColorOverLifetimeModule lifetimeColor = fog.colorOverLifetime;
            lifetimeColor.enabled = false;

            ParticleSystem.EmissionModule emission = fog.emission;
            emission.rateOverTime = 8f;
            ParticleSystem.ShapeModule shape = fog.shape;
            shape.shapeType = blackFog ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Box;
            if (blackFog)
            {
                // Keep the emitter inside the corridor.  A huge emitter was
                // clipped by the portal and therefore read as a rectangle.
                shape.radius = 2.0f;
                shape.radiusThickness = 1f;
                fogObject.transform.localScale = new Vector3(1.08f, 1.0f, 0.52f);
            }
            else shape.scale = new Vector3(corridorWidth * 0.98f, 0.36f, 34f);

            ParticleSystem.VelocityOverLifetimeModule velocity = fog.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(blackFog ? -0.10f : -0.18f,
                blackFog ? 0.10f : 0.18f);
            velocity.y = blackFog
                ? new ParticleSystem.MinMaxCurve(-0.05f, 0.12f)
                : new ParticleSystem.MinMaxCurve(0.015f, 0.06f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

            renderer.material = CreateParticleFogMaterial(renderer,
                blackFog ? "Greve GPU black fog" : "GPU blue-white floor fog");
            renderer.renderMode = blackFog
                ? ParticleSystemRenderMode.Billboard
                : ParticleSystemRenderMode.HorizontalBillboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = blackFog ? 19 : 12;
            renderer.enableGPUInstancing = true;
            fogObject.SetActive(true);
            fog.Play();
            if (blackFog) BuildOpaqueBlackFogCore(prefab);
            return true;
        }

        private void BuildRisingFloorWisps(GameObject prefab)
        {
            GameObject wispObject = Instantiate(prefab, transform, false);
            wispObject.name = "GPU Fog Particles - stigande blåvita golvslöjor";
            wispObject.SetActive(false);
            wispObject.transform.localPosition = new Vector3(0f, 0.52f, -1.7f);
            wispObject.transform.localRotation = Quaternion.identity;
            wispObject.transform.localScale = Vector3.one;

            ParticleSystem wisps = wispObject.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = wispObject.GetComponent<ParticleSystemRenderer>();
            if (wisps == null || renderer == null)
            {
                Destroy(wispObject);
                return;
            }

            wisps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = wisps.main;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.0f, 3.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.56f, 0.74f, 1f, 0.19f),
                new Color(0.96f, 0.99f, 1f, 0.32f));
            main.maxParticles = 280;
            ParticleSystem.ColorOverLifetimeModule lifetimeColor = wisps.colorOverLifetime;
            lifetimeColor.enabled = false;

            ParticleSystem.EmissionModule emission = wisps.emission;
            emission.rateOverTime = 32f;
            ParticleSystem.ShapeModule shape = wisps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            // Concentrate the blue wisps around the player. Extending them
            // behind the player lit up the seam under Gast's black mass.
            shape.scale = new Vector3(corridorWidth * 0.98f, 0.50f, 7f);

            ParticleSystem.VelocityOverLifetimeModule velocity = wisps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.01f, 0.045f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.025f, 0.025f);

            renderer.material = CreateParticleFogMaterial(renderer, "GPU blue-white floor wisps");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.sortingOrder = 24;
            renderer.enableGPUInstancing = true;
            wispObject.SetActive(true);
            wisps.Play();

            // A second, raised sheet gives the bank visible body above the
            // floor without letting its particles drift into the ceiling.
            GameObject liftedObject = Instantiate(wispObject, transform, false);
            liftedObject.name = "GPU Fog Particles - högre blåvita dimslöjor";
            liftedObject.SetActive(false);
            liftedObject.transform.localPosition = new Vector3(0f, 1.35f, -1.5f);
            ParticleSystem lifted = liftedObject.GetComponent<ParticleSystem>();
            lifted.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule liftedMain = lifted.main;
            liftedMain.startSize = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
            liftedMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.62f, 0.78f, 1f, 0.11f),
                new Color(0.96f, 0.99f, 1f, 0.21f));
            liftedMain.maxParticles = 150;
            ParticleSystem.EmissionModule liftedEmission = lifted.emission;
            liftedEmission.rateOverTime = 16f;
            ParticleSystem.ShapeModule liftedShape = lifted.shape;
            liftedShape.scale = new Vector3(corridorWidth * 0.92f, 0.42f, 7f);
            ParticleSystem.VelocityOverLifetimeModule liftedVelocity = lifted.velocityOverLifetime;
            liftedVelocity.y = new ParticleSystem.MinMaxCurve(0.005f, 0.025f);
            liftedObject.SetActive(true);
            lifted.Play();

            // A separate shallow bank sits between the camera and the player.
            // It keeps the near floor from becoming an empty stretch while the
            // smaller lifted wisps retain visible gaps and depth.
            GameObject foregroundObject = Instantiate(wispObject, transform, false);
            foregroundObject.name = "GPU Fog Particles - förgrund vid spelaren";
            foregroundObject.SetActive(false);
            foregroundObject.transform.localPosition = new Vector3(0f, 0.42f, -3.3f);
            ParticleSystem foreground = foregroundObject.GetComponent<ParticleSystem>();
            foreground.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule foregroundMain = foreground.main;
            foregroundMain.startSize = new ParticleSystem.MinMaxCurve(2.1f, 3.4f);
            foregroundMain.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.60f, 0.76f, 1f, 0.14f),
                new Color(0.94f, 0.98f, 1f, 0.25f));
            foregroundMain.maxParticles = 135;
            ParticleSystem.EmissionModule foregroundEmission = foreground.emission;
            foregroundEmission.rateOverTime = 18f;
            ParticleSystem.ShapeModule foregroundShape = foreground.shape;
            foregroundShape.scale = new Vector3(corridorWidth * 0.92f, 0.32f, 7f);
            ParticleSystemRenderer foregroundRenderer =
                foregroundObject.GetComponent<ParticleSystemRenderer>();
            foregroundRenderer.sortingOrder = 25;
            foregroundObject.SetActive(true);
            foreground.Play();
        }

        private void BuildOpaqueBlackFogCore(GameObject prefab)
        {
            // A second, tighter layer makes the middle truly coal black.  It
            // still consists of the library's radial/noise particles, so its
            // silhouette stays soft instead of exposing a quad or box edge.
            GameObject coreObject = Instantiate(prefab, darknessRoot, false);
            coreObject.name = "GPU Fog Particles - ogenomskinlig svart kärna";
            coreObject.SetActive(false);
            coreObject.transform.localPosition = new Vector3(0f, corridorHeight * 0.42f, 5.5f);
            coreObject.transform.localRotation = Quaternion.identity;
            coreObject.transform.localScale = new Vector3(1.05f, 1.08f, 0.56f);

            ParticleSystem core = coreObject.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = coreObject.GetComponent<ParticleSystemRenderer>();
            if (core == null || renderer == null)
            {
                Destroy(coreObject);
                return;
            }

            core.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = core.main;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.003f, 0.018f);
            main.startSize = new ParticleSystem.MinMaxCurve(14f, 18f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0f, 0f, 0f, 1f), Color.black);
            main.maxParticles = 30;
            ParticleSystem.ColorOverLifetimeModule lifetimeColor = core.colorOverLifetime;
            lifetimeColor.enabled = false;

            ParticleSystem.EmissionModule emission = core.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)6)
            });
            ParticleSystem.ShapeModule shape = core.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.20f;
            shape.radiusThickness = 1f;

            ParticleSystem.VelocityOverLifetimeModule velocity = core.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.03f, 0.09f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

            renderer.material = CreateParticleFogMaterial(renderer, "Greve GPU black core fog");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.sortingOrder = 18;
            renderer.enableGPUInstancing = true;
            coreObject.SetActive(true);
            core.Play();

            BuildBlackSurfaceFog(prefab, "svart rök längs golvet",
                new Vector3(0f, 0.28f, -1.5f),
                new Vector3(corridorWidth * 1.08f, 0.65f, 22f), true);
            BuildBlackSurfaceFog(prefab, "svart rök längs taket",
                new Vector3(0f, corridorHeight - 0.55f, -4f),
                new Vector3(corridorWidth * 0.88f, 0.55f, 12f));
            BuildBlackSurfaceFog(prefab, "svart rök längs vänster vägg",
                new Vector3(-corridorWidth * 0.47f, corridorHeight * 0.50f, -4f),
                new Vector3(0.65f, corridorHeight * 0.88f, 12f));
            BuildBlackSurfaceFog(prefab, "svart rök längs höger vägg",
                new Vector3(corridorWidth * 0.47f, corridorHeight * 0.50f, -4f),
                new Vector3(0.65f, corridorHeight * 0.88f, 12f));
        }

        private void BuildBlackSurfaceFog(GameObject prefab, string fogName,
            Vector3 localPosition, Vector3 emitterScale, bool horizontal = false)
        {
            GameObject edgeObject = Instantiate(prefab, darknessRoot, false);
            edgeObject.name = "GPU Fog Particles - " + fogName;
            edgeObject.SetActive(false);
            edgeObject.transform.localPosition = localPosition;
            edgeObject.transform.localRotation = Quaternion.identity;
            edgeObject.transform.localScale = Vector3.one;

            ParticleSystem fog = edgeObject.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = edgeObject.GetComponent<ParticleSystemRenderer>();
            if (fog == null || renderer == null)
            {
                Destroy(edgeObject);
                return;
            }

            fog.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = fog.main;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.008f, 0.05f);
            main.startSize = horizontal
                ? new ParticleSystem.MinMaxCurve(6.5f, 11.5f)
                : new ParticleSystem.MinMaxCurve(5.5f, 9.5f);
            main.maxParticles = horizontal ? 240 : 160;
            
            ParticleSystem.ColorOverLifetimeModule lifetimeColor = fog.colorOverLifetime;
            lifetimeColor.enabled = false;

            ParticleSystem.EmissionModule emission = fog.emission;
            emission.rateOverTime = horizontal ? 28f : 18f;
            ParticleSystem.ShapeModule shape = fog.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = emitterScale;

            ParticleSystem.VelocityOverLifetimeModule velocity = fog.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.06f, 0.10f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);

            renderer.material = CreateParticleFogMaterial(renderer, "Greve GPU black edge fog");
            renderer.renderMode = horizontal
                ? ParticleSystemRenderMode.HorizontalBillboard
                : ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.sortingOrder = 17;
            renderer.enableGPUInstancing = true;
            edgeObject.SetActive(true);
            fog.Play();
        }

        private Material CreateParticleFogMaterial(ParticleSystemRenderer renderer, string materialName)
        {
            Material template = Resources.Load<Material>("GPUFog/Fog Large");
            Shader shader = Shader.Find("Mirza Beig/GPU Fog (Built-In)");
            if (template == null && shader == null) return renderer.sharedMaterial;
            Material material = template != null ? new Material(template) : new Material(shader);
            material.name = materialName;
            bool blackFog = materialName.StartsWith("Greve");
            bool blackCore = materialName.Contains("core");
            material.SetColor("_Albedo", blackFog
                ? new Color(0.001f, 0f, 0.004f, 0.96f)
                : new Color(0.64f, 0.80f, 1f, 0.62f));
            material.SetFloat("_SimpleNoiseScale", blackFog ? 7.5f : 11f);
            material.SetFloat("_SimplexNoiseScale", blackFog ? 3.2f : 4.5f);
            material.SetFloat("_VoronoiScale", blackFog ? 3.8f : 5.5f);
            material.SetFloat("_SimpleNoiseAmount", blackCore ? 0.12f : (blackFog ? 0.34f : 0.24f));
            material.SetFloat("_SimplexNoiseAmount", blackCore ? 0.10f : (blackFog ? 0.28f : 0.20f));
            material.SetFloat("_VoronoiNoiseAmount", blackCore ? 0.08f : (blackFog ? 0.22f : 0.16f));
            material.SetFloat("_SimpleNoiseRemap", 0f);
            material.SetFloat("_SimplexNoiseRemap", 0f);
            material.SetFloat("_VoronoiNoiseRemap", 0f);
            material.SetFloat("_CombinedNoiseRemap", 0f);
            material.SetVector("_SimpleNoiseAnimation", new Vector4(-0.035f, 0.022f, 0f, 0f));
            material.SetVector("_SimplexNoiseAnimation", new Vector4(0.016f, 0.009f, 0.025f, 0f));
            material.SetVector("_VoronoiNoiseAnimation", new Vector4(-0.012f, 0.008f, 0.018f, 0f));
            // Black smoke must cover geometry, not reveal transparent pockets
            // around it.  Only retain a hairline intersection fade to avoid
            // z-fighting; the blue floor fog keeps a visibly soft intersection.
            material.SetFloat("_SurfaceDepthFade", blackCore ? 0.001f : (blackFog ? 0.015f : 0.08f));
            material.SetFloat("_CameraDepthFadeRange", blackFog ? 1.5f : 0.30f);
            material.SetFloat("_CameraDepthFadeOffset", 0f);
            return material;
        }

        private SpriteRenderer BuildActor(string actorName, Sprite sprite, float height, Vector3 position)
        {
            GameObject go = new GameObject(actorName + " 2.5D Billboard");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            go.transform.localRotation = worldCamera.transform.localRotation;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 30;
            Material actorMaterial = new Material(Shader.Find("Sprites/Default"));
            actorMaterial.name = actorName + " foreground sprite material";
            actorMaterial.renderQueue = 3020;
            renderer.material = actorMaterial;
            SizeSpriteToHeight(renderer, height);
            return renderer;
        }

        private void Update()
        {
            if (body == null) return;
            float dt = Time.deltaTime;
            if (captureStarted)
            {
                captureFadeElapsed += dt;
                if (music != null)
                {
                    music.volume = captureMusicVolume * (1f - Mathf.SmoothStep(0f, 1f,
                        captureFadeElapsed / CaptureFadeDuration));
                    if (captureFadeElapsed >= CaptureFadeDuration && music.isPlaying) music.Stop();
                }
                return;
            }
            body.Update();
            if (!chaseStarted)
            {
                introElapsed += dt;
                DriveIntro(dt);
                if (introElapsed >= IntroChaseStart) BeginChase();
                return;
            }
            elapsed += dt;
            ReadInput(dt);
            DriveWorld(dt);
            StreamSegments(dt);
            MoveIllustratedVista(dt);
            DriveCharacters(dt);
            if (darknessRoot != null)
            {
                float growth = Mathf.SmoothStep(0f, 1f, elapsed / 3f);
                darknessRoot.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, growth);
                if (livingFogMass != null)
                    livingFogMass.SetVisibility(Mathf.SmoothStep(0f, 1f, elapsed / 1.4f));
            }
            TickHazards(dt);
            TickFinalApproach(dt);
            TickThemeSequence();
        }

        private void TickFinalApproach(float dt)
        {
            bool finalStage = chase >= 0.94f || (greveFinalReachPose && chase > 0.86f);
            if (!finalStage)
            {
                finalStageElapsed = 0f;
                return;
            }
            finalStageElapsed += dt;
            if (finalStageElapsed < FinalStageCaptureDelay) return;
            captureStarted = true;
            captureFadeElapsed = 0f;
            captureMusicVolume = music != null ? music.volume : 0f;
            worldSpeed = 0f;
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

            bool sprinting = body.RunEnergy > 0.4f || Input.GetKey(KeyCode.LeftShift) ||
                             Input.GetKey(KeyCode.RightShift);
            bool running = body.Current == GreveGastAction.Run || Input.GetKey(KeyCode.W) || sprinting;
            float targetSpeed = sprinting ? runningSpeed * Mathf.Max(1f, sprintSpeedMultiplier)
                : running ? runningSpeed : idleSpeed;
            worldSpeed = Mathf.Lerp(worldSpeed, targetSpeed, 1f - Mathf.Exp(-4f * dt));
            float retreat = Mathf.Max(0f, recoverPerSecond);
            if (sprinting) retreat += Mathf.Max(0f, runDrainPerSecond);
            chase = Mathf.Clamp01(chase - retreat * dt);
        }

        private void StreamSegments(float dt)
        {
            float move = worldSpeed * dt;
            if (introWall != null && introWall.gameObject.activeSelf)
            {
                introWall.localPosition += Vector3.forward * move;
                if (introWall.localPosition.z > 82f) introWall.gameObject.SetActive(false);
            }
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
                bool flashing = Time.unscaledTime < hitFlashUntil
                    && Mathf.FloorToInt(Time.unscaledTime * 18f) % 2 == 0;
                player.color = flashing ? new Color(1f, 0.25f, 0.25f) : Color.white;
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
                else if (playerMotion == PlayerMotion.Ducking)
                {
                    float slideProgress = Mathf.Clamp01(playerMotionTime / Mathf.Max(0.1f, playerDuckDuration));
                    p.y = -0.16f;
                    p.z = playerZ - Mathf.Sin(slideProgress * Mathf.PI) * 0.42f;
                }
                else p.y = regularRun ? Mathf.Abs(stride) * 0.13f : 0f;
                // The player is the only non-chaser object allowed to advance
                // toward the camera. It settles at a stable gameplay depth.
                p.z = Mathf.MoveTowards(p.z, playerZ, 1.2f * dt);
                if (playerMotion == PlayerMotion.Ducking)
                    p.z = playerZ - Mathf.Sin(Mathf.Clamp01(playerMotionTime / Mathf.Max(0.1f, playerDuckDuration)) * Mathf.PI) * 0.42f;
                player.transform.localPosition = p;
            }
            if (greve != null)
            {
                // Greven måste alltid läsas som en hel figur under jakten. De
                // beskurna sånghuvudena används därför inte här: flygsekvensen
                // bär sången; nära spelaren håller Greve en stadig jaktpose.
                Sprite sprite;
                bool sillyDuck = false;
                bool nearPlayer = chase >= 0.76f;
                // Enter the reaching pose only at the final approach. A wider
                // exit threshold prevents rapid pose switching near the limit.
                if (chase >= 0.94f) greveFinalReachPose = true;
                else if (chase <= 0.86f) greveFinalReachPose = false;
                if (greveFinalReachPose) sprite = ResolveGreve("reach");
                else if (nearPlayer) sprite = ResolveGreve("chase_near");
                else if (playerMotion == PlayerMotion.Ducking && greveDuckReactFrames.Length > 0)
                {
                    sprite = Frame(greveDuckReactFrames, playerMotionTime, 6f, true);
                    sillyDuck = true;
                }
                else if (Mathf.Repeat(elapsed, 2.35f) < 0.38f) sprite = ResolveGreve("threaten");
                else if (greveFlyFrames.Length > 0) sprite = Frame(greveFlyFrames, elapsed, 7.5f, true);
                else sprite = ResolveGreve("chase");
                if (sprite != null && greve.sprite != sprite)
                    greve.sprite = sprite;

                // Perspective already contributes to apparent size, but the
                // authored chase also calls for an unmistakable far/near read.
                // Recalculate from the current frame every tick (no accumulated
                // scaling), small in the distance and imposing near the player.
                float chaseEase = chase * chase * (3f - 2f * chase);
                float distanceScale = Mathf.Lerp(1.05f, 2.08f, chaseEase);
                SizeSpriteToHeight(greve, greveHeight * distanceScale * (sillyDuck ? 1.05f : 1f));
                Vector3 p = greve.transform.localPosition;
                float hoverX = Mathf.Sin(elapsed * 1.15f) * Mathf.Lerp(1.15f, 0.28f, chaseEase);
                p.x = Mathf.Lerp(p.x, lateral * laneSpacing * 0.72f + hoverX, 1f - Mathf.Exp(-4f * dt));
                p.y = 0.38f + Mathf.Sin(elapsed * (sillyDuck ? 8f : 2.1f)) * (sillyDuck ? 0.20f : 0.22f);
                p.z = Mathf.Lerp(greveFarZ, greveNearZ, chase);
                greve.transform.localPosition = p;
                greve.transform.localRotation = Quaternion.Euler(0f, 0f,
                    sillyDuck ? Mathf.Sin(elapsed * 9f) * 7f : -hoverX * 1.8f);
                UpdateDarknessFront(p, chaseEase);
            }
        }

        private void UpdateDarknessFront(Vector3 grevePosition, float chaseEase)
        {
            if (darknessRoot == null) return;
            darknessRoot.localPosition = new Vector3(grevePosition.x * 0.18f, 0f,
                grevePosition.z + Mathf.Lerp(0.55f, 0.22f, chaseEase));
            if (livingFogController != null)
                livingFogController.aggression = 0.45f + chaseEase * 0.55f;
            // Box edges become conspicuous when Gast fills the frame. Let the
            // local wisps remain while the two raymarched sheets dissolve.
            float volumeFade = 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(0.60f, 0.87f, chaseEase));
            if (floorFogVolumeMaterial != null)
                floorFogVolumeMaterial.SetColor("_FogColor",
                    new Color(0.52f, 0.70f, 1f, 0.48f * volumeFade));
            if (foregroundFloorFogMaterial != null)
                foregroundFloorFogMaterial.SetColor("_FogColor",
                    new Color(0.50f, 0.68f, 0.98f, 0.55f * volumeFade));
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
            hitFlashUntil = Time.unscaledTime + 0.48f;
            currentDodge = DodgeAction.None;
            laneMoveDirection = 0;
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
            music.clip = clip; music.loop = true; music.playOnAwake = false; music.volume = 0f; music.spatialBlend = 0f;
            // Keep the soundtrack clock aligned with the scene from frame one.
            // The player hears it only after leaving the wall opening.
            music.Play();
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
            GUI.depth = -100;
            if (hud == null)
            {
                hud = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 40, 16, 30), fontStyle = FontStyle.Bold };
                hud.normal.textColor = Color.white;
            }
            if (!captureStarted)
            {
                if (!chaseStarted)
                    GUI.Label(new Rect(30, 24, 1050, 34), "RUM: " + requestedTheme, hud);
                else
                    GUI.Label(new Rect(30, 24, 1050, 34), "RUM: " + requestedTheme + "  |  BANA: " + lane +
                        "  |  Spring (W), byt bana (A/D), center (S), hoppa (Space)", hud);
            }

            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            if (chaseStarted && !captureStarted && finalStageElapsed >= FinalStageWarningDelay)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);
                GUI.color = new Color(0.8f, 0.02f, 0.05f, 0.05f + pulse * 0.17f);
                GUI.DrawTexture(screen, Texture2D.whiteTexture);
                GUI.color = new Color(0.85f, 0.02f, 0.04f, 0.30f + pulse * 0.46f);
                float border = Mathf.Max(18f, Screen.height * 0.028f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, border), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, Screen.height - border, Screen.width, border), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, 0f, border, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width - border, 0f, border, Screen.height), Texture2D.whiteTexture);
            }

            float black = captureStarted
                ? Mathf.SmoothStep(0f, 1f, captureFadeElapsed / CaptureFadeDuration)
                : !chaseStarted ? 1f - Mathf.SmoothStep(0f, 1f, introElapsed / IntroFadeDuration) : 0f;
            if (black > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, black);
                GUI.DrawTexture(screen, Texture2D.whiteTexture);
            }
            if (captureStarted && captureFadeElapsed > CaptureFadeDuration * 0.55f)
            {
                GUIStyle caughtStyle = new GUIStyle(hud)
                {
                    fontSize = Mathf.Clamp(Screen.height / 12, 46, 100),
                    alignment = TextAnchor.MiddleCenter
                };
                caughtStyle.normal.textColor = new Color(1f, 1f, 1f,
                    Mathf.SmoothStep(0f, 1f, (captureFadeElapsed / CaptureFadeDuration - 0.55f) / 0.45f));
                GUI.color = Color.white;
                GUI.Label(screen, "GREVEN TOG OSS", caughtStyle);
            }
            GUI.color = Color.white;
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
