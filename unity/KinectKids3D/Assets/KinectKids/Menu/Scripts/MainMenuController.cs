using UnityEngine;

namespace KinectKids3D.Platform
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameRegistry registry;
        private int selected;
        private bool details;
        private Rect selectedRect;
        private float dwell;
        private GUIStyle title;
        private GUIStyle card;
        private GUIStyle body;

        public void SetRegistry(GameRegistry value) => registry = value;

        private void Update()
        {
            int count = registry != null && registry.games != null ? registry.games.Length : 0;
            if (count == 0) return;
            if (!details && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))) selected = (selected + count - 1) % count;
            if (!details && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))) selected = (selected + 1) % count;
            if (Input.GetKeyDown(KeyCode.Escape)) details = false;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (details) Play(); else details = true;
            }

            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null || !input.KinectConnected || selectedRect.width <= 0f) { dwell = 0f; return; }
            Vector2 hand = input.Frame.RightHand;
            Vector2 cursor = new Vector2(hand.x * Screen.width, hand.y * Screen.height);
            if (selectedRect.Contains(cursor))
            {
                dwell += Time.unscaledDeltaTime;
                if (dwell >= 1f) { dwell = 0f; if (details) Play(); else details = true; }
            }
            else dwell = 0f;
        }

        private void Play()
        {
            GameDefinition game = registry.games[selected];
            if (game != null && game.isAvailable) KinectKidsPlatformRoot.Instance.Scenes.LoadGame(game.sceneName);
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUI.color = new Color(0.96f, 0.91f, 0.76f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            DrawPaperDecorations();
            GUI.color = Color.white;
            GUI.Label(new Rect(55, 30, Screen.width - 110, 75), "KINECT KIDS", title);
            GUI.Label(new Rect(58, 100, Screen.width - 116, 40), "Välj ett äventyr", body);

            if (registry == null || registry.games == null || registry.games.Length == 0)
            {
                GUI.Label(new Rect(60, 180, 700, 70), "Inga spel är registrerade ännu.", body);
                return;
            }

            if (details) DrawDetails(); else DrawCards();
            DrawCursor();
        }

        private void DrawCards()
        {
            float x = 70f, w = Mathf.Min(650f, Screen.width * 0.43f), h = Mathf.Min(112f, Screen.height * 0.13f);
            float centerY = Screen.height * 0.47f - h * 0.5f;
            int visible = Mathf.Min(5, registry.games.Length);
            int half = visible / 2;
            for (int offset = -half; offset <= half; offset++)
            {
                if (visible % 2 == 0 && offset == half) continue;
                int i = (selected + offset + registry.games.Length) % registry.games.Length;
                GameDefinition game = registry.games[i];
                float curve = Mathf.Abs(offset) * 42f;
                float scale = offset == 0 ? 1f : 0.86f - Mathf.Abs(offset) * 0.05f;
                Rect rect = new Rect(x + curve, centerY + offset * (h + 15f), w * scale, h * scale);
                GUI.color = new Color(0.18f, 0.10f, 0.22f, 0.28f);
                GUI.DrawTexture(new Rect(rect.x + 8, rect.y + 9, rect.width, rect.height), Texture2D.whiteTexture);
                GUI.color = i == selected ? game.cardColor : Color.Lerp(game.cardColor, Color.white, 0.28f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (GUI.Button(rect, game.displayName, card)) { selected = i; details = true; }
                if (offset == 0) selectedRect = rect;
            }
            GameDefinition active = registry.games[selected];
            Rect preview = new Rect(Screen.width * 0.56f, 180f, Screen.width * 0.37f, Screen.height * 0.56f);
            GUI.color = Color.Lerp(active.cardColor, new Color(1f, 0.95f, 0.72f), 0.72f);
            GUI.DrawTexture(preview, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(preview.x + 35, preview.y + 35, preview.width - 70, 100), active.displayName, title);
            GUI.Label(new Rect(preview.x + 42, preview.y + 145, preview.width - 84, 180), active.description, body);
            string hint = KinectKidsInputManager.Instance != null && KinectKidsInputManager.Instance.KinectConnected
                ? "Håll handen över kortet i 1 sekund" : "Klicka eller tryck Enter för att välja";
            GUI.Label(new Rect(preview.x + 42, preview.yMax - 90, preview.width - 84, 55), hint, body);
        }

        private void DrawDetails()
        {
            GameDefinition game = registry.games[selected];
            Rect panel = new Rect(Screen.width * 0.16f, Screen.height * 0.18f, Screen.width * 0.68f, Screen.height * 0.66f);
            GUI.color = new Color(game.cardColor.r, game.cardColor.g, game.cardColor.b, 0.92f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 50, panel.y + 35, panel.width - 100, 90), game.displayName, title);
            GUI.Label(new Rect(panel.x + 60, panel.y + 140, panel.width - 120, 160), game.description, body);
            selectedRect = new Rect(panel.x + panel.width * 0.24f, panel.yMax - 130f, panel.width * 0.52f, 72f);
            if (GUI.Button(selectedRect, "SPELA", card)) Play();
            if (GUI.Button(new Rect(panel.x + 22, panel.y + 18, 110, 48), "TILLBAKA")) details = false;
        }

        private void DrawCursor()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null || !input.KinectConnected) return;
            Vector2 hand = input.Frame.RightHand;
            Vector2 p = new Vector2(hand.x * Screen.width, hand.y * Screen.height);
            GUI.color = new Color(0.2f, 0.85f, 1f);
            GUI.DrawTexture(new Rect(p.x - 17, p.y - 17, 34, 34), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.85f, 0.2f);
            GUI.DrawTexture(new Rect(p.x - 21, p.y + 23, 42 * Mathf.Clamp01(dwell), 7), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawPaperDecorations()
        {
            Color[] crayons =
            {
                new Color(0.92f, 0.22f, 0.35f), new Color(0.10f, 0.63f, 0.82f),
                new Color(0.98f, 0.62f, 0.12f), new Color(0.31f, 0.67f, 0.30f)
            };
            for (int i = 0; i < 12; i++)
            {
                GUI.color = new Color(crayons[i % crayons.Length].r, crayons[i % crayons.Length].g,
                    crayons[i % crayons.Length].b, 0.32f);
                float x = (i * 173f + 35f) % Mathf.Max(1f, Screen.width - 120f);
                float y = (i % 2 == 0 ? 145f : Screen.height - 85f) + (i % 3) * 9f;
                GUI.DrawTexture(new Rect(x, y, 92f, 7f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 17, 34, 68), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(0.17f, 0.08f, 0.22f);
            card = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 30, 24, 40), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            card.normal.textColor = Color.white;
            card.hover.textColor = new Color(1f, 0.93f, 0.35f);
            card.active.textColor = Color.white;
            body = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 42, 18, 30), wordWrap = true, alignment = TextAnchor.UpperCenter };
            body.normal.textColor = new Color(0.20f, 0.12f, 0.24f);
        }
    }
}
