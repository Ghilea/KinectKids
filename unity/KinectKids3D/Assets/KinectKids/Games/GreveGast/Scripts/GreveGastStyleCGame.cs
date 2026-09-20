using System;
using System.Collections.Generic;
using UnityEngine;
using GreveGast2D;

namespace KinectKids3D.Platform
{
    /// <summary>
    /// Style C+ vertical slice: intro, first corridor and first refrain.
    /// Music/timeline/input remain the authoritative legacy-tested systems.
    /// </summary>
    public sealed class GreveGastStyleCGame : MonoBehaviour
    {
        private sealed class PaperObstacle
        {
            public GreveGastCue Cue;
            public bool Resolved;
        }

        private readonly List<PaperObstacle> obstacles = new List<PaperObstacle>();
        private GreveGastSongController song;
        private GreveGastBodyInput body;
        private GreveGastDrawnController greve;
        private Texture2D greveTexture;
        private Camera worldCamera;
        private int lane;
        private bool laneArmed = true;
        private float chase = 0.34f;
        private float feedbackUntil;
        private string feedback = "";
        private Color feedbackColor = Color.white;
        private GreveGastCue warning;
        private float environmentPulseUntil;
        private float activeRunUntil;
        private float caughtUntil;
        private bool debug;
        private GUIStyle huge;
        private GUIStyle medium;
        private GUIStyle small;

        private void Start()
        {
            CreateCamera();
            body = new GreveGastBodyInput();
            body.Start();
            song = gameObject.AddComponent<GreveGastSongController>();
            song.Warning += OnWarning;
            song.Cue += OnCue;
            song.SectionChanged += section => feedback = section.name;

            GameObject prefab = Resources.Load<GameObject>("GreveGast2D/GreveGastDrawn");
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, new Vector3(100f, 100f, 0f), Quaternion.identity);
                greve = instance.GetComponent<GreveGastDrawnController>();
                SpriteRenderer renderer = instance.GetComponentInChildren<SpriteRenderer>(true);
                if (renderer != null && renderer.sprite != null) greveTexture = renderer.sprite.texture;
            }

            if (!song.Begin()) return;
            GreveGastDrawnIntro intro = gameObject.AddComponent<GreveGastDrawnIntro>();
            intro.Configure(greve, song);
        }

        private void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Style C+ papperskamera");
            cameraObject.transform.SetParent(transform, false);
            worldCamera = cameraObject.AddComponent<Camera>();
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = new Color(0.96f, 0.91f, 0.77f);
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 5f;
            worldCamera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private void Update()
        {
            if (song == null || body == null) return;
            body.Update();
            if (Input.GetKeyDown(KeyCode.F2)) debug = !debug;
            if (Input.GetKeyDown(KeyCode.F3)) song.Seek(58f);
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                GreveGastCue next = song.NextCue();
                if (next != null) song.Seek(Mathf.Max(song.GameplayStartTime, next.time - next.warning - 0.25f));
            }
            if (song.SongTime < song.GameplayStartTime) return;

            bool left = body.Current == GreveGastAction.Left || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            bool right = body.Current == GreveGastAction.Right || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
            if (laneArmed && left) { lane = Mathf.Max(-1, lane - 1); laneArmed = false; }
            else if (laneArmed && right) { lane = Mathf.Min(1, lane + 1); laneArmed = false; }
            if (!left && !right) laneArmed = true;

            if (song.SongTime <= activeRunUntil)
            {
                float direction = body.Current == GreveGastAction.Run ? -0.055f : 0.035f;
                chase = Mathf.Clamp01(chase + direction * Time.deltaTime);
            }
            else chase = Mathf.MoveTowards(chase, 0.43f, Time.deltaTime * 0.012f);
            if (chase >= 0.98f && caughtUntil <= 0f)
            {
                caughtUntil = Time.unscaledTime + 2.4f;
                feedback = "GREVE GAST FICK TAG I DIG!";
                feedbackColor = new Color(0.74f, 0.16f, 0.72f);
                feedbackUntil = caughtUntil;
                if (greve != null) greve.PlayCatch();
            }
            if (caughtUntil > 0f && Time.unscaledTime >= caughtUntil)
            {
                chase = 0.58f;
                caughtUntil = 0f;
            }
            if (greve != null)
            {
                greve.SetRunning(true);
                greve.SetChaseDistance(chase);
            }
        }

        private void OnWarning(GreveGastCue cue)
        {
            if (song == null || cue.time < song.GameplayStartTime) return;
            if (string.Equals(cue.kind, "action", StringComparison.OrdinalIgnoreCase))
            {
                warning = cue;
                obstacles.Add(new PaperObstacle { Cue = cue });
            }
        }

        private void OnCue(GreveGastCue cue)
        {
            if (cue.time < song.GameplayStartTime) return;
            if (string.Equals(cue.kind, "action", StringComparison.OrdinalIgnoreCase))
            {
                bool success = Matches(cue.action);
                if (string.Equals(cue.action, "run", StringComparison.OrdinalIgnoreCase))
                    activeRunUntil = cue.time + Mathf.Max(1f, cue.duration);
                chase = Mathf.Clamp01(chase + (success ? -0.095f : 0.14f));
                feedback = success ? "BRA!" : "OJ!";
                feedbackColor = success ? new Color(0.08f, 0.66f, 0.32f) : new Color(0.93f, 0.22f, 0.24f);
                feedbackUntil = Time.unscaledTime + 0.75f;
                for (int i = 0; i < obstacles.Count; i++)
                    if (obstacles[i].Cue == cue) obstacles[i].Resolved = true;
                if (warning == cue) warning = null;
                if (greve != null)
                {
                    if (success) greve.PlayStumble(); else greve.PlayReach();
                }
            }
            else
            {
                environmentPulseUntil = Time.unscaledTime + Mathf.Max(0.5f, cue.duration);
                if (greve == null) return;
                if (cue.kind == "gast_reach" || cue.kind == "gast_closer") greve.PlayReach();
                else if (cue.kind == "gast_dance") greve.PlayDance();
                else if (cue.kind == "scare") greve.PlaySurprise();
            }
        }

        private bool Matches(string action)
        {
            switch ((action ?? "").ToLowerInvariant())
            {
                case "jump": return body.Current == GreveGastAction.Jump;
                case "duck": return body.Current == GreveGastAction.Duck;
                case "left": return lane < 0 || body.Current == GreveGastAction.Left;
                case "right": return lane > 0 || body.Current == GreveGastAction.Right;
                case "run": return body.Current == GreveGastAction.Run;
                default: return true;
            }
        }

        private void OnGUI()
        {
            if (song == null || song.SongTime < song.GameplayStartTime) return;
            EnsureStyles();
            DrawPaperWorld();
            DrawGreve();
            DrawObstacles();
            DrawPlayer();
            DrawHud();
            if (debug) DrawDebug();
        }

        private void DrawPaperWorld()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.96f, 0.91f, 0.77f));
            float horizon = Screen.height * 0.29f;
            float centre = Screen.width * 0.5f;
            float bottom = Screen.height * 0.93f;
            Color ink = new Color(0.12f, 0.08f, 0.15f, 0.92f);
            for (int i = -2; i <= 2; i++)
            {
                float bx = centre + i * Screen.width * 0.17f;
                float hx = centre + i * Screen.width * 0.038f;
                DrawLine(new Vector2(hx, horizon), new Vector2(bx, bottom), 5f + (i & 1), ink);
            }
            float scroll = (song.SongTime - song.GameplayStartTime) * 0.65f;
            for (int i = 0; i < 11; i++)
            {
                float p = Mathf.Repeat(i / 10f + scroll * 0.09f, 1f);
                p *= p;
                float y = Mathf.Lerp(horizon, bottom, p);
                float half = Mathf.Lerp(Screen.width * 0.10f, Screen.width * 0.49f, p);
                DrawLine(new Vector2(centre - half, y), new Vector2(centre + half, y), Mathf.Lerp(2f, 8f, p),
                    new Color(0.35f, 0.22f, 0.28f, 0.35f));
            }
            DrawDoodleWalls(horizon, bottom);
        }

        private void DrawDoodleWalls(float horizon, float bottom)
        {
            Color[] colors = { new Color(0.12f, 0.62f, 0.86f), new Color(1f, 0.48f, 0.08f), new Color(0.70f, 0.20f, 0.75f) };
            for (int i = 0; i < 9; i++)
            {
                float y = horizon + i * (bottom - horizon) / 9f;
                float size = 12f + i * 5f;
                Color color = colors[i % colors.Length];
                color.a = environmentPulseUntil > Time.unscaledTime ? 0.95f : 0.55f;
                Fill(new Rect(18f + (i % 3) * 18f, y, size * 2.6f, 8f), color);
                Fill(new Rect(Screen.width - 18f - size * 2.6f - (i % 3) * 18f, y + 9f, size * 2.6f, 8f), color);
            }
            float pulse = 0.5f + Mathf.Sin(song.SongTime * 2.1f) * 0.5f;
            DrawPaperDoor(new Rect(Screen.width * 0.055f, Screen.height * 0.37f, Screen.width * 0.11f, Screen.height * 0.25f),
                new Color(0.94f, 0.30f, 0.25f), pulse);
            DrawPaperDoor(new Rect(Screen.width * 0.84f, Screen.height * 0.34f, Screen.width * 0.10f, Screen.height * 0.23f),
                new Color(0.18f, 0.58f, 0.88f), 1f - pulse);

            // Små teckningar lever i väggarna och blinkar i takt med musiken.
            float eyeHeight = Mathf.Sin(song.SongTime * 3.4f) > 0.82f ? 2f : 9f;
            Fill(new Rect(Screen.width * 0.205f, Screen.height * 0.29f, 12f, eyeHeight), new Color(0.12f, 0.08f, 0.15f));
            Fill(new Rect(Screen.width * 0.226f, Screen.height * 0.29f, 12f, eyeHeight), new Color(0.12f, 0.08f, 0.15f));
            Fill(new Rect(Screen.width * 0.75f, Screen.height * 0.25f, 10f, eyeHeight), new Color(0.12f, 0.08f, 0.15f));
            Fill(new Rect(Screen.width * 0.77f, Screen.height * 0.25f, 10f, eyeHeight), new Color(0.12f, 0.08f, 0.15f));
        }

        private static void DrawPaperDoor(Rect rect, Color crayon, float open)
        {
            Fill(new Rect(rect.x - 6f, rect.y - 6f, rect.width + 12f, rect.height + 12f), new Color(0.14f, 0.09f, 0.16f));
            Fill(rect, Color.Lerp(crayon, new Color(1f, 0.91f, 0.67f), 0.30f));
            float gap = rect.width * 0.42f * open;
            Fill(new Rect(rect.center.x - gap * 0.5f, rect.y + 8f, gap, rect.height - 8f), new Color(0.22f, 0.12f, 0.27f));
            Fill(new Rect(rect.xMax - 18f, rect.center.y, 8f, 8f), new Color(1f, 0.78f, 0.12f));
        }

        private void DrawGreve()
        {
            if (greveTexture == null) return;
            float size = Mathf.Lerp(Screen.height * 0.14f, Screen.height * 0.34f, chase);
            float y = Mathf.Lerp(Screen.height * 0.24f, Screen.height * 0.39f, chase);
            Rect rect = new Rect(Screen.width * 0.5f - size * 0.42f, y - size * 0.5f, size * 0.84f, size);
            GUI.color = Color.white;
            GUI.DrawTexture(rect, greveTexture, ScaleMode.ScaleToFit, true);
        }

        private void DrawObstacles()
        {
            float now = song.SongTime;
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                PaperObstacle obstacle = obstacles[i];
                float lead = Mathf.Max(0.35f, obstacle.Cue.warning);
                float progress = Mathf.Clamp01((now - (obstacle.Cue.time - lead)) / lead);
                if (now > obstacle.Cue.time + 0.9f) { obstacles.RemoveAt(i); continue; }
                if (obstacle.Resolved) continue;
                DrawObstacle(obstacle.Cue, progress);
            }
        }

        private void DrawObstacle(GreveGastCue cue, float progress)
        {
            float eased = progress * progress;
            float y = Mathf.Lerp(Screen.height * 0.31f, Screen.height * 0.82f, eased);
            float laneWidth = Mathf.Lerp(Screen.width * 0.055f, Screen.width * 0.23f, eased);
            float x = Screen.width * 0.5f;
            Color charcoal = new Color(0.10f, 0.07f, 0.12f);
            string action = (cue.action ?? "").ToLowerInvariant();
            if (action == "jump")
            {
                Fill(new Rect(x - laneWidth * 0.52f, y, laneWidth, Mathf.Lerp(8f, 45f, eased)), charcoal);
                Fill(new Rect(x - laneWidth * 0.42f, y - 7f, laneWidth * 0.84f, 7f), new Color(0.62f, 0.22f, 0.70f));
            }
            else if (action == "duck")
            {
                Fill(new Rect(x - laneWidth * 1.7f, y - Mathf.Lerp(18f, 110f, eased), laneWidth * 3.4f,
                    Mathf.Lerp(12f, 32f, eased)), new Color(0.95f, 0.39f, 0.08f));
                Fill(new Rect(x - laneWidth * 1.72f, y - Mathf.Lerp(22f, 116f, eased), laneWidth * 3.44f, 5f), charcoal);
            }
            else if (action == "left" || action == "right")
            {
                float side = action == "left" ? 1f : -1f;
                float wallX = x + side * laneWidth * 0.84f;
                Fill(new Rect(wallX - laneWidth * 0.48f, y - laneWidth * 0.9f, laneWidth * 0.96f, laneWidth * 1.25f),
                    action == "left" ? new Color(0.96f, 0.22f, 0.38f) : new Color(0.10f, 0.64f, 0.88f));
            }
        }

        private void DrawPlayer()
        {
            float x = Screen.width * 0.5f + lane * Screen.width * 0.205f;
            float jump = body.Current == GreveGastAction.Jump ? Screen.height * 0.08f : 0f;
            float duck = body.Current == GreveGastAction.Duck ? Screen.height * 0.055f : 0f;
            float y = Screen.height * 0.82f - jump + duck;
            float scale = body.Current == GreveGastAction.Duck ? 0.72f : 1f;
            Color ink = new Color(0.10f, 0.08f, 0.14f);
            Fill(new Rect(x - 26f, y - 48f * scale, 52f, 52f * scale), new Color(1f, 0.78f, 0.22f));
            Fill(new Rect(x - 18f, y + 2f, 36f, 72f * scale), new Color(0.16f, 0.62f, 0.90f));
            DrawLine(new Vector2(x - 7f, y + 70f * scale), new Vector2(x - 30f, y + 110f * scale), 10f, ink);
            DrawLine(new Vector2(x + 7f, y + 70f * scale), new Vector2(x + 30f, y + 110f * scale), 10f, ink);
            Fill(new Rect(x - 13f, y - 29f * scale, 8f, 8f), ink);
            Fill(new Rect(x + 6f, y - 29f * scale, 8f, 8f), ink);
        }

        private void DrawHud()
        {
            Rect meter = new Rect(35, 28, Mathf.Min(470f, Screen.width * 0.31f), 25f);
            Fill(new Rect(meter.x - 8, meter.y - 8, meter.width + 16, meter.height + 16), new Color(1f, 0.98f, 0.87f, 0.88f));
            Fill(meter, new Color(0.30f, 0.19f, 0.31f, 0.22f));
            Fill(new Rect(meter.x, meter.y, meter.width * chase, meter.height), Color.Lerp(new Color(0.16f, 0.72f, 0.42f), new Color(0.94f, 0.20f, 0.30f), chase));
            GUI.Label(new Rect(meter.x, meter.y + 31f, meter.width, 32f), "GREVE GAST KOMMER NÄRMARE", small);
            if (warning != null)
            {
                string symbol = Symbol(warning.action);
                GUI.color = new Color(0.20f, 0.10f, 0.25f);
                GUI.Label(new Rect(0, Screen.height * 0.10f, Screen.width, 150f), symbol, huge);
                GUI.color = Color.white;
            }
            if (Time.unscaledTime < feedbackUntil)
            {
                GUI.color = feedbackColor;
                GUI.Label(new Rect(0, Screen.height * 0.43f, Screen.width, 110f), feedback, huge);
                GUI.color = Color.white;
            }
        }

        private void DrawDebug()
        {
            Fill(new Rect(20, Screen.height - 150f, 560f, 130f), new Color(1f, 0.98f, 0.87f, 0.92f));
            GUI.Label(new Rect(35, Screen.height - 140f, 530f, 110f),
                "STYLE C+ VERTICAL SLICE\nTid " + song.SongTime.ToString("0.00") +
                "  Kropp " + body.Current + "  Fil " + lane + "\n" + body.Status, small);
        }

        private static string Symbol(string action)
        {
            switch ((action ?? "").ToLowerInvariant())
            {
                case "jump": return "↑";
                case "duck": return "↓";
                case "left": return "←";
                case "right": return "→";
                case "run": return "↑ ↑";
                default: return "!";
            }
        }

        private void EnsureStyles()
        {
            if (huge != null) return;
            huge = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 8, 72, 150), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            medium = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 48, 17, 25), fontStyle = FontStyle.Bold, wordWrap = true };
            huge.normal.textColor = medium.normal.textColor = small.normal.textColor = new Color(0.14f, 0.08f, 0.18f);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void DrawLine(Vector2 from, Vector2 to, float width, Color color)
        {
            Matrix4x4 old = GUI.matrix;
            Color oldColor = GUI.color;
            Vector2 delta = to - from;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
            GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, delta.magnitude, width), Texture2D.whiteTexture);
            GUI.matrix = old;
            GUI.color = oldColor;
        }

        private void OnDestroy()
        {
            if (song != null) { song.Warning -= OnWarning; song.Cue -= OnCue; }
            if (body != null) body.Dispose();
        }
    }
}
