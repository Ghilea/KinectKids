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
        private int selected = -1;
        private int hoverTarget = int.MinValue;
        private int hoverHand = int.MinValue;
        private float dwell;
        private float lastHoverSeenAt;
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
        private GUIStyle pickerTitleStyle;
        private GUIStyle pickerSongTitleStyle;
        private GUIStyle pickerTagStyle;
        private GUIStyle pickerHeadingStyle;
        private GUIStyle pickerBodyStyle;
        private GUIStyle pickerHintStyle;
        private int activatedDwellTarget = int.MinValue;
        private Texture2D pickerBanner;
        private Texture2D pickerPlayingBanner;
        private AudioSource music;
        private AudioSource songPreview;
        private Texture2D menuLogo;
        private Sprite gateGreve;
        private readonly AudioClip[] songs = new AudioClip[4];
        private int selectedSong = -1;
        private int keyboardFocus;
        private bool keyboardNavigationActive;
        private int hoveredMenuItem = -1;
        private bool showAdventurePicker;
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
            if (menuMusic == null) menuMusic = Resources.Load<AudioClip>("Audio/MenuTheme");
            if (menuMusic != null)
            {
                music.clip = menuMusic;
                music.Play();
            }
            menuLogo = Resources.Load<Texture2D>("KinectKidsMenuLogo");
            gateGreve = Resources.Load<Sprite>("GreveChase/GreveGast/greve_reach");
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
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.UpArrow)) SelectSong(-1);
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.DownArrow)) SelectSong(1);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) PlaySelectedSong();
                UpdateKinectDwell();
                return;
            }
            if (!showAdventurePicker && !showInformation)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { keyboardFocus = (keyboardFocus + 3) % 4; keyboardNavigationActive = true; }
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { keyboardFocus = (keyboardFocus + 1) % 4; keyboardNavigationActive = true; }
            }
            if (showAdventurePicker && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))) ChangeSelected(-2);
            if (showAdventurePicker && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))) ChangeSelected(2);
            if (showAdventurePicker && Input.GetKeyDown(KeyCode.LeftArrow)) ChangeSelected(-1);
            if (showAdventurePicker && Input.GetKeyDown(KeyCode.RightArrow)) ChangeSelected(1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (showAdventurePicker) Play();
                else if (!showInformation) ActivateMenuItem(keyboardFocus);
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
            if (input == null || !input.PlayerDetected)
            {
                ResetDwell();
                activatedDwellTarget = int.MinValue;
                return;
            }
            if (input.Frame.PlayerChanged)
            {
                ResetDwell();
                activatedDwellTarget = int.MinValue;
            }

            int foundTarget = int.MinValue;
            int foundHand = int.MinValue;
            IReadOnlyList<AimSample> hands = input.AimSamples;
            float hitPadding = Mathf.Max(10f, Screen.height * 0.012f);
            for (int handIndex = 0; handIndex < hands.Count && foundTarget == int.MinValue; handIndex++)
            {
                AimSample hand = hands[handIndex];
                if (hand.PlayerIndex != 0) continue;
                Vector2 cursor = new Vector2(hand.Position.x * Screen.width, hand.Position.y * Screen.height);
                if (Expanded(playRect, hitPadding).Contains(cursor))
                {
                    foundTarget = showAdventurePicker || showSongsPanel || showInformation ? -2 : -1;
                    foundHand = hand.HandId;
                    break;
                }
                for (int slot = 0; slot < visibleCardRects.Length; slot++)
                {
                    if (visibleCardRects[slot].width <= 0f) continue;
                    if (!Expanded(visibleCardRects[slot], hitPadding).Contains(cursor)) continue;
                    foundTarget = visibleGameIndices[slot];
                    foundHand = hand.HandId;
                    break;
                }
            }

            if (foundTarget == int.MinValue)
            {
                if (Time.unscaledTime - lastHoverSeenAt > 0.24f)
                {
                    ResetDwell();
                    activatedDwellTarget = int.MinValue;
                }
                return;
            }
            // A held hand activates a card once. Move away before choosing again.
            if (foundTarget == activatedDwellTarget) return;
            activatedDwellTarget = int.MinValue;
            if (foundTarget != hoverTarget)
            {
                hoverTarget = foundTarget;
                dwell = 0f;
            }
            hoverHand = foundHand;
            lastHoverSeenAt = Time.unscaledTime;
            dwell += Time.unscaledDeltaTime;

            float required = DwellDuration;
            if (dwell < required) return;
            activatedDwellTarget = foundTarget;
            if (foundTarget == -2) CloseOverlay();
            else if (showAdventurePicker && foundTarget >= 0) { SetSelected(foundTarget); Play(); }
            else if (showSongsPanel && foundTarget >= 0) { selectedSong = foundTarget; PlaySelectedSong(); }
            else if (foundTarget == -1) StartStory();
            ResetDwell();
        }

        private const float DwellDuration = 1f;

        private void ResetDwell()
        {
            hoverTarget = int.MinValue;
            hoverHand = int.MinValue;
            dwell = 0f;
            lastHoverSeenAt = 0f;
        }

        private static Rect Expanded(Rect rect, float padding) =>
            new Rect(rect.x - padding, rect.y - padding,
                rect.width + padding * 2f, rect.height + padding * 2f);

        private void ChangeSelected(int direction)
        {
            if (registry == null || registry.games == null || registry.games.Length == 0) return;
            int count = registry.games.Length;
            SetSelected(selected < 0 ? direction < 0 ? count - 1 : 0
                : ((selected + direction) % count + count) % count);
        }

        private void SetSelected(int index)
        {
            if (index == selected) return;
            selected = index;
        }

        private void Play()
        {
            if (registry == null || registry.games == null || selected < 0 || selected >= registry.games.Length) return;
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
            Texture2D concept = background != null ? background : cardSheet;
            if (concept != null) GUI.DrawTexture(new Rect(0f, 0f, DesignWidth, DesignHeight), concept, ScaleMode.StretchToFill);
            else Fill(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(0.02f, 0.06f, 0.14f));
            DrawMenuArtwork();

            playRect = ToScreen(new Rect(47f, 250f, 365f, 120f), scale, offsetX, offsetY);
            for (int i = 0; i < visibleCardRects.Length; i++) visibleCardRects[i] = Rect.zero;
            if (!showAdventurePicker && !showSongsPanel && !showInformation) DrawConceptHotspots();
            DrawKinectStatusBadge();
            if (showAdventurePicker) DrawAdventurePicker();
            else if (showSongsPanel) DrawSongsOverlay();
            else if (showInformation) DrawInformationOverlay();
        }

        private void DrawMenuArtwork()
        {
            if (menuLogo != null)
                GUI.DrawTexture(new Rect(25f, 18f, 430f, 215f), menuLogo, ScaleMode.ScaleToFit, true);
            if (gateGreve != null)
                GUI.DrawTexture(new Rect(1000f, 360f, 250f, 295f), gateGreve.texture, ScaleMode.ScaleToFit, true);

            if (iconSheet == null) return;
            float shimmer = 0.55f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.24f;
            DrawSheetTinted(new Rect(1190f, 105f, 84f, 83f),
                new Rect(1480f, 430f, 165f, 135f), shimmer);
            DrawSheetTinted(new Rect(685f, 142f, 72f, 72f),
                new Rect(1480f, 430f, 165f, 135f), shimmer * 0.75f);
        }

        private void DrawSheetTinted(Rect destination, Rect sourcePixels, float alpha)
        {
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            DrawSheet(destination, sourcePixels);
            GUI.color = old;
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
            if (InvisibleButton(new Rect(66f, 378f, 350f, 96f)))
            {
                OpenAdventurePicker();
            }
            if (InvisibleButton(new Rect(68f, 480f, 350f, 95f))) OpenSongsPanel();
            if (InvisibleButton(new Rect(68f, 580f, 350f, 95f))) ShowInformation("INSTÄLLNINGAR", "Använd helskärm och ställ ljudnivån på datorn före spelet.");
        }

        private static Rect[] ConceptButtonRects() => new[]
        {
            new Rect(47f, 250f, 365f, 120f),
            new Rect(66f, 378f, 350f, 96f),
            new Rect(68f, 480f, 350f, 95f),
            new Rect(68f, 580f, 350f, 95f)
        };

        private void DrawMenuButtons(Rect[] targets)
        {
            if (buttonSheet == null) return;
            Rect[] normalSources =
            {
                new Rect(36f, 48f, 264f, 100f),
                new Rect(40f, 152f, 260f, 84f),
                new Rect(36f, 248f, 264f, 80f),
                new Rect(36f, 344f, 264f, 80f)
            };
            Rect[] hoverSources =
            {
                new Rect(1384f, 48f, 256f, 100f),
                new Rect(1388f, 152f, 256f, 84f),
                new Rect(1392f, 248f, 252f, 80f),
                new Rect(1392f, 344f, 252f, 80f)
            };
            for (int i = 0; i < normalSources.Length; i++)
            {
                DrawButtonSprite(targets[i], normalSources[i], 1f);
                bool active = hoveredMenuItem == i || (keyboardNavigationActive && keyboardFocus == i);
                if (!active) continue;
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.018f;
                Rect animated = ScaleAroundCenter(targets[i], pulse);
                DrawSparkles(animated, 0.82f + Mathf.Sin(Time.unscaledTime * 5f) * 0.12f);
            }
        }

        private void DrawSparkles(Rect target, float alpha)
        {
            Rect sparkleSource = new Rect(44f, 800f, 104f, 116f);
            float size = Mathf.Min(target.width, target.height) * 0.72f;
            Rect topLeft = new Rect(target.x - size * 0.24f, target.y - size * 0.28f, size, size);
            Rect bottomRight = new Rect(target.xMax - size * 0.76f, target.yMax - size * 0.70f, size, size);
            DrawButtonSpriteTinted(topLeft, sparkleSource, alpha);
            DrawButtonSpriteTinted(bottomRight, sparkleSource, alpha * 0.86f);
        }

        private void DrawButtonSprite(Rect destination, Rect sourcePixels, float alpha)
        {
            DrawButtonSpriteTinted(destination, sourcePixels, alpha, Color.white);
        }

        private void DrawButtonSpriteTinted(Rect destination, Rect sourcePixels, float alpha)
        {
            DrawButtonSpriteTinted(destination, sourcePixels, alpha, new Color(1f, 0.82f, 0.22f));
        }

        private void DrawButtonSpriteTinted(Rect destination, Rect sourcePixels, float alpha, Color tint)
        {
            Color old = GUI.color;
            GUI.color = new Color(tint.r, tint.g, tint.b, alpha);
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
                    StartStory();
                    break;
                case 1: OpenAdventurePicker(); break;
                case 2: OpenSongsPanel(); break;
                case 3: ShowInformation("INSTÄLLNINGAR", "Använd helskärm och ställ ljudnivån på datorn före spelet."); break;
            }
        }

        private void DrawKinectStatusBadge()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            bool connected = input != null && input.KinectConnected;
            bool tracked = input != null && input.PlayerDetected;
            Color edge = tracked ? new Color(0.28f, 0.86f, 0.47f)
                : connected ? new Color(0.34f, 0.72f, 0.95f) : new Color(0.96f, 0.62f, 0.24f);
            Rect badge = new Rect(1205f, 810f, 425f, 88f);
            DrawFramedPanel(badge, edge, new Color(0.025f, 0.065f, 0.13f, 0.94f), 3f);
            string title = tracked ? "KINECT: SPELARE HITTAD"
                : connected ? "KINECT: ANSLUTEN" : "KINECT: SÖKER";
            string detail = tracked ? "Styr med händerna"
                : connected ? "Ställ dig framför kameran" : "Ansluter automatiskt · musen fungerar";
            GUI.Label(new Rect(1224f, 818f, 385f, 34f), title, kinectTitleStyle);
            GUI.Label(new Rect(1224f, 851f, 385f, 33f), detail, kinectDetailStyle);
        }

        private void DrawAdventurePicker()
        {
            Rect panel = new Rect(230f, 55f, 1212f, 830f);
            DrawPickerPanel(panel, "VÄLJ ÄVENTYR", "Välj ett äventyr och börja spela");
            int count = registry.games.Length;
            for (int i = 0; i < count; i++)
            {
                GameDefinition game = registry.games[i];
                if (game == null) continue;
                int column = i % 2;
                int row = i / 2;
                Rect rect = new Rect(panel.x + 56f + column * 562f, panel.y + 164f + row * 124f, 538f, 108f);
                if (i < visibleCardRects.Length)
                {
                    visibleCardRects[i] = game.isAvailable ? ToScreen(rect, menuScale, menuOffsetX, menuOffsetY) : Rect.zero;
                    visibleGameIndices[i] = i;
                }
                bool active = i == selected || rect.Contains(DesignMousePosition()) || hoverTarget == i;
                DrawPickerCard(rect, game.displayName, game.isAvailable ? Tagline(game.sceneName) : "Kommer snart",
                    game.sceneName, game.preview, active, false, game.isAvailable);
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && game.isAvailable;
                if (InvisibleButton(rect)) { SetSelected(i); Play(); }
                GUI.enabled = previousEnabled;
            }
            DrawPickerBack(new Rect(panel.center.x - 145f, panel.yMax - 145f, 290f, 73f));
        }

        private void DrawInformationOverlay()
        {
            Rect panel = new Rect(390f, 195f, 892f, 550f);
            DrawPickerPanel(panel, informationTitle, "Gör dig redo för äventyret");
            if (buttonSheet != null)
                DrawButtonSprite(new Rect(panel.x + 66f, panel.y + 214f, 92f, 92f),
                    new Rect(62f, 350f, 56f, 66f), 1f);
            GUI.Label(new Rect(panel.x + 190f, panel.y + 210f, panel.width - 265f, 180f), informationText, pickerBodyStyle);
            DrawPickerBack(new Rect(panel.center.x - 145f, panel.yMax - 145f, 290f, 73f));
        }

        private void ShowInformation(string title, string text)
        {
            informationTitle = title;
            informationText = text;
            showInformation = true;
            showAdventurePicker = false;
            showSongsPanel = false;
        }

        private void OpenAdventurePicker()
        {
            selected = -1;
            showAdventurePicker = true;
            showInformation = false;
            showSongsPanel = false;
            ResetDwell();
        }

        private void OpenSongsPanel()
        {
            if (music != null && music.isPlaying) music.Pause();
            showSongsPanel = true;
            showAdventurePicker = false;
            showInformation = false;
            selectedSong = -1;
            ResetDwell();
        }

        private void SelectSong(int direction)
        {
            selectedSong = selectedSong < 0 ? direction < 0 ? songs.Length - 1 : 0
                : (selectedSong + direction + songs.Length) % songs.Length;
        }

        private void PlaySelectedSong()
        {
            if (selectedSong < 0 || selectedSong >= songs.Length) return;
            AudioClip clip = songs[selectedSong];
            if (clip == null || songPreview == null) return;
            songPreview.Stop();
            songPreview.clip = clip;
            songPreview.loop = false;
            songPreview.Play();
        }

        private void DrawSongsOverlay()
        {
            Rect panel = new Rect((DesignWidth - 706f) * 0.5f, 55f, 706f, 830f);
            DrawPickerPanel(panel, "SÅNGER", "Välj en sång att lyssna på");
            for (int i = 0; i < songs.Length; i++)
            {
                Rect row = new Rect(panel.x + 84f, panel.y + 164f + i * 124f, 538f, 108f);
                bool playing = songPreview != null && songPreview.isPlaying && songPreview.clip == songs[i];
                bool active = i == selectedSong || row.Contains(DesignMousePosition()) || hoverTarget == i;
                visibleCardRects[i] = songs[i] != null ? ToScreen(row, menuScale, menuOffsetX, menuOffsetY) : Rect.zero;
                visibleGameIndices[i] = i;
                DrawPickerCard(row, SongName(i), string.Empty,
                    i == 0 ? "GreveGast" : i == 1 ? "Kitchen" : "Gnista", null, active, playing, songs[i] != null, true);
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && songs[i] != null;
                if (InvisibleButton(row)) { selectedSong = i; PlaySelectedSong(); }
                GUI.enabled = previousEnabled;
            }
            DrawPickerBack(new Rect(panel.center.x - 145f, panel.yMax - 145f, 290f, 73f));
        }

        private static string SongName(int index)
        {
            if (index == 0) return "GREVE GASTS JAKT";
            if (index == 1) return "GNISTAS GASTVAGN";
            if (index == 2) return "GNISTA";
            return "DÄR GNISTORNA VAKNAR";
        }

        private void DrawPickerPanel(Rect panel, string title, string subtitle)
        {
            Fill(new Rect(0f, 0f, DesignWidth, DesignHeight), new Color(0.005f, 0.012f, 0.035f, 0.78f));
            Fill(new Rect(panel.x + 10f, panel.y + 16f, panel.width, panel.height), new Color(0f, 0f, 0f, 0.35f));
            DrawOrnateFrame(panel);
            GUI.Label(new Rect(panel.x + 70f, panel.y + 38f, panel.width - 140f, 63f), title, pickerHeadingStyle);
            GUI.Label(new Rect(panel.x + 70f, panel.y + 108f, panel.width - 140f, 32f), subtitle, pickerHintStyle);
        }

        private void DrawOrnateFrame(Rect rect)
        {
            Fill(new Rect(rect.x + 22f, rect.y + 22f, rect.width - 44f, rect.height - 44f),
                new Color(0.025f, 0.045f, 0.105f, 0.98f));
            if (buttonSheet == null) return;
            // Every edge shares its source coordinates and thickness with its
            // adjoining corners. This preserves the rail alignment at each seam.
            const float corner = 64f;
            DrawButtonSprite(new Rect(rect.x + 20f, rect.y + 20f, rect.width - 40f, rect.height - 40f),
                new Rect(105f, 625f, 360f, 100f), 0.65f);
            DrawButtonSprite(new Rect(rect.x + corner, rect.y, rect.width - 2f * corner, corner), new Rect(152f, 568f, 2f, 64f), 1f);
            DrawButtonSprite(new Rect(rect.x + corner, rect.yMax - corner, rect.width - 2f * corner, corner), new Rect(175f, 726f, 2f, 64f), 1f);
            DrawButtonSprite(new Rect(rect.x, rect.y + corner, corner, rect.height - 2f * corner), new Rect(24f, 638f, 64f, 64f), 1f);
            DrawButtonSprite(new Rect(rect.xMax - corner, rect.y + corner, corner, rect.height - 2f * corner), new Rect(467f, 638f, 64f, 64f), 1f);
            DrawButtonSprite(new Rect(rect.x, rect.y, corner, corner), new Rect(24f, 568f, 64f, 64f), 1f);
            DrawButtonSprite(new Rect(rect.xMax - corner, rect.y, corner, corner), new Rect(467f, 568f, 64f, 64f), 1f);
            DrawButtonSprite(new Rect(rect.x, rect.yMax - corner, corner, corner), new Rect(24f, 726f, 64f, 64f), 1f);
            DrawButtonSprite(new Rect(rect.xMax - corner, rect.yMax - corner, corner, corner), new Rect(467f, 726f, 64f, 64f), 1f);
            // The crest starts 39 atlas pixels above the frame's top slice.
            // Preserve that offset so both portions share the same gold rail.
            DrawButtonSprite(new Rect(rect.center.x - 64f, rect.y - 39f, 128f, 86f), new Rect(215f, 529f, 128f, 86f), 1f);
        }

        private void DrawPickerCard(Rect rect, string title, string subtitle, string scene,
            Sprite preview, bool active, bool playing, bool available, bool songCard = false)
        {
            Color previous = GUI.color;
            GUI.color = available ? Color.white : new Color(0.65f, 0.65f, 0.7f, 0.85f);
            if (buttonSheet != null)
            {
                if (pickerBanner == null) pickerBanner = IsolateBanner(buttonSheet, out pickerPlayingBanner);
                GUI.DrawTexture(rect, playing ? pickerPlayingBanner : pickerBanner, ScaleMode.StretchToFill, true);
            }
            else DrawFramedPanel(rect, new Color(0.76f, 0.53f, 0.22f), new Color(0.04f, 0.1f, 0.2f), 2f);
            Rect illustration = new Rect(rect.x + 20f, rect.y + 13f, 96f, rect.height - 26f);
            if (preview != null) DrawSpritePreview(illustration, preview);
            else DrawPickerIllustration(illustration, scene);
            GUI.color = previous;
            if (songCard)
            {
                Rect titleRect = new Rect(rect.x + 134f, rect.y + 20f,
                    rect.width - 134f - (playing ? 90f : 40f), rect.height - 40f);
                pickerSongTitleStyle.fontSize = 25;
                float textWidth = pickerSongTitleStyle.CalcSize(new GUIContent(title)).x;
                if (textWidth > titleRect.width)
                    pickerSongTitleStyle.fontSize = Mathf.Max(16, Mathf.FloorToInt(25f * titleRect.width / textWidth));
                GUI.Label(titleRect, title, pickerSongTitleStyle);
            }
            else
            {
                GUI.Label(new Rect(rect.x + 134f, rect.y + 25f, rect.width - 190f, 37f), title, pickerTitleStyle);
                GUI.Label(new Rect(rect.x + 134f, rect.y + 65f, rect.width - 190f, 26f), subtitle, pickerTagStyle);
            }
            if (available && playing)
            {
                Rect indicator = new Rect(rect.xMax - 72f, rect.center.y - 14f, 22f, 28f);
                for (int i = 0; i < 3; i++)
                {
                    float height = 7f + (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f + i * 1.7f)) * 18f;
                    Fill(new Rect(indicator.x + i * 7f, indicator.yMax - height, 4f, height), new Color(0.4f, 0.95f, 1f));
                }
            }
            if (active && available)
            {
                if (buttonSheet != null)
                {
                    DrawButtonSpriteTinted(new Rect(rect.x + 4f, rect.y + 4f, 36f, 40f), new Rect(44f, 800f, 104f, 116f), 0.55f);
                    DrawButtonSpriteTinted(new Rect(rect.xMax - 40f, rect.yMax - 44f, 36f, 40f), new Rect(44f, 800f, 104f, 116f), 0.45f);
                }
            }
        }

        private static Texture2D IsolateBanner(Texture2D atlas, out Texture2D playingTexture)
        {
            const int left = 850, top = 600, width = 306, height = 110;
            Color[] pixels = atlas.GetPixels(left, atlas.height - top - height, width, height);
            bool[] connected = new bool[pixels.Length];
            Queue<int> pending = new Queue<int>();
            // The opaque centre belongs to the banner. Keep its connected
            // silhouette and discard glow fragments from neighbouring buttons.
            int seed = (height / 2) * width + width / 2;
            connected[seed] = true;
            pending.Enqueue(seed);
            while (pending.Count > 0)
            {
                int index = pending.Dequeue();
                int x = index % width, y = index / width;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    int neighbour = ny * width + nx;
                    // Faint glow can bridge the banner to neighbouring art.
                    // Follow its solid silhouette first, then restore its fringe.
                    if (connected[neighbour] || pixels[neighbour].a <= 0.2f) continue;
                    connected[neighbour] = true;
                    pending.Enqueue(neighbour);
                }
            }
            for (int i = 0; i < pixels.Length; i++)
            {
                if (connected[i]) continue;
                int x = i % width, y = i / width;
                bool fringe = false;
                for (int dy = -1; dy <= 1 && !fringe; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    if (connected[ny * width + nx]) { fringe = true; break; }
                }
                if (!fringe) pixels[i] = Color.clear;
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Isolated menu banner",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            // Recolour the warm metal pixels; retain the navy interior and the
            // original highlights and shading of the illustrated frame.
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                if (pixel.a <= 0f || pixel.r <= pixel.b * 1.4f || pixel.g <= pixel.b * 1.2f) continue;
                Color.RGBToHSV(pixel, out _, out float saturation, out float brightness);
                Color cyan = Color.HSVToRGB(0.51f, Mathf.Clamp(saturation, 0.45f, 0.75f), brightness);
                cyan.a = pixel.a;
                pixels[i] = cyan;
            }
            playingTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Playing song cyan frame",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            playingTexture.SetPixels(pixels);
            playingTexture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            if (pickerBanner != null) Destroy(pickerBanner);
            if (pickerPlayingBanner != null) Destroy(pickerPlayingBanner);
        }

        private void DrawPickerIllustration(Rect rect, string scene)
        {
            if (iconSheet == null) return;
            Rect source;
            switch (scene)
            {
                case "GreveGast": source = new Rect(785f, 18f, 333f, 338f); break;
                case "Spokjakten": case "SimonSager": source = new Rect(525f, 26f, 267f, 323f); break;
                case "Matematikbanan": case "Bokstavsjakten": case "Gnista": source = new Rect(1125f, 20f, 264f, 340f); break;
                case "Kitchen": source = new Rect(811f, 786f, 180f, 85f); break;
                case "Monsterjakten": source = new Rect(1159f, 640f, 137f, 108f); break;
                case "Formverkstan": source = new Rect(1420f, 420f, 221f, 168f); break;
                default: source = new Rect(1290f, 410f, 142f, 195f); break;
            }
            DrawTexturePartContained(rect, iconSheet, source);
            if (scene == "Matematikbanan" || scene == "Bokstavsjakten")
                GUI.Label(new Rect(rect.x, rect.yMax - 26f, rect.width, 26f),
                    scene == "Matematikbanan" ? "1 2 3" : "ABC", pickerHintStyle);
        }

        private static void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            Rect pixels = sprite.textureRect;
            float scale = Mathf.Min(rect.width / pixels.width, rect.height / pixels.height);
            Rect fitted = new Rect(rect.center.x - pixels.width * scale * 0.5f,
                rect.center.y - pixels.height * scale * 0.5f, pixels.width * scale, pixels.height * scale);
            GUI.DrawTextureWithTexCoords(fitted, sprite.texture, new Rect(pixels.x / sprite.texture.width,
                pixels.y / sprite.texture.height, pixels.width / sprite.texture.width, pixels.height / sprite.texture.height));
        }

        private void DrawPickerBack(Rect rect)
        {
            playRect = ToScreen(rect, menuScale, menuOffsetX, menuOffsetY);
            if (buttonSheet != null)
                DrawButtonSprite(rect, new Rect(1154f, 689f, 275f, 96f), 1f);
            else GUI.Label(rect, "Tillbaka", footerStyle);
            if (buttonSheet != null && (rect.Contains(DesignMousePosition()) || hoverTarget == -2)) DrawSparkles(rect, 0.55f);
            if (InvisibleButton(rect)) CloseOverlay();
        }

        private void CloseOverlay()
        {
            bool leavingSongs = showSongsPanel;
            KinectKidsStoryMode.Cancel();
            showAdventurePicker = false;
            showSongsPanel = false;
            showInformation = false;
            if (songPreview != null) songPreview.Stop();
            if (leavingSongs && music != null) music.UnPause();
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
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            string inputHint = input != null && input.PlayerDetected
                ? "FLYTTA HANDEN F\u00d6R ATT NAVIGERA     \u2022     H\u00c5LL KVAR F\u00d6R ATT V\u00c4LJA"
                : input != null && input.KinectConnected
                    ? "VISA HELA KROPPEN F\u00d6R KINECT     \u2022     MUS OCH TANGENTBORD FUNGERAR UNDER TIDEN"
                    : "PILTANGENTER / MUS F\u00d6R ATT NAVIGERA     \u2022     ENTER F\u00d6R ATT STARTA";
            GUI.Label(new Rect(Screen.width * 0.04f, footer.y, Screen.width * 0.92f, footer.height), inputHint, footerStyle);
        }

        private void DrawCursors()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null || !input.PlayerDetected) return;
            foreach (AimSample hand in input.AimSamples)
            {
                if (hand.PlayerIndex != 0) continue;
                Vector2 point = new Vector2(hand.Position.x * Screen.width, hand.Position.y * Screen.height);
                float size = Mathf.Max(34f, Screen.height * 0.045f);
                GUI.color = hand.HandId == hoverHand ? new Color(1f, 0.82f, 0.15f) : new Color(0.25f, 0.82f, 1f);
                GUI.DrawTexture(new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size), Texture2D.whiteTexture);
                if (hand.HandId == hoverHand)
                {
                    float required = DwellDuration;
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
            // Overlay coordinates are already scaled by GUI.matrix. Keep font
            // sizes in design pixels so text scales once with the artwork.
            pickerTitleStyle = NewStyle(25, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.95f, 0.83f));
            pickerTitleStyle.clipping = TextClipping.Clip;
            pickerSongTitleStyle = new GUIStyle(pickerTitleStyle) { wordWrap = false };
            pickerTagStyle = NewStyle(16, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.7f, 0.8f, 0.94f));
            pickerHeadingStyle = NewStyle(42, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.79f, 0.35f));
            pickerBodyStyle = NewStyle(26, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.88f, 0.91f, 0.97f));
            pickerBodyStyle.wordWrap = true;
            pickerHintStyle = NewStyle(20, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.72f, 0.82f, 0.93f));
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
