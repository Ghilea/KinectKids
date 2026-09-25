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
        [SerializeField] private AudioClip menuMusic;

        private readonly Rect[] visibleCardRects = new Rect[8];
        private readonly int[] visibleGameIndices = new int[8];
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
        private GUIStyle kinectTitleStyle;
        private GUIStyle kinectDetailStyle;
        private GUIStyle kinectButtonStyle;
        private AudioSource voice;
        private AudioSource music;
        private AudioSource songPreview;
        private readonly AudioClip[] songs = new AudioClip[4];
        private int selectedSong;
        private int keyboardFocus;
        private bool keyboardNavigationActive;
        private int hoveredMenuItem = -1;
        private bool showAdventurePicker;
        private bool showKinectPanel;
        private bool showSongsPanel;
        private bool showInformation;
        private string informationTitle;
        private string informationText;
        private float menuScale = 1f;
        private float menuOffsetX;
        private float menuOffsetY;
        private const float DesignWidth = 1672f;
        private const float DesignHeight = 941f;

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

        public void SetMenuMusic(AudioClip clip) => menuMusic = clip;

        private void Awake()
        {
            EnsurePlatformAndCamera();
            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 0f;
            voice.playOnAwake = false;
            music = gameObject.AddComponent<AudioSource>();
            music.spatialBlend = 0f;
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0f;
            songPreview = gameObject.AddComponent<AudioSource>();
            songPreview.spatialBlend = 0f;
            songPreview.playOnAwake = false;
        }

        private static void EnsurePlatformAndCamera()
        {
            if (KinectKidsPlatformRoot.Instance == null)
                new GameObject("KinectKids Platform").AddComponent<KinectKidsPlatformRoot>();

            if (Camera.main != null) return;
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.035f, 0.08f);
            camera.cullingMask = 0;
            camera.depth = -100f;
            cameraObject.AddComponent<AudioListener>();
        }

        private void Start()
        {
            if (registry != null && registry.games != null)
            {
                for (int i = 0; i < registry.games.Length; i++)
                    if (registry.games[i] != null && registry.games[i].sceneName == "Spokjakten")
                    {
                        selected = i;
                        break;
                    }
            }
            AnnounceSelected();
            if (menuMusic == null) menuMusic = Resources.Load<AudioClip>("Audio/MenuTheme");
            if (menuMusic != null)
            {
                music.clip = menuMusic;
                music.Play();
            }
            LoadSongs();
        }

        private void LoadSongs()
        {
            songs[0] = Resources.Load<AudioClip>("Audio/Music/GreveGastsJakt");
            songs[1] = Resources.Load<AudioClip>("Audio/Music/Gnistas Gastvagn");
            songs[2] = Resources.Load<AudioClip>("Audio/Music/gnista");
            songs[3] = Resources.Load<AudioClip>("Audio/Music/Dar gnistorna vaknar");
        }

        private void Update()
        {
            int count = registry != null && registry.games != null ? registry.games.Length : 0;
            if (count == 0) return;

            if (Input.GetKeyDown(KeyCode.Escape)) CloseOverlay();
            if (showSongsPanel)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) SelectSong(-1);
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) SelectSong(1);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) PlaySelectedSong();
                return;
            }
            if (!showAdventurePicker && !showKinectPanel && !showInformation)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { keyboardFocus = (keyboardFocus + 8) % 9; keyboardNavigationActive = true; }
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { keyboardFocus = (keyboardFocus + 1) % 9; keyboardNavigationActive = true; }
            }
            if (showAdventurePicker && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))) ChangeSelected(-1);
            if (showAdventurePicker && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))) ChangeSelected(1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (showAdventurePicker) Play();
                else if (!showKinectPanel && !showInformation) ActivateMenuItem(keyboardFocus);
            }
            if (Input.GetKeyDown(KeyCode.F5)) RetryKinect();

            float wheel = Input.mouseScrollDelta.y;
            if (showAdventurePicker && Mathf.Abs(wheel) > 0.2f && Time.unscaledTime >= nextScrollAt)
            {
                ChangeSelected(wheel > 0f ? -1 : 1);
                nextScrollAt = Time.unscaledTime + 0.22f;
            }

            UpdateKinectDwell();
            if (music != null && music.isPlaying)
                music.volume = Mathf.MoveTowards(music.volume, 0.42f, Time.unscaledDeltaTime * 0.28f);
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

        private void StartStory()
        {
            KinectKidsStoryMode.Start();
        }

        private void OnGUI()
        {
            EnsureStyles();
            Fill(new Rect(0, 0, Screen.width, Screen.height), Color.black);

            if (registry == null || registry.games == null || registry.games.Length == 0)
            {
                GUI.Label(new Rect(40, 40, Screen.width - 80, 80), "Inga spel \u00e4r registrerade.", gameTitleStyle);
                return;
            }

            float scale = Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight);
            float offsetX = (Screen.width - DesignWidth * scale) * 0.5f;
            float offsetY = (Screen.height - DesignHeight * scale) * 0.5f;
            menuScale = scale;
            menuOffsetX = offsetX;
            menuOffsetY = offsetY;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity,
                new Vector3(scale, scale, 1f));
            DrawConceptMenu(scale, offsetX, offsetY);
            GUI.matrix = previous;
            DrawCursors();
        }

        private void DrawConceptMenu(float scale, float offsetX, float offsetY)
        {
            Texture2D concept = cardSheet != null ? cardSheet : background;
            if (concept != null) GUI.DrawTexture(new Rect(0f, 0f, DesignWidth, DesignHeight), concept, ScaleMode.StretchToFill);
            else Fill(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(0.02f, 0.06f, 0.14f));

            playRect = ToScreen(new Rect(47f, 250f, 365f, 120f), scale, offsetX, offsetY);
            if (!showAdventurePicker)
                for (int i = 0; i < visibleCardRects.Length; i++) visibleCardRects[i] = Rect.zero;
            if (!showAdventurePicker && !showKinectPanel && !showSongsPanel && !showInformation) DrawConceptHotspots();
            DrawKinectIndicator();
            if (showAdventurePicker) DrawAdventurePicker();
            else if (showKinectPanel) DrawKinectOverlay();
            else if (showSongsPanel) DrawSongsOverlay();
            else if (showInformation) DrawInformationOverlay();
        }

        private void DrawConceptHotspots()
        {
            Rect[] targets = ConceptButtonRects();
            hoveredMenuItem = -1;
            Vector2 mouse = DesignMousePosition();
            for (int i = 0; i < targets.Length; i++)
                if (targets[i].Contains(mouse)) { hoveredMenuItem = i; break; }
            DrawMenuButtons(targets);

            if (InvisibleButton(new Rect(47f, 250f, 365f, 120f)))
            {
                StartStory();
            }
            if (InvisibleButton(new Rect(505f, 776f, 318f, 116f))) Play();
            if (InvisibleButton(new Rect(66f, 378f, 350f, 96f)) || InvisibleButton(new Rect(830f, 790f, 282f, 101f)))
            {
                showAdventurePicker = true;
                showKinectPanel = false;
                showInformation = false;
                showSongsPanel = false;
            }
            if (InvisibleButton(new Rect(68f, 480f, 350f, 95f))) OpenSongsPanel();
            if (InvisibleButton(new Rect(68f, 580f, 350f, 95f))) ShowInformation("INSTÄLLNINGAR", "Använd helskärm och ställ ljudnivån på datorn före spelet.");
            if (InvisibleButton(new Rect(68f, 680f, 350f, 96f)))
            {
                showKinectPanel = true;
                showAdventurePicker = false;
                showInformation = false;
                showSongsPanel = false;
            }
            if (InvisibleButton(new Rect(1120f, 790f, 220f, 101f))) ShowInformation("FÖRÄLDRAR", "Välj ett äventyr och kontrollera Kinect under Kalibrera innan barnet börjar spela.");
            if (InvisibleButton(new Rect(1350f, 790f, 245f, 101f))) CloseOverlay();
        }

        private static Rect[] ConceptButtonRects() => new[]
        {
            new Rect(47f, 250f, 365f, 120f),
            new Rect(66f, 378f, 350f, 96f),
            new Rect(68f, 480f, 350f, 95f),
            new Rect(68f, 580f, 350f, 95f),
            new Rect(68f, 680f, 350f, 96f),
            new Rect(505f, 776f, 318f, 116f),
            new Rect(830f, 790f, 282f, 101f),
            new Rect(1120f, 790f, 220f, 101f),
            new Rect(1350f, 790f, 245f, 101f)
        };

        private void DrawMenuButtons(Rect[] targets)
        {
            if (buttonSheet == null) return;
            Rect[] normalSources =
            {
                new Rect(36f, 48f, 264f, 100f),
                new Rect(40f, 152f, 260f, 84f),
                new Rect(36f, 248f, 264f, 80f),
                new Rect(36f, 344f, 264f, 80f),
                new Rect(36f, 436f, 264f, 84f)
            };
            Rect[] hoverSources =
            {
                new Rect(1384f, 48f, 256f, 100f),
                new Rect(1388f, 152f, 256f, 84f),
                new Rect(1392f, 248f, 252f, 80f),
                new Rect(1392f, 344f, 252f, 80f),
                new Rect(1392f, 436f, 252f, 84f)
            };
            for (int i = 0; i < normalSources.Length; i++)
            {
                DrawButtonSprite(targets[i], normalSources[i], 1f);
                bool active = hoveredMenuItem == i || (keyboardNavigationActive && keyboardFocus == i);
                if (!active) continue;
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.018f;
                Rect animated = ScaleAroundCenter(targets[i], pulse);
                float alpha = 0.78f + Mathf.Sin(Time.unscaledTime * 5f) * 0.12f;
                DrawButtonSprite(animated, hoverSources[i], alpha);
            }
        }

        private void DrawButtonSprite(Rect destination, Rect sourcePixels, float alpha)
        {
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Rect uv = new Rect(sourcePixels.x / buttonSheet.width,
                1f - (sourcePixels.y + sourcePixels.height) / buttonSheet.height,
                sourcePixels.width / buttonSheet.width, sourcePixels.height / buttonSheet.height);
            GUI.DrawTextureWithTexCoords(destination, buttonSheet, uv, true);
            GUI.color = old;
        }

        private Vector2 DesignMousePosition()
        {
            Vector3 mouse = Input.mousePosition;
            return new Vector2((mouse.x - menuOffsetX) / Mathf.Max(0.001f, menuScale),
                (Screen.height - mouse.y - menuOffsetY) / Mathf.Max(0.001f, menuScale));
        }

        private void ActivateMenuItem(int item)
        {
            switch (item)
            {
                case 0:
                    showAdventurePicker = true;
                    showKinectPanel = false;
                    showInformation = false;
                    break;
                case 5: Play(); break;
                case 1:
                case 6: showAdventurePicker = true; showKinectPanel = false; showInformation = false; break;
                case 2: OpenSongsPanel(); break;
                case 3: ShowInformation("INSTÄLLNINGAR", "Använd helskärm och ställ ljudnivån på datorn före spelet."); break;
                case 4: showKinectPanel = true; showAdventurePicker = false; showInformation = false; break;
                case 7: ShowInformation("FÖRÄLDRAR", "Välj ett äventyr och kontrollera Kinect under Kalibrera innan barnet börjar spela."); break;
                case 8: CloseOverlay(); break;
            }
        }

        private void DrawKinectIndicator()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            bool connected = input != null && input.KinectConnected;
            GUIStyle indicator = new GUIStyle(footerStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
            indicator.normal.textColor = connected ? new Color(0.35f, 1f, 0.50f) : new Color(1f, 0.50f, 0.20f);
            GUI.Label(new Rect(374f, 700f, 34f, 34f), "●", indicator);
        }

        private void DrawAdventurePicker()
        {
            Fill(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(0f, 0f, 0f, 0.72f));
            Rect panel = new Rect(310f, 95f, 1052f, 750f);
            DrawFramedPanel(panel, new Color(0.83f, 0.52f, 0.18f), new Color(0.025f, 0.065f, 0.14f, 0.98f), 6f);
            GUI.Label(new Rect(350f, 115f, 972f, 80f), "VÄLJ ÄVENTYR", gameTitleStyle);
            int count = registry.games.Length;
            for (int i = 0; i < count; i++)
            {
                int column = i % 2;
                int row = i / 2;
                Rect rect = new Rect(365f + column * 490f, 215f + row * 125f, 440f, 98f);
                if (i < visibleCardRects.Length)
                {
                    visibleCardRects[i] = ToScreen(rect, menuScale, menuOffsetX, menuOffsetY);
                    visibleGameIndices[i] = i;
                }
                Color edge = i == selected ? new Color(1f, 0.68f, 0.15f) : new Color(0.48f, 0.32f, 0.17f);
                DrawFramedPanel(rect, edge, new Color(0.04f, 0.11f, 0.22f, 0.98f), i == selected ? 5f : 3f);
                GUI.Label(new Rect(rect.x + 22f, rect.y + 8f, rect.width - 44f, 48f), registry.games[i].displayName.ToUpperInvariant(), cardTitleStyle);
                GUI.Label(new Rect(rect.x + 22f, rect.y + 53f, rect.width - 44f, 30f), Tagline(registry.games[i].sceneName), cardTagStyle);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) SetSelected(i);
            }
            Rect start = new Rect(550f, 735f, 300f, 82f);
            Rect close = new Rect(875f, 735f, 250f, 82f);
            DrawFramedPanel(start, new Color(1f, 0.69f, 0.14f), new Color(0.05f, 0.43f, 0.20f), 5f);
            DrawFramedPanel(close, new Color(0.69f, 0.43f, 0.18f), new Color(0.20f, 0.08f, 0.08f), 4f);
            GUI.Label(start, "STARTA", gameTitleStyle);
            GUI.Label(close, "TILLBAKA", footerStyle);
            if (GUI.Button(start, GUIContent.none, GUIStyle.none)) Play();
            if (GUI.Button(close, GUIContent.none, GUIStyle.none)) CloseOverlay();
        }

        private void DrawKinectOverlay()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            bool connected = input != null && input.KinectConnected;
            bool tracked = input != null && input.PlayerDetected;
            Fill(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(0f, 0f, 0f, 0.72f));
            Rect panel = new Rect(440f, 180f, 792f, 560f);
            DrawFramedPanel(panel, new Color(0.84f, 0.53f, 0.18f), new Color(0.025f, 0.075f, 0.16f, 0.99f), 7f);
            string title = tracked ? "KINECT KLAR – SPELARE HITTAD" : connected ? "KINECT ANSLUTEN" : "KINECT INTE KLAR";
            GUI.Label(new Rect(485f, 220f, 702f, 80f), title, gameTitleStyle);
            GUI.Label(new Rect(515f, 320f, 642f, 150f), input != null ? input.Status : "Kinect-systemet startar…", bodyStyle);
            Rect retry = new Rect(535f, 560f, 285f, 85f);
            Rect close = new Rect(850f, 560f, 285f, 85f);
            DrawFramedPanel(retry, new Color(0.90f, 0.58f, 0.16f), new Color(0.08f, 0.32f, 0.48f), 4f);
            DrawFramedPanel(close, new Color(0.65f, 0.42f, 0.18f), new Color(0.20f, 0.08f, 0.08f), 4f);
            GUI.Label(retry, "FÖRSÖK IGEN", footerStyle);
            GUI.Label(close, "TILLBAKA", footerStyle);
            if (GUI.Button(retry, GUIContent.none, GUIStyle.none)) RetryKinect();
            if (GUI.Button(close, GUIContent.none, GUIStyle.none)) CloseOverlay();
        }

        private void DrawInformationOverlay()
        {
            Fill(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(0f, 0f, 0f, 0.72f));
            Rect panel = new Rect(440f, 210f, 792f, 500f);
            DrawFramedPanel(panel, new Color(0.84f, 0.53f, 0.18f), new Color(0.025f, 0.075f, 0.16f, 0.99f), 7f);
            GUI.Label(new Rect(490f, 250f, 692f, 80f), informationTitle, gameTitleStyle);
            GUI.Label(new Rect(515f, 360f, 642f, 150f), informationText, bodyStyle);
            Rect close = new Rect(690f, 585f, 292f, 82f);
            DrawFramedPanel(close, new Color(0.73f, 0.46f, 0.18f), new Color(0.20f, 0.08f, 0.08f), 4f);
            GUI.Label(close, "TILLBAKA", footerStyle);
            if (GUI.Button(close, GUIContent.none, GUIStyle.none)) CloseOverlay();
        }

        private void ShowInformation(string title, string text)
        {
            informationTitle = title;
            informationText = text;
            showInformation = true;
            showAdventurePicker = false;
            showKinectPanel = false;
            showSongsPanel = false;
        }

        private void OpenSongsPanel()
        {
            showSongsPanel = true;
            showAdventurePicker = false;
            showKinectPanel = false;
            showInformation = false;
        }

        private void SelectSong(int direction)
        {
            selectedSong = (selectedSong + direction + songs.Length) % songs.Length;
        }

        private void PlaySelectedSong()
        {
            AudioClip clip = songs[selectedSong];
            if (clip == null || songPreview == null) return;
            songPreview.Stop();
            songPreview.clip = clip;
            songPreview.loop = false;
            songPreview.Play();
        }

        private void DrawSongsOverlay()
        {
            Fill(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(0f, 0f, 0f, 0.72f));
            Rect panel = new Rect(350f, 125f, 972f, 690f);
            DrawFramedPanel(panel, new Color(0.86f, 0.55f, 0.16f), new Color(0.025f, 0.065f, 0.14f, 0.99f), 7f);
            GUI.Label(new Rect(400f, 155f, 872f, 80f), "SÅNGER", gameTitleStyle);
            for (int i = 0; i < songs.Length; i++)
            {
                Rect row = new Rect(435f, 255f + i * 92f, 902f, 70f);
                bool active = i == selectedSong;
                DrawFramedPanel(row, active ? new Color(1f, 0.68f, 0.16f) : new Color(0.42f, 0.29f, 0.18f),
                    active ? new Color(0.10f, 0.30f, 0.42f) : new Color(0.035f, 0.12f, 0.22f), active ? 5f : 3f);
                GUI.Label(new Rect(row.x + 24f, row.y, row.width - 48f, row.height), SongName(i), footerStyle);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none)) { selectedSong = i; PlaySelectedSong(); }
            }
            Rect close = new Rect(685f, 705f, 302f, 72f);
            DrawFramedPanel(close, new Color(0.70f, 0.45f, 0.18f), new Color(0.20f, 0.08f, 0.08f), 4f);
            GUI.Label(close, "TILLBAKA", footerStyle);
            if (GUI.Button(close, GUIContent.none, GUIStyle.none)) CloseOverlay();
        }

        private static string SongName(int index)
        {
            if (index == 0) return "GREVE GASTS JAKT";
            if (index == 1) return "GNISTAS GASTVAGN";
            if (index == 2) return "GNISTA";
            return "DÄR GNISTORNA VAKNAR";
        }

        private void CloseOverlay()
        {
            KinectKidsStoryMode.Cancel();
            showAdventurePicker = false;
            showKinectPanel = false;
            showSongsPanel = false;
            showInformation = false;
            if (songPreview != null) songPreview.Stop();
        }

        private static bool InvisibleButton(Rect rect) => GUI.Button(rect, GUIContent.none, GUIStyle.none);

        private static Rect ToScreen(Rect designRect, float scale, float offsetX, float offsetY) =>
            new Rect(offsetX + designRect.x * scale, offsetY + designRect.y * scale,
                designRect.width * scale, designRect.height * scale);

        private static Rect ScaleAroundCenter(Rect rect, float scale)
        {
            Vector2 center = rect.center;
            float width = rect.width * scale;
            float height = rect.height * scale;
            return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
        }

        private static void DrawTexturePartContained(Rect destination, Texture2D texture, Rect sourcePixels)
        {
            sourcePixels.x = Mathf.Clamp(sourcePixels.x, 0f, texture.width - 1f);
            sourcePixels.y = Mathf.Clamp(sourcePixels.y, 0f, texture.height - 1f);
            sourcePixels.width = Mathf.Clamp(sourcePixels.width, 1f, texture.width - sourcePixels.x);
            sourcePixels.height = Mathf.Clamp(sourcePixels.height, 1f, texture.height - sourcePixels.y);
            float sourceAspect = sourcePixels.width / sourcePixels.height;
            float destinationAspect = destination.width / destination.height;
            Rect fitted = destination;
            if (sourceAspect > destinationAspect)
            {
                fitted.height = destination.width / sourceAspect;
                fitted.y = destination.center.y - fitted.height * 0.5f;
            }
            else
            {
                fitted.width = destination.height * sourceAspect;
                fitted.x = destination.center.x - fitted.width * 0.5f;
            }
            Rect uv = new Rect(sourcePixels.x / texture.width,
                1f - (sourcePixels.y + sourcePixels.height) / texture.height,
                sourcePixels.width / texture.width, sourcePixels.height / texture.height);
            GUI.DrawTextureWithTexCoords(fitted, texture, uv, true);
        }

        private static Rect ScaleSheetRect(Rect source, float referenceWidth, float referenceHeight)
        {
            return new Rect(source.x / referenceWidth * 1672f, source.y / referenceHeight * 941f,
                source.width / referenceWidth * 1672f, source.height / referenceHeight * 941f);
        }

        private void DrawBrand()
        {
            if (iconSheet != null)
                DrawSheet(ScaleRect(0.018f, 0.012f, 0.315f, 0.19f), new Rect(12f, 10f, 500f, 305f));
            else
            {
                GUI.Label(ScaleRect(0.035f, 0.025f, 0.34f, 0.07f), "KINECT KIDS", logoStyle);
                GUI.Label(ScaleRect(0.038f, 0.092f, 0.34f, 0.035f), "R\u00d6RELSE, SPELGL\u00c4DJE, F\u00d6R ALLA", subtitleStyle);
            }
        }

        private void DrawKinectStatus()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            bool connected = input != null && input.KinectConnected;
            bool playerDetected = input != null && input.PlayerDetected;
            Rect panel = ScaleRect(0.805f, 0.48f, 0.175f, connected ? 0.16f : 0.22f);
            Color panelColor = playerDetected
                ? new Color(0.05f, 0.43f, 0.24f, 0.96f)
                : connected
                    ? new Color(0.04f, 0.34f, 0.48f, 0.96f)
                    : new Color(0.55f, 0.20f, 0.08f, 0.97f);
            Fill(new Rect(panel.x - 3f, panel.y - 3f, panel.width + 6f, panel.height + 6f), new Color(0.78f, 0.47f, 0.13f, 0.98f));
            Fill(panel, panelColor);

            string title = playerDetected
                ? "● KINECT KLAR – SPELARE HITTAD"
                : connected
                    ? "● KINECT ANSLUTEN – STÄLL DIG FRAMFÖR"
                    : "● KINECT INTE KLAR";
            GUI.Label(new Rect(panel.x + 18f, panel.y + 5f, panel.width - 36f, panel.height * 0.38f),
                title, kinectTitleStyle);

            string detail = input != null ? input.Status : "Kinect-systemet startar…";
            GUI.Label(new Rect(panel.x + 18f, panel.y + panel.height * 0.37f,
                panel.width - 36f, connected ? panel.height * 0.55f : panel.height * 0.30f),
                detail, kinectDetailStyle);

            if (!connected)
            {
                Rect retry = new Rect(panel.x + panel.width * 0.12f, panel.y + panel.height * 0.72f,
                    panel.width * 0.76f, panel.height * 0.20f);
                if (GUI.Button(retry, "FÖRSÖK IGEN  [F5]", kinectButtonStyle)) RetryKinect();
            }
        }

        private static void RetryKinect()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input != null) input.RetryKinect();
        }

        private void DrawGameList()
        {
            int count = registry.games.Length;
            float listX = Screen.width * 0.035f;
            float listW = Screen.width * 0.275f;
            float rowH = Screen.height * 0.095f;
            float gap = Screen.height * 0.012f;
            float top = Screen.height * 0.285f;

            for (int slot = 0; slot < 5; slot++)
            {
                int offset = slot - 2;
                int gameIndex = (selected + offset + count) % count;
                bool active = offset == 0;
                float curve = Mathf.Abs(offset) * Screen.width * 0.006f;
                Rect rect = new Rect(listX + curve, top + slot * (rowH + gap), listW - curve, rowH);
                visibleCardRects[slot] = rect;
                visibleGameIndices[slot] = gameIndex;

                Color edge = active ? new Color(1f, 0.63f, 0.12f, 1f) : new Color(0.55f, 0.36f, 0.16f, 0.96f);
                Color inside = active ? new Color(0.22f, 0.12f, 0.30f, 0.97f) : new Color(0.025f, 0.09f, 0.20f, 0.94f);
                DrawFramedPanel(rect, edge, inside, active ? 5f : 3f);

                GameDefinition game = registry.games[gameIndex];
                Rect marker = new Rect(rect.x + rect.height * 0.16f, rect.y + rect.height * 0.25f,
                    rect.height * 0.50f, rect.height * 0.50f);
                Fill(marker, active ? new Color(1f, 0.72f, 0.18f) : new Color(0.22f, 0.55f, 0.84f));
                float textX = rect.x + rect.height * 0.86f;
                GUI.Label(new Rect(textX, rect.y + rect.height * 0.05f, rect.width - (textX - rect.x) - 12f, rect.height * 0.55f),
                    game.displayName.ToUpperInvariant(), cardTitleStyle);
                GUI.Label(new Rect(textX, rect.y + rect.height * 0.57f, rect.width - (textX - rect.x) - 12f, rect.height * 0.28f),
                    Tagline(game.sceneName), cardTagStyle);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) SetSelected(gameIndex);
            }

            GUI.Label(new Rect(listX, top - 40f, listW, 35f), "▲  VÄLJ ÄVENTYR", footerStyle);
            GUI.Label(new Rect(listX, top + 5f * (rowH + gap) - 2f, listW, 35f), "FLER ÄVENTYR  ▼", footerStyle);
        }

        private void DrawGamePresentation()
        {
            GameDefinition game = registry.games[selected];
            Rect adventure = ScaleRect(0.34f, 0.19f, 0.45f, 0.355f);
            if (iconSheet != null)
            {
                DrawSheet(adventure, new Rect(350f, 370f, 615f, 284f));
                DrawSheet(ScaleRect(0.43f, 0.075f, 0.31f, 0.29f), new Rect(540f, 12f, 595f, 350f));
                DrawSheet(ScaleRect(0.815f, 0.12f, 0.155f, 0.31f), new Rect(1140f, 12f, 245f, 345f));
            }
            else DrawFramedPanel(adventure, new Color(0.82f, 0.50f, 0.15f), new Color(0.03f, 0.10f, 0.20f), 5f);

            Rect titlePanel = ScaleRect(0.355f, 0.55f, 0.42f, 0.075f);
            DrawFramedPanel(titlePanel, new Color(0.84f, 0.51f, 0.15f), new Color(0.025f, 0.075f, 0.16f, 0.96f), 3f);
            GUI.Label(new Rect(titlePanel.x + 18f, titlePanel.y, titlePanel.width - 36f, titlePanel.height),
                game.displayName.ToUpperInvariant(), gameTitleStyle);

            GUI.Label(ScaleRect(0.37f, 0.64f, 0.39f, 0.085f), game.description, bodyStyle);

            Rect infoRect = ScaleRect(0.355f, 0.735f, 0.42f, 0.075f);
            DrawFramedPanel(infoRect, new Color(0.43f, 0.32f, 0.22f), new Color(0.02f, 0.08f, 0.16f, 0.96f), 2f);
            float labelY = infoRect.y;
            float labelH = infoRect.height;
            GUI.Label(new Rect(infoRect.x, labelY, infoRect.width * 0.25f, labelH), "1\u20132 SPELARE", footerStyle);
            GUI.Label(new Rect(infoRect.x + infoRect.width * 0.25f, labelY, infoRect.width * 0.25f, labelH), "R\u00d6RELSE", footerStyle);
            GUI.Label(new Rect(infoRect.x + infoRect.width * 0.50f, labelY, infoRect.width * 0.25f, labelH), "MUSIK", footerStyle);
            GUI.Label(new Rect(infoRect.x + infoRect.width * 0.75f, labelY, infoRect.width * 0.25f, labelH), "CA 5 MIN", footerStyle);

            playRect = ScaleRect(0.46f, 0.825f, 0.22f, 0.105f);
            if (iconSheet != null) DrawSheet(playRect, new Rect(335f, 660f, 250f, 100f));
            else DrawFramedPanel(playRect, new Color(1f, 0.67f, 0.13f), new Color(0.06f, 0.48f, 0.24f), 5f);
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
            cardTitleStyle = NewStyle(Mathf.Clamp(Screen.height / 48, 18, 30), FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            cardTagStyle = NewStyle(Mathf.Clamp(Screen.height / 76, 12, 18), FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.96f));
            gameTitleStyle = NewStyle(Mathf.Clamp(Screen.height / 26, 28, 48), FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.28f));
            bodyStyle = NewStyle(Mathf.Clamp(Screen.height / 39, 20, 32), FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            bodyStyle.wordWrap = true;
            footerStyle = NewStyle(Mathf.Clamp(Screen.height / 62, 14, 22), FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            kinectTitleStyle = NewStyle(Mathf.Clamp(Screen.height / 62, 14, 22), FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            kinectDetailStyle = NewStyle(Mathf.Clamp(Screen.height / 78, 12, 17), FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            kinectDetailStyle.wordWrap = true;
            kinectButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(Screen.height / 78, 12, 17),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private static GUIStyle NewStyle(int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fontStyle, alignment = alignment };
            style.normal.textColor = color;
            return style;
        }

        private static Rect ScaleRect(float x, float y, float width, float height) =>
            new Rect(Screen.width * x, Screen.height * y, Screen.width * width, Screen.height * height);

        private void DrawSheet(Rect destination, Rect sourcePixels)
        {
            if (iconSheet == null) return;
            Rect uv = new Rect(
                sourcePixels.x / iconSheet.width,
                1f - (sourcePixels.y + sourcePixels.height) / iconSheet.height,
                sourcePixels.width / iconSheet.width,
                sourcePixels.height / iconSheet.height);
            GUI.DrawTextureWithTexCoords(destination, iconSheet, uv, true);
        }

        private static void DrawFramedPanel(Rect rect, Color edge, Color inside, float border)
        {
            Fill(rect, edge);
            Fill(new Rect(rect.x + border, rect.y + border,
                Mathf.Max(1f, rect.width - border * 2f), Mathf.Max(1f, rect.height - border * 2f)), inside);
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
