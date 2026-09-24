using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D.Platform
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameRegistry registry;
        [SerializeField] private Texture2D background;
        [SerializeField] private Texture2D cardSheet;
        [SerializeField] private Texture2D panelSheet;
        [SerializeField] private Texture2D buttonSheet;
        [SerializeField] private Texture2D iconSheet;

        private readonly Rect[] visibleCardRects = new Rect[5];
        private readonly int[] visibleGameIndices = new int[5];
        private int selected;
        private int hoverTarget = int.MinValue;
        private int hoverHand = int.MinValue;
        private float dwell;
        private float nextScrollAt;
        private Rect playRect;
        private GUIStyle logoStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardTagStyle;
        private GUIStyle gameTitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle footerStyle;
        private AudioSource voice;

        public void SetRegistry(GameRegistry value) => registry = value;

        public void SetVisualAssets(Texture2D menuBackground, Texture2D cards, Texture2D panels,
            Texture2D buttons, Texture2D icons)
        {
            background = menuBackground;
            cardSheet = cards;
            panelSheet = panels;
            buttonSheet = buttons;
            iconSheet = icons;
        }

        private void Awake()
        {
            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 0f;
            voice.playOnAwake = false;
        }

        private void Start()
        {
            AnnounceSelected();
        }

        private void Update()
        {
            int count = registry != null && registry.games != null ? registry.games.Length : 0;
            if (count == 0) return;

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) ChangeSelected(-1);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) ChangeSelected(1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Play();

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.2f && Time.unscaledTime >= nextScrollAt)
            {
                ChangeSelected(wheel > 0f ? -1 : 1);
                nextScrollAt = Time.unscaledTime + 0.22f;
            }

            UpdateKinectDwell();
        }

        private void UpdateKinectDwell()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null || !input.KinectConnected)
            {
                ResetDwell();
                return;
            }

            int foundTarget = int.MinValue;
            int foundHand = int.MinValue;
            IReadOnlyList<AimSample> hands = input.AimSamples;
            for (int handIndex = 0; handIndex < hands.Count && foundTarget == int.MinValue; handIndex++)
            {
                AimSample hand = hands[handIndex];
                if (hand.PlayerIndex != 0) continue;
                Vector2 cursor = new Vector2(hand.Position.x * Screen.width, hand.Position.y * Screen.height);
                if (playRect.Contains(cursor))
                {
                    foundTarget = -1;
                    foundHand = hand.HandId;
                    break;
                }
                for (int slot = 0; slot < visibleCardRects.Length; slot++)
                {
                    if (!visibleCardRects[slot].Contains(cursor)) continue;
                    foundTarget = visibleGameIndices[slot];
                    foundHand = hand.HandId;
                    break;
                }
            }

            if (foundTarget == int.MinValue)
            {
                ResetDwell();
                return;
            }
            if (foundTarget != hoverTarget || foundHand != hoverHand)
            {
                hoverTarget = foundTarget;
                hoverHand = foundHand;
                dwell = 0f;
            }
            dwell += Time.unscaledDeltaTime;

            float required = foundTarget == -1 ? 1f : 0.42f;
            if (dwell < required) return;
            if (foundTarget == -1) Play();
            else if (foundTarget != selected) SetSelected(foundTarget);
            ResetDwell();
        }

        private void ResetDwell()
        {
            hoverTarget = int.MinValue;
            hoverHand = int.MinValue;
            dwell = 0f;
        }

        private void ChangeSelected(int direction)
        {
            if (registry == null || registry.games == null || registry.games.Length == 0) return;
            SetSelected((selected + direction + registry.games.Length) % registry.games.Length);
        }

        private void SetSelected(int index)
        {
            if (index == selected) return;
            selected = index;
            AnnounceSelected();
        }

        private void AnnounceSelected()
        {
            if (voice == null || registry == null || registry.games == null || registry.games.Length == 0) return;
            string scene = registry.games[selected].sceneName;
            string key = scene == "GreveGast" ? "menu_greve_gast" :
                scene == "Spokjakten" ? "menu_spooky" :
                scene == "Matematikbanan" ? "menu_math" :
                scene == "Bokstavsjakten" ? "menu_swedish" :
                scene == "Formverkstan" ? "menu_shapes" :
                scene == "Monsterjakten" ? "menu_patterns" :
                scene == "SimonSager" ? "menu_simon" : "menu_balloons";
            AudioClip clip = Resources.Load<AudioClip>("Voice/" + key);
            if (clip == null) return;
            voice.Stop();
            voice.clip = clip;
            voice.Play();
        }

        private void Play()
        {
            if (registry == null || registry.games == null || selected >= registry.games.Length) return;
            GameDefinition game = registry.games[selected];
            if (game == null || !game.isAvailable) return;

            KinectKidsPlatformRoot platform = KinectKidsPlatformRoot.Instance;
            if (platform == null)
            {
                GameObject platformObject = new GameObject("KinectKids Platform");
                platform = platformObject.AddComponent<KinectKidsPlatformRoot>();
            }

            platform.Scenes.LoadGame(game.sceneName);
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (background != null) GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), background, ScaleMode.ScaleAndCrop);
            else Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.12f, 0.20f));

            if (registry == null || registry.games == null || registry.games.Length == 0)
            {
                GUI.Label(new Rect(40, 40, Screen.width - 80, 80), "Inga spel \u00e4r registrerade.", gameTitleStyle);
                return;
            }

            DrawBrand();
            DrawGameList();
            DrawGamePresentation();
            DrawFooter();
            DrawCursors();
        }

        private void DrawBrand()
        {
            GUI.Label(ScaleRect(0.035f, 0.025f, 0.34f, 0.07f), "KINECT KIDS", logoStyle);
            GUI.Label(ScaleRect(0.038f, 0.092f, 0.34f, 0.035f), "R\u00d6RELSE, SPELGL\u00c4DJE, F\u00d6R ALLA", subtitleStyle);
        }

        private void DrawGameList()
        {
            int count = registry.games.Length;
            float listX = Screen.width * 0.035f;
            float listW = Screen.width * 0.34f;
            float rowH = Screen.height * 0.118f;
            float gap = Screen.height * 0.008f;
            float top = Screen.height * 0.18f;

            for (int slot = 0; slot < 5; slot++)
            {
                int offset = slot - 2;
                int gameIndex = (selected + offset + count) % count;
                bool active = offset == 0;
                float curve = Mathf.Abs(offset) * Screen.width * 0.012f;
                Rect rect = new Rect(listX + curve, top + slot * (rowH + gap), listW - curve, rowH);
                visibleCardRects[slot] = rect;
                visibleGameIndices[slot] = gameIndex;

                if (cardSheet != null)
                {
                    Rect uv = active ? new Rect(0.075f, 0.68f, 0.90f, 0.165f) : new Rect(0.075f, 0.535f, 0.90f, 0.14f);
                    GUI.DrawTextureWithTexCoords(rect, cardSheet, uv, true);
                }
                else
                {
                    Fill(rect, active ? new Color(1f, 0.93f, 0.72f, 0.97f) : new Color(0.78f, 0.82f, 0.85f, 0.95f));
                }

                GameDefinition game = registry.games[gameIndex];
                float textX = rect.x + rect.width * 0.34f;
                GUI.Label(new Rect(textX, rect.y + rect.height * 0.08f, rect.width * 0.62f, rect.height * 0.50f),
                    game.displayName.ToUpperInvariant(), cardTitleStyle);
                GUI.Label(new Rect(textX, rect.y + rect.height * 0.56f, rect.width * 0.62f, rect.height * 0.30f),
                    Tagline(game.sceneName), cardTagStyle);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) SetSelected(gameIndex);
            }

            GUI.Label(new Rect(listX + listW * 0.43f, top - 35f, 60f, 35f), "\u25b2", cardTitleStyle);
            GUI.Label(new Rect(listX + listW * 0.43f, top + 5f * (rowH + gap) - 5f, 60f, 35f), "\u25bc", cardTitleStyle);
        }

        private void DrawGamePresentation()
        {
            GameDefinition game = registry.games[selected];
            Rect titlePanel = ScaleRect(0.41f, 0.43f, 0.40f, 0.14f);
            if (panelSheet != null)
                GUI.DrawTextureWithTexCoords(titlePanel, panelSheet, new Rect(0.01f, 0.765f, 0.61f, 0.22f), true);
            else Fill(titlePanel, new Color(1f, 0.96f, 0.84f, 0.95f));
            GUI.Label(new Rect(titlePanel.x + 25f, titlePanel.y + 10f, titlePanel.width - 50f, titlePanel.height - 20f),
                game.displayName.ToUpperInvariant(), gameTitleStyle);

            GUI.Label(ScaleRect(0.43f, 0.585f, 0.38f, 0.14f), game.description, bodyStyle);

            Rect infoRect = ScaleRect(0.42f, 0.74f, 0.39f, 0.11f);
            if (panelSheet != null)
                GUI.DrawTextureWithTexCoords(infoRect, panelSheet, new Rect(0.01f, 0.49f, 0.61f, 0.24f), true);
            else Fill(infoRect, new Color(0.03f, 0.14f, 0.25f, 0.95f));
            float labelY = infoRect.y + infoRect.height * 0.58f;
            float labelH = infoRect.height * 0.28f;
            GUI.Label(new Rect(infoRect.x, labelY, infoRect.width * 0.25f, labelH), "1\u20132 SPELARE", footerStyle);
            GUI.Label(new Rect(infoRect.x + infoRect.width * 0.25f, labelY, infoRect.width * 0.25f, labelH), "R\u00d6RELSE", footerStyle);
            GUI.Label(new Rect(infoRect.x + infoRect.width * 0.50f, labelY, infoRect.width * 0.25f, labelH), "MUSIK", footerStyle);
            GUI.Label(new Rect(infoRect.x + infoRect.width * 0.75f, labelY, infoRect.width * 0.25f, labelH), "CA 5 MIN", footerStyle);

            playRect = ScaleRect(0.80f, 0.62f, 0.17f, 0.28f);
            if (buttonSheet != null)
                GUI.DrawTextureWithTexCoords(playRect, buttonSheet, new Rect(0.0f, 0.52f, 0.34f, 0.47f), true);
            else Fill(playRect, new Color(1f, 0.75f, 0.12f, 0.98f));
            GUIStyle playLabel = new GUIStyle(cardTitleStyle) { alignment = TextAnchor.MiddleCenter };
            playLabel.normal.textColor = new Color(0.03f, 0.08f, 0.14f);
            GUI.Label(new Rect(playRect.x, playRect.y + playRect.height * 0.76f, playRect.width, playRect.height * 0.18f),
                "STARTA SPEL", playLabel);
            if (GUI.Button(playRect, GUIContent.none, GUIStyle.none)) Play();
        }

        private void DrawFooter()
        {
            Rect footer = new Rect(0, Screen.height * 0.925f, Screen.width, Screen.height * 0.075f);
            Fill(footer, new Color(0.025f, 0.09f, 0.16f, 0.96f));
            string inputHint = KinectKidsInputManager.Instance != null && KinectKidsInputManager.Instance.KinectConnected
                ? "FLYTTA HANDEN F\u00d6R ATT NAVIGERA     \u2022     H\u00c5LL KVAR F\u00d6R ATT V\u00c4LJA"
                : "PILTANGENTER / MUS F\u00d6R ATT NAVIGERA     \u2022     ENTER F\u00d6R ATT STARTA";
            GUI.Label(new Rect(Screen.width * 0.04f, footer.y, Screen.width * 0.92f, footer.height), inputHint, footerStyle);
        }

        private void DrawCursors()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null || !input.KinectConnected) return;
            foreach (AimSample hand in input.AimSamples)
            {
                if (hand.PlayerIndex != 0) continue;
                Vector2 point = new Vector2(hand.Position.x * Screen.width, hand.Position.y * Screen.height);
                float size = Mathf.Max(34f, Screen.height * 0.045f);
                GUI.color = hand.HandId == hoverHand ? new Color(1f, 0.82f, 0.15f) : new Color(0.25f, 0.82f, 1f);
                GUI.DrawTexture(new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size), Texture2D.whiteTexture);
                if (hand.HandId == hoverHand)
                {
                    float required = hoverTarget == -1 ? 1f : 0.42f;
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(point.x - size * 0.6f, point.y + size * 0.62f,
                        size * 1.2f * Mathf.Clamp01(dwell / required), 7f), Texture2D.whiteTexture);
                }
            }
            GUI.color = Color.white;
        }

        private static string Tagline(string scene)
        {
            if (scene == "GreveGast") return "SPRING \u2022 HOPPA \u2022 DUCKA";
            if (scene == "Spokjakten") return "SIKTA \u2022 KASTA \u2022 V\u00c4J";
            if (scene == "Matematikbanan") return "R\u00c4KNA \u2022 F\u00c5NGA \u2022 L\u00c4R";
            if (scene == "Bokstavsjakten") return "LYSSNA \u2022 BOKST\u00c4VER \u2022 ORD";
            if (scene == "Formverkstan") return "FORMER \u2022 F\u00c4RGER \u2022 R\u00d6RELSE";
            if (scene == "Monsterjakten") return "M\u00d6NSTER \u2022 T\u00c4NK \u2022 F\u00c5NGA";
            if (scene == "SimonSager") return "LYSSNA \u2022 H\u00c4RMA \u2022 R\u00d6R DIG";
            return "F\u00c5NGA \u2022 POPPA \u2022 SAMARBETA";
        }

        private void EnsureStyles()
        {
            if (logoStyle != null) return;
            logoStyle = NewStyle(Mathf.Clamp(Screen.height / 21, 34, 58), FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            subtitleStyle = NewStyle(Mathf.Clamp(Screen.height / 55, 15, 24), FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.86f, 0.90f, 0.94f));
            cardTitleStyle = NewStyle(Mathf.Clamp(Screen.height / 38, 22, 36), FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.04f, 0.10f, 0.17f));
            cardTagStyle = NewStyle(Mathf.Clamp(Screen.height / 70, 13, 20), FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.08f, 0.13f, 0.20f));
            gameTitleStyle = NewStyle(Mathf.Clamp(Screen.height / 18, 36, 66), FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.03f, 0.08f, 0.14f));
            bodyStyle = NewStyle(Mathf.Clamp(Screen.height / 39, 20, 32), FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            bodyStyle.wordWrap = true;
            footerStyle = NewStyle(Mathf.Clamp(Screen.height / 62, 14, 22), FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        }

        private static GUIStyle NewStyle(int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fontStyle, alignment = alignment };
            style.normal.textColor = color;
            return style;
        }

        private static Rect ScaleRect(float x, float y, float width, float height) =>
            new Rect(Screen.width * x, Screen.height * y, Screen.width * width, Screen.height * height);

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
