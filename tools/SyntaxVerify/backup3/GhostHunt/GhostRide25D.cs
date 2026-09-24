using System.Collections.Generic;
using UnityEngine;
using KinectKids.Scene25D;
using KinectKids3D;
using KinectKids3D.Platform;

namespace KinectKids.Games.GhostHunt
{
    /// <summary>
    /// 2.5D version of Spökjakten (new-way.md situation 3: the wagon dark-ride).
    ///
    /// The camera is locked on the wagon; the haunted castle streams toward it as
    /// parallax layers while ghost targets rise out of the depth. The player aims
    /// with the Kinect hand (a world-space cursor) and "fires" to banish ghosts.
    /// No full 3D world exists behind what is seen — everything is layered sprites
    /// scaled toward the camera, so final art drops in without gameplay changes.
    ///
    /// Voice/SFX resource keys are reused from the existing project so audio keeps
    /// working. Ghost targets are modular and art-agnostic.
    /// </summary>
    public sealed class GhostRide25D : MonoBehaviour
    {
        private sealed class GhostTarget
        {
            public GameObject Go;
            public SpriteRenderer Body;
            public SpriteRenderer Glow;
            public CameraDepthScaler Scaler;
            public float Lane;      // -1..1 across the track
            public float Depth;     // 0 far -> 1 near
            public float Speed;
            public bool Banished;
        }

        private readonly List<GhostTarget> ghosts = new List<GhostTarget>();
        private readonly Dictionary<int, Vector2> lockedHands = new Dictionary<int, Vector2>();

        private ParallaxController parallax;
        private ScriptedCamera scriptedCamera;
        private Camera cam;
        private AudioSource voice;

        private int score;
        private float endsAt;
        private float spawnTimer;
        private float rideSpeed = 5f;
        private float messageUntil;
        private string message = "";
        private Color messageColor = Color.white;

        private GUIStyle title;
        private GUIStyle hud;

        private void Start()
        {
            EnsureInputManager();

            var theme = Scene25DBackdrop.Theme.Tinted(new Color(0.10f, 0.58f, 0.78f));
            cam = Scene25DBackdrop.BuildCamera(transform, theme.sky, out scriptedCamera);
            parallax = Scene25DBackdrop.Build(transform, theme);

            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 0f;
            voice.playOnAwake = false;

            endsAt = Time.unscaledTime + 90f;
            spawnTimer = 0.6f;
            PlayClip("Audio/SFX/Ambience/ambient_horror");
        }

        private void EnsureInputManager()
        {
            if (KinectKidsInputManager.Instance == null)
                new GameObject("KinectKidsInputManager").AddComponent<KinectKidsInputManager>();
        }

        private void Update()
        {
            if (Time.unscaledTime >= endsAt) return;

            // The wagon rolls forward; parallax streams the castle past the camera.
            if (parallax != null) parallax.forwardSpeed = rideSpeed;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnGhost();
                spawnTimer = Mathf.Lerp(1.6f, 0.8f, Mathf.InverseLerp(0f, 60f, Time.unscaledTime - (endsAt - 90f)));
            }

            UpdateGhosts();
            ProcessAim();
        }

        private void SpawnGhost()
        {
            var go = new GameObject("Ghost");
            go.transform.SetParent(transform, false);
            var scaler = go.AddComponent<CameraDepthScaler>();
            scaler.band = SceneBand.Hazards;
            scaler.farScale = 0.3f;
            scaler.nearScale = 1.8f;
            scaler.farY = 1.6f;
            scaler.nearY = -1.4f;

            var glow = PlaceholderArt.NewSpriteObject("Glow", PlaceholderArt.Glow(),
                new Color(PlaceholderArt.GhostGlow.r, PlaceholderArt.GhostGlow.g, PlaceholderArt.GhostGlow.b, 0.5f),
                go.transform, SceneBand.Hazards, 0.4f);
            glow.transform.localScale = Vector3.one * 2.2f;

            var body = PlaceholderArt.NewSpriteObject("Body", PlaceholderArt.GhostSilhouette(),
                new Color(0.85f, 0.9f, 1f, 0.9f), go.transform, SceneBand.Hazards, 0.5f);
            body.transform.localScale = Vector3.one * 0.02f;

            float lane = Random.Range(-1f, 1f);
            go.transform.localPosition = new Vector3(lane * 3f, 0, LayerSorting.BandZ(SceneBand.Hazards));
            ghosts.Add(new GhostTarget
            {
                Go = go, Body = body, Glow = glow, Scaler = scaler,
                Lane = lane, Depth = 0f, Speed = Random.Range(0.28f, 0.42f)
            });
        }

        private void UpdateGhosts()
        {
            for (int i = ghosts.Count - 1; i >= 0; i--)
            {
                GhostTarget g = ghosts[i];
                if (g.Banished) { ghosts.RemoveAt(i); continue; }
                g.Depth += g.Speed * Time.deltaTime;
                float x = g.Lane * Mathf.Lerp(1.2f, 4.5f, g.Depth);
                g.Go.transform.localPosition = new Vector3(x, g.Go.transform.localPosition.y, g.Go.transform.localPosition.z);
                g.Scaler.Apply(g.Depth);
                if (g.Depth >= 1f)
                {
                    // Ghost reached the wagon: startle, small penalty.
                    Destroy(g.Go);
                    ghosts.RemoveAt(i);
                    Feedback("BUH!", new Color(0.8f, 0.4f, 0.9f));
                    scriptedCamera?.Shake(0.25f);
                    PlayClip("Audio/SFX/Creatures/ghost_moan_01");
                }
            }
        }

        private void ProcessAim()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null || cam == null) return;
            foreach (AimSample sample in input.AimSamples)
            {
                if (lockedHands.TryGetValue(sample.HandId, out Vector2 lockedAt))
                {
                    if (Vector2.Distance(lockedAt, sample.Position) < 0.08f) continue;
                    lockedHands.Remove(sample.HandId);
                }
                if (!input.KinectConnected && !sample.Fire) continue;

                Vector3 world = ScreenToScene(sample.Position);
                GhostTarget hit = GhostAt(world);
                if (hit == null) continue;
                Banish(hit);
                lockedHands[sample.HandId] = sample.Position;
                return;
            }

            // Mouse/keyboard fallback for development.
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mouse = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
                GhostTarget hit = GhostAt(ScreenToScene(mouse));
                if (hit != null) Banish(hit);
            }
        }

        private Vector3 ScreenToScene(Vector2 normalized)
        {
            float h = cam.orthographicSize;
            float w = h * cam.aspect;
            float x = (normalized.x - 0.5f) * 2f * w + cam.transform.position.x;
            float y = (normalized.y - 0.5f) * 2f * h + cam.transform.position.y;
            return new Vector3(x, y, 0f);
        }

        private GhostTarget GhostAt(Vector3 world)
        {
            GhostTarget best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < ghosts.Count; i++)
            {
                GhostTarget g = ghosts[i];
                if (g.Banished || g.Depth < 0.15f) continue;
                float radius = Mathf.Lerp(0.5f, 2.0f, g.Depth);
                float d = Vector2.Distance(new Vector2(world.x, world.y),
                    new Vector2(g.Go.transform.position.x, g.Go.transform.position.y));
                if (d <= radius && d < bestDist) { bestDist = d; best = g; }
            }
            return best;
        }

        private void Banish(GhostTarget g)
        {
            g.Banished = true;
            Destroy(g.Go);
            score += 10;
            Feedback("BRA TRÄFF!", new Color(0.4f, 0.85f, 0.55f));
            PlayClip("Audio/SFX/Environment/success_bell");
        }

        private void Feedback(string text, Color color)
        {
            message = text;
            messageColor = color;
            messageUntil = Time.unscaledTime + 0.8f;
        }

        private void PlayClip(string resourcePath)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null || voice == null) return;
            voice.PlayOneShot(clip);
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawCursors();

            Fill(new Rect(0, 0, Screen.width, 120), new Color(0.05f, 0.05f, 0.12f, 0.75f));
            GUI.Label(new Rect(20, 12, Screen.width - 40, 60), "SPÖKJAKTEN", title);
            GUI.Label(new Rect(30, 74, 300, 36), "POÄNG: " + score, hud);
            GUI.Label(new Rect(Screen.width - 320, 74, 300, 36),
                "TID: " + Mathf.CeilToInt(Mathf.Max(0, endsAt - Time.unscaledTime)), hud);

            if (Time.unscaledTime < messageUntil)
            {
                GUI.color = messageColor;
                GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 100), message, title);
                GUI.color = Color.white;
            }

            if (Time.unscaledTime >= endsAt)
            {
                Fill(new Rect(Screen.width * 0.2f, Screen.height * 0.3f, Screen.width * 0.6f, Screen.height * 0.36f),
                    new Color(0.05f, 0.05f, 0.12f, 0.92f));
                GUI.Label(new Rect(0, Screen.height * 0.36f, Screen.width, 80), "BRA JOBBAT!  " + score + " POÄNG", title);
                if (GUI.Button(new Rect(Screen.width * 0.34f, Screen.height * 0.52f, Screen.width * 0.32f, 60), "TILL SPELMENYN"))
                    KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
            }
        }

        private void DrawCursors()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null) return;
            foreach (AimSample sample in input.AimSamples)
            {
                Vector2 p = new Vector2(sample.Position.x * Screen.width, (1f - sample.Position.y) * Screen.height);
                Color color = (sample.HandId & 1) == 0
                    ? new Color(0.10f, 0.72f, 0.92f, 0.85f)
                    : new Color(0.96f, 0.30f, 0.52f, 0.85f);
                Fill(new Rect(p.x - 20, p.y - 20, 40, 40), color);
            }
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 14, 40, 76),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hud = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Screen.height / 40, 18, 30),
                fontStyle = FontStyle.Bold
            };
            title.normal.textColor = new Color(0.95f, 0.95f, 1f);
            hud.normal.textColor = new Color(0.9f, 0.9f, 1f);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
