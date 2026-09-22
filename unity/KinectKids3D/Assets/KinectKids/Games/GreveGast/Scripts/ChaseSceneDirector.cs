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

            // Player near the camera, running toward it.
            playerRig = PuppetBuilder.BuildPlayer(transform);
            playerRig.transform.localPosition = new Vector3(0f, -1.4f, LayerSorting.BandZ(SceneBand.Actors));
            playerRig.transform.localScale = Vector3.one * 1.4f;

            // Greve Gast behind the player, scaled by chase distance.
            grevePuppet = PuppetBuilder.BuildGreveGast(transform);
            grevePuppet.transform.localPosition = new Vector3(0f, 0.5f, LayerSorting.BandZ(SceneBand.Actors, 0.2f));
            grevePuppet.SetPose("chase");

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
                grevePuppet?.SetPose("threaten");
                chase = 0.55f; // reset for continued play
            }
            else if (scriptedCamera != null && chase < 0.9f)
            {
                scriptedCamera.BlendTo("Chase");
            }
        }

        private void DriveCharacters()
        {
            // Player pose from current action.
            if (playerRig != null)
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
                Vector3 p = playerRig.transform.localPosition;
                p.x = Mathf.Lerp(p.x, lateral * 1.8f, 1f - Mathf.Exp(-10f * Time.deltaTime));
                playerRig.transform.localPosition = p;
            }

            // Greve Gast scales / rises with chase closeness.
            if (grevePuppet != null)
            {
                float scale = Mathf.Lerp(0.7f, 2.0f, chase);
                grevePuppet.transform.localScale = Vector3.one * scale;
                Vector3 gp = grevePuppet.transform.localPosition;
                gp.y = Mathf.Lerp(1.4f, 0.2f, chase);
                gp.x = Mathf.Lerp(gp.x, lateral * 1.2f, 1f - Mathf.Exp(-4f * Time.deltaTime));
                grevePuppet.transform.localPosition = gp;
                if (chase > 0.75f) grevePuppet.SetPose("reach");
                else grevePuppet.SetPose("chase");
            }
        }

        private void TickHazards()
        {
            hazardTimer -= Time.deltaTime;
            if (hazardTimer > 0f) return;
            hazardTimer = hazardInterval;

            HazardKind kind = (HazardKind)Random.Range(0, 4);
            HazardBase hazard = HazardFactory.Spawn(kind, transform, this);
            hazard.transform.localPosition = new Vector3(0, 0, LayerSorting.BandZ(SceneBand.Hazards));
            hazard.Resolved += OnHazardResolved;
        }

        private void OnHazardResolved(HazardBase hazard, bool avoided)
        {
            if (avoided)
            {
                chase = Mathf.Clamp01(chase - 0.05f);
                Feedback("BRA!", new Color(0.3f, 0.85f, 0.45f));
                grevePuppet?.SetPose("stunned");
            }
            else
            {
                chase = Mathf.Clamp01(chase + catchOnHit);
                Feedback("OJ!", new Color(0.9f, 0.3f, 0.3f));
                scriptedCamera?.Shake(0.4f);
                grevePuppet?.SetPose("reach");
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
