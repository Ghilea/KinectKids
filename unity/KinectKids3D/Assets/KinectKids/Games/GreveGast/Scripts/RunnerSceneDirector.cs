using UnityEngine;
using KinectKids.Scene25D;
using KinectKids.Scene25D.Sections;
using KinectKids3D;
using KinectKids3D.Platform;
// Disambiguate: both KinectKids3D (RideHazard.cs) and KinectKids.Scene25D
// declare a HazardKind. This scene uses the Scene25D one (HazardFactory /
// SectionDefinition), so alias it explicitly.
using HazardKind = KinectKids.Scene25D.HazardKind;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// The modular, section-based 2.5D runner director. Unlike
    /// <see cref="ChaseSceneDirector"/> (a single hard-coded corridor), this
    /// streams a SEQUENCE of rooms (corridor → great hall → kitchen …) driven by
    /// the song's structure, each built from reusable modules.
    ///
    /// It composes the existing Core pieces and adds nothing to gameplay rules:
    ///   * <see cref="SectionStreamer"/> builds/streams the current room.
    ///   * <see cref="SongStructureDriver"/> switches rooms on the beat and sets
    ///     intensity.
    ///   * hazards spawn from the ACTIVE room's allowed list (art-agnostic).
    ///   * the player + Greve Gast reuse <see cref="PuppetBuilder"/> / real
    ///     sprites exactly as the chase does.
    ///
    /// Left the original <see cref="ChaseSceneDirector"/> untouched so existing
    /// gameplay keeps working; this is an additive alternative director.
    /// </summary>
    public sealed class RunnerSceneDirector : MonoBehaviour, IDodgeSource
    {
        [Header("Tuning")]
        public float baseWorldSpeed = 6f;
        public float catchOnHit = 0.16f;
        public float recoverPerSecond = 0.05f;

        private ScriptedCamera scriptedCamera;
        private ParallaxController parallax;
        private SectionStreamer streamer;
        private SongStructureDriver songDriver;
        private ModuleLibrary library;

        private PuppetRig playerRig;
        private PuppetRig grevePuppet;
        private Transform playerTransform;
        private Transform greveTransform;

        private GreveGastBodyInput body;
        private AudioSource music;

        private float chase = 0.35f;
        private float lateral;
        private float hazardTimer;
        private int hazardCounter;
        private DodgeAction currentDodge = DodgeAction.None;

        private RoomKind builtRoom = RoomKind.Custom;

        // IDodgeSource
        public DodgeAction CurrentDodge => currentDodge;
        public float LateralPosition => Mathf.Clamp(lateral, -1f, 1f);

        private void Start()
        {
            EnsureInputManager();
            body = new GreveGastBodyInput();
            body.Start();

            BuildCamera();

            // Parallax treadmill lives on its own root so the streamer can add
            // room layers under it and CollectLayers() picks them up.
            var worldGo = new GameObject("RunnerWorld");
            worldGo.transform.SetParent(transform, false);
            parallax = worldGo.AddComponent<ParallaxController>();

            // The one seam to real art: reuse the chase environment resolver.
            library = new ModuleLibrary(GreveChaseSprites.Environment);

            streamer = worldGo.AddComponent<SectionStreamer>();
            streamer.Init(parallax, library, PerspectiveModel.Default);
            streamer.forwardSpeed = baseWorldSpeed;

            StartMusic();

            // Song structure drives the room sequence. Song clock = music time,
            // or an unscaled timer if the clip is missing.
            songDriver = worldGo.AddComponent<SongStructureDriver>();
            songDriver.Init(streamer, RoomCatalog.SampleSong(),
                SongClock, RoomFactory);
            songDriver.SectionChanged += OnSectionChanged;

            BuildCharacters();
            hazardTimer = 2.4f;
        }

        private float SongClock()
        {
            if (music != null && music.clip != null && music.isPlaying)
                return music.time;
            return Time.unscaledTime;
        }

        /// <summary>Room factory that also applies each room's preferred perspective.</summary>
        private SectionDefinition RoomFactory(RoomKind room)
        {
            // Ask the streamer to adopt this room's perspective on entry.
            streamer.Init(parallax, library, RoomCatalog.PerspectiveFor(room));
            return RoomCatalog.Build(room);
        }

        private void OnSectionChanged(SongSection section)
        {
            builtRoom = section.room;
        }

        private void BuildCharacters()
        {
            playerRig = PuppetBuilder.BuildPlayer(transform);
            playerRig.transform.localPosition = new Vector3(0f, -1.4f, LayerSorting.BandZ(SceneBand.Actors));
            playerRig.transform.localScale = Vector3.one * 1.4f;
            playerTransform = playerRig.transform;

            grevePuppet = PuppetBuilder.BuildGreveGast(transform);
            grevePuppet.transform.localPosition = new Vector3(0f, 0.5f, LayerSorting.BandZ(SceneBand.Actors, 0.2f));
            grevePuppet.SetPose("chase");
            greveTransform = grevePuppet.transform;
        }

        private void StartMusic()
        {
            AudioClip clip = Resources.Load<AudioClip>("Audio/Music/GreveGastsJakt");
            if (clip == null) return;
            music = gameObject.AddComponent<AudioSource>();
            music.clip = clip;
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0.82f;
            music.spatialBlend = 0f;
            music.Play();
        }

        private void EnsureInputManager()
        {
            if (KinectKidsInputManager.Instance == null)
                new GameObject("KinectKidsInputManager").AddComponent<KinectKidsInputManager>();
        }

        private void BuildCamera()
        {
            var camGo = new GameObject("RunnerCamera");
            camGo.transform.SetParent(transform, false);
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PlaceholderArt.NightBlue;
            cam.transform.position = new Vector3(0f, 0f, -15f);

            scriptedCamera = camGo.AddComponent<ScriptedCamera>();
            scriptedCamera.shots.Clear();
            scriptedCamera.shots.Add(new ScriptedCamera.Shot
            {
                name = "Run",
                position = new Vector3(0f, 0f, -15f),
                eulerAngles = Vector3.zero,
                orthographicSize = 5f,
                blendSeconds = 0.8f,
            });
        }

        private void Update()
        {
            if (body == null) return;
            body.Update();
            ReadInput();
            DriveWorld();
            DriveCharacters();
            TickHazards();
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

            float targetLateral = body.HorizontalDelta * 3f;
            if (body.Current == GreveGastAction.Left) targetLateral = -1f;
            if (body.Current == GreveGastAction.Right) targetLateral = 1f;
            lateral = Mathf.Lerp(lateral, Mathf.Clamp(targetLateral, -1f, 1f),
                1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        private void DriveWorld()
        {
            bool running = body.Current == GreveGastAction.Run || body.RunEnergy > 0.4f
                || Input.GetKey(KeyCode.LeftShift);
            // Song intensity scales the base pace so choruses feel faster.
            float intensity = songDriver != null ? songDriver.CurrentIntensity : 0.5f;
            float pace = baseWorldSpeed * Mathf.Lerp(0.85f, 1.35f, intensity);
            float speed = running ? pace * 1.6f : pace;

            if (streamer != null)
            {
                streamer.forwardSpeed = speed;
                streamer.lateralTarget = lateral * 1.2f;
            }

            chase += (running ? -0.06f : recoverPerSecond) * Time.deltaTime;
            chase = Mathf.Clamp01(chase);
        }

        private void DriveCharacters()
        {
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
                p.x = Mathf.Lerp(p.x, lateral * 1.8f, 1f - Mathf.Exp(-10f * Time.deltaTime));
                playerTransform.localPosition = p;
            }

            if (greveTransform != null)
            {
                float t = chase * chase * (3f - 2f * chase);
                float scale = Mathf.Lerp(0.28f, 1.25f, t);
                greveTransform.localScale = Vector3.one * scale;
                Vector3 gp = greveTransform.localPosition;
                gp.y = Mathf.Lerp(1.55f, -0.4f, t);
                gp.x = Mathf.Lerp(gp.x, lateral * 1.2f, 1f - Mathf.Exp(-4f * Time.deltaTime));
                greveTransform.localPosition = gp;
                grevePuppet.SetPose(chase > 0.75f ? "reach" : "chase");
            }
        }

        private void TickHazards()
        {
            SectionDefinition room = streamer != null ? streamer.ActiveDefinition : null;
            if (room == null || !room.HasHazards) return;
            // Don't spawn hazards mid-transition so a room change reads cleanly.
            if (streamer.IsTransitioning) return;

            hazardTimer -= Time.deltaTime;
            if (hazardTimer > 0f) return;

            // Room sets its own cadence; song intensity tightens it.
            float intensity = songDriver != null ? songDriver.CurrentIntensity : 0.5f;
            hazardTimer = room.hazardInterval * Mathf.Lerp(1.2f, 0.7f, intensity);

            HazardKind kind = room.HazardAt(hazardCounter++);
            HazardBase hazard = HazardFactory.Spawn(kind, transform, this);
            hazard.transform.localPosition = new Vector3(0, 0, LayerSorting.BandZ(SceneBand.Hazards));

            // Swap in real hazard art where a matching cut sprite exists.
            switch (kind)
            {
                case HazardKind.LowBeam: hazard.OverrideSprite(GreveChaseSprites.Hazard("haz_1")); break;
                case HazardKind.Falling: hazard.OverrideSprite(GreveChaseSprites.Hazard("haz_0")); break;
                case HazardKind.Side: hazard.OverrideSprite(GreveChaseSprites.Hazard("haz_4")); break;
                case HazardKind.Jump: hazard.OverrideSprite(GreveChaseSprites.Hazard("haz_0")); break;
            }
            hazard.Resolved += OnHazardResolved;
        }

        private void OnHazardResolved(HazardBase hazard, bool avoided)
        {
            if (avoided)
            {
                chase = Mathf.Clamp01(chase - 0.05f);
                grevePuppet?.SetPose("stunned");
            }
            else
            {
                chase = Mathf.Clamp01(chase + catchOnHit);
                scriptedCamera?.Shake(0.4f);
                grevePuppet?.SetPose("reach");
            }
        }

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

            string label = songDriver != null ? songDriver.CurrentLabel : "";
            GUI.Label(new Rect(30, 24, 700, 30),
                "RUM: " + builtRoom + "   LÅTDEL: " + label, hud);
        }

        private void OnDestroy()
        {
            if (music != null) music.Stop();
            body?.Dispose();
        }
    }
}
