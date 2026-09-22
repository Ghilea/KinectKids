using System.Collections.Generic;
using UnityEngine;
using KinectKids.Scene25D;
using KinectKids3D;
using KinectKids3D.Platform;

namespace KinectKids.Games.Movement
{
    /// <summary>
    /// Shared base for the 2.5D movement games. Builds the themed dark-storybook
    /// backdrop + camera, keeps the platform voice/scoring helpers, and lets each
    /// game render its interactive elements as sprites in the layered scene.
    /// Gameplay logic and voice keys are preserved from MovementGamesUnity.
    /// </summary>
    public abstract class Movement25DGame : MonoBehaviour
    {
        protected AudioSource Voice;
        protected Camera Cam;
        protected ScriptedCamera ScriptedCam;
        protected ParallaxController Parallax;
        protected float EndsAt;
        protected int Score;
        protected GUIStyle TitleStyle;
        protected GUIStyle TextStyle;

        protected bool Finished => Time.unscaledTime >= EndsAt;
        protected abstract Color Signature { get; }
        protected virtual float Duration => 60f;

        protected virtual void Start()
        {
            if (KinectKidsInputManager.Instance == null)
                new GameObject("KinectKidsInputManager").AddComponent<KinectKidsInputManager>();

            var theme = Scene25DBackdrop.Theme.Tinted(Signature);
            Cam = Scene25DBackdrop.BuildCamera(transform, theme.sky, out ScriptedCam);
            Parallax = Scene25DBackdrop.Build(transform, theme);
            Parallax.forwardSpeed = 0.6f;

            Voice = gameObject.AddComponent<AudioSource>();
            Voice.spatialBlend = 0f;
            Voice.playOnAwake = false;
            EndsAt = Time.unscaledTime + Duration;
        }

        protected void PlayVoice(string key)
        {
            AudioClip clip = Resources.Load<AudioClip>("Voice/" + key);
            if (clip == null || Voice == null) return;
            Voice.Stop();
            Voice.clip = clip;
            Voice.Play();
        }

        protected Vector3 ScreenToScene(Vector2 normalized)
        {
            float h = Cam.orthographicSize;
            float w = h * Cam.aspect;
            float x = (normalized.x - 0.5f) * 2f * w + Cam.transform.position.x;
            float y = (normalized.y - 0.5f) * 2f * h + Cam.transform.position.y;
            return new Vector3(x, y, 0f);
        }

        protected void EnsureStyles()
        {
            if (TitleStyle != null) return;
            TitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 15, 40, 76),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            TextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 35, 22, 36),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            TitleStyle.normal.textColor = TextStyle.normal.textColor = new Color(0.95f, 0.95f, 1f);
        }

        protected void DrawHeader(string title)
        {
            EnsureStyles();
            Fill(new Rect(0, 0, Screen.width, 130), new Color(0.05f, 0.05f, 0.12f, 0.72f));
            GUI.Label(new Rect(20, 8, Screen.width - 40, 70), title, TitleStyle);
            GUI.Label(new Rect(30, 82, 280, 40), "POÄNG: " + Score, TextStyle);
            GUI.Label(new Rect(Screen.width - 310, 82, 280, 40),
                "TID: " + Mathf.CeilToInt(Mathf.Max(0, EndsAt - Time.unscaledTime)), TextStyle);
        }

        protected void DrawEndPanel()
        {
            if (!Finished) return;
            Fill(new Rect(Screen.width * 0.2f, Screen.height * 0.3f, Screen.width * 0.6f, Screen.height * 0.36f),
                new Color(0.05f, 0.05f, 0.12f, 0.92f));
            GUI.Label(new Rect(0, Screen.height * 0.34f, Screen.width, 80), "BRA JOBBAT!  " + Score + " POÄNG", TitleStyle);
            if (GUI.Button(new Rect(Screen.width * 0.34f, Screen.height * 0.52f, Screen.width * 0.32f, 60), "TILL SPELMENYN"))
                KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
        }

        protected static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }

    /// <summary>2.5D Ballongjakten: balloons are depth-scaled sprites popped with the Kinect hand.</summary>
    public sealed class Balloon25DGame : Movement25DGame
    {
        private sealed class Balloon
        {
            public GameObject Go;
            public SpriteRenderer Sr;
            public CameraDepthScaler Scaler;
            public Vector2 Drift;   // lateral drift + rise in depth
            public float Lane;
            public float Depth;
            public Color Color;
        }

        private readonly List<Balloon> balloons = new List<Balloon>();
        private readonly Dictionary<int, Vector2> lockedHands = new Dictionary<int, Vector2>();
        private readonly Color[] palette =
        {
            new Color(0.95f, 0.24f, 0.38f), new Color(1f, 0.63f, 0.12f),
            new Color(0.12f, 0.68f, 0.48f), new Color(0.22f, 0.48f, 0.9f), new Color(0.67f, 0.25f, 0.8f)
        };

        protected override Color Signature => new Color(0.20f, 0.55f, 0.85f);

        protected override void Start()
        {
            base.Start();
            for (int i = 0; i < 10; i++) balloons.Add(NewBalloon(i));
            PlayVoice("balloon_instruction");
        }

        private Balloon NewBalloon(int seed)
        {
            var go = new GameObject("Balloon");
            go.transform.SetParent(transform, false);
            var scaler = go.AddComponent<CameraDepthScaler>();
            scaler.band = SceneBand.Actors;
            scaler.farScale = 0.35f;
            scaler.nearScale = 1.6f;
            scaler.farY = 1.6f;
            scaler.nearY = -1.6f;
            Color color = palette[seed % palette.Length];
            var sr = PlaceholderArt.NewSpriteObject("Body", PlaceholderArt.Capsule(90, 110), color,
                go.transform, SceneBand.Actors, 0.5f);
            sr.transform.localScale = Vector3.one * 0.02f;
            float lane = Random.Range(-1f, 1f);
            go.transform.localPosition = new Vector3(lane * 3f, 0, LayerSorting.BandZ(SceneBand.Actors));
            return new Balloon
            {
                Go = go, Sr = sr, Scaler = scaler, Lane = lane, Depth = Random.Range(0f, 0.4f),
                Drift = new Vector2(Random.Range(-0.05f, 0.05f), Random.Range(0.10f, 0.18f)), Color = color
            };
        }

        private void Update()
        {
            if (Finished) return;
            for (int i = 0; i < balloons.Count; i++)
            {
                Balloon b = balloons[i];
                b.Depth += b.Drift.y * Time.deltaTime;
                b.Lane += b.Drift.x * Time.deltaTime;
                if (b.Depth >= 1f) { Destroy(b.Go); balloons[i] = NewBalloon(i); continue; }
                float x = b.Lane * Mathf.Lerp(1.2f, 4.0f, b.Depth);
                b.Go.transform.localPosition = new Vector3(x, b.Go.transform.localPosition.y, b.Go.transform.localPosition.z);
                b.Scaler.Apply(b.Depth);
            }

            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null || Cam == null) return;
            foreach (AimSample hand in input.AimSamples)
            {
                if (lockedHands.TryGetValue(hand.HandId, out Vector2 lockedAt))
                {
                    if (Vector2.Distance(lockedAt, hand.Position) < 0.075f) continue;
                    lockedHands.Remove(hand.HandId);
                }
                if (!input.KinectConnected && !hand.Fire) continue;
                Vector3 world = ScreenToScene(hand.Position);
                for (int i = 0; i < balloons.Count; i++)
                {
                    Balloon b = balloons[i];
                    if (b.Depth < 0.12f) continue;
                    float radius = Mathf.Lerp(0.5f, 1.6f, b.Depth);
                    if (Vector2.Distance(new Vector2(world.x, world.y),
                        new Vector2(b.Go.transform.position.x, b.Go.transform.position.y)) >= radius) continue;
                    Score += 10;
                    lockedHands[hand.HandId] = hand.Position;
                    Destroy(b.Go);
                    balloons[i] = NewBalloon(i);
                    break;
                }
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawCursors();
            DrawHeader("BALLONGJAKTEN");
            DrawEndPanel();
        }

        private void DrawCursors()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null) return;
            foreach (AimSample sample in input.AimSamples)
            {
                Vector2 p = new Vector2(sample.Position.x * Screen.width, (1f - sample.Position.y) * Screen.height);
                Color color = (sample.HandId & 1) == 0
                    ? new Color(0.10f, 0.72f, 0.92f, 0.85f) : new Color(0.96f, 0.30f, 0.52f, 0.85f);
                Fill(new Rect(p.x - 18, p.y - 18, 36, 36), color);
            }
        }
    }

    /// <summary>2.5D Simon säger: a big puppet demonstrates; matching preserved from SimonGameUnity.</summary>
    public sealed class Simon25DGame : Movement25DGame
    {
        private enum Command { HandsUp, ArmsOut, HandsTogether, Duck }
        private Command command;
        private float commandStarted;
        private float nextCommandAt;
        private bool armed;
        private PuppetRig puppet;

        protected override Color Signature => new Color(0.75f, 0.35f, 0.85f);
        protected override float Duration => 75f;

        protected override void Start()
        {
            base.Start();
            puppet = PuppetBuilder.BuildPlayer(transform);
            puppet.transform.localPosition = new Vector3(0f, -0.6f, LayerSorting.BandZ(SceneBand.Actors));
            puppet.transform.localScale = Vector3.one * 2.4f;
            nextCommandAt = Time.unscaledTime + 0.8f;
            PlayVoice("simon_ready");
        }

        private void Update()
        {
            if (Finished) return;
            if (!armed && Time.unscaledTime >= nextCommandAt)
            {
                command = (Command)Random.Range(0, 4);
                commandStarted = Time.unscaledTime;
                armed = true;
                DemonstratePose();
                PlayVoice(VoiceKey());
                return;
            }
            if (!armed || Time.unscaledTime - commandStarted < 0.55f) return;
            if (Matches())
            {
                Score += 10;
                armed = false;
                nextCommandAt = Time.unscaledTime + 1.1f;
                PlayVoice("correct");
                puppet?.SetPose("idle");
            }
            else if (Time.unscaledTime - commandStarted > 7f)
            {
                armed = false;
                nextCommandAt = Time.unscaledTime + 1f;
                PlayVoice("new_movement");
                puppet?.SetPose("idle");
            }
        }

        private void DemonstratePose()
        {
            if (puppet == null) return;
            switch (command)
            {
                case Command.HandsUp: puppet.SetPose("jump"); break;   // arms raised
                case Command.ArmsOut: puppet.SetPose("right"); break;  // wide gesture placeholder
                case Command.HandsTogether: puppet.SetPose("idle"); break;
                case Command.Duck: puppet.SetPose("duck"); break;
            }
        }

        private bool Matches()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null) return false;
            PlayerInputFrame frame = input.Frame;
            if (command == Command.Duck) return frame.Duck || Input.GetKey(KeyCode.DownArrow);
            if (command == Command.HandsUp)
                return (frame.IsTracked && frame.LeftHand.y < 0.34f && frame.RightHand.y < 0.34f) || Input.GetKey(KeyCode.Alpha1);
            if (command == Command.ArmsOut)
                return (frame.IsTracked && Mathf.Abs(frame.LeftHand.x - frame.RightHand.x) > 0.46f) || Input.GetKey(KeyCode.Alpha2);
            return (frame.IsTracked && Vector2.Distance(frame.LeftHand, frame.RightHand) < 0.15f) || Input.GetKey(KeyCode.Alpha3);
        }

        private string VoiceKey() => command == Command.HandsUp ? "simon_hands_up" :
            command == Command.ArmsOut ? "simon_arms_out" :
            command == Command.HandsTogether ? "simon_hands_together" : "simon_duck";

        private string Label() => command == Command.HandsUp ? "HÄNDERNA UPP!" :
            command == Command.ArmsOut ? "ARMARNA UT!" :
            command == Command.HandsTogether ? "HÄNDERNA IHOP!" : "DUCKA!";

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader("SIMON SÄGER");
            if (armed)
            {
                GUI.Label(new Rect(Screen.width * 0.12f, Screen.height * 0.16f, Screen.width * 0.76f, 100), Label(), TitleStyle);
                float progress = Mathf.Clamp01((Time.unscaledTime - commandStarted) / 7f);
                Fill(new Rect(Screen.width * 0.25f, Screen.height * 0.30f, Screen.width * 0.5f, 24), new Color(0.1f, 0.08f, 0.15f, 0.5f));
                Fill(new Rect(Screen.width * 0.25f, Screen.height * 0.30f, Screen.width * 0.5f * (1f - progress), 24), new Color(0.25f, 0.66f, 0.42f));
            }
            GUI.Label(new Rect(Screen.width * 0.2f, Screen.height * 0.85f, Screen.width * 0.6f, 70),
                "Tangentbord: 1 = händer upp, 2 = armar ut, 3 = händer ihop, nedåtpil = ducka", TextStyle);
            DrawEndPanel();
        }
    }
}
