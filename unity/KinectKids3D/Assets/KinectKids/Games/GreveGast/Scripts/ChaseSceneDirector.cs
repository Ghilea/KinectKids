using UnityEngine;
using KinectKids.Scene25D;
using KinectKids3D;
using KinectKids3D.Platform;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// Assembles and runs the first working 2.5D chase scene described in
    /// new-way.md:
    ///   * the player runs TOWARD the camera (seen from the front),
    ///   * Greve Gast floats BEHIND and chases,
    ///   * the environment is built from parallax layers for depth,
    ///   * Kinect movements (duck / jump / weave) resolve modular hazards,
    ///   * a scripted camera keeps the player framed.
    ///
    /// It owns only gameplay orchestration. All visuals come from reusable Core
    /// pieces (parallax, puppet, hazards, placeholder art), so swapping in final
    /// sprites needs no changes here. It also acts as the <see cref="IDodgeSource"/>
    /// bridge between the platform Kinect input and the art-agnostic hazards.
    /// </summary>
    public sealed class ChaseSceneDirector : MonoBehaviour, IDodgeSource
    {
        [Header("Tuning")]
        public float baseWorldSpeed = 6f;
        public float hazardInterval = 2.4f;
        public float catchOnHit = 0.16f;
        public float recoverPerSecond = 0.05f;

        private ScriptedCamera scriptedCamera;
        private ParallaxController parallax;
        private PuppetRig playerRig;
        private PuppetRig grevePuppet;
        private PoseSpriteCharacter playerSprite;
        private PoseSpriteCharacter greveSprite;
        private Transform playerTransform;
        private Transform greveTransform;
        private bool useSpriteArt;
        private GreveGastBodyInput body;

        private float chase = 0.35f;      // 0 = far behind, 1 = caught
        private float lateral;            // smoothed -1..1
        private float hazardTimer;
        private DodgeAction currentDodge = DodgeAction.None;
        private float feedbackUntil;
        private string feedback = "";
        private Color feedbackColor = Color.white;

        // IDodgeSource
        public DodgeAction CurrentDodge => currentDodge;
        public float LateralPosition => Mathf.Clamp(lateral, -1f, 1f);

        private void Start()
        {
            EnsureInputManager();
            body = new GreveGastBodyInput();
            body.Start();

            BuildCamera();
            parallax = ChaseEnvironmentBuilder.Build(transform);

            useSpriteArt = GreveChaseSprites.HasPlayerArt && GreveChaseSprites.HasGreveArt;
            if (useSpriteArt)
            {
                // Real cut sprites: pose-swapping single-sprite characters.
                var playerGo = new GameObject("Player");
                playerGo.transform.SetParent(transform, false);
                playerGo.transform.localPosition = new Vector3(0f, -1.4f, LayerSorting.BandZ(SceneBand.Actors));
                playerGo.transform.localScale = Vector3.one * 3.2f;
                playerSprite = playerGo.AddComponent<PoseSpriteCharacter>();
                playerSprite.Init(GreveChaseSprites.Player, SceneBand.Actors, 0.9f);
                playerSprite.SetPose("run_near");
                playerTransform = playerGo.transform;

                var greveGo = new GameObject("GreveGast");
                greveGo.transform.SetParent(transform, false);
                greveGo.transform.localPosition = new Vector3(0f, 0.5f, LayerSorting.BandZ(SceneBand.Actors, 0.2f));
                greveSprite = greveGo.AddComponent<PoseSpriteCharacter>();
                greveSprite.Init(GreveChaseSprites.Greve, SceneBand.Actors, 0.3f);
                greveSprite.SetPose("chase");
                greveTransform = greveGo.transform;
            }
            else
            {
                // Placeholder puppets.
                playerRig = PuppetBuilder.BuildPlayer(transform);
                playerRig.transform.localPosition = new Vector3(0f, -1.4f, LayerSorting.BandZ(SceneBand.Actors));
                playerRig.transform.localScale = Vector3.one * 1.4f;
                playerTransform = playerRig.transform;

                grevePuppet = PuppetBuilder.BuildGreveGast(transform);
                grevePuppet.transform.localPosition = new Vector3(0f, 0.5f, LayerSorting.BandZ(SceneBand.Actors, 0.2f));
                grevePuppet.SetPose("chase");
                greveTransform = grevePuppet.transform;
            }

            hazardTimer = hazardInterval;
        }

        private void EnsureInputManager()
        {
            if (KinectKidsInputManager.Instance == null)
            {
                var go = new GameObject("KinectKidsInputManager");
                go.AddComponent<KinectKidsInputManager>();
            }
        }

        private void BuildCamera()
        {
            GameObject camGo = new GameObject("ChaseCamera");
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
                name = "Chase",
                position = new Vector3(0f, 0f, -15f),
                eulerAngles = Vector3.zero,
                orthographicSize = 5f,
                blendSeconds = 0.8f
            });
            scriptedCamera.shots.Add(new ScriptedCamera.Shot
            {
                name = "Caught",
                position = new Vector3(0f, 0.6f, -12f),
                eulerAngles = Vector3.zero,
                orthographicSize = 4.2f,
                blendSeconds = 0.6f
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
            // Translate the shared body input into an abstract dodge + lateral.
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
            float speed = running ? baseWorldSpeed * 1.6f : baseWorldSpeed;
            if (parallax != null)
            {
                parallax.forwardSpeed = speed;
                parallax.lateralTarget = lateral * 1.2f;
            }

            // Running gains distance on Greve Gast; standing lets him close in.
            chase += (running ? -0.06f : recoverPerSecond) * Time.deltaTime;
            chase = Mathf.Clamp01(chase);

            if (chase >= 0.99f)
            {
                Feedback("GREVE GAST TOG DIG!", new Color(0.8f, 0.3f, 0.85f));
                scriptedCamera?.BlendTo("Caught");
                SetGrevePose("threaten");
                chase = 0.55f; // reset for continued play
            }
            else if (scriptedCamera != null && chase < 0.9f)
            {
                scriptedCamera.BlendTo("Chase");
            }
        }

        private void DriveCharacters()
        {
            // --- Player ---
            if (playerTransform != null)
            {
                if (useSpriteArt)
                {
                    string pose = "run_near";
                    switch (currentDodge)
                    {
                        case DodgeAction.Jump: pose = "jump"; break;
                        case DodgeAction.Duck: pose = "duck"; break;
                        case DodgeAction.Left: pose = "sidestep_left"; break;
                        case DodgeAction.Right: pose = "sidestep_right"; break;
                    }
                    playerSprite.SetPose(pose);
                }
                else
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
                }
                Vector3 p = playerTransform.localPosition;
                p.x = Mathf.Lerp(p.x, lateral * 1.8f, 1f - Mathf.Exp(-10f * Time.deltaTime));
                playerTransform.localPosition = p;
            }

            // --- Greve Gast: scales / rises with chase closeness ---
            if (greveTransform != null)
            {
                float baseScale = useSpriteArt ? 3.0f : 1f;
                float scale = Mathf.Lerp(0.5f, 1.3f, chase) * baseScale;
                greveTransform.localScale = Vector3.one * scale;
                Vector3 gp = greveTransform.localPosition;
                gp.y = Mathf.Lerp(1.4f, 0.2f, chase);
                gp.x = Mathf.Lerp(gp.x, lateral * 1.2f, 1f - Mathf.Exp(-4f * Time.deltaTime));
                greveTransform.localPosition = gp;

                string grevePose = chase > 0.75f ? "reach" : "chase";
                if (useSpriteArt) greveSprite.SetPose(grevePose);
                else grevePuppet.SetPose(grevePose);
            }
        }

        private void TickHazards()
        {
            hazardTimer -= Time.deltaTime;
            if (hazardTimer > 0f) return;
            hazardTimer = hazardInterval;

            KinectKids.Scene25D.HazardKind kind =
                (KinectKids.Scene25D.HazardKind)Random.Range(0, 4);
            HazardBase hazard = HazardFactory.Spawn(kind, transform, this);
            hazard.transform.localPosition = new Vector3(0, 0, LayerSorting.BandZ(SceneBand.Hazards));

            // Swap in real hazard art where a matching cut sprite exists.
            switch (kind)
            {
                case KinectKids.Scene25D.HazardKind.LowBeam: hazard.OverrideSprite(GreveChaseSprites.Hazard("haz_1")); break; // hanging beam
                case KinectKids.Scene25D.HazardKind.Falling: hazard.OverrideSprite(GreveChaseSprites.Hazard("haz_0")); break; // falling rock
                case KinectKids.Scene25D.HazardKind.Side: hazard.OverrideSprite(GreveChaseSprites.Hazard("haz_4")); break;    // rolling barrel
                case KinectKids.Scene25D.HazardKind.Jump: hazard.OverrideSprite(GreveChaseSprites.Hazard("haz_0")); break;    // rock/obstacle
            }

            hazard.Resolved += OnHazardResolved;
        }

        private void SetGrevePose(string pose)
        {
            if (useSpriteArt) greveSprite?.SetPose(pose);
            else grevePuppet?.SetPose(pose);
        }

        private void OnHazardResolved(HazardBase hazard, bool avoided)
        {
            if (avoided)
            {
                chase = Mathf.Clamp01(chase - 0.05f);
                Feedback("BRA!", new Color(0.3f, 0.85f, 0.45f));
                SetGrevePose("stunned");
            }
            else
            {
                chase = Mathf.Clamp01(chase + catchOnHit);
                Feedback("OJ!", new Color(0.9f, 0.3f, 0.3f));
                scriptedCamera?.Shake(0.4f);
                SetGrevePose("reach");
            }
        }

        private void Feedback(string text, Color color)
        {
            feedback = text;
            feedbackColor = color;
            feedbackUntil = Time.unscaledTime + 0.8f;
        }

        private GUIStyle bigStyle;
        private GUIStyle smallStyle;

        private void OnGUI()
        {
            if (bigStyle == null)
            {
                bigStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Clamp(Screen.height / 10, 40, 120),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                smallStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Clamp(Screen.height / 40, 16, 28),
                    fontStyle = FontStyle.Bold
                };
                smallStyle.normal.textColor = Color.white;
            }

            // Chase meter.
            float w = Mathf.Min(460f, Screen.width * 0.3f);
            Rect meter = new Rect(30, 26, w, 22);
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(meter.x - 6, meter.y - 6, meter.width + 12, meter.height + 12), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, 0.15f);
            GUI.DrawTexture(meter, Texture2D.whiteTexture);
            GUI.color = Color.Lerp(new Color(0.2f, 0.8f, 0.4f), new Color(0.9f, 0.2f, 0.3f), chase);
            GUI.DrawTexture(new Rect(meter.x, meter.y, meter.width * chase, meter.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(meter.x, meter.y + 26, meter.width + 200, 26), "GREVE GAST NÄRMAR SIG", smallStyle);
            GUI.Label(new Rect(meter.x, meter.y + 52, meter.width + 400, 26),
                useSpriteArt ? "Grafik: riktiga sprites" : "Grafik: platshållare (sprites hittades inte)", smallStyle);

            if (Time.unscaledTime < feedbackUntil)
            {
                GUI.color = feedbackColor;
                GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 120), feedback, bigStyle);
                GUI.color = Color.white;
            }
        }

        private void OnDestroy()
        {
            body?.Dispose();
        }
    }
}
