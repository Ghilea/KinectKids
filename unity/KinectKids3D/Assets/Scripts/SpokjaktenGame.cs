using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D
{
    public sealed class SpokjaktenGame : MonoBehaviour
    {
        private enum GameFlow
        {
            Menu,
            Calibration,
            Countdown,
            Riding
        }

        public static SpokjaktenGame Instance { get; private set; }
        public static float CurrentEffectsVolume => Instance != null ? Instance.effectsVolume : 1f;
        private const float CountdownSeconds = 3f;
        private const float RideSpeed = 2.18f;
        private const float BossStopDistance = 344f;
        private const float QuickEventCueDistance = 4.8f;
        private const float QuickEventActionDistance = 4.15f;
        private const float QuickEventPassedDistance = -0.30f;
        private const float TargetGraceSeconds = 0.48f;
        private static readonly float[] ScareDistances =
        {
            26f, 48f, 68f, 91f, 111f, 132f, 151f, 171f, 188f, 218f, 242f, 270f, 299f, 326f, 338f
        };
        private static readonly float[] EncounterDistances =
        {
            38f, 74f, 104f, 136f, 158f, 181f, 231f, 253f, 282f, 316f, 333f
        };
        private static readonly float[] ZoneDistances = { 6f, 50f, 107f, 149f, 202f, 309f, 334f };
        private static readonly float[] MiniBossDistances = { 82f, 176f, 303f };
        private static readonly string[] ZoneTitles =
        {
            "DEN ÖVERGIVNA BORGEN",
            "DE VAKNANDE GRAVARNA",
            "DEN FÖRBANNADE BORGGÅRDEN",
            "GALLERIET SOM SER DIG",
            "TVÅ VÄGAR – ETT ÖDE",
            "DEN GLÖMDA FÄNGELSEHÅLAN",
            "KONDUKTÖRENS SISTA STATION"
        };
        private readonly List<GhostTarget> targets = new List<GhostTarget>();
        private readonly List<RideHazard> hazards = new List<RideHazard>();
        private readonly Dictionary<int, AimLock> aimLocks = new Dictionary<int, AimLock>();
        private readonly Dictionary<int, ReticleState> reticles = new Dictionary<int, ReticleState>();
        private readonly Dictionary<int, Light> reticleLights = new Dictionary<int, Light>();
        private readonly Dictionary<int, HandFilterState> handFilters = new Dictionary<int, HandFilterState>();
        private readonly Dictionary<long, PoseCalibration> poseCalibrations = new Dictionary<long, PoseCalibration>();
        private readonly Dictionary<int, PoseState> currentPoses = new Dictionary<int, PoseState>();
        private readonly int[] scores = new int[2];
        private readonly int[] lives = new int[2];
        private readonly int[] shots = new int[2];
        private readonly int[] hits = new int[2];
        private readonly int[] kills = new int[2];
        private readonly int[] combos = new int[2];
        private readonly int[] maxCombos = new int[2];
        private readonly int[] rescueProgress = new int[2];
        private readonly int[] leftHandShots = new int[2];
        private readonly int[] rightHandShots = new int[2];
        private readonly float[] lastHitAt = new float[2];
        private Camera rideCamera;
        private IAimProvider aimProvider;
        private float gameTime;
        private float rideDistance;
        private float nextSpawnAt;
        private bool bossSpawned;
        private bool bossBattle;
        private bool bossDefeated;
        private bool finished;
        private bool paused;
        private string inputStatus;
        private Texture2D playerOneRing;
        private Texture2D playerTwoRing;
        private Texture2D whiteTexture;
        private Texture2D goldTexture;
        private Texture2D movementArrow;
        private GUIStyle titleStyle;
        private GUIStyle hudStyle;
        private GUIStyle smallStyle;
        private GUIStyle centerStyle;
        private GUIStyle centeredSmallStyle;
        private AudioSource effects;
        private AudioSource musicSource;
        private AudioSource ambienceSource;
        private AudioClip[] hitSounds;
        private AudioClip[] bossSounds;
        private AudioClip[] castSounds;
        private AudioClip[] movementSuccessSounds;
        private AudioClip[] collisionSounds;
        private AudioClip[] scareSounds;
        private AudioClip[] hazardCueSounds;
        private AudioClip[] chainRattleSounds;
        private AudioClip[] batRushSounds;
        private AudioClip[] phantomSounds;
        private AudioSource ghostVoice;
        private AudioSource environmentVoice;
        private AudioSource announcerVoice;
        private AudioClip duckSuccessVoice;
        private AudioClip dodgeSuccessVoice;
        private AudioClip bossSuccessVoice;
        private AudioClip playerHitVoice;
        private AudioClip wagonHitVoice;
        private float nextAnnouncementAt;
        private AudioClip[] ghostSounds;
        private float nextGhostSoundAt;
        private RideHazard currentHazard;
        private float cameraShakeUntil;
        private float cameraDuck;
        private float cameraLean;
        private int nextScareIndex;
        private int nextEncounterIndex;
        private string actionMessage;
        private float actionMessageUntil;
        private BossProjectile bossProjectile;
        private float nextBossAttackAt;
        private int selectedRoute;
        private bool routeChoiceActive;
        private int routeCandidate;
        private float routeCandidateSince;
        private bool routeHazardsAdded;
        private bool routeCollectiblesAdded;
        private bool easyMode;
        private bool gameOver;
        private int wagonHealth;
        private int bossPhase;
        private int collectedRelics;
        private int startingWagonHealth;
        private int wagonDamageTaken;
        private int breakablesDestroyed;
        private int secretsFound;
        private int directorEvents;
        private bool secretRouteUnlocked;
        private bool repairSigilSpawned;
        private float nextDirectorBeatAt;
        private int lastDirectorEncounter = -1;
        private float bossWeakPointUntil;
        private int nextZoneIndex;
        private int nextMiniBossIndex;
        private string zoneTitle;
        private float zoneTitleUntil;
        private bool showSettings;
        private bool musicMuted;
        private bool recordSaved;
        private float musicVolume = 1f;
        private float brightnessLevel = 1f;
        private float effectsVolume = 0.86f;
        private float voiceVolume = 1f;
        private GameFlow flow = GameFlow.Menu;
        private int requestedPlayers = 1;
        private float calibrationStableSeconds;
        private readonly bool[] calibrationCastPassed = new bool[2];
        private readonly bool[] calibrationLeftCastPassed = new bool[2];
        private readonly bool[] calibrationRightCastPassed = new bool[2];
        private readonly bool[] calibrationDuckPassed = new bool[2];
        private readonly bool[] calibrationLeanPassed = new bool[2];
        private readonly float[] calibrationDuckTime = new float[2];
        private readonly float[] calibrationLeanTime = new float[2];
        private readonly int[] calibrationVisibleHands = new int[2];
        private float calibrationUnstableSeconds;
        private bool calibrationTrackingStable;
        private string interfaceDwellAction;
        private float interfaceDwellStartedAt;
        private const int TotalRelics = 6;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            LoadPreferences();
            CreateCamera();
            new DarkRideWorld(transform).Build(rideCamera);
            ApplyInitialBrightness();
            CreateInput();
            CreateAudio();
            CreateHudTextures();
            ResetRide();
            flow = GameFlow.Menu;
        }

        private void CreateCamera()
        {
            rideCamera = Camera.main;
            if (rideCamera == null)
            {
                GameObject cameraObject = new GameObject("Spökvagnens kamera");
                cameraObject.tag = "MainCamera";
                rideCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            rideCamera.clearFlags = CameraClearFlags.SolidColor;
            rideCamera.backgroundColor = new Color(0.003f, 0.0045f, 0.009f);
            rideCamera.fieldOfView = 63f;
            rideCamera.nearClipPlane = 0.06f;
            rideCamera.farClipPlane = 95f;
            rideCamera.allowHDR = true;
        }

        private void CreateInput()
        {
            var kinect = new KinectBridgeAimProvider();
            if (kinect.TryStart())
            {
                aimProvider = kinect;
                inputStatus = kinect.Status
                    + " – båda händerna har varsitt sikte och kastar framåt";
            }
            else
            {
                inputStatus = kinect.Status + "  |  Musen styr siktet";
                kinect.Dispose();
                aimProvider = new MouseAimProvider();
            }
        }

        private void CreateAudio()
        {
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0;
            hitSounds = LoadClipSet("Audio/SFX/Impacts", "hit_",
                CreateTone("Träff", 640f, 0.10f, 0.20f));
            bossSounds = LoadNamedClips(CreateTone("Bossträff", 185f, 0.22f, 0.28f),
                "Audio/SFX/Impacts/horror_bass_01", "Audio/SFX/Impacts/horror_bass_02");
            castSounds = LoadClipSet("Audio/SFX/Magic", "magic_",
                CreateNoiseBurst("Magikast", 0.12f, 0.16f, 1101));
            movementSuccessSounds = LoadNamedClips(CreateTone("Undanmanöver", 880f, 0.22f, 0.20f),
                "Audio/SFX/Environment/success_bell");
            collisionSounds = LoadClipSet("Audio/SFX/Impacts", "slam_",
                CreateNoiseBurst("Krock", 0.34f, 0.26f, 9001));
            scareSounds = LoadNamedClips(CreateNoiseBurst("Överraskning", 0.25f, 0.11f, 4404),
                "Audio/SFX/Impacts/horror_high_01", "Audio/SFX/Impacts/horror_mid_01",
                "Audio/SFX/Environment/weird_01", "Audio/SFX/Environment/weird_03",
                "Audio/SFX/Environment/weird_05");
            hazardCueSounds = LoadNamedClips(CreateWarningSound(),
                "Audio/SFX/Environment/metal_03", "Audio/SFX/Environment/metal_08");
            chainRattleSounds = LoadNamedClips(CreateMetalRattle(),
                "Audio/SFX/Environment/chain_rattle", "Audio/SFX/Environment/metal_clank");
            batRushSounds = LoadNamedClips(CreateNoiseBurst("Fladdermöss", 0.72f, 0.14f, 7281),
                "Audio/SFX/Creatures/bat_wings");
            phantomSounds = LoadClipSet("Audio/SFX/Creatures", "ghost_moan_",
                CreateGhostVoice("Vålnad nära vagnen", 1.22f, 96f, 3108));

            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.clip = Resources.Load<AudioClip>("Audio/SFX/Ambience/ambient_horror") ?? CreateAmbience();
            ambienceSource.loop = true;
            ambienceSource.volume = 0.16f * effectsVolume;
            ambienceSource.spatialBlend = 0;
            ambienceSource.Play();

            musicSource = gameObject.AddComponent<AudioSource>();
            AudioClip licensedMusic = Resources.Load<AudioClip>("Audio/CustomRideMusic");
            if (licensedMusic == null) licensedMusic = Resources.Load<AudioClip>("Audio/RideMusic");
            musicSource.clip = licensedMusic != null ? licensedMusic : CreateRideMusic();
            musicSource.loop = true;
            musicSource.volume = (licensedMusic != null ? 1f : 0.62f) * musicVolume;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 0;
            musicSource.mute = false;
            musicSource.ignoreListenerPause = true;
            musicSource.bypassEffects = true;
            musicSource.bypassListenerEffects = true;
            musicSource.bypassReverbZones = true;
            AudioListener.pause = false;
            AudioListener.volume = 1f;
            StartCoroutine(StartMusicWhenReady(licensedMusic != null));

            ghostVoice = gameObject.AddComponent<AudioSource>();
            ghostVoice.playOnAwake = false;
            ghostVoice.spatialBlend = 0f;
            ghostSounds = phantomSounds;

            environmentVoice = gameObject.AddComponent<AudioSource>();
            environmentVoice.playOnAwake = false;
            environmentVoice.spatialBlend = 0f;

            announcerVoice = gameObject.AddComponent<AudioSource>();
            announcerVoice.playOnAwake = false;
            announcerVoice.spatialBlend = 0f;
            announcerVoice.volume = voiceVolume;
            announcerVoice.priority = 8;
            announcerVoice.bypassReverbZones = true;
            duckSuccessVoice = Resources.Load<AudioClip>("Audio/SFX/Voice/duck_success");
            dodgeSuccessVoice = Resources.Load<AudioClip>("Audio/SFX/Voice/dodge_success");
            bossSuccessVoice = Resources.Load<AudioClip>("Audio/SFX/Voice/boss_success");
            playerHitVoice = Resources.Load<AudioClip>("Audio/SFX/Voice/player_hit");
            wagonHitVoice = Resources.Load<AudioClip>("Audio/SFX/Voice/wagon_hit");
            ApplyAudioVolumes();
        }

        private void LoadPreferences()
        {
            easyMode = PlayerPrefs.GetInt("SpokjaktenEasyMode", 1) != 0;
            requestedPlayers = Mathf.Clamp(PlayerPrefs.GetInt("SpokjaktenPlayers", 1), 1, 2);
            musicVolume = Mathf.Clamp(PlayerPrefs.GetFloat("SpokjaktenMusicVolume", 0.90f), 0f, 1.2f);
            musicMuted = PlayerPrefs.GetInt("SpokjaktenMusicMuted", 0) != 0;
            effectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("SpokjaktenEffectsVolume", 0.86f));
            voiceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("SpokjaktenVoiceVolume", 1f));
            brightnessLevel = Mathf.Clamp(PlayerPrefs.GetFloat("SpokjaktenBrightness", 1f), 0.65f, 1.55f);
            bool fullscreen = PlayerPrefs.GetInt("SpokjaktenFullscreen", Screen.fullScreen ? 1 : 0) != 0;
            if (Screen.fullScreen != fullscreen) Screen.fullScreen = fullscreen;
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetInt("SpokjaktenEasyMode", easyMode ? 1 : 0);
            PlayerPrefs.SetInt("SpokjaktenPlayers", requestedPlayers);
            PlayerPrefs.SetFloat("SpokjaktenMusicVolume", musicVolume);
            PlayerPrefs.SetInt("SpokjaktenMusicMuted", musicMuted ? 1 : 0);
            PlayerPrefs.SetFloat("SpokjaktenEffectsVolume", effectsVolume);
            PlayerPrefs.SetFloat("SpokjaktenVoiceVolume", voiceVolume);
            PlayerPrefs.SetFloat("SpokjaktenBrightness", brightnessLevel);
            PlayerPrefs.SetInt("SpokjaktenFullscreen", Screen.fullScreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyInitialBrightness()
        {
            RenderSettings.ambientIntensity *= brightnessLevel;
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light != null && light.type != LightType.Directional)
                    light.intensity *= brightnessLevel;
        }

        private void ApplyAudioVolumes()
        {
            if (effects != null) effects.volume = effectsVolume;
            if (ghostVoice != null) ghostVoice.volume = effectsVolume;
            if (environmentVoice != null) environmentVoice.volume = effectsVolume;
            if (ambienceSource != null) ambienceSource.volume = 0.16f * effectsVolume;
            if (announcerVoice != null) announcerVoice.volume = voiceVolume;
        }

        private IEnumerator StartMusicWhenReady(bool importedMusic)
        {
            AudioClip clip = musicSource != null ? musicSource.clip : null;
            if (clip == null)
            {
                Debug.LogError("Spökjakten kunde inte skapa eller läsa in någon musik.");
                yield break;
            }

            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            float timeout = Time.realtimeSinceStartup + 8f;
            while (clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < timeout)
                yield return null;

            if (clip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogError("Musikfilen importerades men ljuddata kunde inte laddas: " + clip.name);
                yield break;
            }

            musicSource.Play();
            Debug.Log((importedMusic ? "Spökjakten spelar importerad musik: " : "Spökjakten spelar reservmusik: ")
                + clip.name + " | loadState=" + clip.loadState + " | playing=" + musicSource.isPlaying);
        }

        private void ResetRide()
        {
            // Listan innehåller dynamiska mål och reliker. Miljömonster byggs före
            // första ResetRide och får därför inte raderas här.
            foreach (GhostTarget target in targets.Where(item => item != null)) Destroy(target.gameObject);
            foreach (RideHazard hazard in hazards.Where(item => item != null)) Destroy(hazard.gameObject);
            targets.Clear();
            hazards.Clear();
            aimLocks.Clear();
            reticles.Clear();
            handFilters.Clear();
            poseCalibrations.Clear();
            currentPoses.Clear();
            scores[0] = scores[1] = 0;
            for (int player = 0; player < 2; player++)
            {
                lives[player] = easyMode ? 4 : 3;
                shots[player] = hits[player] = kills[player] = combos[player] = maxCombos[player] = 0;
                leftHandShots[player] = rightHandShots[player] = 0;
                rescueProgress[player] = 0;
                lastHitAt[player] = -10f;
            }
            wagonHealth = easyMode ? 7 : 5;
            startingWagonHealth = wagonHealth;
            wagonDamageTaken = 0;
            breakablesDestroyed = 0;
            secretsFound = 0;
            directorEvents = 0;
            secretRouteUnlocked = false;
            repairSigilSpawned = false;
            nextDirectorBeatAt = 7f;
            lastDirectorEncounter = -1;
            bossWeakPointUntil = 0f;
            collectedRelics = 0;
            nextZoneIndex = 0;
            nextMiniBossIndex = 0;
            zoneTitle = string.Empty;
            zoneTitleUntil = 0f;
            gameOver = false;
            recordSaved = false;
            bossPhase = 1;
            gameTime = 0;
            rideDistance = 0;
            nextSpawnAt = 4.3f;
            bossSpawned = false;
            bossBattle = false;
            bossDefeated = false;
            finished = false;
            paused = false;
            currentHazard = null;
            cameraShakeUntil = 0;
            cameraDuck = 0f;
            cameraLean = 0f;
            nextScareIndex = 0;
            nextEncounterIndex = 0;
            nextGhostSoundAt = Time.time + UnityEngine.Random.Range(4.5f, 7.5f);
            actionMessage = string.Empty;
            actionMessageUntil = 0;
            if (bossProjectile != null) Destroy(bossProjectile.gameObject);
            bossProjectile = null;
            nextBossAttackAt = 0;
            selectedRoute = 0;
            routeChoiceActive = false;
            routeCandidate = 0;
            routeCandidateSince = 0f;
            routeHazardsAdded = false;
            routeCollectiblesAdded = false;
            RouteDoor.ResetAll();
            StalkingMonster.ResetStalker();
            hazards.Add(RideHazard.Create(HazardKind.Duck, 43f));
            hazards.Add(RideHazard.Create(HazardKind.DodgeLeft, 78f));
            hazards.Add(RideHazard.Create(HazardKind.DodgeRight, 111f));
            hazards.Add(RideHazard.Create(HazardKind.Duck, 143f));
            AddRelic(58f, 0, -2.8f, 0);
            AddRelic(121f, 0, 2.9f, 1);
            AddRelic(184f, 0, -2.4f, 2);
            targets.Add(SecretRouteSeal.Create(new Vector3(
                DarkRideWorld.TrackCenter(181f), 2.25f, 181f)));
            if (WagonDamageVisual.Instance != null)
                WagonDamageVisual.Instance.SetHealth(wagonHealth, startingWagonHealth, false);
            PositionCamera(0);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11))
            {
                Screen.fullScreen = !Screen.fullScreen;
                SavePreferences();
            }
            if (Input.GetKeyDown(KeyCode.F1)) showSettings = !showSettings;
            if (Input.GetKeyDown(KeyCode.M))
            {
                musicMuted = !musicMuted;
                SavePreferences();
            }
            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                musicVolume = Mathf.Max(0f, musicVolume - 0.1f);
                SavePreferences();
            }
            if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                musicVolume = Mathf.Min(1.2f, musicVolume + 0.1f);
                SavePreferences();
            }
            if (Input.GetKeyDown(KeyCode.Minus)) AdjustBrightness(-0.1f);
            if (Input.GetKeyDown(KeyCode.Equals)) AdjustBrightness(0.1f);
            UpdateDynamicMusic();

            if (flow == GameFlow.Menu)
            {
                PositionCamera(0f);
                UpdateMenuAim();
                if (Input.GetKeyDown(KeyCode.Return)) BeginCalibration();
                return;
            }

            if (flow == GameFlow.Calibration)
            {
                UpdateCalibration();
                PositionCamera(0f);
                UpdateCalibrationDwell();
                if (calibrationTrackingStable && Input.GetKeyDown(KeyCode.Return)) CompleteCalibration();
                if (Input.GetKeyDown(KeyCode.Escape)) flow = GameFlow.Menu;
                return;
            }

            if (Input.GetKeyDown(KeyCode.F2)) JumpToNextCheckpoint();
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Space)) paused = !paused;
            if (finished && Input.GetKeyDown(KeyCode.R)) ReloadRide();
            if (Input.GetKeyDown(KeyCode.B) && !bossDefeated)
            {
                rideDistance = BossStopDistance;
                bossBattle = true;
            }
            if (paused || finished || gameOver)
            {
                if (finished) SaveRecord();
                return;
            }

            gameTime += Time.deltaTime;
            float rideTime = Mathf.Max(0, gameTime - CountdownSeconds);
            UpdatePlayerPoses();
            if (flow == GameFlow.Countdown)
            {
                PositionCamera(0);
                if (gameTime >= CountdownSeconds) flow = GameFlow.Riding;
                return;
            }

            if (!bossBattle)
                rideDistance = Mathf.Min(DarkRideWorld.TrackLength - 3f,
                    rideDistance + RideSpeed * (easyMode ? 0.91f : 1f)
                    * (bossDefeated ? 1.35f : 1f) * Time.deltaTime);
            UpdateRouteChoice();
            if (!bossDefeated && rideDistance >= BossStopDistance)
            {
                rideDistance = BossStopDistance;
                bossBattle = true;
            }

            PositionCamera(rideDistance);
            UpdateTargets(rideTime, rideDistance);
            UpdateHazards(rideDistance);
            UpdateEnvironmentEvents();
            UpdateZoneStory();
            UpdateGhostAudio();
            UpdateAim();
            UpdateBossBattle();
            if (bossDefeated && rideDistance >= DarkRideWorld.TrackLength - 3f)
            {
                finished = true;
                reticles.Clear();
                SaveRecord();
            }
        }

        private void BeginCalibration()
        {
            poseCalibrations.Clear();
            currentPoses.Clear();
            reticles.Clear();
            handFilters.Clear();
            calibrationStableSeconds = 0f;
            calibrationUnstableSeconds = 0f;
            calibrationTrackingStable = false;
            interfaceDwellAction = null;
            interfaceDwellStartedAt = 0f;
            for (int player = 0; player < 2; player++)
            {
                calibrationCastPassed[player] = false;
                calibrationLeftCastPassed[player] = false;
                calibrationRightCastPassed[player] = false;
                calibrationDuckPassed[player] = false;
                calibrationLeanPassed[player] = false;
                calibrationDuckTime[player] = 0f;
                calibrationLeanTime[player] = 0f;
            }
            flow = GameFlow.Calibration;
            SavePreferences();
        }

        private void UpdateMenuAim()
        {
            IReadOnlyList<AimSample> samples = aimProvider.GetAimSamples();
            reticles.Clear();
            foreach (Light reticleLight in reticleLights.Values)
                if (reticleLight != null) reticleLight.enabled = false;
            foreach (AimSample rawSample in samples)
            {
                int player = Mathf.Clamp(rawSample.PlayerIndex, 0, 1);
                if (player >= requestedPlayers) continue;
                AimSample sample = SmoothHand(rawSample);
                Ray ray = rideCamera.ViewportPointToRay(
                    new Vector3(sample.Position.x, 1f - sample.Position.y, 0f));
                UpdateReticleLight(sample.HandId, player, ray);
                reticles[sample.HandId] = new ReticleState
                {
                    PlayerIndex = player,
                    Position = sample.Position,
                    IsRightHand = sample.HandId % 2 == 1,
                };
            }
            UpdateInterfaceDwell(false);
        }

        private void UpdateCalibrationDwell()
        {
            UpdateInterfaceDwell(true);
        }

        private void UpdateInterfaceDwell(bool calibration)
        {
            string hovered = null;
            foreach (ReticleState reticle in reticles.Values)
            {
                string action = calibration
                    ? CalibrationActionAt(reticle.Position)
                    : MenuActionAt(reticle.Position);
                if (!string.IsNullOrEmpty(action))
                {
                    hovered = action;
                    break;
                }
            }

            if (hovered != interfaceDwellAction)
            {
                interfaceDwellAction = hovered;
                interfaceDwellStartedAt = Time.time;
            }
            float progress = string.IsNullOrEmpty(hovered)
                ? 0f : Mathf.Clamp01((Time.time - interfaceDwellStartedAt) / 0.85f);
            foreach (int key in reticles.Keys.ToArray())
            {
                ReticleState state = reticles[key];
                string action = calibration
                    ? CalibrationActionAt(state.Position)
                    : MenuActionAt(state.Position);
                state.Progress = action == hovered ? progress : 0f;
                reticles[key] = state;
            }
            if (progress < 1f) return;

            interfaceDwellAction = null;
            interfaceDwellStartedAt = Time.time;
            if (calibration)
            {
                if (hovered == "back") flow = GameFlow.Menu;
                else if (hovered == "start" && calibrationTrackingStable) CompleteCalibration();
            }
            else
            {
                ExecuteMenuAction(hovered);
            }
        }

        private string MenuActionAt(Vector2 normalizedPosition)
        {
            float width = Mathf.Min(680f, Screen.width - 60f);
            float height = Mathf.Min(720f, Screen.height - 50f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            float rowY = y + 128f;
            Vector2 point = new Vector2(normalizedPosition.x * Screen.width,
                normalizedPosition.y * Screen.height);
            if (new Rect(x + 240f, rowY, 170f, 38f).Contains(point)) return "easy";
            if (new Rect(x + 420f, rowY, 170f, 38f).Contains(point)) return "normal";
            rowY += 55f;
            if (new Rect(x + 240f, rowY, 170f, 38f).Contains(point)) return "one";
            if (new Rect(x + 420f, rowY, 170f, 38f).Contains(point)) return "two";
            rowY = y + 440f;
            if (new Rect(x + 185f, rowY, 310f, 40f).Contains(point)) return "fullscreen";
            if (new Rect(x + 145f, y + height - 92f, width - 290f, 58f).Contains(point)) return "calibrate";
            return null;
        }

        private string CalibrationActionAt(Vector2 normalizedPosition)
        {
            float width = Mathf.Min(760f, Screen.width - 50f);
            float height = Mathf.Min(650f, Screen.height - 40f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            Vector2 point = new Vector2(normalizedPosition.x * Screen.width,
                normalizedPosition.y * Screen.height);
            if (new Rect(x + 45f, y + height - 72f, 150f, 42f).Contains(point)) return "back";
            if (calibrationTrackingStable
                && new Rect(x + width - 315f, y + height - 78f, 270f, 52f).Contains(point)) return "start";
            return null;
        }

        private void ExecuteMenuAction(string action)
        {
            if (action == "easy") easyMode = true;
            else if (action == "normal") easyMode = false;
            else if (action == "one") requestedPlayers = 1;
            else if (action == "two") requestedPlayers = 2;
            else if (action == "fullscreen") Screen.fullScreen = !Screen.fullScreen;
            else if (action == "calibrate")
            {
                BeginCalibration();
                return;
            }
            SavePreferences();
        }

        private void UpdateCalibration()
        {
            UpdatePlayerPoses();
            IReadOnlyList<AimSample> samples = aimProvider.GetAimSamples();
            reticles.Clear();
            foreach (Light reticleLight in reticleLights.Values)
                if (reticleLight != null) reticleLight.enabled = false;

            int[] handCounts = new int[2];
            foreach (AimSample rawSample in samples)
            {
                AimSample sample = SmoothHand(rawSample);
                int player = Mathf.Clamp(sample.PlayerIndex, 0, 1);
                if (player >= requestedPlayers) continue;
                handCounts[player]++;
                if (sample.Fire)
                {
                    calibrationCastPassed[player] = true;
                    if (sample.HandId % 2 == 1) calibrationRightCastPassed[player] = true;
                    else calibrationLeftCastPassed[player] = true;
                }
                if (!reticles.ContainsKey(sample.HandId))
                {
                    Ray ray = rideCamera.ViewportPointToRay(
                        new Vector3(sample.Position.x, 1f - sample.Position.y, 0f));
                    UpdateReticleLight(sample.HandId, player, ray);
                    reticles[sample.HandId] = new ReticleState
                    {
                        PlayerIndex = player,
                        Position = sample.Position,
                        Progress = sample.GestureProgress,
                        IsRightHand = sample.HandId % 2 == 1,
                    };
                }
            }

            calibrationVisibleHands[0] = handCounts[0];
            calibrationVisibleHands[1] = handCounts[1];

            bool neutralPose = currentPoses.Count >= requestedPlayers;
            bool handsFound = true;
            bool kinectInput = aimProvider is KinectBridgeAimProvider || aimProvider is KinectV1AimProvider;
            for (int player = 0; player < requestedPlayers; player++)
            {
                PoseState pose;
                if (!currentPoses.TryGetValue(player, out pose))
                {
                    neutralPose = false;
                    handsFound = false;
                    continue;
                }
                calibrationDuckTime[player] = pose.DuckAmount >= 0.15f
                    ? calibrationDuckTime[player] + Time.deltaTime
                    : Mathf.Max(0f, calibrationDuckTime[player] - Time.deltaTime * 2f);
                calibrationLeanTime[player] = Mathf.Abs(pose.LeanAmount) >= 0.14f
                    ? calibrationLeanTime[player] + Time.deltaTime
                    : Mathf.Max(0f, calibrationLeanTime[player] - Time.deltaTime * 2f);
                if (calibrationDuckTime[player] >= 0.30f) calibrationDuckPassed[player] = true;
                if (calibrationLeanTime[player] >= 0.30f) calibrationLeanPassed[player] = true;
                if (pose.DuckAmount > 0.08f || Mathf.Abs(pose.LeanAmount) > 0.11f)
                    neutralPose = false;
                if (handCounts[player] < (kinectInput ? 2 : 1)) handsFound = false;
            }

            if (!calibrationTrackingStable)
            {
                if (neutralPose && handsFound)
                {
                    calibrationStableSeconds += Time.deltaTime;
                    calibrationUnstableSeconds = 0f;
                }
                else
                {
                    calibrationUnstableSeconds += Time.deltaTime;
                    if (calibrationUnstableSeconds > 0.45f)
                        calibrationStableSeconds = Mathf.Max(0f,
                            calibrationStableSeconds - Time.deltaTime * 0.35f);
                }
                if (calibrationStableSeconds >= 1.6f) calibrationTrackingStable = true;
            }
        }

        private void CompleteCalibration()
        {
            SetDifficulty(easyMode);
            gameTime = 0f;
            paused = false;
            reticles.Clear();
            actionMessage = string.Empty;
            actionMessageUntil = 0f;
            flow = GameFlow.Countdown;
            SavePreferences();
        }

        private void ReloadRide()
        {
            SavePreferences();
            enabled = false;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        private void UpdateDynamicMusic()
        {
            if (musicSource == null) return;
            float targetPitch = bossBattle ? 1f + bossPhase * 0.025f
                : Mathf.Max(combos[0], combos[1]) >= 10 ? 1.035f : 1f;
            musicSource.pitch = Mathf.Lerp(musicSource.pitch, targetPitch, Time.unscaledDeltaTime * 2f);
            float voiceDuck = announcerVoice != null && announcerVoice.isPlaying ? 0.48f : 1f;
            float targetVolume = musicMuted ? 0f
                : musicVolume * (bossBattle ? 1.08f : 0.92f) * voiceDuck;
            musicSource.volume = Mathf.Lerp(musicSource.volume, targetVolume, Time.unscaledDeltaTime * 4f);
        }

        private void AdjustBrightness(float change)
        {
            float previous = brightnessLevel;
            brightnessLevel = Mathf.Clamp(brightnessLevel + change, 0.65f, 1.55f);
            float ratio = brightnessLevel / previous;
            RenderSettings.ambientIntensity *= ratio;
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light != null && light.type != LightType.Directional) light.intensity *= ratio;
            actionMessage = "LJUSSTYRKA " + Mathf.RoundToInt(brightnessLevel * 100f) + "%";
            actionMessageUntil = Time.time + 1f;
            SavePreferences();
        }

        private void JumpToNextCheckpoint()
        {
            float[] checkpoints = { 48f, 106f, 148f, 189f, 230f, 308f, BossStopDistance };
            float next = checkpoints.FirstOrDefault(value => value > rideDistance + 2f);
            if (next <= 0f) return;
            rideDistance = next;
            if (rideDistance >= DarkRideWorld.BranchChoiceStart && selectedRoute == 0) SelectRoute(-1);
            actionMessage = "TESTHOPP " + Mathf.RoundToInt(rideDistance) + " m";
            actionMessageUntil = Time.time + 1.2f;
        }

        private void SaveRecord()
        {
            if (recordSaved) return;
            recordSaved = true;
            int totalScore = scores[0] + scores[1];
            PlayerPrefs.SetInt("SpokjaktenHighScore",
                Mathf.Max(totalScore, PlayerPrefs.GetInt("SpokjaktenHighScore", 0)));
            PlayerPrefs.SetInt("SpokjaktenBestCombo",
                Mathf.Max(Mathf.Max(maxCombos[0], maxCombos[1]), PlayerPrefs.GetInt("SpokjaktenBestCombo", 0)));
            PlayerPrefs.SetInt("SpokjaktenBestRelics",
                Mathf.Max(collectedRelics, PlayerPrefs.GetInt("SpokjaktenBestRelics", 0)));
            PlayerPrefs.SetInt("SpokjaktenBestMedals",
                Mathf.Max(CountMedals(), PlayerPrefs.GetInt("SpokjaktenBestMedals", 0)));
            PlayerPrefs.Save();
        }

        private void UpdateZoneStory()
        {
            if (nextZoneIndex >= ZoneDistances.Length || rideDistance < ZoneDistances[nextZoneIndex]) return;
            zoneTitle = ZoneTitles[nextZoneIndex];
            zoneTitleUntil = Time.time + 2.8f;
            nextZoneIndex++;
            PlayRandom(effects, doorCreakSoundsForZone(), 0.36f);
        }

        private AudioClip[] doorCreakSoundsForZone()
        {
            return LoadNamedClips(null, "Audio/SFX/Doors/floor_creak_01", "Audio/SFX/Doors/floor_creak_02");
        }

        private void SetDifficulty(bool easy)
        {
            easyMode = easy;
            for (int player = 0; player < 2; player++) lives[player] = easy ? 4 : 3;
            wagonHealth = easy ? 7 : 5;
            startingWagonHealth = wagonHealth;
            wagonDamageTaken = 0;
            if (WagonDamageVisual.Instance != null)
                WagonDamageVisual.Instance.SetHealth(wagonHealth, startingWagonHealth, false);
            actionMessage = easy ? "BARNLÄGE – STÖRRE SIKTHJÄLP" : "NORMALT LÄGE";
            actionMessageUntil = Time.time + 1.4f;
            SavePreferences();
        }

        private void PositionCamera(float z)
        {
            float x = DarkRideWorld.TrackCenter(z, selectedRoute);
            float bounce = Mathf.Sin(Time.time * 4.4f) * 0.018f;
            float targetDuck = 0f;
            float targetLean = 0f;
            foreach (PoseState pose in currentPoses.Values)
            {
                targetDuck = Mathf.Max(targetDuck, Mathf.Clamp01(pose.DuckAmount / 0.34f));
                float lean = Mathf.Clamp(pose.LeanAmount / 0.34f, -1f, 1f);
                if (Mathf.Abs(lean) > Mathf.Abs(targetLean)) targetLean = lean;
            }
            cameraDuck = Mathf.Lerp(cameraDuck, targetDuck, 1f - Mathf.Exp(-9f * Time.deltaTime));
            cameraLean = Mathf.Lerp(cameraLean, targetLean, 1f - Mathf.Exp(-8f * Time.deltaTime));
            x += cameraLean * 0.92f;
            if (Time.time < cameraShakeUntil)
            {
                x += UnityEngine.Random.Range(-0.12f, 0.12f);
                bounce += UnityEngine.Random.Range(-0.09f, 0.09f);
            }
            Vector3 position = new Vector3(x, 1.72f - cameraDuck * 0.68f + bounce, z);
            Vector3 look = new Vector3(DarkRideWorld.TrackCenter(z + 7f, selectedRoute) + cameraLean * 0.38f,
                1.62f - cameraDuck * 0.32f, z + 7f);
            Quaternion bodyMotion = Quaternion.LookRotation(look - position, Vector3.up)
                * Quaternion.Euler(cameraDuck * 3f, 0f, -cameraLean * 7.5f);
            rideCamera.transform.position = position;
            rideCamera.transform.rotation = Quaternion.Slerp(
                rideCamera.transform.rotation,
                bodyMotion,
                1f - Mathf.Exp(-7f * Time.deltaTime));
        }

        private void UpdateTargets(float rideTime, float distance)
        {
            targets.RemoveAll(item => item == null);
            foreach (GhostTarget target in targets.Where(item => item != null && item.transform.position.z < distance - 3f).ToArray())
            {
                targets.Remove(target);
                if (target.Health > 0 && target.GetComponent<CursedRelic>() == null
                    && target.GetComponent<BreakableProp>() == null
                    && target.GetComponent<HauntedIllusion>() == null
                    && target.GetComponent<SecretRouteSeal>() == null
                    && target.GetComponent<RepairSigil>() == null && !target.IsBoss)
                    DamageWagon(target.GetComponent<MiniBossTarget>() != null ? 2 : 1,
                        "ETT MONSTER NÅDDE VAGNEN!");
                Destroy(target.gameObject);
            }

            if (rideTime >= nextSpawnAt && distance < BossStopDistance - 13f && !routeChoiceActive)
            {
                SpawnRegular(distance, rideTime);
                float performance = Mathf.Clamp01((Mathf.Max(combos[0], combos[1]) - 3f) / 12f);
                float dangerRelief = wagonHealth <= 2 ? 1.15f : wagonHealth <= 3 ? 0.55f : 0f;
                float minimumDelay = easyMode ? 3.0f : Mathf.Lerp(2.65f, 2.05f, performance);
                float maximumDelay = easyMode ? 4.15f : Mathf.Lerp(3.65f, 2.85f, performance);
                nextSpawnAt = rideTime + UnityEngine.Random.Range(minimumDelay, maximumDelay) + dangerRelief;
            }

            if (nextMiniBossIndex < MiniBossDistances.Length
                && distance >= MiniBossDistances[nextMiniBossIndex])
            {
                SpawnMiniBoss(nextMiniBossIndex, distance);
                nextMiniBossIndex++;
            }

            if (!bossSpawned && distance >= BossStopDistance)
            {
                bossSpawned = true;
                float z = BossStopDistance + 16f;
                targets.Add(GhostTarget.Create(TargetKind.ConductorBoss,
                    new Vector3(DarkRideWorld.TrackCenter(z, selectedRoute), 0.35f, z)));
                nextBossAttackAt = Time.time + 2.2f;
                actionMessage = "VAGNEN STANNAR – BESEGRA KONDUKTÖREN!";
                actionMessageUntil = Time.time + 2.4f;
            }
        }

        private void SpawnRegular(float distance, float rideTime)
        {
            float z = Mathf.Min(DarkRideWorld.TrackLength - 8f, distance + UnityEngine.Random.Range(18f, 27f));
            float lane = UnityEngine.Random.value < 0.25f
                ? UnityEngine.Random.Range(-1.1f, 1.1f)
                : UnityEngine.Random.Range(2.1f, 3.9f) * (UnityEngine.Random.value < 0.5f ? -1 : 1);
            TargetKind kind = rideTime < 23f || UnityEngine.Random.value < 0.42f ? TargetKind.Ghost : TargetKind.Zombie;
            float y = kind == TargetKind.Ghost ? UnityEngine.Random.Range(0.65f, 1.5f) : 0.28f;
            GhostTarget spawned = GhostTarget.Create(kind,
                new Vector3(DarkRideWorld.TrackCenter(z, selectedRoute) + lane, y, z));
            if (kind == TargetKind.Ghost && UnityEngine.Random.value < 0.16f)
            {
                spawned.gameObject.AddComponent<HauntedIllusion>();
                spawned.name = "Falsk spökillusion";
            }
            MonsterTelegraph.Attach(spawned, easyMode ? 17.5f : 15.5f, lane < 0f ? -1 : 1);
            targets.Add(spawned);
        }

        private void SpawnMiniBoss(int index, float distance)
        {
            MiniBossKind kind = (MiniBossKind)(index % 3);
            float z = Mathf.Min(BossStopDistance - 5f, distance + 18f);
            GhostTarget target = MiniBossTarget.Create(kind,
                new Vector3(DarkRideWorld.TrackCenter(z, selectedRoute), 0.25f, z));
            MonsterTelegraph.Attach(target, easyMode ? 20f : 18f, index % 2 == 0 ? -1 : 1);
            targets.Add(target);
            string name = kind == MiniBossKind.GiantSpider ? "JÄTTESPINDELN"
                : kind == MiniBossKind.Vampire ? "VAMPYREN" : "DEN BESATTA RUSTNINGEN";
            actionMessage = "MINIBOSS – " + name;
            actionMessageUntil = Time.time + 2.2f;
            PlayRandom(effects, scareSounds, 0.9f);
        }

        private void AddRelic(float z, int route, float lane, int variant)
        {
            GhostTarget relic = CursedRelic.Create(
                new Vector3(DarkRideWorld.TrackCenter(z, route) + lane, 1.25f, z), variant);
            targets.Add(relic);
        }

        public static void ReportMonsterEscape(int damage = 1)
        {
            if (Instance != null) Instance.DamageWagon(damage, "ETT MONSTER KOM UNDAN!");
        }

        public static void ReportThreatCue(int side)
        {
            if (Instance == null) return;
            if (Instance.environmentVoice != null) Instance.environmentVoice.panStereo = side < 0 ? -0.62f : 0.62f;
            PlayRandom(Instance.environmentVoice, Instance.hazardCueSounds, 0.42f);
            Instance.PlayGhostSound(side, 0.44f);
        }

        private void DamagePlayer(int player, int amount)
        {
            player = Mathf.Clamp(player, 0, 1);
            combos[player] = 0;
            if (lives[player] > 0)
            {
                lives[player] = Mathf.Max(0, lives[player] - amount);
                PlayAnnouncement(playerHitVoice, true);
            }
            else DamageWagon(amount, "VAGNEN TOG SKADA!");
        }

        private void TryRescueTeammate(int rescuer)
        {
            int teammate = rescuer == 0 ? 1 : 0;
            if (lives[teammate] > 0 || (!currentPoses.ContainsKey(teammate)
                && !reticles.Values.Any(item => item.PlayerIndex == teammate))) return;
            rescueProgress[teammate]++;
            if (rescueProgress[teammate] < 3)
            {
                actionMessage = "RÄDDA SPELARE " + (teammate + 1) + "  " + rescueProgress[teammate] + "/3";
                actionMessageUntil = Time.time + 1f;
                return;
            }
            lives[teammate] = 1;
            rescueProgress[teammate] = 0;
            actionMessage = "SPELARE " + (teammate + 1) + " ÄR TILLBAKA!";
            actionMessageUntil = Time.time + 1.8f;
            PlayRandom(effects, movementSuccessSounds);
        }

        private int TriggerTrap(Vector3 position, int player)
        {
            int defeated = 0;
            foreach (GhostTarget victim in GhostTarget.ActiveTargets.Where(target =>
                         target.IsTargetable && !target.IsBoss
                         && target.GetComponent<TrapTrigger>() == null
                         && target.GetComponent<CursedRelic>() == null
                         && target.GetComponent<BreakableProp>() == null
                         && Vector3.Distance(target.transform.position, position) <= 9f).ToArray())
            {
                while (victim != null && victim.Health > 0) victim.Hit();
                defeated++;
            }
            if (defeated <= 0) return 15;
            kills[player] += defeated;
            for (int i = 0; i < defeated; i++) TryRescueTeammate(player);
            cameraShakeUntil = Time.time + 0.42f;
            PlayRandom(effects, collisionSounds, 1f);
            return defeated * 60;
        }

        private void DamageWagon(int amount, string message)
        {
            if (finished || gameOver) return;
            wagonHealth = Mathf.Max(0, wagonHealth - amount);
            wagonDamageTaken += amount;
            combos[0] = combos[1] = 0;
            cameraShakeUntil = Mathf.Max(cameraShakeUntil, Time.time + 0.45f);
            actionMessage = message + "  VAGN " + wagonHealth;
            actionMessageUntil = Time.time + 1.35f;
            PlayRandom(effects, collisionSounds, 0.85f);
            PlayAnnouncement(wagonHitVoice, true);
            if (WagonDamageVisual.Instance != null)
                WagonDamageVisual.Instance.SetHealth(wagonHealth, startingWagonHealth, true);
            if (!repairSigilSpawned && wagonHealth > 0 && wagonHealth <= startingWagonHealth - 2)
                SpawnRepairSigil();
            if (wagonHealth > 0) return;
            gameOver = true;
            finished = true;
            reticles.Clear();
            actionMessage = "VAGNEN ÄR FÖRSTÖRD";
            actionMessageUntil = float.PositiveInfinity;
        }

        private void SpawnRepairSigil()
        {
            repairSigilSpawned = true;
            float z = Mathf.Min(BossStopDistance - 8f, rideDistance + 19f);
            float lane = UnityEngine.Random.value < 0.5f ? -2.7f : 2.7f;
            GhostTarget repair = RepairSigil.Create(new Vector3(
                DarkRideWorld.TrackCenter(z, selectedRoute) + lane, 1.65f, z));
            targets.Add(repair);
            actionMessage = "ETT REPARATIONSSIGILL HAR VAKNAT!";
            actionMessageUntil = Time.time + 1.7f;
        }

        private void UpdateRouteChoice()
        {
            if (selectedRoute != 0 || rideDistance < DarkRideWorld.BranchChoiceStart) return;
            routeChoiceActive = true;

            int candidate = 0;
            PoseState playerOne;
            if (currentPoses.TryGetValue(0, out playerOne))
            {
                if (playerOne.LeanAmount <= -0.13f) candidate = -1;
                else if (playerOne.LeanAmount >= 0.13f) candidate = 1;
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) candidate = -1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) candidate = 1;
            if (Input.GetMouseButtonDown(0))
            {
                SelectRoute(Input.mousePosition.x < Screen.width * 0.5f ? -1 : 1);
                return;
            }

            if (candidate != routeCandidate)
            {
                routeCandidate = candidate;
                routeCandidateSince = Time.time;
            }
            else if (candidate != 0 && Time.time - routeCandidateSince >= 0.32f)
            {
                SelectRoute(candidate);
                return;
            }

            if (rideDistance < DarkRideWorld.BranchSplitStart - 1.2f) return;
            ReticleState reticle = reticles.Values.FirstOrDefault(item => item.PlayerIndex == 0);
            int automaticRoute = reticles.Values.Any(item => item.PlayerIndex == 0)
                ? (reticle.Position.x < 0.5f ? -1 : 1)
                : (UnityEngine.Random.value < 0.5f ? -1 : 1);
            SelectRoute(automaticRoute);
        }

        private void SelectRoute(int route)
        {
            if (route < 0 && !secretRouteUnlocked)
            {
                route = 1;
                actionMessage = "SIGILLET HÖLL DEN HEMLIGA DÖRREN STÄNGD – HÖGER VÄG!";
                actionMessageUntil = Time.time + 2.4f;
            }
            selectedRoute = route < 0 ? -1 : 1;
            routeChoiceActive = false;
            if (selectedRoute < 0)
            {
                actionMessage = "DEN HEMLIGA GÅNGEN ÄR ÖPPEN!";
                actionMessageUntil = Time.time + 2.1f;
            }
            else if (secretRouteUnlocked)
            {
                actionMessage = "HÖGRA FÖRBANNADE GÅNGEN!";
                actionMessageUntil = Time.time + 2.1f;
            }
            PlayRandom(effects, movementSuccessSounds);
            RouteDoor.OpenRoute(selectedRoute);

            if (!routeCollectiblesAdded)
            {
                routeCollectiblesAdded = true;
                AddRelic(238f, selectedRoute, selectedRoute < 0 ? 2.8f : -2.8f, 3);
                AddRelic(281f, selectedRoute, selectedRoute < 0 ? -2.7f : 2.7f, 4);
                AddRelic(329f, 0, 2.5f, 5);
            }

            if (routeHazardsAdded) return;
            routeHazardsAdded = true;
            if (selectedRoute < 0)
            {
                hazards.Add(RideHazard.Create(HazardKind.DodgeRight, 238f, selectedRoute));
                hazards.Add(RideHazard.Create(HazardKind.Duck, 272f, selectedRoute));
                hazards.Add(RideHazard.Create(HazardKind.DodgeLeft, 299f, selectedRoute));
            }
            else
            {
                hazards.Add(RideHazard.Create(HazardKind.Duck, 235f, selectedRoute));
                hazards.Add(RideHazard.Create(HazardKind.DodgeLeft, 266f, selectedRoute));
                hazards.Add(RideHazard.Create(HazardKind.DodgeRight, 296f, selectedRoute));
            }
        }

        private void UpdatePlayerPoses()
        {
            currentPoses.Clear();
            foreach (PlayerPose pose in aimProvider.GetPlayerPoses())
            {
                if (pose.PlayerIndex < 0 || pose.PlayerIndex >= requestedPlayers) continue;
                PoseCalibration calibration;
                if (!poseCalibrations.TryGetValue(pose.TrackingId, out calibration))
                {
                    calibration = new PoseCalibration
                    {
                        CenterX = pose.CenterX,
                        StandingHeadY = pose.HeadY
                    };
                    poseCalibrations[pose.TrackingId] = calibration;
                }

                if (flow == GameFlow.Calibration)
                {
                    float headDifference = pose.HeadY - calibration.StandingHeadY;
                    if (headDifference >= -0.06f && headDifference <= 0.12f)
                        calibration.StandingHeadY = Mathf.Lerp(
                            calibration.StandingHeadY, pose.HeadY, 0.08f);
                }
                if (currentHazard == null)
                    calibration.CenterX = Mathf.Lerp(calibration.CenterX, pose.CenterX, 0.035f);

                currentPoses[pose.PlayerIndex] = new PoseState
                {
                    DuckAmount = calibration.StandingHeadY - pose.HeadY,
                    LeanAmount = pose.CenterX - calibration.CenterX
                };
            }
        }

        private void UpdateHazards(float distance)
        {
            currentHazard = null;
            foreach (RideHazard hazard in hazards.Where(item => item != null && !item.Resolved))
            {
                float gap = hazard.TrackZ - distance;
                if (gap <= QuickEventCueDistance && gap >= QuickEventPassedDistance
                    && (currentHazard == null || gap < currentHazard.TrackZ - distance))
                    currentHazard = hazard;

                if (gap <= QuickEventCueDistance && gap >= QuickEventPassedDistance && hazard.Reveal())
                    PlayRandom(effects, hazardCueSounds, 0.78f);

                if (gap <= QuickEventActionDistance && gap >= QuickEventPassedDistance)
                {
                    foreach (KeyValuePair<int, PoseState> pair in currentPoses)
                    {
                        bool succeeds = hazard.Kind == HazardKind.Duck
                            ? pair.Value.DuckAmount >= (easyMode ? 0.16f : 0.20f)
                            : hazard.Kind == HazardKind.DodgeLeft
                                ? pair.Value.LeanAmount <= (easyMode ? -0.14f : -0.17f)
                                : pair.Value.LeanAmount >= (easyMode ? 0.14f : 0.17f);
                        if (!succeeds || !hazard.MarkSuccess(pair.Key)) continue;
                        int player = Mathf.Clamp(pair.Key, 0, 1);
                        scores[player] += 40;
                        PlayRandom(effects, movementSuccessSounds);
                        if (hazard.TryClaimPraise())
                            PlayAnnouncement(hazard.Kind == HazardKind.Duck
                                ? duckSuccessVoice : dodgeSuccessVoice);
                    }
                }

                if (gap >= QuickEventPassedDistance) continue;
                bool everyoneSucceeded = true;
                foreach (int player in currentPoses.Keys.ToArray())
                {
                    if (hazard.HasSucceeded(player)) continue;
                    everyoneSucceeded = false;
                    scores[Mathf.Clamp(player, 0, 1)] = Mathf.Max(0, scores[Mathf.Clamp(player, 0, 1)] - 25);
                }
                hazard.Resolved = true;
                hazard.ResolveVisual(everyoneSucceeded);
                if (!everyoneSucceeded)
                {
                    PlayRandom(effects, collisionSounds);
                    foreach (int player in currentPoses.Keys.ToArray())
                        if (!hazard.HasSucceeded(player)) DamagePlayer(Mathf.Clamp(player, 0, 1), 1);
                    if (currentPoses.Count == 0) DamageWagon(1, "INGEN UNDVIKNING – VAGNEN SKADAS!");
                    cameraShakeUntil = Time.time + 0.55f;
                }
            }
        }

        private void UpdateEnvironmentEvents()
        {
            if (nextScareIndex < ScareDistances.Length && rideDistance >= ScareDistances[nextScareIndex])
            {
                int side = UnityEngine.Random.value < 0.5f ? -1 : 1;
                nextScareIndex++;
                SideScare.Create(rideCamera.transform, side);
                PlayRandom(effects, scareSounds);
                PlayGhostSound(side, 0.82f);
                cameraShakeUntil = Mathf.Max(cameraShakeUntil, Time.time + 0.22f);
            }

            if (nextEncounterIndex < EncounterDistances.Length
                && rideDistance >= EncounterDistances[nextEncounterIndex])
            {
                int encounterSide = UnityEngine.Random.value < 0.5f ? -1 : 1;
                HauntedEncounterKind kind = NextDirectorEncounter();
                nextEncounterIndex++;
                TriggerEncounter(kind, encounterSide);
            }

            UpdateScareDirector();
        }

        private void UpdateScareDirector()
        {
            float rideTime = Mathf.Max(0f, gameTime - CountdownSeconds);
            if (rideTime < nextDirectorBeatAt || bossBattle || routeChoiceActive) return;
            int livingThreats = GhostTarget.ActiveTargets.Count(target => target.IsTargetable
                && target.GetComponent<CursedRelic>() == null
                && target.GetComponent<BreakableProp>() == null);
            float performance = Mathf.Clamp01((Mathf.Max(maxCombos[0], maxCombos[1]) - 2f) / 12f);
            float relief = wagonHealth <= 2 ? 4f : 0f;
            nextDirectorBeatAt = rideTime + UnityEngine.Random.Range(8.0f, 12.5f)
                - performance * 2.0f + relief;
            if (livingThreats >= (easyMode ? 2 : 3)) return;

            int side = UnityEngine.Random.value < 0.5f ? -1 : 1;
            directorEvents++;
            float roll = UnityEngine.Random.value;
            if (roll < 0.24f)
            {
                // Ett falsklarm bryter mönstret utan att alltid visa ett monster.
                PlayRandom(environmentVoice, chainRattleSounds, 0.46f);
                PlayGhostSound(-side, 0.30f);
                StartCoroutine(PulseNearbyTorches(1.15f));
            }
            else if (roll < 0.48f)
            {
                StartCoroutine(DirectorScareChain(side));
            }
            else
            {
                TriggerEncounter(NextDirectorEncounter(), side);
            }
        }

        private HauntedEncounterKind NextDirectorEncounter()
        {
            int choice = UnityEngine.Random.Range(0, 3);
            if (choice == lastDirectorEncounter) choice = (choice + UnityEngine.Random.Range(1, 3)) % 3;
            lastDirectorEncounter = choice;
            return (HauntedEncounterKind)choice;
        }

        private void TriggerEncounter(HauntedEncounterKind kind, int side)
        {
            HauntedEncounter.Create(rideCamera.transform, kind, side);
            AudioClip sound = RandomClip(kind == HauntedEncounterKind.BatBurst
                ? batRushSounds
                : kind == HauntedEncounterKind.SwingingChain ? chainRattleSounds : phantomSounds);
            if (environmentVoice == null || sound == null) return;
            environmentVoice.clip = sound;
            environmentVoice.panStereo = side * 0.58f;
            environmentVoice.volume = 0.74f * effectsVolume;
            environmentVoice.pitch = UnityEngine.Random.Range(0.92f, 1.06f);
            environmentVoice.Play();
        }

        private IEnumerator DirectorScareChain(int side)
        {
            PlayGhostSound(side, 0.34f);
            yield return PulseNearbyTorches(0.72f);
            TriggerEncounter(HauntedEncounterKind.PhantomFace, -side);
            yield return new WaitForSeconds(0.48f);
            PlayRandom(environmentVoice, scareSounds, 0.62f);
        }

        private IEnumerator PulseNearbyTorches(float duration)
        {
            Light[] torches = FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(light => light != null
                    && light.name.IndexOf("Fackelljus", StringComparison.OrdinalIgnoreCase) >= 0
                    && Vector3.Distance(light.transform.position, rideCamera.transform.position) < 24f)
                .ToArray();
            float[] intensities = torches.Select(light => light.intensity).ToArray();
            float end = Time.time + duration;
            while (Time.time < end)
            {
                bool on = Mathf.FloorToInt((end - Time.time) * 12f) % 2 == 0;
                for (int i = 0; i < torches.Length; i++)
                    if (torches[i] != null) torches[i].intensity = on ? intensities[i] : 0.03f;
                yield return null;
            }
            for (int i = 0; i < torches.Length; i++)
                if (torches[i] != null) torches[i].intensity = intensities[i];
        }

        private void UpdateGhostAudio()
        {
            if (Time.time < nextGhostSoundAt || ghostVoice == null || ghostVoice.isPlaying) return;
            PlayGhostSound(UnityEngine.Random.value < 0.5f ? -1 : 1,
                UnityEngine.Random.Range(0.28f, 0.48f));
            nextGhostSoundAt = Time.time + UnityEngine.Random.Range(5.5f, 11.5f);
        }

        private void PlayGhostSound(int side, float volume)
        {
            if (ghostVoice == null || ghostSounds == null || ghostSounds.Length == 0) return;
            ghostVoice.clip = ghostSounds[UnityEngine.Random.Range(0, ghostSounds.Length)];
            ghostVoice.panStereo = side < 0 ? -0.72f : 0.72f;
            ghostVoice.pitch = UnityEngine.Random.Range(0.86f, 1.08f);
            ghostVoice.volume = volume * effectsVolume;
            ghostVoice.Play();
        }

        private void UpdateAim()
        {
            IReadOnlyList<AimSample> samples = aimProvider.GetAimSamples();
            reticles.Clear();
            foreach (Light reticleLight in reticleLights.Values)
                if (reticleLight != null) reticleLight.enabled = false;
            foreach (AimSample rawSample in samples)
            {
                int player = Mathf.Clamp(rawSample.PlayerIndex, 0, 1);
                if (player >= requestedPlayers) continue;
                AimSample sample = SmoothHand(rawSample);
                Ray ray = rideCamera.ViewportPointToRay(new Vector3(sample.Position.x, 1f - sample.Position.y, 0));
                UpdateReticleLight(sample.HandId, player, ray);
                GhostTarget target = FindAimTarget(ray, sample.Position);
                PoseState playerPose;
                bool attackBlocked = currentPoses.TryGetValue(player, out playerPose)
                    && playerPose.DuckAmount >= 0.15f;

                AimLock aimLock;
                if (!aimLocks.TryGetValue(sample.HandId, out aimLock))
                {
                    aimLock = new AimLock();
                    aimLocks[sample.HandId] = aimLock;
                }
                if (target != null && !attackBlocked)
                {
                    aimLock.RecentTarget = target;
                    aimLock.LastTargetAt = Time.time;
                }

                if (attackBlocked)
                {
                    // En duckning är en ren undanmanöver: ingen gammal dwell-
                    // laddning eller mållåsning får ge ett skott på vägen upp.
                    aimLock.RecentTarget = null;
                }

                // Varje hand siktar och kastar självständigt. Kinect kräver
                // ett riktigt framåtkast; att bara hålla siktet på ett monster
                // avfyrar aldrig automatiskt.
                if (!attackBlocked && sample.Fire
                    && Time.time >= aimLock.CooldownUntil)
                {
                    GhostTarget firedTarget = target;
                    if (firedTarget == null && Time.time - aimLock.LastTargetAt <= TargetGraceSeconds)
                        firedTarget = aimLock.RecentTarget;

                    bool rightHand = sample.HandId % 2 == 1;
                    Color boltColor = player == 1
                        ? (rightHand ? new Color(1f, 0.25f, 0.58f) : new Color(1f, 0.50f, 0.20f))
                        : (rightHand ? new Color(0.20f, 0.68f, 1f) : new Color(0.18f, 1f, 0.78f));
                    Vector3 boltStart = ray.origin + ray.direction * 0.78f;
                    Vector3 boltEnd = firedTarget != null
                        ? firedTarget.transform.position + Vector3.up
                        : ray.GetPoint(18f);
                    MagicBolt.Launch(boltStart, boltEnd, boltColor);
                    PlayRandom(effects, castSounds, 0.92f);
                    shots[player]++;
                    if (rightHand) rightHandShots[player]++;
                    else leftHandShots[player]++;
                    if (firedTarget != null)
                    {
                        bool wasBoss = firedTarget.IsBoss;
                        bool wasRelic = firedTarget.GetComponent<CursedRelic>() != null;
                        bool wasBreakable = firedTarget.GetComponent<BreakableProp>() != null;
                        bool wasIllusion = firedTarget.GetComponent<HauntedIllusion>() != null;
                        bool wasTrap = firedTarget.GetComponent<TrapTrigger>() != null;
                        bool wasSecretSeal = firedTarget.GetComponent<SecretRouteSeal>() != null;
                        bool wasRepairSigil = firedTarget.GetComponent<RepairSigil>() != null;
                        hits[player]++;
                        combos[player] = Time.time - lastHitAt[player] <= 2.6f ? combos[player] + 1 : 1;
                        lastHitAt[player] = Time.time;
                        maxCombos[player] = Mathf.Max(maxCombos[player], combos[player]);
                        int multiplier = Mathf.Clamp(1 + combos[player] / 5, 1, 3);
                        int points = firedTarget.Hit() * multiplier;
                        bool killedNow = firedTarget.Health <= 0;
                        if (wasIllusion) points = 0;
                        if (killedNow && !wasBreakable && !wasIllusion && !wasTrap
                            && !wasRelic && !wasSecretSeal && !wasRepairSigil)
                        {
                            kills[player]++;
                            TryRescueTeammate(player);
                        }
                        if (wasRelic && killedNow)
                        {
                            collectedRelics++;
                            points += 100;
                            PlayRandom(effects, movementSuccessSounds, 0.9f);
                        }
                        if (wasBreakable && killedNow) points += 25;
                        if (wasTrap && killedNow) points += TriggerTrap(firedTarget.transform.position, player);
                        if (wasBreakable && killedNow) breakablesDestroyed++;
                        if (wasSecretSeal && killedNow)
                        {
                            secretRouteUnlocked = true;
                            secretsFound++;
                            points += 175;
                            actionMessage = "HEMLIG VÄG UPPLÅST!";
                            actionMessageUntil = Time.time + 2.2f;
                        }
                        if (wasRepairSigil && killedNow)
                        {
                            wagonHealth = Mathf.Min(startingWagonHealth, wagonHealth + 1);
                            points += 90;
                            actionMessage = "VAGNEN REPARERAD!  VAGN " + wagonHealth;
                            actionMessageUntil = Time.time + 2.0f;
                            if (WagonDamageVisual.Instance != null)
                                WagonDamageVisual.Instance.SetHealth(wagonHealth, startingWagonHealth, false);
                            PlayRandom(effects, movementSuccessSounds, 1f);
                        }
                        scores[player] += points;
                        PlayRandom(effects, wasBoss ? bossSounds : hitSounds, wasBoss ? 0.95f : 0.82f);
                        if (!wasSecretSeal && !wasRepairSigil)
                            actionMessage = (wasRelic && killedNow ? "FÖRBANNAD RELIK!  "
                                : wasBreakable && killedNow ? "KROSSAT!  "
                                : wasIllusion ? "ILLUSION!  "
                                : wasTrap && killedNow ? "FÄLLA UTLÖST!  " : string.Empty)
                                + "+" + points + (multiplier > 1 ? "  x" + multiplier : string.Empty);
                        if (!wasSecretSeal && !wasRepairSigil) actionMessageUntil = Time.time + 0.7f;
                        if (wasBoss && !killedNow) UpdateBossPhase(firedTarget);
                        if (wasBoss && firedTarget.Health <= 0)
                        {
                            bossDefeated = true;
                            bossBattle = false;
                            if (bossProjectile != null) Destroy(bossProjectile.gameObject);
                            bossProjectile = null;
                            actionMessage = "KONDUKTÖREN ÄR BESEGRAD – VAGNEN KÖR VIDARE!";
                            actionMessageUntil = Time.time + 2.8f;
                        }
                    }
                    else combos[player] = 0;
                    aimLock.RecentTarget = null;
                    aimLock.CooldownUntil = Time.time + 0.32f;
                }

                reticles[sample.HandId] = new ReticleState
                {
                    PlayerIndex = player,
                    Position = sample.Position,
                    Progress = attackBlocked ? 0f : sample.GestureProgress,
                    OnTarget = target != null,
                    AttackBlocked = attackBlocked,
                    IsRightHand = sample.HandId % 2 == 1
                };
            }
        }

        private AimSample SmoothHand(AimSample sample)
        {
            HandFilterState state;
            if (!handFilters.TryGetValue(sample.HandId, out state))
            {
                state = new HandFilterState
                {
                    Position = sample.Position,
                    LastRaw = sample.Position
                };
                handFilters[sample.HandId] = state;
                return sample;
            }

            Vector2 raw = new Vector2(Mathf.Clamp(sample.Position.x, 0.025f, 0.975f),
                Mathf.Clamp(sample.Position.y, 0.025f, 0.975f));
            float rawJump = Vector2.Distance(raw, state.LastRaw);
            state.LastRaw = raw;

            // Begränsa enskilda Kinect-spikar. Stora riktiga handrörelser
            // kommer fortfarande ikapp över flera bildrutor i stället för att
            // teleportera siktet tvärs över skärmen.
            Vector2 delta = raw - state.Position;
            const float maximumStep = 0.115f;
            if (delta.magnitude > maximumStep)
                raw = state.Position + delta.normalized * maximumStep;

            float distance = Vector2.Distance(state.Position, raw);
            if (distance < 0.0045f) raw = state.Position;
            float smoothTime = Mathf.Lerp(0.15f, 0.075f,
                Mathf.InverseLerp(0.015f, 0.14f, distance));
            if (rawJump > 0.22f) smoothTime = Mathf.Max(smoothTime, 0.13f);
            state.Position = Vector2.SmoothDamp(state.Position, raw, ref state.Velocity,
                smoothTime, 1.65f, Mathf.Max(0.001f, Time.deltaTime));
            sample.Position = state.Position;
            return sample;
        }

        private GhostTarget FindAimTarget(Ray ray, Vector2 handPosition)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 80f))
            {
                GhostTarget direct = hit.collider.GetComponentInParent<GhostTarget>();
                if (direct != null) return direct;
            }

            Vector2 viewportAim = new Vector2(handPosition.x, 1f - handPosition.y);
            GhostTarget best = null;
            float bestDistance = easyMode ? 0.12f : 0.085f;
            foreach (GhostTarget candidate in GhostTarget.ActiveTargets
                .Where(item => item.Health > 0 && item.IsTargetable))
            {
                Vector3 viewport = rideCamera.WorldToViewportPoint(candidate.transform.position + Vector3.up * 1.05f);
                if (viewport.z <= 0f) continue;
                float distance = Vector2.Distance(viewportAim, new Vector2(viewport.x, viewport.y));
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = candidate;
            }
            return best;
        }

        private void UpdateReticleLight(int lightKey, int player, Ray ray)
        {
            Light flashlight;
            if (!reticleLights.TryGetValue(lightKey, out flashlight) || flashlight == null)
            {
                GameObject lightObject = new GameObject("Siktesficklampa spelare " + (player + 1));
                lightObject.transform.SetParent(rideCamera.transform, false);
                flashlight = lightObject.AddComponent<Light>();
                flashlight.type = LightType.Spot;
                flashlight.color = player == 1
                    ? new Color(1f, 0.62f, 0.74f)
                    : new Color(0.70f, 0.86f, 1f);
                flashlight.intensity = 5.25f * brightnessLevel;
                flashlight.range = 30f;
                flashlight.spotAngle = 38f;
                flashlight.innerSpotAngle = 22f;
                flashlight.shadows = LightShadows.None;
                reticleLights[lightKey] = flashlight;
            }

            flashlight.enabled = true;
            flashlight.transform.position = ray.origin + rideCamera.transform.forward * 0.16f;
            flashlight.transform.rotation = Quaternion.LookRotation(ray.direction, rideCamera.transform.up);
        }

        private void UpdateBossBattle()
        {
            if (!bossBattle || bossDefeated) return;
            GhostTarget boss = targets.FirstOrDefault(item => item != null && item.IsBoss && item.Health > 0);
            if (boss == null) return;

            if (bossPhase >= 2 && bossWeakPointUntil > 0f
                && Time.time >= bossWeakPointUntil && boss.IsTargetable)
            {
                boss.SetTargetable(false);
                bossWeakPointUntil = 0f;
            }

            if (bossProjectile != null)
            {
                if (bossProjectile.Arrived) ResolveBossAttack();
                return;
            }

            if (Time.time < nextBossAttackAt) return;
            HazardKind kind = (HazardKind)UnityEngine.Random.Range(0, 3);
            Vector3 start = boss.transform.position + new Vector3(0f, 2.15f, -0.6f);
            Vector3 end = rideCamera.transform.position + rideCamera.transform.forward * 1.15f;
            bossProjectile = BossProjectile.Create(kind, start, end, easyMode);
            if (bossPhase >= 2) boss.SetTargetable(false);
            if (bossPhase >= 2)
            {
                Vector3 shifted = boss.transform.position;
                shifted.x = DarkRideWorld.TrackCenter(shifted.z) + UnityEngine.Random.Range(-3.2f, 3.2f);
                boss.transform.position = shifted;
            }
            float delay = bossPhase == 1 ? UnityEngine.Random.Range(3.0f, 4.0f)
                : bossPhase == 2 ? UnityEngine.Random.Range(2.4f, 3.2f)
                : UnityEngine.Random.Range(1.8f, 2.6f);
            nextBossAttackAt = Time.time + delay;
            PlayRandom(effects, scareSounds);
        }

        private void UpdateBossPhase(GhostTarget boss)
        {
            int newPhase = boss.Health <= 5 ? 3 : boss.Health <= 10 ? 2 : 1;
            if (newPhase <= bossPhase) return;
            bossPhase = newPhase;
            boss.SetTargetable(false);
            bossWeakPointUntil = 0f;
            actionMessage = newPhase == 2
                ? "FAS 2 – KONDUKTÖREN KALLAR PÅ DE DÖDA!"
                : "FAS 3 – ALLA LJUS SLOCKNAR!";
            actionMessageUntil = Time.time + 2.4f;
            PlayRandom(effects, scareSounds, 1f);
            SummonBossMinions(newPhase);
            if (newPhase == 3) StartCoroutine(BossBlackout());
        }

        private void SummonBossMinions(int phase)
        {
            int count = phase == 2 ? 2 : 3;
            for (int i = 0; i < count; i++)
            {
                float lane = (i - (count - 1) * 0.5f) * 2.25f;
                float z = BossStopDistance + 10.5f + (i % 2) * 2f;
                TargetKind kind = i % 2 == 0 ? TargetKind.Ghost : TargetKind.Zombie;
                targets.Add(GhostTarget.Create(kind,
                    new Vector3(DarkRideWorld.TrackCenter(z) + lane, kind == TargetKind.Ghost ? 1f : 0.3f, z)));
            }
        }

        private IEnumerator BossBlackout()
        {
            Light[] torches = FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(light => light != null && light.name.IndexOf("Fackelljus", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            foreach (Light torch in torches) torch.enabled = false;
            yield return new WaitForSeconds(2.8f);
            foreach (Light torch in torches)
                if (torch != null) torch.enabled = true;
        }

        private void ResolveBossAttack()
        {
            bool anyoneFailed = false;
            foreach (KeyValuePair<int, PoseState> pair in currentPoses)
            {
                bool succeeds = bossProjectile.Kind == HazardKind.Duck
                    ? pair.Value.DuckAmount >= (easyMode ? 0.14f : 0.18f)
                    : bossProjectile.Kind == HazardKind.DodgeLeft
                        ? pair.Value.LeanAmount <= (easyMode ? -0.12f : -0.15f)
                        : pair.Value.LeanAmount >= (easyMode ? 0.12f : 0.15f);
                int player = Mathf.Clamp(pair.Key, 0, 1);
                if (succeeds)
                {
                    scores[player] += 35;
                    PlayRandom(effects, movementSuccessSounds);
                }
                else
                {
                    anyoneFailed = true;
                    scores[player] = Mathf.Max(0, scores[player] - 30);
                    DamagePlayer(player, 1);
                }
            }

            if (anyoneFailed || currentPoses.Count == 0)
            {
                if (currentPoses.Count == 0) DamageWagon(1, "BOSSEN TRÄFFADE VAGNEN!");
                PlayRandom(effects, collisionSounds);
                cameraShakeUntil = Time.time + 0.55f;
            }
            else
            {
                PlayAnnouncement(bossSuccessVoice);
                GhostTarget boss = targets.FirstOrDefault(item => item != null && item.IsBoss && item.Health > 0);
                if (boss != null)
                {
                    boss.SetTargetable(true);
                    bossWeakPointUntil = Time.time + (easyMode ? 3.4f : 2.7f);
                    actionMessage = "SVAG PUNKT ÖPPEN – ATTACKERA!";
                    actionMessageUntil = bossWeakPointUntil;
                    nextBossAttackAt = Mathf.Max(nextBossAttackAt, bossWeakPointUntil + 0.35f);
                }
            }
            Destroy(bossProjectile.gameObject);
            bossProjectile = null;
        }

        private void PlayAnnouncement(AudioClip clip, bool interrupt = false)
        {
            if (announcerVoice == null || clip == null) return;
            if (!interrupt && (announcerVoice.isPlaying || Time.time < nextAnnouncementAt)) return;
            if (interrupt) announcerVoice.Stop();
            announcerVoice.clip = clip;
            announcerVoice.pitch = 1f;
            announcerVoice.Play();
            nextAnnouncementAt = Time.time + 0.30f;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (flow == GameFlow.Menu)
            {
                DrawStartMenu();
                return;
            }
            if (flow == GameFlow.Calibration)
            {
                DrawCalibration();
                return;
            }
            float rideTime = Mathf.Max(0, gameTime - CountdownSeconds);

            GUI.Box(new Rect(18, 16, 430, 82), string.Empty);
            GUI.Label(new Rect(34, 23, 400, 34), "SPÖKJAKTEN 3D", titleStyle);
            GUI.Label(new Rect(35, 58, Mathf.Max(400, Screen.width - 70), 42), inputStatus, smallStyle);

            GUI.Box(new Rect(Screen.width - 315, 16, 297, 126), string.Empty);
            GUI.Label(new Rect(Screen.width - 298, 23, 280, 32), "SPELARE 1   " + scores[0], hudStyle);
            GUI.Label(new Rect(Screen.width - 298, 57, 280, 26), bossBattle
                ? "SLUTBOSS – VAGNEN STÅR STILL"
                : "FÄRD   " + Mathf.RoundToInt(rideDistance / DarkRideWorld.TrackLength * 100f) + " %", smallStyle);
            GUI.Label(new Rect(Screen.width - 145, Screen.height - 30, 130, 22), "F11  HELSKÄRM", smallStyle);
            if (requestedPlayers > 1)
                GUI.Label(new Rect(Screen.width - 298, 83, 280, 26), "SPELARE 2   " + scores[1], smallStyle);
            GUI.Label(new Rect(Screen.width - 298, 105, 280, 25),
                "VAGN " + new string('♥', wagonHealth) + "   COMBO " + Mathf.Max(combos[0], combos[1]), smallStyle);

            GUI.Label(new Rect(22, 102, 390, 24),
                "LIV P1 " + new string('♥', lives[0])
                + (requestedPlayers > 1 ? "    P2 " + new string('♥', lives[1]) : string.Empty)
                + "    RELIKER " + collectedRelics + "/" + TotalRelics, smallStyle);

            if (showSettings)
            {
                GUI.Box(new Rect(18, 138, 390, 164), string.Empty);
                GUI.Label(new Rect(34, 148, 355, 145),
                    "INSTÄLLNINGAR / TEST\n"
                     + "F1: stäng   M: musik " + (musicMuted ? "av" : "på")
                     + "   [ ]: musikvolym " + Mathf.RoundToInt(musicVolume * 100f) + "%\n"
                     + "Effekter " + Mathf.RoundToInt(effectsVolume * 100f) + "%   Röst "
                     + Mathf.RoundToInt(voiceVolume * 100f) + "%\n"
                     + "- / +: ljusstyrka " + Mathf.RoundToInt(brightnessLevel * 100f) + "%\n"
                    + "F2: nästa kontrollpunkt   B: slutboss\n"
                    + "F11: helskärm   P/Mellanslag: paus", smallStyle);
            }

            foreach (ReticleState reticle in reticles.Values)
            {
                float x = reticle.Position.x * Screen.width;
                float y = reticle.Position.y * Screen.height;
                Texture2D ring = reticle.PlayerIndex == 1 ? playerTwoRing : playerOneRing;
                Color previous = GUI.color;
                GUI.color = reticle.OnTarget ? Color.white : new Color(1, 1, 1, 0.72f);
                GUI.DrawTexture(new Rect(x - 34, y - 34, 68, 68), ring);
                GUI.color = previous;
                GUI.DrawTexture(new Rect(x - 34, y + 39, 68, 9), whiteTexture);
                GUI.DrawTexture(new Rect(x - 32, y + 41, 64 * reticle.Progress, 5), goldTexture);
                string handLabel = reticle.IsRightHand
                    ? "HÖGER HAND – KASTA FRAMÅT"
                    : "VÄNSTER HAND – KASTA FRAMÅT";
                if (reticle.AttackBlocked) handLabel = "DUCKAR – ATTACK LÅST";
                GUI.Label(new Rect(x - 105, y + 51, 210, 22), handLabel, smallStyle);
            }

            if (currentHazard != null)
            {
                float gap = currentHazard.TrackZ - rideDistance;
                DrawMovementCue(currentHazard.Kind, Mathf.InverseLerp(QuickEventCueDistance, 0f, gap));
            }

            // Bossens riktningspil är en ren reaktionssignal. Visa den först
            // under den sista delen av kastet, när spelaren faktiskt ska röra sig.
            if (bossProjectile != null && bossProjectile.Progress >= 0.58f && !bossProjectile.Arrived)
            {
                DrawMovementCue(bossProjectile.Kind,
                    Mathf.InverseLerp(0.58f, 1f, bossProjectile.Progress));
            }

            DrawEnemyHealthBars();

            if (routeChoiceActive) DrawRouteChoice();

            if (Time.time < zoneTitleUntil)
            {
                float alpha = Mathf.Clamp01((zoneTitleUntil - Time.time) / 0.45f);
                Color old = GUI.color;
                GUI.color = new Color(1f, 0.82f, 0.58f, alpha);
                GUI.Label(new Rect(Screen.width * 0.5f - 330f, Screen.height * 0.18f, 660f, 54f),
                    zoneTitle, centerStyle);
                GUI.color = old;
            }

            if (Time.time < actionMessageUntil)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 190, Screen.height * 0.34f, 380, 54),
                    actionMessage, centerStyle);
            }

            GhostTarget boss = targets.FirstOrDefault(item => item != null && item.IsBoss);
            if (boss != null)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 190, Screen.height - 72, 380, 48), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 170, Screen.height - 66, 340, 24),
                    bossPhase >= 2
                        ? (boss.IsTargetable ? "SVAG PUNKT – SKJUT!" : "KONDUKTÖREN ÄR SKYDDAD")
                        : "ZOMBIE-KONDUKTÖREN", centerStyle);
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 155, Screen.height - 38, 310, 10), whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 153, Screen.height - 36,
                    306f * boss.Health / boss.MaxHealth, 6), goldTexture);
            }

            if (flow == GameFlow.Countdown)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 285, Screen.height * 0.5f - 115, 570, 230), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 250, Screen.height * 0.5f - 91, 500, 58),
                    Mathf.CeilToInt(CountdownSeconds - gameTime).ToString(), centerStyle);
                GUI.Label(new Rect(Screen.width * 0.5f - 255, Screen.height * 0.5f - 20, 510, 105),
                    "GÖR ER REDO!\n" + (easyMode ? "BARNLÄGE" : "NORMALT LÄGE"), centerStyle);
            }
            else if (rideTime < 9f)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 330, 112, 660, 68), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 315, 118, 630, 54),
                    "Sikta och gör en snabb stöt framåt med valfri hand – ducka när pilarna visas!", centerStyle);
            }

            if (paused)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 180, Screen.height * 0.5f - 60, 360, 120), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.5f - 36, 300, 72),
                    "PAUS\nMellanslag för att fortsätta", centerStyle);
            }
            else if (finished)
            {
                int totalShots = shots[0] + shots[1];
                int totalHits = hits[0] + hits[1];
                int accuracy = totalShots > 0 ? Mathf.RoundToInt(totalHits * 100f / totalShots) : 0;
                GUI.Box(new Rect(Screen.width * 0.5f - 330, Screen.height * 0.5f - 195, 660, 390), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 305, Screen.height * 0.5f - 178, 610, 350),
                    (gameOver ? "VAGNEN GICK SÖNDER" : "BRA JAGAT!")
                    + "\nBETYG: " + FinalGrade(accuracy)
                    + "\nPoäng: " + (scores[0] + scores[1])
                    + "   Besegrade: " + (kills[0] + kills[1])
                    + "   Träffsäkerhet: " + accuracy + "%"
                    + "\nBästa combo: " + Mathf.Max(maxCombos[0], maxCombos[1])
                    + "   Reliker: " + collectedRelics + "/" + TotalRelics
                    + "   Vagn: " + wagonHealth
                    + "\nREKORD  Poäng: " + PlayerPrefs.GetInt("SpokjaktenHighScore", 0)
                    + "   Combo: " + PlayerPrefs.GetInt("SpokjaktenBestCombo", 0)
                    + "   Reliker: " + PlayerPrefs.GetInt("SpokjaktenBestRelics", 0)
                    + "\nHemligheter: " + secretsFound + "   Krossat: " + breakablesDestroyed
                    + "   Extra skräckhändelser: " + directorEvents
                    + "\nMEDALJER: " + MedalText(accuracy)
                    + "\nBästa antal medaljer: " + PlayerPrefs.GetInt("SpokjaktenBestMedals", 0)
                    + "\nTryck R för att gå tillbaka till startmenyn", centerStyle);
            }
        }

        private void DrawStartMenu()
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), string.Empty);
            float width = Mathf.Min(680f, Screen.width - 60f);
            float height = Mathf.Min(720f, Screen.height - 50f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            GUI.Box(new Rect(x, y, width, height), string.Empty);
            GUI.Label(new Rect(x + 30f, y + 20f, width - 60f, 54f), "SPÖKJAKTEN 3D", centerStyle);
            GUI.Label(new Rect(x + 35f, y + 70f, width - 70f, 42f),
                "Välj inställningar och kalibrera Kinect innan vagnen startar", centerStyle);

            float rowY = y + 128f;
            GUI.Label(new Rect(x + 48f, rowY, 180f, 30f), "SVÅRIGHET", hudStyle);
            if (GUI.Button(new Rect(x + 240f, rowY, 170f, 38f), "BARNLÄGE" + (easyMode ? "  ✓" : string.Empty)))
            {
                easyMode = true;
                SavePreferences();
            }
            if (GUI.Button(new Rect(x + 420f, rowY, 170f, 38f), "NORMALT" + (!easyMode ? "  ✓" : string.Empty)))
            {
                easyMode = false;
                SavePreferences();
            }

            rowY += 55f;
            GUI.Label(new Rect(x + 48f, rowY, 180f, 30f), "SPELARE", hudStyle);
            if (GUI.Button(new Rect(x + 240f, rowY, 170f, 38f), "1 SPELARE" + (requestedPlayers == 1 ? "  ✓" : string.Empty)))
            {
                requestedPlayers = 1;
                SavePreferences();
            }
            if (GUI.Button(new Rect(x + 420f, rowY, 170f, 38f), "2 SPELARE" + (requestedPlayers == 2 ? "  ✓" : string.Empty)))
            {
                requestedPlayers = 2;
                SavePreferences();
            }

            rowY += 65f;
            DrawMenuSlider(x, width, ref rowY, "MUSIK", ref musicVolume, 0f, 1.2f, true);
            DrawMenuSlider(x, width, ref rowY, "LJUDEFFEKTER", ref effectsVolume, 0f, 1f, false);
            DrawMenuSlider(x, width, ref rowY, "SVENSK RÖST", ref voiceVolume, 0f, 1f, false);

            GUI.Label(new Rect(x + 48f, rowY, 190f, 30f), "LJUSSTYRKA", hudStyle);
            float newBrightness = GUI.HorizontalSlider(new Rect(x + 240f, rowY + 8f, 270f, 24f),
                brightnessLevel, 0.65f, 1.55f);
            GUI.Label(new Rect(x + 525f, rowY, 80f, 30f), Mathf.RoundToInt(newBrightness * 100f) + "%", smallStyle);
            if (Mathf.Abs(newBrightness - brightnessLevel) > 0.002f)
                AdjustBrightness(newBrightness - brightnessLevel);
            rowY += 48f;

            string fullscreenLabel = Screen.fullScreen ? "HELSKÄRM: PÅ" : "HELSKÄRM: AV";
            if (GUI.Button(new Rect(x + 185f, rowY, 310f, 40f), fullscreenLabel + "   (F11)"))
            {
                Screen.fullScreen = !Screen.fullScreen;
                SavePreferences();
            }

            float statusY = y + height - 176f;
            GUI.Label(new Rect(x + 45f, statusY, width - 90f, 58f), MenuInputStatus(), centeredSmallStyle);
            if (GUI.Button(new Rect(x + 145f, y + height - 92f, width - 290f, 58f), "KALIBRERA OCH STARTA"))
                BeginCalibration();
            DrawInterfaceReticles();
        }

        private string MenuInputStatus()
        {
            bool kinect = aimProvider != null && aimProvider.IsAvailable
                && (aimProvider is KinectBridgeAimProvider || aimProvider is KinectV1AimProvider);
            if (kinect) return "KINECT ANSLUTEN\n" + aimProvider.Status;
            return "KINECT EJ TILLGÄNGLIG – MUSLÄGE AKTIVT\n"
                + "Kontrollera USB/ström och stäng andra Kinect-program innan spelet startas om.";
        }

        private void DrawMenuSlider(float x, float width, ref float rowY, string label,
            ref float value, float minimum, float maximum, bool music)
        {
            GUI.Label(new Rect(x + 48f, rowY, 190f, 30f), label, hudStyle);
            float changed = GUI.HorizontalSlider(new Rect(x + 240f, rowY + 8f, 270f, 24f),
                value, minimum, maximum);
            GUI.Label(new Rect(x + 525f, rowY, 80f, 30f), Mathf.RoundToInt(changed * 100f) + "%", smallStyle);
            if (Mathf.Abs(changed - value) > 0.002f)
            {
                value = changed;
                if (music) musicMuted = false;
                ApplyAudioVolumes();
                SavePreferences();
            }
            rowY += 48f;
        }

        private void DrawCalibration()
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), string.Empty);
            float width = Mathf.Min(760f, Screen.width - 50f);
            float height = Mathf.Min(650f, Screen.height - 40f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            GUI.Box(new Rect(x, y, width, height), string.Empty);
            GUI.Label(new Rect(x + 30f, y + 18f, width - 60f, 48f), "KINECT-KALIBRERING", centerStyle);
            GUI.Label(new Rect(x + 45f, y + 68f, width - 90f, 58f),
                "Stå rakt och stilla tills mätaren är full. Sikta och gör sedan en snabb stöt framåt med varje hand.", centerStyle);

            float progress = Mathf.Clamp01(calibrationStableSeconds / 1.6f);
            GUI.DrawTexture(new Rect(x + 110f, y + 132f, width - 220f, 18f), whiteTexture);
            GUI.DrawTexture(new Rect(x + 113f, y + 135f, (width - 226f) * progress, 12f), goldTexture);
            GUI.Label(new Rect(x + 110f, y + 128f, width - 220f, 25f),
                Mathf.RoundToInt(progress * 100f) + "%", centeredSmallStyle);
            GUI.Label(new Rect(x + 80f, y + 154f, width - 160f, 30f),
                CalibrationProgressText(), centerStyle);

            float cardWidth = requestedPlayers == 1 ? width - 110f : (width - 135f) * 0.5f;
            for (int player = 0; player < requestedPlayers; player++)
            {
                float cardX = requestedPlayers == 1 ? x + 55f : x + 45f + player * (cardWidth + 45f);
                DrawCalibrationCard(player, new Rect(cardX, y + 195f, cardWidth, 245f));
            }

            DrawInterfaceReticles();

            GUI.Label(new Rect(x + 45f, y + 448f, width - 90f, 44f),
                "BÅDA HÄNDER: sikta stilla och slå snabbt framåt direkt från siktläget.", centeredSmallStyle);
            if (GUI.Button(new Rect(x + 45f, y + height - 72f, 150f, 42f), "TILLBAKA"))
                flow = GameFlow.Menu;
            GUI.enabled = calibrationTrackingStable;
            if (GUI.Button(new Rect(x + width - 315f, y + height - 78f, 270f, 52f),
                    calibrationTrackingStable ? "STARTA ÅKTUREN" : "VÄNTAR PÅ STABIL KINECT"))
                CompleteCalibration();
            GUI.enabled = true;
        }

        private void DrawCalibrationCard(int player, Rect card)
        {
            GUI.Box(card, string.Empty);
            bool body = currentPoses.ContainsKey(player);
            bool kinectInput = aimProvider is KinectBridgeAimProvider || aimProvider is KinectV1AimProvider;
            bool hands = calibrationVisibleHands[player] >= (kinectInput ? 2 : 1);
            string attackTests = kinectInput
                ? CalibrationMark(calibrationLeftCastPassed[player]) + " Vänster kast testat\n"
                    + CalibrationMark(calibrationRightCastPassed[player]) + " Höger kast testat\n"
                : CalibrationMark(calibrationCastPassed[player]) + " Musklick testat\n";
            GUI.Label(new Rect(card.x + 16f, card.y + 12f, card.width - 32f, 32f),
                "SPELARE " + (player + 1), hudStyle);
            GUI.Label(new Rect(card.x + 18f, card.y + 55f, card.width - 36f, 170f),
                CalibrationMark(body) + " Kropp hittad\n"
                + CalibrationMark(hands) + " Händer och sikte\n"
                + attackTests
                + CalibrationMark(calibrationDuckPassed[player]) + " Duckning testad\n"
                + CalibrationMark(calibrationLeanPassed[player]) + " Sidoväjning testad",
                centerStyle);
        }

        private void DrawInterfaceReticles()
        {
            foreach (ReticleState reticle in reticles.Values)
            {
                float x = reticle.Position.x * Screen.width;
                float y = reticle.Position.y * Screen.height;
                GUI.DrawTexture(new Rect(x - 27f, y - 27f, 54f, 54f),
                    reticle.PlayerIndex == 1 ? playerTwoRing : playerOneRing);
                GUI.DrawTexture(new Rect(x - 28f, y + 32f, 56f, 8f), whiteTexture);
                GUI.DrawTexture(new Rect(x - 26f, y + 34f, 52f * reticle.Progress, 4f), goldTexture);
            }
        }

        private string CalibrationProgressText()
        {
            if (calibrationTrackingStable) return "SPÅRNINGEN ÄR STABIL";
            bool kinectInput = aimProvider is KinectBridgeAimProvider || aimProvider is KinectV1AimProvider;
            for (int player = 0; player < requestedPlayers; player++)
            {
                if (!currentPoses.ContainsKey(player))
                    return "STÄLL SPELARE " + (player + 1) + " MITT FRAMFÖR KINECT";
                if (calibrationVisibleHands[player] < (kinectInput ? 2 : 1))
                    return "VISA BÅDA HÄNDERNA TYDLIGT";
            }
            if (calibrationStableSeconds > 0.05f)
                return "BRA – HÅLL ER STILLA  "
                    + Mathf.CeilToInt(1.6f - calibrationStableSeconds) + " s";
            return "STÅ RAKT OCH HÅLL ER STILLA";
        }

        private static string CalibrationMark(bool complete)
        {
            return complete ? "✓" : "•";
        }

        private void DrawEnemyHealthBars()
        {
            foreach (GhostTarget target in GhostTarget.ActiveTargets
                .Where(item => item.IsTargetable && !item.IsBoss && item.MaxHealth > 1))
            {
                Vector3 viewport = rideCamera.WorldToViewportPoint(target.transform.position + Vector3.up * 2.15f);
                if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) continue;
                float x = viewport.x * Screen.width;
                float y = (1f - viewport.y) * Screen.height;
                GUI.DrawTexture(new Rect(x - 31f, y, 62f, 7f), whiteTexture);
                GUI.DrawTexture(new Rect(x - 29f, y + 2f, 58f * target.Health / target.MaxHealth, 3f), goldTexture);
            }
        }

        private string FinalGrade(int accuracy)
        {
            if (gameOver) return "D";
            int value = scores[0] + scores[1] + collectedRelics * 100 + wagonHealth * 40 + accuracy * 3;
            return value >= 1900 ? "S" : value >= 1400 ? "A" : value >= 950 ? "B" : value >= 550 ? "C" : "D";
        }

        private int CountMedals()
        {
            int totalShots = shots[0] + shots[1];
            int accuracy = totalShots > 0 ? Mathf.RoundToInt((hits[0] + hits[1]) * 100f / totalShots) : 0;
            int count = 0;
            if (!gameOver && wagonDamageTaken == 0) count++;
            if (accuracy >= 70) count++;
            if (collectedRelics >= TotalRelics) count++;
            if (leftHandShots.Sum() >= 5 && rightHandShots.Sum() >= 5) count++;
            if (secretRouteUnlocked) count++;
            if (breakablesDestroyed >= 4) count++;
            return count;
        }

        private string MedalText(int accuracy)
        {
            List<string> medals = new List<string>();
            if (!gameOver && wagonDamageTaken == 0) medals.Add("OSKADD VAGN");
            if (accuracy >= 70) medals.Add("SKARPSKYTT");
            if (collectedRelics >= TotalRelics) medals.Add("RELIKJÄGARE");
            if (leftHandShots.Sum() >= 5 && rightHandShots.Sum() >= 5) medals.Add("DUBBELHAND");
            if (secretRouteUnlocked) medals.Add("HEMLIGHETSFUNNEN");
            if (breakablesDestroyed >= 4) medals.Add("KROSSARE");
            return medals.Count == 0 ? "INGA ÄNNU" : string.Join("  •  ", medals);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.76f, 0.91f, 1f) } };
            centeredSmallStyle = new GUIStyle(smallStyle)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            centerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                wordWrap = true
            };
        }

        private void CreateHudTextures()
        {
            playerOneRing = CreateRing(new Color(0.25f, 0.72f, 1f));
            playerTwoRing = CreateRing(new Color(1f, 0.35f, 0.62f));
            whiteTexture = SolidTexture(new Color(1, 1, 1, 0.88f));
            goldTexture = SolidTexture(new Color(1f, 0.72f, 0.08f));
            movementArrow = CreateArrowTexture();
        }

        private void DrawMovementCue(HazardKind kind, float urgency)
        {
            if (movementArrow == null) return;
            float rotation = kind == HazardKind.Duck ? 90f : kind == HazardKind.DodgeLeft ? 180f : 0f;
            Vector2 direction = kind == HazardKind.Duck ? Vector2.down
                : kind == HazardKind.DodgeLeft ? Vector2.left : Vector2.right;
            float wave = Mathf.Repeat(Time.time * 2.8f, 1f);
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            for (int i = 0; i < 3; i++)
            {
                float phase = Mathf.Repeat(wave + i * 0.24f, 1f);
                float size = Mathf.Lerp(76f, 112f, urgency) * (0.90f + phase * 0.10f);
                Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.25f)
                    + direction * (phase * 66f - 25f);
                GUI.color = new Color(1f, Mathf.Lerp(0.82f, 0.28f, urgency), 0.08f,
                    Mathf.Sin(phase * Mathf.PI) * 0.82f + 0.12f);
                GUIUtility.RotateAroundPivot(rotation, center);
                GUI.DrawTexture(new Rect(center.x - size * 0.5f, center.y - size * 0.35f,
                    size, size * 0.70f), movementArrow);
                GUI.matrix = oldMatrix;
            }
            GUI.color = oldColor;
            GUI.matrix = oldMatrix;
        }

        private void DrawRouteChoice()
        {
            float pulse = 0.65f + Mathf.Sin(Time.time * 5f) * 0.12f;
            GUI.Box(new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.14f, 300f, 50f), string.Empty);
            GUI.Label(new Rect(Screen.width * 0.5f - 140f, Screen.height * 0.145f, 280f, 40f),
                secretRouteUnlocked ? "VÄLJ VÄG – HEMLIG VÄG UPPLÅST" : "VÄLJ VÄG – VÄNSTER ÄR LÅST", centerStyle);
            DrawRouteArrow(new Vector2(Screen.width * 0.27f, Screen.height * 0.35f), 180f, pulse,
                new Color(0.18f, 1f, 0.52f));
            DrawRouteArrow(new Vector2(Screen.width * 0.73f, Screen.height * 0.35f), 0f, pulse,
                new Color(0.74f, 0.22f, 1f));
        }

        private void DrawRouteArrow(Vector2 center, float rotation, float pulse, Color color)
        {
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(rotation, center);
            float width = 150f * pulse;
            GUI.DrawTexture(new Rect(center.x - width * 0.5f, center.y - 44f, width, 88f), movementArrow);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private static Texture2D CreateArrowTexture()
        {
            const int width = 96;
            const int height = 64;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool shaft = x >= 8 && x <= 57 && y >= 23 && y <= 40;
                int arrowX = x - 50;
                bool head = arrowX >= 0 && arrowX <= 40
                    && Mathf.Abs(y - 31) <= (40 - arrowX) * 0.72f;
                texture.SetPixel(x, y, shaft || head ? Color.white : Color.clear);
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateRing(Color color)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                bool ring = distance > 23f && distance < 30f;
                bool cross = (Mathf.Abs(x - 31.5f) < 1.4f || Mathf.Abs(y - 31.5f) < 1.4f) && distance < 12f;
                texture.SetPixel(x, y, ring || cross ? color : Color.clear);
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D SolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static AudioClip[] LoadClipSet(string resourceFolder, string namePrefix, AudioClip fallback)
        {
            AudioClip[] loaded = Resources.LoadAll<AudioClip>(resourceFolder)
                .Where(clip => clip != null && clip.name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(clip => clip.name)
                .ToArray();
            return loaded.Length > 0 ? loaded : new[] { fallback };
        }

        private static AudioClip[] LoadNamedClips(AudioClip fallback, params string[] resourcePaths)
        {
            AudioClip[] loaded = resourcePaths
                .Select(path => Resources.Load<AudioClip>(path))
                .Where(clip => clip != null)
                .ToArray();
            return loaded.Length > 0 ? loaded : new[] { fallback };
        }

        private static AudioClip RandomClip(AudioClip[] clips)
        {
            return clips == null || clips.Length == 0 ? null : clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        private static void PlayRandom(AudioSource source, AudioClip[] clips, float volume = 1f)
        {
            AudioClip clip = RandomClip(clips);
            if (source != null && clip != null) source.PlayOneShot(clip, volume);
        }

        private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - i / (float)length;
                samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateAmbience()
        {
            const int sampleRate = 22050;
            const int seconds = 4;
            float[] samples = new float[sampleRate * seconds];
            var random = new System.Random(731);
            float filteredNoise = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float noise = (float)(random.NextDouble() * 2 - 1);
                filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.015f);
                samples[i] = (Mathf.Sin(t * 43f * Mathf.PI * 2f) * 0.035f + filteredNoise * 0.06f);
            }
            AudioClip clip = AudioClip.Create("Spöktunnelns atmosfär", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateRideMusic()
        {
            const int sampleRate = 22050;
            const int seconds = 16;
            float[] samples = new float[sampleRate * seconds];
            float[] melody = { 220f, 261.63f, 293.66f, 329.63f, 293.66f, 261.63f, 246.94f, 196f };
            float[] bass = { 55f, 65.41f, 49f, 55f };
            var random = new System.Random(1313);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                int melodyStep = Mathf.FloorToInt(t * 2f) % melody.Length;
                int bassStep = Mathf.FloorToInt(t / 4f) % bass.Length;
                float notePhase = (t * 2f) % 1f;
                float noteEnvelope = Mathf.Clamp01(1f - notePhase * 1.15f);
                float arpeggio = Mathf.Sin(t * melody[melodyStep] * Mathf.PI * 2f) * noteEnvelope * 0.055f;
                float bell = Mathf.Sin(t * melody[melodyStep] * 2f * Mathf.PI * 2f) * noteEnvelope * 0.014f;
                float lowDrone = Mathf.Sin(t * bass[bassStep] * Mathf.PI * 2f) * 0.052f;
                float beatPhase = (t * 2f) % 1f;
                float beat = beatPhase < 0.055f
                    ? ((float)random.NextDouble() * 2f - 1f) * (1f - beatPhase / 0.055f) * 0.055f
                    : 0f;
                float swell = Mathf.Sin(t * Mathf.PI / 4f) * 0.018f;
                samples[i] = Mathf.Clamp(arpeggio + bell + lowDrone + beat + swell, -0.35f, 0.35f);
            }
            AudioClip clip = AudioClip.Create("Spökjaktens musik", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateGhostVoice(string clipName, float duration, float baseFrequency, int seed)
        {
            const int sampleRate = 22050;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(seed);
            float breath = 0f;
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = i / (float)length;
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                float wobble = Mathf.Sin(t * 3.1f + seed) * 18f + Mathf.Sin(t * 7.7f) * 6f;
                phase += (baseFrequency + wobble) / sampleRate * Mathf.PI * 2f;
                float noise = (float)random.NextDouble() * 2f - 1f;
                breath = Mathf.Lerp(breath, noise, 0.035f);
                float voice = Mathf.Sin(phase) * 0.55f
                    + Mathf.Sin(phase * 0.503f) * 0.24f
                    + breath * 0.34f;
                samples[i] = voice * envelope * (0.11f + Mathf.Sin(t * 5.2f) * 0.025f);
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateEvilLaugh(string clipName, float duration, int seed)
        {
            const int sampleRate = 22050;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(seed);
            float phase = 0f;
            float rasp = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = i / (float)length;
                float syllable = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 5.4f * Mathf.PI)), 1.7f);
                float envelope = Mathf.Sin(normalized * Mathf.PI) * (0.22f + syllable * 0.78f);
                float pitch = 132f + Mathf.Sin(t * 4.6f) * 31f + syllable * 42f;
                phase += pitch / sampleRate * Mathf.PI * 2f;
                float noise = (float)random.NextDouble() * 2f - 1f;
                rasp = Mathf.Lerp(rasp, noise, 0.08f);
                samples[i] = (Mathf.Sin(phase) * 0.12f + Mathf.Sin(phase * 0.51f) * 0.06f
                    + rasp * 0.025f) * envelope;
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateWarningSound()
        {
            const int sampleRate = 22050;
            const float duration = 0.58f;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Sin(i / (float)length * Mathf.PI);
                float pulse = 0.45f + Mathf.Max(0f, Mathf.Sin(t * 13f * Mathf.PI)) * 0.55f;
                samples[i] = (Mathf.Sin(t * 520f * Mathf.PI * 2f) * 0.08f
                    + Mathf.Sin(t * 780f * Mathf.PI * 2f) * 0.035f) * envelope * pulse;
            }
            AudioClip clip = AudioClip.Create("Varning inför rörelsehinder", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateCreakSound()
        {
            const int sampleRate = 22050;
            const float duration = 1.85f;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(5531);
            float rough = 0f;
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = i / (float)length;
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                rough = Mathf.Lerp(rough, (float)random.NextDouble() * 2f - 1f, 0.055f);
                phase += (72f + Mathf.Sin(t * 5.2f) * 24f) / sampleRate * Mathf.PI * 2f;
                samples[i] = (Mathf.Sin(phase) * 0.07f + rough * 0.075f) * envelope;
            }
            AudioClip clip = AudioClip.Create("Tung port öppnas", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateMetalRattle()
        {
            const int sampleRate = 22050;
            const float duration = 1.18f;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(8802);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float hit = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 19f * Mathf.PI)), 7f);
                float metal = Mathf.Sin(t * 1360f * Mathf.PI * 2f) + Mathf.Sin(t * 1870f * Mathf.PI * 2f) * 0.55f;
                float noise = (float)random.NextDouble() * 2f - 1f;
                samples[i] = (metal * 0.045f + noise * 0.022f) * hit * (1f - i / (float)length);
            }
            AudioClip clip = AudioClip.Create("Kedjor skramlar", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateNoiseBurst(string clipName, float duration, float volume, int seed)
        {
            const int sampleRate = 22050;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(seed);
            float filtered = 0f;
            for (int i = 0; i < length; i++)
            {
                float envelope = Mathf.Pow(1f - i / (float)length, 2f);
                float noise = (float)random.NextDouble() * 2f - 1f;
                filtered = Mathf.Lerp(filtered, noise, 0.16f);
                samples[i] = filtered * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (aimProvider != null) aimProvider.Dispose();
            foreach (Light reticleLight in reticleLights.Values)
                if (reticleLight != null) Destroy(reticleLight.gameObject);
        }

        private sealed class AimLock
        {
            public GhostTarget RecentTarget;
            public GhostTarget ChargeTarget;
            public float LastTargetAt;
            public float CooldownUntil;
            public float Charge;
        }

        private sealed class PoseCalibration
        {
            public float CenterX;
            public float StandingHeadY;
        }

        private struct PoseState
        {
            public float DuckAmount;
            public float LeanAmount;
        }

        private struct ReticleState
        {
            public int PlayerIndex;
            public Vector2 Position;
            public float Progress;
            public bool OnTarget;
            public bool IsRightHand;
            public bool AttackBlocked;
        }

        private sealed class HandFilterState
        {
            public Vector2 Position;
            public Vector2 LastRaw;
            public Vector2 Velocity;
        }
    }
}
