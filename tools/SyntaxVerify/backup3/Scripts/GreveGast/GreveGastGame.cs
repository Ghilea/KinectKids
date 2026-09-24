using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class GreveGastGame : MonoBehaviour
    {
        private sealed class Obstacle
        {
            public GreveGastCue Cue;
            public GameObject Object;
            public float Speed;
        }

        private sealed class PendingAction
        {
            public GreveGastCue Cue;
            public bool Resolved;
        }

        private readonly List<Transform> corridor = new List<Transform>();
        private readonly List<Obstacle> obstacles = new List<Obstacle>();
        private readonly List<PendingAction> pending = new List<PendingAction>();
        private readonly float[] lastActions = new float[6];
        private GreveGastSongController song;
        private GreveGastBodyInput body;
        private Transform player;
        private Transform playerLeftArm;
        private Transform playerRightArm;
        private Transform playerLeftLeg;
        private Transform playerRightLeg;
        private GameObject playerAvatarVisual;
        private GameObject playerSkeletonVisual;
        private Transform skeletonLeftArm;
        private Transform skeletonRightArm;
        private Transform skeletonLeftLeg;
        private Transform skeletonRightLeg;
        private Transform greve;
        private global::GreveGast.GreveGastAnimationDriver greveAnimation;
        private global::GreveGast2D.GreveGastDrawnController drawnGreveAnimation;
        private global::GreveGast2D.GreveGastDrawnIntro drawnIntro;
        private global::GreveGast2D.GreveGastDrawingPowers drawingPowers;
        private float chaserGroundY = -0.8f;
        private Light moon;
        private AudioSource effects;
        private AudioSource ambience;
        private AudioClip warningTone;
        private Material stone;
        private Material floor;
        private Material purple;
        private Material danger;
        private Material pit;
        private Material hauntedWood;
        private Material rustedIron;
        private string warningSymbol;
        private float warningUntil;
        private float feedbackUntil;
        private string feedback;
        private Color feedbackColor;
        private float chase = 0.5f;
        private float scriptedApproachUntil;
        private float activeRunUntil = -1f;
        private float runSpeedBoost;
        private bool paused;
        private bool debug;
        private bool skeletonMode;
        private GreveGastAction previousBodyAction;
        private float jumpStartedAt = -10f;
        private int jumpObstacleCounter;
        private int currentLane;
        private bool laneGestureArmed = true;
        private float laneNeutralSince = -1f;
        private float laneChangeCooldownUntil;
        private bool caught;
        private float catchReleaseAt;
        private bool catchPausedSong;
        private string section = "Intro";
        private GUIStyle hugeStyle;
        private GUIStyle titleStyle;
        private GUIStyle panelStyle;
        private GUIStyle hudStyle;
        private GUIStyle catchStyle;
        private const float CorridorSpeed = 7f;
        // Hindren borjar i forgrunden, mellan kameran och spelaren, och aker
        // fran nederkanten upp mot spelarfiguren.
        private const float ObstacleStartZ = 18.5f;
        private const float PlayerZ = 0f;
        private const float CameraZ = 22f;
        private const float LaneSpacing = 7.5f;
        private const float JumpVisualDuration = 0.82f;

        private void Start()
        {
            name = "Greve Gasts teckningsjakt";
            if (ApplyDefaultFullscreen()) Invoke(nameof(ForceFullscreen), 0.75f);
            BuildWorld();
            body = new GreveGastBodyInput();
            body.Start();
            song = gameObject.AddComponent<GreveGastSongController>();
            song.Warning += OnWarning;
            song.Cue += OnCue;
            song.SectionChanged += value => section = value.name;
            song.Begin();
            if (drawnGreveAnimation != null)
            {
                drawnIntro = gameObject.AddComponent<global::GreveGast2D.GreveGastDrawnIntro>();
                drawnIntro.Configure(drawnGreveAnimation, song);
            }
        }

        private static bool ApplyDefaultFullscreen()
        {
            if (Application.isEditor) return false;
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], "-screen-fullscreen", StringComparison.OrdinalIgnoreCase)
                    && arguments[index + 1] == "0") return false;
            }
            Resolution resolution = Screen.currentResolution;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow);
            return true;
        }

        private void ForceFullscreen()
        {
            Resolution resolution = Screen.currentResolution;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow);
            Screen.fullScreen = true;
        }

        private void BuildWorld()
        {
            foreach (Camera oldCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Destroy(oldCamera.gameObject);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.fogColor = new Color(0.008f, 0.010f, 0.018f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.040f, 0.047f, 0.067f);
            RenderSettings.ambientEquatorColor = new Color(0.022f, 0.027f, 0.040f);
            RenderSettings.ambientGroundColor = new Color(0.009f, 0.011f, 0.018f);
            RenderSettings.ambientIntensity = 0.48f;
            RenderSettings.reflectionIntensity = 0f;

            Texture2D castleStone = Resources.Load<Texture2D>("Textures/CastleStone");
            Texture2D castleRoad = Resources.Load<Texture2D>("Textures/CastleRoad");
            if (castleStone == null)
                castleStone = HauntedTextureFactory.DampStone(31,
                    new Color(0.16f, 0.18f, 0.20f), new Color(0.012f, 0.018f, 0.025f));
            if (castleRoad == null)
                castleRoad = HauntedTextureFactory.DampStone(51,
                    new Color(0.22f, 0.20f, 0.18f), new Color(0.025f, 0.026f, 0.030f));
            castleStone.wrapMode = TextureWrapMode.Repeat;
            castleRoad.wrapMode = TextureWrapMode.Repeat;
            stone = DarkRideWorld.TexturedMaterial(new Color(0.32f, 0.35f, 0.39f), castleStone, 0.05f);
            floor = DarkRideWorld.TexturedMaterial(new Color(0.39f, 0.31f, 0.25f), castleRoad, 0.03f);
            stone.mainTextureScale = new Vector2(4f, 2f);
            floor.mainTextureScale = new Vector2(3f, 5f);
            purple = MakeMaterial(new Color(0.42f, 0.08f, 0.63f), new Color(0.22f, 0.02f, 0.42f));
            danger = MakeMaterial(new Color(0.75f, 0.1f, 0.16f), new Color(0.45f, 0.02f, 0.04f));
            pit = MakeMaterial(new Color(0.006f, 0.004f, 0.012f));
            hauntedWood = DarkRideWorld.TexturedMaterial(new Color(0.34f, 0.17f, 0.07f),
                HauntedTextureFactory.OldWood(317), 0f);
            rustedIron = DarkRideWorld.TexturedMaterial(new Color(0.27f, 0.25f, 0.23f),
                HauntedTextureFactory.RustedMetal(411), 0.68f);

            var cameraObject = new GameObject("Musikalisk foljkamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            // Bred jaktkamera: spelaren ar liten i bild och hinder syns langt
            // innan de kommer fram, som i musikjaktsreferensen.
            camera.transform.position = new Vector3(0f, 7.5f, CameraZ);
            camera.transform.LookAt(new Vector3(0f, 1.25f, PlayerZ), Vector3.up);
            camera.fieldOfView = 76f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;

            moon = cameraObject.AddComponent<Light>();
            moon.type = LightType.Spot;
            moon.range = 52f;
            moon.spotAngle = 66f;
            moon.intensity = 3.2f;
            moon.color = new Color(0.55f, 0.66f, 1f);

            GameObject ceilingFillObject = new GameObject("Svagt kallt takljus");
            ceilingFillObject.transform.SetParent(transform, false);
            ceilingFillObject.transform.position = new Vector3(0f, 10.5f, 10f);
            Light ceilingFill = ceilingFillObject.AddComponent<Light>();
            ceilingFill.type = LightType.Point;
            ceilingFill.range = 42f;
            ceilingFill.intensity = 0.72f;
            ceilingFill.color = new Color(0.20f, 0.25f, 0.42f);
            ceilingFill.shadows = LightShadows.None;

            for (int i = 0; i < 16; i++)
            {
                GameObject segment = new GameObject("Slottsdel " + i);
                segment.transform.position = new Vector3(0f, 0f, 18f - i * 12f);
                corridor.Add(segment.transform);
                // En sammanhangande grund under de tre vagarna tar bort de
                // svarta springorna och gor korridoren till ett riktigt rum.
                Cube("Sammanhangande stengolv", segment.transform, new Vector3(0f, -0.48f, 0f),
                    new Vector3(29.35f, 0.42f, 12f), stone);
                for (int lane = -1; lane <= 1; lane++)
                {
                    float laneX = lane * LaneSpacing;
                    Cube("Vag " + lane, segment.transform, new Vector3(laneX, -0.25f, 0),
                        new Vector3(6.5f, 0.5f, 12f), floor);
                    Cube("Vagskarv " + lane, segment.transform, new Vector3(laneX, 0.015f, 0),
                        new Vector3(6.5f, 0.035f, 0.15f), stone);
                }
                Cube("Vanster vagg", segment.transform, new Vector3(-14.5f, 8.5f, 0), new Vector3(0.65f, 18f, 12f), stone);
                Cube("Hoger vagg", segment.transform, new Vector3(14.5f, 8.5f, 0), new Vector3(0.65f, 18f, 12f), stone);
                Cube("Stentak", segment.transform, new Vector3(0f, 16.35f, 0f),
                    new Vector3(29.35f, 0.42f, 12f), stone);
                Cube("Takbjalk", segment.transform, new Vector3(0, 16f, -4.5f), new Vector3(29.5f, 0.65f, 0.75f), floor);
                AddTorch(segment.transform, -14.05f, i % 2 == 0 ? 3f : -3f);
                AddTorch(segment.transform, 14.05f, i % 2 == 0 ? -3f : 3f);
                AddHauntedDecoration(segment.transform, i);
            }

            // Kameran star ovanfor den rorliga banans framkant. En fast
            // forgrundsdel gor att golvet alltid fortsatter under kameran och
            // anda ned till bildkanten medan segmenten ateranvands.
            for (int lane = -1; lane <= 1; lane++)
                Cube("Vag under kameran " + lane, transform,
                    new Vector3(lane * LaneSpacing, -0.27f, 28f),
                    new Vector3(6.5f, 0.48f, 20f), floor);
            Cube("Stengolv under kameran", transform, new Vector3(0f, -0.50f, 28f),
                new Vector3(29.35f, 0.42f, 20f), stone);
            Cube("Stentak over kameran", transform, new Vector3(0f, 16.35f, 28f),
                new Vector3(29.35f, 0.42f, 20f), stone);

            BuildPlayerCharacter();

            GameObject drawnGrevePrefab = Resources.Load<GameObject>("GreveGast2D/GreveGastDrawn");
            GameObject imported = drawnGrevePrefab != null ? Instantiate(drawnGrevePrefab, transform, false) : null;
            if (imported != null)
            {
                imported.name = "Greve Gast - tecknad skepnad";
                greve = imported.transform;
                chaserGroundY = 0f;
                greve.position = new Vector3(0f, chaserGroundY, -38f);
                greve.rotation = Quaternion.identity;
                greve.localScale = Vector3.one * 2.4f;
                drawnGreveAnimation = imported.GetComponent<global::GreveGast2D.GreveGastDrawnController>();
                SetChaserRunning(false);
            }
            else
            {
                GameObject grevePrefab = Resources.Load<GameObject>("GreveGastCharacter/GreveGast");
                imported = grevePrefab != null ? Instantiate(grevePrefab, transform, false) : null;
                if (imported != null)
                {
                    imported.name = "Greve Gast - inaktiv reservmodell";
                    greve = imported.transform;
                    greve.position = new Vector3(0f, -0.8f, -38f);
                    greve.rotation = Quaternion.identity;
                    greve.localScale = Vector3.one * 5.2f;
                    greveAnimation = imported.GetComponent<global::GreveGast.GreveGastAnimationDriver>();
                    SetChaserRunning(true);
                }
                else
                {
                    greve = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                    greve.name = "Reservfigur";
                    greve.position = new Vector3(0f, -0.8f, -38f);
                    greve.localScale = Vector3.one * 7f;
                    greve.GetComponent<Renderer>().material = purple;
                }
            }

            effects = gameObject.AddComponent<AudioSource>();
            effects.spatialBlend = 0f;
            effects.volume = 0.45f;
            warningTone = CreateTone();
            drawingPowers = gameObject.AddComponent<global::GreveGast2D.GreveGastDrawingPowers>();
            AudioClip ambienceClip = Resources.Load<AudioClip>("Audio/SFX/Ambience/ambient_horror");
            if (ambienceClip != null)
            {
                ambience = gameObject.AddComponent<AudioSource>();
                ambience.clip = ambienceClip;
                ambience.loop = true;
                ambience.playOnAwake = false;
                ambience.spatialBlend = 0f;
                ambience.volume = 0.11f;
                ambience.Play();
            }
        }

        private void Update()
        {
            if (!global::KinectKids3D.Platform.KinectKidsPlatformRoot.IsActive
                && Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (Input.GetKeyDown(KeyCode.F11))
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.fullScreen = !Screen.fullScreen;
            }
            if (Input.GetKeyDown(KeyCode.F2)) debug = !debug;
            if (Input.GetKeyDown(KeyCode.F4)) TogglePlayerView();
            if (Input.GetKeyDown(KeyCode.F3) && song != null) song.Seek(58f);
            if (Input.GetKeyDown(KeyCode.F6) && song != null) song.TogglePause();
            if (Input.GetKeyDown(KeyCode.PageDown) && song != null)
            {
                GreveGastCue next = song.NextCue();
                if (next != null) song.Seek(Mathf.Max(0f, next.time - next.warning - 0.5f));
            }
            if (caught)
            {
                UpdateCaughtVisual();
                if (Time.unscaledTime >= catchReleaseAt) ReleaseFromCatch();
                return;
            }
            if (paused || body == null || song == null) return;

            body.Update();
            // The intro may reveal the corridor during its final camera pullback,
            // but the actual world must remain completely still until SPRING.
            if (song.SongTime < song.GameplayStartTime)
            {
                moon.intensity = 2.9f + Mathf.Sin(song.SongTime * 1.4f) * 0.12f;
                return;
            }
            UpdateRunChallenge();
            if (body.Current == GreveGastAction.Jump && previousBodyAction != GreveGastAction.Jump)
                jumpStartedAt = Time.time;
            previousBodyAction = body.Current;
            lastActions[(int)body.Current] = song.SongTime;
            AnimateCharacters();
            ScrollCorridor();
            UpdateObstacles();
            ResolveActions();
            if (chase >= 0.995f) TriggerCaught();
            moon.intensity = 2.9f + Mathf.Sin(song.SongTime * 2.3f) * 0.24f;
        }
    }
}