using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D.Platform
{
    public sealed class BalloonGameEntry : MonoBehaviour
    {
        private void Awake() => gameObject.AddComponent<BalloonGameUnity>();
    }

    public sealed class SimonGameEntry : MonoBehaviour
    {
        private void Awake() => gameObject.AddComponent<SimonGameUnity>();
    }

    public abstract class SimpleKidsGame : MonoBehaviour
    {
        protected AudioSource Voice;
        protected float EndsAt;
        protected int Score;
        protected GUIStyle TitleStyle;
        protected GUIStyle TextStyle;
        protected bool Finished => Time.unscaledTime >= EndsAt;
        protected abstract Color Background { get; }
        protected virtual float Duration => 60f;

        protected virtual void Start()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = new GameObject("Spelkamera").AddComponent<Camera>();
                camera.gameObject.AddComponent<AudioListener>();
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            Voice = gameObject.AddComponent<AudioSource>();
            Voice.spatialBlend = 0f;
            EndsAt = Time.unscaledTime + Duration;
        }

        protected void PlayVoice(string key)
        {
            AudioClip clip = Resources.Load<AudioClip>("Voice/" + key);
            if (clip == null) return;
            Voice.Stop();
            Voice.clip = clip;
            Voice.Play();
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
            TitleStyle.normal.textColor = TextStyle.normal.textColor = new Color(0.13f, 0.08f, 0.20f);
        }

        protected void DrawHeader(string title)
        {
            EnsureStyles();
            Fill(new Rect(0, 0, Screen.width, 145), new Color(1f, 0.96f, 0.82f, 0.96f));
            GUI.Label(new Rect(20, 10, Screen.width - 40, 75), title, TitleStyle);
            GUI.Label(new Rect(30, 88, 280, 45), "PO\u00c4NG: " + Score, TextStyle);
            GUI.Label(new Rect(Screen.width - 310, 88, 280, 45),
                "TID: " + Mathf.CeilToInt(Mathf.Max(0, EndsAt - Time.unscaledTime)), TextStyle);
        }

        protected void DrawEndPanel()
        {
            if (!Finished) return;
            Fill(new Rect(Screen.width * 0.2f, Screen.height * 0.3f, Screen.width * 0.6f, Screen.height * 0.36f),
                new Color(1f, 0.96f, 0.82f, 0.96f));
            GUI.Label(new Rect(0, Screen.height * 0.34f, Screen.width, 80),
                "BRA JOBBAT!  " + Score + " PO\u00c4NG", TitleStyle);
            if (GUI.Button(new Rect(Screen.width * 0.34f, Screen.height * 0.52f, Screen.width * 0.32f, 60),
                    "TILL SPELMENYN"))
                KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
        }

        protected static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }

    public sealed class BalloonGameUnity : SimpleKidsGame
    {
        private sealed class Balloon
        {
            public Vector2 Position;
            public Vector2 Speed;
            public float Size;
            public Color Color;
        }

        private readonly List<Balloon> balloons = new List<Balloon>();
        private readonly Dictionary<int, Vector2> lockedHands = new Dictionary<int, Vector2>();
        protected override Color Background => new Color(0.77f, 0.93f, 1f);

        protected override void Start()
        {
            base.Start();
            for (int i = 0; i < 10; i++) balloons.Add(NewBalloon(i));
            PlayVoice("balloon_instruction");
        }

        private Balloon NewBalloon(int seed)
        {
            Color[] colors =
            {
                new Color(0.95f, 0.24f, 0.38f), new Color(1f, 0.63f, 0.12f),
                new Color(0.12f, 0.68f, 0.48f), new Color(0.22f, 0.48f, 0.9f),
                new Color(0.67f, 0.25f, 0.8f)
            };
            return new Balloon
            {
                Position = new Vector2(Random.Range(0.08f, 0.92f), Random.Range(0.2f, 1.1f)),
                Speed = new Vector2(Random.Range(-0.025f, 0.025f), Random.Range(-0.10f, -0.055f)),
                Size = Random.Range(0.065f, 0.105f),
                Color = colors[seed % colors.Length]
            };
        }

        private void Update()
        {
            if (Finished) return;
            for (int i = 0; i < balloons.Count; i++)
            {
                Balloon balloon = balloons[i];
                balloon.Position += balloon.Speed * Time.deltaTime;
                if (balloon.Position.y < 0.14f) balloons[i] = NewBalloon(i);
            }

            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null) return;
            foreach (AimSample hand in input.AimSamples)
            {
                if (lockedHands.TryGetValue(hand.HandId, out Vector2 lockedAt))
                {
                    if (Vector2.Distance(lockedAt, hand.Position) < 0.075f) continue;
                    lockedHands.Remove(hand.HandId);
                }
                for (int i = 0; i < balloons.Count; i++)
                {
                    if (Vector2.Distance(hand.Position, balloons[i].Position) >= balloons[i].Size * 0.7f ||
                        (!input.KinectConnected && !hand.Fire)) continue;
                    Score += 10;
                    lockedHands[hand.HandId] = hand.Position;
                    balloons[i] = NewBalloon(i);
                    break;
                }
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            Fill(new Rect(0, 0, Screen.width, Screen.height), Background);
            foreach (Balloon balloon in balloons)
            {
                float size = balloon.Size * Screen.height;
                Rect rect = new Rect(balloon.Position.x * Screen.width - size,
                    balloon.Position.y * Screen.height - size, size * 2f, size * 2.25f);
                Fill(rect, balloon.Color);
                Fill(new Rect(rect.center.x - 2f, rect.yMax, 4f, size * 0.7f), new Color(0.2f, 0.15f, 0.25f, 0.5f));
            }
            DrawHeader("BALLONGJAKTEN");
            DrawEndPanel();
        }
    }

    public sealed class SimonGameUnity : SimpleKidsGame
    {
        private enum Command { HandsUp, ArmsOut, HandsTogether, Duck }
        private Command command;
        private float commandStarted;
        private float nextCommandAt;
        private bool armed;
        protected override Color Background => new Color(0.91f, 0.84f, 1f);
        protected override float Duration => 75f;

        protected override void Start()
        {
            base.Start();
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
            }
            else if (Time.unscaledTime - commandStarted > 7f)
            {
                armed = false;
                nextCommandAt = Time.unscaledTime + 1f;
                PlayVoice("new_movement");
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

        private string Label() => command == Command.HandsUp ? "H\u00c4NDERNA UPP!" :
            command == Command.ArmsOut ? "ARMARNA UT!" :
            command == Command.HandsTogether ? "H\u00c4NDERNA IHOP!" : "DUCKA!";

        private void OnGUI()
        {
            EnsureStyles();
            Fill(new Rect(0, 0, Screen.width, Screen.height), Background);
            DrawHeader("SIMON S\u00c4GER");
            if (armed)
            {
                GUI.Label(new Rect(Screen.width * 0.12f, Screen.height * 0.27f, Screen.width * 0.76f, 110), Label(), TitleStyle);
                float progress = Mathf.Clamp01((Time.unscaledTime - commandStarted) / 7f);
                Fill(new Rect(Screen.width * 0.25f, Screen.height * 0.48f, Screen.width * 0.5f, 28), new Color(0.2f, 0.12f, 0.25f, 0.18f));
                Fill(new Rect(Screen.width * 0.25f, Screen.height * 0.48f, Screen.width * 0.5f * (1f - progress), 28), new Color(0.25f, 0.66f, 0.42f));
            }
            GUI.Label(new Rect(Screen.width * 0.2f, Screen.height * 0.62f, Screen.width * 0.6f, 100),
                "Tangentbord: 1 = h\u00e4nder upp, 2 = armar ut, 3 = h\u00e4nder ihop, ned\u00e5tpil = ducka", TextStyle);
            DrawEndPanel();
        }
    }
}
