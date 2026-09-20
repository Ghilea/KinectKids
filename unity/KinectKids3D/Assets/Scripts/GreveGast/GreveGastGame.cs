using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public sealed class GreveGastGame : MonoBehaviour
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
        private bool paused;
        private bool debug;
        private bool skeletonMode;
        private GreveGastAction previousBodyAction;
        private float jumpStartedAt = -10f;
        private int jumpObstacleCounter;
        private int currentLane;
        private bool laneGestureArmed = true;
        private string section = "Intro";
        private GUIStyle hugeStyle;
        private GUIStyle titleStyle;
        private GUIStyle panelStyle;
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
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
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
            if (paused || body == null || song == null) return;

            body.Update();
            // The intro may reveal the corridor during its final camera pullback,
            // but the actual world must remain completely still until SPRING.
            if (song.SongTime < song.GameplayStartTime)
            {
                moon.intensity = 2.9f + Mathf.Sin(song.SongTime * 1.4f) * 0.12f;
                return;
            }
            if (body.Current == GreveGastAction.Jump && previousBodyAction != GreveGastAction.Jump)
                jumpStartedAt = Time.time;
            previousBodyAction = body.Current;
            lastActions[(int)body.Current] = song.SongTime;
            AnimateCharacters();
            ScrollCorridor();
            UpdateObstacles();
            ResolveActions();
            moon.intensity = 2.9f + Mathf.Sin(song.SongTime * 2.3f) * 0.24f;
        }

        private void AnimateCharacters()
        {
            if (Mathf.Abs(body.HorizontalDelta) < 0.08f) laneGestureArmed = true;
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                currentLane = Mathf.Max(-1, currentLane - 1);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                currentLane = Mathf.Min(1, currentLane + 1);
            else if (laneGestureArmed && body.HorizontalDelta < -0.16f)
            {
                currentLane = Mathf.Max(-1, currentLane - 1);
                laneGestureArmed = false;
            }
            else if (laneGestureArmed && body.HorizontalDelta > 0.16f)
            {
                currentLane = Mathf.Min(1, currentLane + 1);
                laneGestureArmed = false;
            }
            // Kameran tittar tillbaka langs banan och speglar varlds-X.
            // Negativ kroppslutning ska fortfarande synas som vanster pa skarmen.
            float targetX = -currentLane * LaneSpacing;
            float runBob = body.Current == GreveGastAction.Run
                ? Mathf.Abs(Mathf.Sin(song.SongTime * 10f)) * 0.09f : 0f;
            float jumpProgress = (Time.time - jumpStartedAt) / JumpVisualDuration;
            float jumpHeight = jumpProgress >= 0f && jumpProgress <= 1f
                ? Mathf.Sin(jumpProgress * Mathf.PI) * 2.65f : 0f;
            float targetY = jumpHeight > 0f ? jumpHeight
                : body.Current == GreveGastAction.Duck ? 0.08f : runBob;
            player.position = Vector3.Lerp(player.position, new Vector3(targetX, targetY, PlayerZ),
                1f - Mathf.Exp(-8f * Time.deltaTime));
            player.localScale = Vector3.Lerp(player.localScale,
                body.Current == GreveGastAction.Duck ? new Vector3(1.12f, 0.55f, 1.12f) : Vector3.one,
                1f - Mathf.Exp(-10f * Time.deltaTime));
            float stride = Mathf.Sin(song.SongTime * 10f) * 48f;
            playerLeftArm.localRotation = Quaternion.Euler(stride, 0f, 0f);
            playerRightArm.localRotation = Quaternion.Euler(-stride, 0f, 0f);
            playerLeftLeg.localRotation = Quaternion.Euler(-stride * 0.72f, 0f, 0f);
            playerRightLeg.localRotation = Quaternion.Euler(stride * 0.72f, 0f, 0f);
            if (skeletonLeftArm != null)
            {
                skeletonLeftArm.localRotation = playerLeftArm.localRotation;
                skeletonRightArm.localRotation = playerRightArm.localRotation;
                skeletonLeftLeg.localRotation = playerLeftLeg.localRotation;
                skeletonRightLeg.localRotation = playerRightLeg.localRotation;
            }

            // Spelaren springer mot kameran. Jagarfiguren ligger bakom och far
            // aldrig lagga sig mellan kameran och spelarfiguren.
            float lyricApproach = song.SongTime < scriptedApproachUntil ? 10f : 0f;
            float greveZ = Mathf.Lerp(-42f, -25f, chase) + lyricApproach;
            Vector3 target = new Vector3(Mathf.Sin(song.SongTime * 1.4f) * 1.3f,
                chaserGroundY + Mathf.Sin(song.SongTime * 2.2f) * 0.10f, greveZ);
            greve.position = Vector3.Lerp(greve.position, target, 1f - Mathf.Exp(-3f * Time.deltaTime));
            if (drawnGreveAnimation != null) drawnGreveAnimation.SetChaseDistance(chase);
        }

        private void ScrollCorridor()
        {
            float corridorLength = corridor.Count * 12f;
            float backLimit = 18f - (corridor.Count - 1) * 12f;
            for (int i = 0; i < corridor.Count; i++)
            {
                Transform segment = corridor[i];
                // Banmarkeringarna kommer fran nederkanten och flyttas upp mot
                // spelarens position. En ny del matas in fran forgrunden.
                segment.position += Vector3.back * (CorridorSpeed * Time.deltaTime);
                // Ateranvand delen tillrackligt langt bakom kameran sa att de
                // tre vagarna alltid fortsatter hela vagen ned till bildkanten.
                if (segment.position.z < backLimit)
                    segment.position += Vector3.forward * corridorLength;
            }
        }

        private void OnWarning(GreveGastCue cue)
        {
            GreveGastAction action = ParseAction(cue.action);
            if (action != GreveGastAction.None)
            {
                if (cue.time < song.GameplayStartTime || song.SongTime < song.GameplayStartTime) return;
                warningSymbol = Symbol(action);
                warningUntil = cue.time + cue.duration;
                effects.PlayOneShot(warningTone);
                SpawnObstacle(cue, action);
                if (chase > 0.68f) PlayChaserReach();
            }
            else if (cue.kind == "scare" && cue.time >= song.GameplayStartTime)
            {
                warningUntil = cue.time + 0.35f;
                warningSymbol = "!";
            }
        }

        private void OnCue(GreveGastCue cue)
        {
            GreveGastAction action = ParseAction(cue.action);
            if (action != GreveGastAction.None && cue.time >= song.GameplayStartTime)
            {
                pending.Add(new PendingAction { Cue = cue });
                if (action == GreveGastAction.Run) SetChaserRunning(true);
            }
            if (greveAnimation != null || drawnGreveAnimation != null)
            {
                switch (cue.kind)
                {
                    case "gast_run":
                        if (cue.time >= song.GameplayStartTime) SetChaserRunning(true);
                        break;
                    case "gast_closer":
                        if (cue.time >= song.GameplayStartTime)
                        {
                            SetChaserRunning(true);
                            scriptedApproachUntil = song.SongTime + Mathf.Max(2f, cue.duration);
                        }
                        break;
                    case "gast_stumble":
                        PlayChaserStumble();
                        break;
                    case "gast_dance":
                        PlayChaserDance();
                        break;
                    case "gast_reach":
                        PlayChaserReach();
                        break;
                    case "gast_catch":
                        PlayChaserCatch();
                        break;
                    case "gast_laugh":
                        PlayChaserLaugh();
                        break;
                    case "gast_surprise":
                        PlayChaserSurprise();
                        break;
                }
            }
            if (cue.kind == "reveal") PlayChaserSurprise();
            if ((cue.kind == "reveal" || cue.kind == "scare")
                && cue.time >= song.GameplayStartTime)
            {
                chase = Mathf.Clamp01(chase + 0.07f);
                if (cue.kind == "scare") PlayChaserStumble();
                if (cue.kind == "scare") SpawnAtmosphereScare(cue);
            }
        }

        private void ResolveActions()
        {
            float now = song.SongTime;
            foreach (PendingAction item in pending)
            {
                if (item.Resolved) continue;
                GreveGastAction expected = ParseAction(item.Cue.action);
                bool matched = body.Current == expected || lastActions[(int)expected] >= item.Cue.time - 0.42f;
                if (matched)
                {
                    item.Resolved = true;
                    chase = Mathf.Clamp01(chase - 0.11f);
                    feedback = "★";
                    feedbackColor = new Color(0.35f, 1f, 0.66f);
                    feedbackUntil = now + 0.75f;
                    PlayChaserStumble();
                }
                else if (now > item.Cue.time + item.Cue.duration)
                {
                    item.Resolved = true;
                    chase = Mathf.Clamp01(chase + 0.12f);
                    feedback = "!";
                    feedbackColor = new Color(1f, 0.34f, 0.28f);
                    feedbackUntil = now + 0.75f;
                    if (chase > 0.9f) PlayChaserCatch();
                    else PlayChaserReach();
                }
            }
        }

        private void SpawnObstacle(GreveGastCue cue, GreveGastAction action)
        {
            GameObject root = new GameObject("Hinder " + cue.id);
            if (action == GreveGastAction.Jump)
            {
                jumpObstacleCounter++;
                if ((jumpObstacleCounter & 1) == 0)
                {
                    Cube("Golvgrop", root.transform, new Vector3(0, 0.035f, 0), new Vector3(23.5f, 0.06f, 4.2f), pit);
                    Cube("Trasig framkant", root.transform, new Vector3(0, 0.10f, 2.08f), new Vector3(23.5f, 0.18f, 0.24f), stone);
                    Cube("Trasig bakkant", root.transform, new Vector3(0, 0.10f, -2.08f), new Vector3(23.5f, 0.18f, 0.24f), stone);
                    for (int lane = -1; lane <= 1; lane++)
                        ImportedModelFactory.Create("Models/KenneyGraveyard/rocks-tall", root.transform,
                            "Rasade stenar vid grop", new Vector3(lane * LaneSpacing, 0.30f, 2.0f),
                            1.25f, Quaternion.Euler(0f, lane * 17f, 0f));
                }
                else
                {
                    Cube("Nedfallen takbjalk", root.transform, new Vector3(0, 0.48f, 0),
                        new Vector3(23.5f, 0.72f, 0.85f), hauntedWood);
                    for (int lane = -1; lane <= 1; lane++)
                        ImportedModelFactory.Create("Models/KenneyGraveyard/debris-wood", root.transform,
                            "Trasigt virke", new Vector3(lane * LaneSpacing, 0.42f, 0.15f),
                            1.6f, Quaternion.Euler(0f, lane * 21f, 0f));
                }
            }
            else if (action == GreveGastAction.Duck)
            {
                Cube("Hangande slottsbjalk", root.transform, new Vector3(0, 2.22f, 0),
                    new Vector3(23.5f, 0.72f, 1.0f), hauntedWood);
                for (int x = -10; x <= 10; x += 10)
                    Cube("Rostig kedja", root.transform, new Vector3(x, 5.3f, 0),
                        new Vector3(0.13f, 5.8f, 0.13f), rustedIron);
                ImportedModelFactory.Create("Models/KenneyGraveyard/lantern-candle", root.transform,
                    "Svangande lykta", new Vector3(0f, 1.62f, 0f), 1.15f,
                    Quaternion.Euler(0f, 180f, 0f));
            }
            else if (action == GreveGastAction.Left)
            {
                // Skarmens vanstra fil ar world +X eftersom kameran ser bakat.
                AddLaneBarricade(root.transform, 0f, 0);
                AddLaneBarricade(root.transform, -LaneSpacing, 1);
            }
            else if (action == GreveGastAction.Right)
            {
                AddLaneBarricade(root.transform, 0f, 2);
                AddLaneBarricade(root.transform, LaneSpacing, 3);
            }
            else
                return;
            root.transform.position = new Vector3(0f, 0f, ObstacleStartZ);
            if (drawingPowers != null)
            {
                global::GreveGast2D.GreveGastDrawnPower power = action == GreveGastAction.Jump
                    && root.transform.Find("Golvgrop") != null
                    ? global::GreveGast2D.GreveGastDrawnPower.DrawHole
                    : action == GreveGastAction.Duck
                        ? global::GreveGast2D.GreveGastDrawnPower.DrawWall
                        : global::GreveGast2D.GreveGastDrawnPower.DrawObstacle;
                drawingPowers.DrawIntoWorld(root, greve, power);
            }
            float remaining = Mathf.Max(0.5f, cue.time - song.SongTime);
            obstacles.Add(new Obstacle
            {
                Cue = cue,
                Object = root,
                Speed = (ObstacleStartZ - PlayerZ) / remaining
            });
        }

        private void AddLaneBarricade(Transform parent, float x, int variant)
        {
            string path;
            float size;
            switch (variant % 4)
            {
                case 0:
                    path = "Models/KenneyGraveyard/coffin-old";
                    size = 3.9f;
                    break;
                case 1:
                    path = "Models/KenneyGraveyard/gravestone-cross-large";
                    size = 3.7f;
                    break;
                case 2:
                    path = "Models/KenneyGraveyard/gravestone-debris";
                    size = 3.5f;
                    break;
                default:
                    path = "Models/KenneyGraveyard/rocks-tall";
                    size = 3.8f;
                    break;
            }
            ImportedModelFactory.Create(path, parent, "Slottsbråte som blockerar filen",
                new Vector3(x, 1.35f, 0f), size,
                Quaternion.Euler(0f, 180f + variant * 13f, 0f));
            Cube("Korslagda plankor A", parent, new Vector3(x, 1.25f, -0.15f),
                new Vector3(4.8f, 0.28f, 0.34f), hauntedWood).transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            Cube("Korslagda plankor B", parent, new Vector3(x, 1.25f, -0.12f),
                new Vector3(4.8f, 0.28f, 0.34f), hauntedWood).transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        }

        private void UpdateObstacles()
        {
            float now = song.SongTime;
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                Obstacle obstacle = obstacles[i];
                if (obstacle.Object == null) { obstacles.RemoveAt(i); continue; }
                // Samma tydliga riktning som korridoren: fran fonden mot skarmen.
                obstacle.Object.transform.position += Vector3.back * (obstacle.Speed * Time.deltaTime);
                if (obstacle.Object.transform.position.z < PlayerZ - 10f
                    || now > obstacle.Cue.time + 2.5f)
                {
                    Destroy(obstacle.Object);
                    obstacles.RemoveAt(i);
                }
            }
        }

        private void TogglePause()
        {
            paused = !paused;
            if (song != null) song.TogglePause();
            if (ambience != null)
            {
                if (paused) ambience.Pause(); else ambience.UnPause();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            float now = song != null ? song.SongTime : 0f;
            if (drawnIntro != null && drawnIntro.enabled) return;
            if (now < 3.0f)
                GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 100), "GREVE GASTS TECKNINGSJAKT", titleStyle);
            if (now < 3.2f)
                GUI.Label(new Rect(0, Screen.height * 0.72f, Screen.width, 60), "Ror hela kroppen i takt med musiken", panelStyle);
            if (!string.IsNullOrEmpty(warningSymbol) && now <= warningUntil)
                GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 190), warningSymbol, hugeStyle);
            if (now <= feedbackUntil)
            {
                Color old = GUI.color;
                GUI.color = feedbackColor;
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 180), feedback, hugeStyle);
                GUI.color = old;
            }

            GUI.Box(new Rect(24, 22, 470, 38), GUIContent.none);
            Color previous = GUI.color;
            GUI.color = Color.Lerp(new Color(0.3f, 0.95f, 0.7f), new Color(0.95f, 0.18f, 0.3f), chase);
            GUI.DrawTexture(new Rect(28, 26, 462 * chase, 30), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(32, 25, 454, 30), "GREVE GAST KOMMER NARMARE", panelStyle);

            if (debug) DrawDebug(now);
            if (paused) DrawPause();
        }

        private void DrawDebug(float now)
        {
            GUI.Box(new Rect(20, 64, 430, 270), "TIDSLINJEDEBUG (F2)");
            GUI.Label(new Rect(35, 92, 350, 90),
                "Tid: " + now.ToString("0.00") + " / " + (song?.Source?.clip?.length ?? 0f).ToString("0.00") +
                "\nSektion: " + section + "\nKropp: " + body.Current +
                "  hojd " + body.HeightDelta.ToString("+0.00;-0.00") +
                "  sida " + body.HorizontalDelta.ToString("+0.00;-0.00") +
                "\n" + body.Status);
            if (GUI.Button(new Rect(35, 188, 82, 30), "Spela/paus")) song.TogglePause();
            if (GUI.Button(new Rect(123, 188, 72, 30), "-0.10")) song.Seek(now - 0.10f);
            if (GUI.Button(new Rect(201, 188, 72, 30), "+0.10")) song.Seek(now + 0.10f);
            if (GUI.Button(new Rect(279, 188, 102, 30), "Nasta cue"))
            {
                GreveGastCue next = song.NextCue();
                if (next != null) song.Seek(Mathf.Max(0, next.time - next.warning - 0.3f));
            }
            GUI.Label(new Rect(35, 226, 340, 35), "F3 = forsta refrangen  |  PgDn = nasta handelse");
            if (greveAnimation != null || drawnGreveAnimation != null)
            {
                if (GUI.Button(new Rect(35, 270, 58, 30), "Idle")) SetChaserRunning(false);
                if (GUI.Button(new Rect(97, 270, 58, 30), "Run")) SetChaserRunning(true);
                if (GUI.Button(new Rect(159, 270, 62, 30), "Reach")) PlayChaserReach();
                if (GUI.Button(new Rect(225, 270, 72, 30), "Stumble")) PlayChaserStumble();
                if (GUI.Button(new Rect(301, 270, 62, 30), "Laugh")) PlayChaserLaugh();
                if (GUI.Button(new Rect(367, 270, 62, 30), "Dance")) PlayChaserDance();
                if (GUI.Button(new Rect(301, 306, 62, 30), "Catch")) PlayChaserCatch();
                if (GUI.Button(new Rect(367, 306, 62, 30), "Surprise")) PlayChaserSurprise();
            }
        }

        private void DrawPause()
        {
            float w = 420f, h = 335f;
            Rect rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(rect, "PAUS");
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 60, 300, 48), "FORTSATT")) TogglePause();
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 120, 300, 48), "STARTA OM"))
            {
                song.Seek(0f);
                chase = 0.5f;
                currentLane = 0;
                laneGestureArmed = true;
                TogglePause();
            }
            string playerView = skeletonMode ? "SPELARE: SKELETT" : "SPELARE: FIGUR";
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 180, 300, 48), playerView))
                TogglePlayerView();
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 240, 300, 48), "TILL GEMENSAM MENY"))
                LauncherReturnService.ReturnToLauncher();
        }

        private void AddHauntedDecoration(Transform segment, int index)
        {
            int side = (index & 1) == 0 ? -1 : 1;
            float x = side * 11.6f;

            // Pelare, nischer och trasiga ramar fran Spokjaktens slottsmiljo.
            Cube("Fuktig stenpelare", segment, new Vector3(side * 13.75f, 4.6f, 0f),
                new Vector3(0.85f, 8.8f, 1.15f), stone);
            if (index % 3 == 0)
            {
                Material oldWood = DarkRideWorld.TexturedMaterial(new Color(0.28f, 0.13f, 0.055f),
                    HauntedTextureFactory.OldWood(90 + index), 0f);
                Cube("Gammal tavla", segment, new Vector3(side * 14.05f, 6.1f, 0.3f),
                    new Vector3(0.18f, 2.6f, 2.1f), oldWood);
                Cube("Morkt portratt", segment, new Vector3(side * 13.93f, 6.1f, 0.3f),
                    new Vector3(0.10f, 1.95f, 1.5f), purple);
            }

            if (index % 4 == 0)
            {
                GameObject armorRoot = new GameObject("Hemsokt riddarrustning");
                armorRoot.transform.SetParent(segment, false);
                ImportedModelFactory.Create("Models/QuaterniusKnight/KnightCharacter", armorRoot.transform,
                    "Animerad rustning", new Vector3(x, 1.65f, 0f), 3.3f,
                    Quaternion.Euler(0f, 180f, 0f), "idle", "stand");
                HauntedProp.Attach(armorRoot, HauntedMotion.Bob, 0.035f, 0.75f);
            }
            else if (index % 4 == 1)
            {
                string[] graves =
                {
                    "Models/KenneyGraveyard/gravestone-broken",
                    "Models/KenneyGraveyard/gravestone-cross-large",
                    "Models/KenneyGraveyard/gravestone-decorative"
                };
                ImportedModelFactory.Create(graves[index % graves.Length], segment, "Gammal gravsten",
                    new Vector3(x, 1.05f, 0f), 2.5f, Quaternion.Euler(0f, 180f, side * 4f));
            }
            else if (index % 4 == 2)
            {
                GameObject swarm = new GameObject("Fladdermoss i taket");
                swarm.transform.SetParent(segment, false);
                for (int batIndex = 0; batIndex < 3; batIndex++)
                {
                    GameObject batRoot = new GameObject("Fladdermus " + batIndex);
                    batRoot.transform.SetParent(swarm.transform, false);
                    ImportedModelFactory.Create("Models/Quaternius/Bat", batRoot.transform,
                        "Animerad fladdermus", new Vector3(x + batIndex * -side * 0.75f,
                            11.5f + batIndex * 0.45f, batIndex * 0.5f), 1.25f,
                        Quaternion.Euler(0f, 180f, 0f), "fly", "flying", "idle");
                    HauntedProp.Attach(batRoot, HauntedMotion.Flutter, 0.30f, 2.4f + batIndex * 0.35f);
                }
            }
            else
            {
                GameObject ghostRoot = new GameObject("Spoke i slottsgangen");
                ghostRoot.transform.SetParent(segment, false);
                ImportedModelFactory.Create("Models/Quaternius/Ghost", ghostRoot.transform,
                    "Smygande spoke", new Vector3(x, 3.0f, 0f), 3.1f,
                    Quaternion.Euler(0f, 180f, 0f), "fly", "idle");
                HauntedProp.Attach(ghostRoot, HauntedMotion.Bob, 0.32f, 1.35f);
            }

            if (index % 5 == 3)
            {
                GameObject spiderRoot = new GameObject("Takspindel");
                spiderRoot.transform.SetParent(segment, false);
                ImportedModelFactory.Create("Models/Quaternius/Spider", spiderRoot.transform,
                    "Animerad takspindel", new Vector3(side * 5.5f, 14.7f, -2f), 2.4f,
                    Quaternion.Euler(180f, 0f, 0f), "walk", "attack", "idle");
                HauntedProp.Attach(spiderRoot, HauntedMotion.Flutter, 0.12f, 1.8f);
            }
        }

        private void SpawnAtmosphereScare(GreveGastCue cue)
        {
            GameObject scare = new GameObject("Tidsstyrd skramsel - " + cue.id);
            scare.transform.SetParent(transform, false);
            float side = cue.id != null && cue.id.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0 ? -1f : 1f;
            GameObject ghost = ImportedModelFactory.Create("Models/Quaternius/Ghost", scare.transform,
                "Spoke som kastar sig fram", new Vector3(side * 8.5f, 3.2f, -1.5f), 4.2f,
                Quaternion.Euler(0f, 180f, 0f), "fly", "attack", "idle");
            if (ghost == null)
                ghost = Cube("Spokskugga", scare.transform, new Vector3(side * 8.5f, 3.2f, -1.5f),
                    new Vector3(2.2f, 4.4f, 0.4f), purple);
            HauntedProp.Attach(scare, HauntedMotion.Flutter, 0.85f, 3.8f);
            AudioClip moan = Resources.Load<AudioClip>("Audio/SFX/Creatures/ghost_moan_03");
            if (moan != null) effects.PlayOneShot(moan, 0.65f);
            Destroy(scare, Mathf.Max(2.2f, cue.duration + 1.1f));
        }

        private void AddTorch(Transform parent, float x, float z)
        {
            GameObject torch = Cube("Fackla", parent, new Vector3(x, 2.25f, z), new Vector3(0.12f, 0.8f, 0.12f), floor);
            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Laga";
            flame.transform.SetParent(torch.transform, false);
            flame.transform.localPosition = new Vector3(0, 0.65f, 0);
            flame.transform.localScale = new Vector3(2.2f, 0.6f, 2.2f);
            flame.GetComponent<Renderer>().material = MakeMaterial(new Color(1f, 0.38f, 0.04f), new Color(1f, 0.18f, 0.01f));
            Light light = flame.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 17f;
            light.intensity = 4.25f;
            light.color = new Color(1f, 0.38f, 0.12f);
            HauntedProp.Attach(flame, HauntedMotion.Flicker, 0f, UnityEngine.Random.Range(7f, 11f));
        }

        private void BuildPlayerCharacter()
        {
            Material clothes = MakeMaterial(new Color(0.08f, 0.48f, 0.88f));
            Material skin = MakeMaterial(new Color(1f, 0.68f, 0.48f));
            Material dark = MakeMaterial(new Color(0.035f, 0.025f, 0.055f));
            Material shoes = MakeMaterial(new Color(0.12f, 0.07f, 0.055f));

            player = new GameObject("Spelaren - springer mot kameran").transform;
            player.position = new Vector3(0f, 0f, PlayerZ);
            player.rotation = Quaternion.Euler(0f, 180f, 0f);
            playerAvatarVisual = new GameObject("Figur");
            playerAvatarVisual.transform.SetParent(player, false);
            GameObject bodyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyObject.name = "Kropp";
            bodyObject.transform.SetParent(playerAvatarVisual.transform, false);
            bodyObject.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            bodyObject.transform.localScale = new Vector3(0.62f, 0.68f, 0.44f);
            bodyObject.GetComponent<Renderer>().material = clothes;
            Destroy(bodyObject.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Huvud med ansikte mot kameran";
            head.transform.SetParent(playerAvatarVisual.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.08f, -0.02f);
            head.transform.localScale = new Vector3(0.84f, 0.9f, 0.78f);
            head.GetComponent<Renderer>().material = skin;
            Destroy(head.GetComponent<Collider>());
            FacePart("Vanster oga", head.transform, new Vector3(-0.19f, 0.10f, -0.47f), new Vector3(0.16f, 0.20f, 0.07f), dark);
            FacePart("Hoger oga", head.transform, new Vector3(0.19f, 0.10f, -0.47f), new Vector3(0.16f, 0.20f, 0.07f), dark);
            FacePart("Leende", head.transform, new Vector3(0f, -0.20f, -0.49f), new Vector3(0.31f, 0.07f, 0.06f), dark);

            playerLeftArm = Limb("Vanster arm", playerAvatarVisual.transform, new Vector3(-0.56f, 1.45f, 0f), 0.72f, skin, false);
            playerRightArm = Limb("Hoger arm", playerAvatarVisual.transform, new Vector3(0.56f, 1.45f, 0f), 0.72f, skin, false);
            playerLeftLeg = Limb("Vanster ben", playerAvatarVisual.transform, new Vector3(-0.24f, 0.58f, 0f), 0.82f, shoes, true);
            playerRightLeg = Limb("Hoger ben", playerAvatarVisual.transform, new Vector3(0.24f, 0.58f, 0f), 0.82f, shoes, true);

            BuildSkeletonCharacter();
            skeletonMode = PlayerPrefs.GetInt("GreveGastPlayerView", 0) == 1;
            ApplyPlayerView();
        }

        private void BuildSkeletonCharacter()
        {
            Material bones = MakeMaterial(new Color(0.32f, 0.9f, 1f), new Color(0.08f, 0.42f, 0.62f));
            playerSkeletonVisual = new GameObject("Skelett");
            playerSkeletonVisual.transform.SetParent(player, false);
            FacePart("Huvud", playerSkeletonVisual.transform, new Vector3(0f, 2.08f, 0f), new Vector3(0.58f, 0.62f, 0.40f), bones);
            Cube("Ryggrad", playerSkeletonVisual.transform, new Vector3(0f, 1.22f, 0f), new Vector3(0.13f, 1.15f, 0.13f), bones);
            Cube("Axlar", playerSkeletonVisual.transform, new Vector3(0f, 1.62f, 0f), new Vector3(1.15f, 0.12f, 0.12f), bones);
            Cube("Hoft", playerSkeletonVisual.transform, new Vector3(0f, 0.68f, 0f), new Vector3(0.58f, 0.12f, 0.12f), bones);
            skeletonLeftArm = ThinLimb("Vanster skelettarm", playerSkeletonVisual.transform, new Vector3(-0.56f, 1.58f, 0f), 0.82f, bones);
            skeletonRightArm = ThinLimb("Hoger skelettarm", playerSkeletonVisual.transform, new Vector3(0.56f, 1.58f, 0f), 0.82f, bones);
            skeletonLeftLeg = ThinLimb("Vanster skelettben", playerSkeletonVisual.transform, new Vector3(-0.20f, 0.68f, 0f), 0.88f, bones);
            skeletonRightLeg = ThinLimb("Hoger skelettben", playerSkeletonVisual.transform, new Vector3(0.20f, 0.68f, 0f), 0.88f, bones);
        }

        private static Transform ThinLimb(string limbName, Transform parent, Vector3 joint, float length, Material material)
        {
            Transform pivot = new GameObject(limbName + " led").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = joint;
            Cube(limbName, pivot, new Vector3(0f, -length * 0.5f, 0f), new Vector3(0.11f, length, 0.11f), material);
            return pivot;
        }

        private void TogglePlayerView()
        {
            skeletonMode = !skeletonMode;
            PlayerPrefs.SetInt("GreveGastPlayerView", skeletonMode ? 1 : 0);
            PlayerPrefs.Save();
            ApplyPlayerView();
        }

        private void ApplyPlayerView()
        {
            if (playerAvatarVisual != null) playerAvatarVisual.SetActive(!skeletonMode);
            if (playerSkeletonVisual != null) playerSkeletonVisual.SetActive(skeletonMode);
        }

        private static Transform Limb(string limbName, Transform parent, Vector3 joint, float length,
            Material material, bool leg)
        {
            Transform pivot = new GameObject(limbName + " led").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = joint;
            GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            limb.name = limbName;
            limb.transform.SetParent(pivot, false);
            limb.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            limb.transform.localScale = new Vector3(leg ? 0.24f : 0.18f, length, leg ? 0.30f : 0.20f);
            limb.GetComponent<Renderer>().material = material;
            Destroy(limb.GetComponent<Collider>());
            return pivot;
        }

        private static void FacePart(string partName, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = material;
            Destroy(part.GetComponent<Collider>());
        }

        private static GameObject Cube(string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().material = material;
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return cube;
        }

        private static Material MakeMaterial(Color color, Color? emission = null)
        {
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = color };
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            return material;
        }

        private static AudioClip CreateTone()
        {
            const int rate = 22050;
            float[] samples = new float[rate / 7];
            for (int i = 0; i < samples.Length; i++)
            {
                float envelope = 1f - i / (float)samples.Length;
                samples[i] = Mathf.Sin(i * Mathf.PI * 2f * 720f / rate) * envelope * 0.34f;
            }
            AudioClip clip = AudioClip.Create("Varningssignal", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static GreveGastAction ParseAction(string value)
        {
            switch (value)
            {
                case "run": return GreveGastAction.Run;
                case "jump": return GreveGastAction.Jump;
                case "duck": return GreveGastAction.Duck;
                case "left": return GreveGastAction.Left;
                case "right": return GreveGastAction.Right;
                default: return GreveGastAction.None;
            }
        }

        private static string Symbol(GreveGastAction action)
        {
            switch (action)
            {
                case GreveGastAction.Jump: return "↑";
                case GreveGastAction.Duck: return "↓";
                case GreveGastAction.Left: return "←";
                case GreveGastAction.Right: return "→";
                case GreveGastAction.Run: return "⚡";
                default: return string.Empty;
            }
        }

        private void SetChaserRunning(bool running)
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.SetRunning(running);
            if (greveAnimation != null) greveAnimation.SetRun(running);
        }

        private void PlayChaserReach()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlayReach();
            if (greveAnimation != null) greveAnimation.PlayReach();
        }

        private void PlayChaserStumble()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlayStumble();
            if (greveAnimation != null) greveAnimation.PlayStumble();
        }

        private void PlayChaserLaugh()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlayLaugh();
            else if (greveAnimation != null) greveAnimation.PlayDance();
        }

        private void PlayChaserDance()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlayDance();
            if (greveAnimation != null) greveAnimation.PlayDance();
        }

        private void PlayChaserCatch()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlayCatch();
            if (greveAnimation != null) greveAnimation.PlayCatch();
        }

        private void PlayChaserSurprise()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlaySurprise();
            else if (greveAnimation != null) greveAnimation.PlayDance();
        }

        private void EnsureStyles()
        {
            if (hugeStyle != null) return;
            hugeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 6, 72, 150),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.75f, 0.1f) }
            };
            titleStyle = new GUIStyle(hugeStyle) { fontSize = Mathf.Clamp(Screen.height / 13, 42, 78) };
            panelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 42, 16, 28),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private void OnDestroy()
        {
            if (body != null) body.Dispose();
        }
    }
}
