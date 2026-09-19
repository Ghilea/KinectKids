using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public sealed class GreveGastGame : MonoBehaviour
    {
        private sealed class Obstacle
        {
            public GreveGastCue Cue;
            public GameObject Object;
            public float Speed;
        }

        private sealed class PendingAction
        {
            public GreveGastCue Cue;
            public bool Resolved;
        }

        private readonly List<Transform> corridor = new List<Transform>();
        private readonly List<Obstacle> obstacles = new List<Obstacle>();
        private readonly List<PendingAction> pending = new List<PendingAction>();
        private readonly float[] lastActions = new float[6];
        private GreveGastSongController song;
        private GreveGastBodyInput body;
        private Transform player;
        private Transform playerLeftArm;
        private Transform playerRightArm;
        private Transform playerLeftLeg;
        private Transform playerRightLeg;
        private Transform greve;
        private Light moon;
        private AudioSource effects;
        private AudioClip warningTone;
        private Material stone;
        private Material floor;
        private Material purple;
        private Material danger;
        private string warningSymbol;
        private float warningUntil;
        private float feedbackUntil;
        private string feedback;
        private Color feedbackColor;
        private float chase = 0.5f;
        private bool paused;
        private bool debug;
        private string section = "Intro";
        private GUIStyle hugeStyle;
        private GUIStyle titleStyle;
        private GUIStyle panelStyle;
        private const float CorridorSpeed = 7f;
        // Hindren borjar i forgrunden, mellan kameran och spelaren, och aker
        // fran nederkanten upp mot spelarfiguren.
        private const float ObstacleStartZ = 14.5f;
        private const float PlayerZ = 0f;
        private const float CameraZ = 18f;
        private const float LaneSpacing = 4.4f;

        private void Start()
        {
            name = "Greve Gasts Jakt";
            if (ApplyDefaultFullscreen()) Invoke(nameof(ForceFullscreen), 0.75f);
            BuildWorld();
            body = new GreveGastBodyInput();
            body.Start();
            song = gameObject.AddComponent<GreveGastSongController>();
            song.Warning += OnWarning;
            song.Cue += OnCue;
            song.SectionChanged += value => section = value.name;
            song.Begin();
        }

        private static bool ApplyDefaultFullscreen()
        {
            if (Application.isEditor) return false;
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], "-screen-fullscreen", StringComparison.OrdinalIgnoreCase)
                    && arguments[index + 1] == "0") return false;
            }
            Resolution resolution = Screen.currentResolution;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow);
            return true;
        }

        private void ForceFullscreen()
        {
            Resolution resolution = Screen.currentResolution;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow);
            Screen.fullScreen = true;
        }

        private void BuildWorld()
        {
            foreach (Camera oldCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Destroy(oldCamera.gameObject);
            RenderSettings.ambientLight = new Color(0.09f, 0.075f, 0.13f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.026f;
            RenderSettings.fogColor = new Color(0.025f, 0.018f, 0.055f);

            stone = MakeMaterial(new Color(0.105f, 0.09f, 0.15f));
            floor = MakeMaterial(new Color(0.17f, 0.105f, 0.075f));
            purple = MakeMaterial(new Color(0.42f, 0.08f, 0.63f), new Color(0.22f, 0.02f, 0.42f));
            danger = MakeMaterial(new Color(0.75f, 0.1f, 0.16f), new Color(0.45f, 0.02f, 0.04f));

            var cameraObject = new GameObject("Musikalisk foljkamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            // Bred jaktkamera: spelaren ar liten i bild och hinder syns langt
            // innan de kommer fram, som i musikjaktsreferensen.
            camera.transform.position = new Vector3(0f, 6f, CameraZ);
            camera.transform.LookAt(new Vector3(0f, 1.25f, PlayerZ), Vector3.up);
            camera.fieldOfView = 74f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;

            moon = cameraObject.AddComponent<Light>();
            moon.type = LightType.Spot;
            moon.range = 34f;
            moon.spotAngle = 66f;
            moon.intensity = 2.0f;
            moon.color = new Color(0.55f, 0.66f, 1f);

            for (int i = 0; i < 16; i++)
            {
                GameObject segment = new GameObject("Slottsdel " + i);
                segment.transform.position = new Vector3(0f, 0f, 12f - i * 12f);
                corridor.Add(segment.transform);
                for (int lane = -1; lane <= 1; lane++)
                {
                    float laneX = lane * LaneSpacing;
                    Cube("Vag " + lane, segment.transform, new Vector3(laneX, -0.25f, 0),
                        new Vector3(3.7f, 0.5f, 12f), floor);
                    Cube("Vagskarv " + lane, segment.transform, new Vector3(laneX, 0.015f, 0),
                        new Vector3(3.7f, 0.035f, 0.15f), stone);
                }
                Cube("Vanster vagg", segment.transform, new Vector3(-7.5f, 5.4f, 0), new Vector3(0.5f, 11.5f, 12f), stone);
                Cube("Hoger vagg", segment.transform, new Vector3(7.5f, 5.4f, 0), new Vector3(0.5f, 11.5f, 12f), stone);
                Cube("Takbjalk", segment.transform, new Vector3(0, 10.2f, -4.5f), new Vector3(15.5f, 0.5f, 0.6f), floor);
                AddTorch(segment.transform, -7.05f, i % 2 == 0 ? 3f : -3f);
                AddTorch(segment.transform, 7.05f, i % 2 == 0 ? -3f : 3f);
            }

            BuildPlayerCharacter();

            GameObject imported = ImportedModelFactory.Create("Models/CuteMonsters/Ghost", transform,
                "Greve Gast", new Vector3(1.35f, 1.55f, -8f), 2.7f,
                Quaternion.identity, "Fly", "Idle", "Walk");
            if (imported != null) greve = imported.transform;
            else
            {
                greve = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                greve.name = "Greve Gast";
                greve.position = new Vector3(1.35f, 1.55f, -8f);
                greve.GetComponent<Renderer>().material = purple;
            }

            effects = gameObject.AddComponent<AudioSource>();
            effects.spatialBlend = 0f;
            effects.volume = 0.45f;
            warningTone = CreateTone();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (Input.GetKeyDown(KeyCode.F11))
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.fullScreen = !Screen.fullScreen;
            }
            if (Input.GetKeyDown(KeyCode.F2)) debug = !debug;
            if (Input.GetKeyDown(KeyCode.F3) && song != null) song.Seek(58f);
            if (Input.GetKeyDown(KeyCode.F6) && song != null) song.TogglePause();
            if (Input.GetKeyDown(KeyCode.PageDown) && song != null)
            {
                GreveGastCue next = song.NextCue();
                if (next != null) song.Seek(Mathf.Max(0f, next.time - next.warning - 0.5f));
            }
            if (paused || body == null || song == null) return;

            body.Update();
            lastActions[(int)body.Current] = song.SongTime;
            AnimateCharacters();
            ScrollCorridor();
            UpdateObstacles();
            ResolveActions();
            moon.intensity = 1.75f + Mathf.Sin(song.SongTime * 2.3f) * 0.18f;
        }

        private void AnimateCharacters()
        {
            int lane = body.HorizontalDelta < -0.10f ? -1 : body.HorizontalDelta > 0.10f ? 1 : 0;
            // Kameran tittar tillbaka langs banan och speglar varlds-X.
            // Negativ kroppslutning ska fortfarande synas som vanster pa skarmen.
            float targetX = -lane * LaneSpacing;
            float runBob = body.Current == GreveGastAction.Run
                ? Mathf.Abs(Mathf.Sin(song.SongTime * 10f)) * 0.09f : 0f;
            float targetY = body.Current == GreveGastAction.Jump ? 0.9f
                : body.Current == GreveGastAction.Duck ? 0.08f : runBob;
            player.position = Vector3.Lerp(player.position, new Vector3(targetX, targetY, PlayerZ),
                1f - Mathf.Exp(-8f * Time.deltaTime));
            player.localScale = Vector3.Lerp(player.localScale,
                body.Current == GreveGastAction.Duck ? new Vector3(1.12f, 0.55f, 1.12f) : Vector3.one,
                1f - Mathf.Exp(-10f * Time.deltaTime));
            float stride = Mathf.Sin(song.SongTime * 10f) * 48f;
            playerLeftArm.localRotation = Quaternion.Euler(stride, 0f, 0f);
            playerRightArm.localRotation = Quaternion.Euler(-stride, 0f, 0f);
            playerLeftLeg.localRotation = Quaternion.Euler(-stride * 0.72f, 0f, 0f);
            playerRightLeg.localRotation = Quaternion.Euler(stride * 0.72f, 0f, 0f);

            // Spelaren springer mot kameran. Greve Gast jagar bakifran och far
            // aldrig lagga sig mellan kameran och spelarfiguren.
            float greveZ = Mathf.Lerp(-9f, -3.4f, chase);
            Vector3 target = new Vector3(1.25f + Mathf.Sin(song.SongTime * 2f) * 0.55f,
                1.55f + Mathf.Sin(song.SongTime * 3.1f) * 0.13f, greveZ);
            greve.position = Vector3.Lerp(greve.position, target, 1f - Mathf.Exp(-3f * Time.deltaTime));
        }

        private void ScrollCorridor()
        {
            float corridorLength = corridor.Count * 12f;
            for (int i = 0; i < corridor.Count; i++)
            {
                Transform segment = corridor[i];
                // Banmarkeringarna kommer fran nederkanten och flyttas upp mot
                // spelarens position. En ny del matas in fran forgrunden.
                segment.position += Vector3.back * (CorridorSpeed * Time.deltaTime);
                // Ateranvand delen tillrackligt langt bakom kameran sa att de
                // tre vagarna alltid fortsatter hela vagen ned till bildkanten.
                if (segment.position.z < PlayerZ - corridorLength + 24f)
                    segment.position += Vector3.forward * corridorLength;
            }
        }

        private void OnWarning(GreveGastCue cue)
        {
            GreveGastAction action = ParseAction(cue.action);
            if (action != GreveGastAction.None)
            {
                warningSymbol = Symbol(action);
                warningUntil = cue.time + cue.duration;
                effects.PlayOneShot(warningTone);
                SpawnObstacle(cue, action);
            }
            else if (cue.kind == "scare")
            {
                warningUntil = cue.time + 0.35f;
                warningSymbol = "!";
            }
        }

        private void OnCue(GreveGastCue cue)
        {
            GreveGastAction action = ParseAction(cue.action);
            if (action != GreveGastAction.None)
                pending.Add(new PendingAction { Cue = cue });
            if (cue.kind == "reveal" || cue.kind == "scare")
            {
                chase = Mathf.Clamp01(chase + 0.07f);
                feedback = "BU!";
                feedbackColor = new Color(0.9f, 0.35f, 1f);
                feedbackUntil = song.SongTime + 0.65f;
            }
        }

        private void ResolveActions()
        {
            float now = song.SongTime;
            foreach (PendingAction item in pending)
            {
                if (item.Resolved) continue;
                GreveGastAction expected = ParseAction(item.Cue.action);
                bool matched = body.Current == expected || lastActions[(int)expected] >= item.Cue.time - 0.42f;
                if (matched)
                {
                    item.Resolved = true;
                    chase = Mathf.Clamp01(chase - 0.11f);
                    feedback = "★";
                    feedbackColor = new Color(0.35f, 1f, 0.66f);
                    feedbackUntil = now + 0.75f;
                }
                else if (now > item.Cue.time + item.Cue.duration)
                {
                    item.Resolved = true;
                    chase = Mathf.Clamp01(chase + 0.12f);
                    feedback = "!";
                    feedbackColor = new Color(1f, 0.34f, 0.28f);
                    feedbackUntil = now + 0.75f;
                }
            }
        }

        private void SpawnObstacle(GreveGastCue cue, GreveGastAction action)
        {
            GameObject root = new GameObject("Hinder " + cue.id);
            if (action == GreveGastAction.Jump)
                Cube("Lagt hinder", root.transform, new Vector3(0, 0.35f, 0), new Vector3(13.8f, 0.7f, 0.7f), danger);
            else if (action == GreveGastAction.Duck)
                Cube("Hog bjalk", root.transform, new Vector3(0, 2.05f, 0), new Vector3(13.8f, 0.75f, 0.8f), danger);
            else if (action == GreveGastAction.Left)
                Cube("Blockering mitten och hoger", root.transform, new Vector3(-2.2f, 1.2f, 0), new Vector3(8.1f, 2.4f, 0.8f), danger);
            else if (action == GreveGastAction.Right)
                Cube("Blockering vanster och mitten", root.transform, new Vector3(2.2f, 1.2f, 0), new Vector3(8.1f, 2.4f, 0.8f), danger);
            else
                return;
            root.transform.position = new Vector3(0f, 0f, ObstacleStartZ);
            float remaining = Mathf.Max(0.5f, cue.time - song.SongTime);
            obstacles.Add(new Obstacle
            {
                Cue = cue,
                Object = root,
                Speed = (ObstacleStartZ - PlayerZ) / remaining
            });
        }

        private void UpdateObstacles()
        {
            float now = song.SongTime;
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                Obstacle obstacle = obstacles[i];
                if (obstacle.Object == null) { obstacles.RemoveAt(i); continue; }
                // Samma tydliga riktning som korridoren: fran fonden mot skarmen.
                obstacle.Object.transform.position += Vector3.back * (obstacle.Speed * Time.deltaTime);
                if (obstacle.Object.transform.position.z < PlayerZ - 10f
                    || now > obstacle.Cue.time + 2.5f)
                {
                    Destroy(obstacle.Object);
                    obstacles.RemoveAt(i);
                }
            }
        }

        private void TogglePause()
        {
            paused = !paused;
            if (song != null) song.TogglePause();
        }

        private void OnGUI()
        {
            EnsureStyles();
            float now = song != null ? song.SongTime : 0f;
            if (now < 3.0f)
                GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 100), "GREVE GASTS JAKT", titleStyle);
            if (now < 3.2f)
                GUI.Label(new Rect(0, Screen.height * 0.72f, Screen.width, 60), "Ror hela kroppen i takt med musiken", panelStyle);
            if (!string.IsNullOrEmpty(warningSymbol) && now <= warningUntil)
                GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 190), warningSymbol, hugeStyle);
            if (now <= feedbackUntil)
            {
                Color old = GUI.color;
                GUI.color = feedbackColor;
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 180), feedback, hugeStyle);
                GUI.color = old;
            }

            GUI.Box(new Rect(24, 22, 350, 38), GUIContent.none);
            Color previous = GUI.color;
            GUI.color = Color.Lerp(new Color(0.3f, 0.95f, 0.7f), new Color(0.95f, 0.18f, 0.3f), chase);
            GUI.DrawTexture(new Rect(28, 26, 342 * chase, 30), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(32, 25, 334, 30), "GREVE GAST KOMMER NARMARE", panelStyle);

            if (debug) DrawDebug(now);
            if (paused) DrawPause();
        }

        private void DrawDebug(float now)
        {
            GUI.Box(new Rect(20, 64, 380, 205), "TIDSLINJEDEBUG (F2)");
            GUI.Label(new Rect(35, 92, 350, 90),
                "Tid: " + now.ToString("0.00") + " / " + (song?.Source?.clip?.length ?? 0f).ToString("0.00") +
                "\nSektion: " + section + "\nKropp: " + body.Current +
                "  hojd " + body.HeightDelta.ToString("+0.00;-0.00") +
                "  sida " + body.HorizontalDelta.ToString("+0.00;-0.00") +
                "\n" + body.Status);
            if (GUI.Button(new Rect(35, 188, 82, 30), "Spela/paus")) song.TogglePause();
            if (GUI.Button(new Rect(123, 188, 72, 30), "-0.10")) song.Seek(now - 0.10f);
            if (GUI.Button(new Rect(201, 188, 72, 30), "+0.10")) song.Seek(now + 0.10f);
            if (GUI.Button(new Rect(279, 188, 102, 30), "Nasta cue"))
            {
                GreveGastCue next = song.NextCue();
                if (next != null) song.Seek(Mathf.Max(0, next.time - next.warning - 0.3f));
            }
            GUI.Label(new Rect(35, 226, 340, 35), "F3 = forsta refrangen  |  PgDn = nasta handelse");
        }

        private void DrawPause()
        {
            float w = 420f, h = 270f;
            Rect rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(rect, "PAUS");
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 60, 300, 48), "FORTSATT")) TogglePause();
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 120, 300, 48), "STARTA OM"))
            {
                song.Seek(0f);
                chase = 0.5f;
                TogglePause();
            }
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 180, 300, 48), "TILL GEMENSAM MENY"))
                LauncherReturnService.ReturnToLauncher();
        }

        private void AddTorch(Transform parent, float x, float z)
        {
            GameObject torch = Cube("Fackla", parent, new Vector3(x, 2.25f, z), new Vector3(0.12f, 0.8f, 0.12f), floor);
            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Laga";
            flame.transform.SetParent(torch.transform, false);
            flame.transform.localPosition = new Vector3(0, 0.65f, 0);
            flame.transform.localScale = new Vector3(2.2f, 0.6f, 2.2f);
            flame.GetComponent<Renderer>().material = MakeMaterial(new Color(1f, 0.38f, 0.04f), new Color(1f, 0.18f, 0.01f));
            Light light = flame.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 2.6f;
            light.color = new Color(1f, 0.38f, 0.12f);
        }

        private void BuildPlayerCharacter()
        {
            Material clothes = MakeMaterial(new Color(0.08f, 0.48f, 0.88f));
            Material skin = MakeMaterial(new Color(1f, 0.68f, 0.48f));
            Material dark = MakeMaterial(new Color(0.035f, 0.025f, 0.055f));
            Material shoes = MakeMaterial(new Color(0.12f, 0.07f, 0.055f));

            player = new GameObject("Spelaren - springer mot kameran").transform;
            player.position = new Vector3(0f, 0f, PlayerZ);
            player.rotation = Quaternion.Euler(0f, 180f, 0f);
            GameObject bodyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyObject.name = "Kropp";
            bodyObject.transform.SetParent(player, false);
            bodyObject.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            bodyObject.transform.localScale = new Vector3(0.62f, 0.68f, 0.44f);
            bodyObject.GetComponent<Renderer>().material = clothes;
            Destroy(bodyObject.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Huvud med ansikte mot kameran";
            head.transform.SetParent(player, false);
            head.transform.localPosition = new Vector3(0f, 2.08f, -0.02f);
            head.transform.localScale = new Vector3(0.84f, 0.9f, 0.78f);
            head.GetComponent<Renderer>().material = skin;
            Destroy(head.GetComponent<Collider>());
            FacePart("Vanster oga", head.transform, new Vector3(-0.19f, 0.10f, -0.47f), new Vector3(0.16f, 0.20f, 0.07f), dark);
            FacePart("Hoger oga", head.transform, new Vector3(0.19f, 0.10f, -0.47f), new Vector3(0.16f, 0.20f, 0.07f), dark);
            FacePart("Leende", head.transform, new Vector3(0f, -0.20f, -0.49f), new Vector3(0.31f, 0.07f, 0.06f), dark);

            playerLeftArm = Limb("Vanster arm", player, new Vector3(-0.56f, 1.45f, 0f), 0.72f, skin, false);
            playerRightArm = Limb("Hoger arm", player, new Vector3(0.56f, 1.45f, 0f), 0.72f, skin, false);
            playerLeftLeg = Limb("Vanster ben", player, new Vector3(-0.24f, 0.58f, 0f), 0.82f, shoes, true);
            playerRightLeg = Limb("Hoger ben", player, new Vector3(0.24f, 0.58f, 0f), 0.82f, shoes, true);
        }

        private static Transform Limb(string limbName, Transform parent, Vector3 joint, float length,
            Material material, bool leg)
        {
            Transform pivot = new GameObject(limbName + " led").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = joint;
            GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            limb.name = limbName;
            limb.transform.SetParent(pivot, false);
            limb.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            limb.transform.localScale = new Vector3(leg ? 0.24f : 0.18f, length, leg ? 0.30f : 0.20f);
            limb.GetComponent<Renderer>().material = material;
            Destroy(limb.GetComponent<Collider>());
            return pivot;
        }

        private static void FacePart(string partName, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = material;
            Destroy(part.GetComponent<Collider>());
        }

        private static GameObject Cube(string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().material = material;
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return cube;
        }

        private static Material MakeMaterial(Color color, Color? emission = null)
        {
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = color };
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            return material;
        }

        private static AudioClip CreateTone()
        {
            const int rate = 22050;
            float[] samples = new float[rate / 7];
            for (int i = 0; i < samples.Length; i++)
            {
                float envelope = 1f - i / (float)samples.Length;
                samples[i] = Mathf.Sin(i * Mathf.PI * 2f * 720f / rate) * envelope * 0.34f;
            }
            AudioClip clip = AudioClip.Create("Varningssignal", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static GreveGastAction ParseAction(string value)
        {
            switch (value)
            {
                case "run": return GreveGastAction.Run;
                case "jump": return GreveGastAction.Jump;
                case "duck": return GreveGastAction.Duck;
                case "left": return GreveGastAction.Left;
                case "right": return GreveGastAction.Right;
                default: return GreveGastAction.None;
            }
        }

        private static string Symbol(GreveGastAction action)
        {
            switch (action)
            {
                case GreveGastAction.Jump: return "↑";
                case GreveGastAction.Duck: return "↓";
                case GreveGastAction.Left: return "←";
                case GreveGastAction.Right: return "→";
                case GreveGastAction.Run: return "⚡";
                default: return string.Empty;
            }
        }

        private void EnsureStyles()
        {
            if (hugeStyle != null) return;
            hugeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 6, 72, 150),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.75f, 0.1f) }
            };
            titleStyle = new GUIStyle(hugeStyle) { fontSize = Mathf.Clamp(Screen.height / 13, 42, 78) };
            panelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 42, 16, 28),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private void OnDestroy()
        {
            if (body != null) body.Dispose();
        }
    }
}
