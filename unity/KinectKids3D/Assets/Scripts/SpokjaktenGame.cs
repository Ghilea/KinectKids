using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D
{
    public sealed partial class SpokjaktenGame : MonoBehaviour
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
        private readonly ActiveHandSelector activeHandSelector = new ActiveHandSelector();
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
        private string sessionDwellAction;
        private float sessionDwellStartedAt;
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
            if (global::KinectKids3D.Platform.KinectKidsPlatformRoot.IsActive)
                CompleteCalibration();
            else flow = GameFlow.Menu;
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
            RefreshInputStatus();

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
            bool platform = global::KinectKids3D.Platform.KinectKidsPlatformRoot.IsActive;
            if (!finished && (Input.GetKeyDown(KeyCode.P)
                || (!platform && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))))
                paused = !paused;
            if (finished && Input.GetKeyDown(KeyCode.R)) ReloadRide();
            if (Input.GetKeyDown(KeyCode.B) && !bossDefeated)
            {
                rideDistance = BossStopDistance;
                bossBattle = true;
            }
            if (paused || finished || gameOver)
            {
                UpdateSessionAim();
                UpdateSessionDwell();
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
            UpdateSessionDwell();
            UpdateBossBattle();
            if (bossDefeated && rideDistance >= DarkRideWorld.TrackLength - 3f)
            {
                finished = true;
                reticles.Clear();
                SaveRecord();
            }
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