using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KinectKids3D
{
    public sealed class SpokjaktenGame : MonoBehaviour
    {
        private const float CountdownSeconds = 3f;
        private const float RideSeconds = 75f;
        private const float RideSpeed = 2.32f;
        private const float LockSeconds = 0.46f;
        private readonly List<GhostTarget> targets = new List<GhostTarget>();
        private readonly Dictionary<int, AimLock> aimLocks = new Dictionary<int, AimLock>();
        private readonly Dictionary<int, ReticleState> reticles = new Dictionary<int, ReticleState>();
        private readonly int[] scores = new int[2];
        private Camera rideCamera;
        private IAimProvider aimProvider;
        private float gameTime;
        private float nextSpawnAt;
        private bool bossSpawned;
        private bool finished;
        private bool paused;
        private string inputStatus;
        private Texture2D playerOneRing;
        private Texture2D playerTwoRing;
        private Texture2D whiteTexture;
        private Texture2D goldTexture;
        private GUIStyle titleStyle;
        private GUIStyle hudStyle;
        private GUIStyle smallStyle;
        private GUIStyle centerStyle;
        private AudioSource effects;
        private AudioClip hitSound;
        private AudioClip bossSound;

        private void Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            CreateCamera();
            new DarkRideWorld(transform).Build(rideCamera);
            CreateInput();
            CreateAudio();
            CreateHudTextures();
            ResetRide();
        }

        private void CreateCamera()
        {
            rideCamera = Camera.main;
            if (rideCamera == null)
            {
                GameObject cameraObject = new GameObject("Spökvagnens kamera");
                cameraObject.tag = "MainCamera";
                rideCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            rideCamera.clearFlags = CameraClearFlags.SolidColor;
            rideCamera.backgroundColor = new Color(0.008f, 0.012f, 0.030f);
            rideCamera.fieldOfView = 63f;
            rideCamera.nearClipPlane = 0.06f;
            rideCamera.farClipPlane = 95f;
            rideCamera.allowHDR = true;
        }

        private void CreateInput()
        {
            var kinect = new KinectV1AimProvider();
            if (kinect.TryStart())
            {
                aimProvider = kinect;
                inputStatus = kinect.Status;
            }
            else
            {
                inputStatus = kinect.Status + "  |  Musen styr siktet";
                kinect.Dispose();
                aimProvider = new MouseAimProvider();
            }
        }

        private void CreateAudio()
        {
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0;
            hitSound = CreateTone("Träff", 640f, 0.10f, 0.20f);
            bossSound = CreateTone("Bossträff", 185f, 0.22f, 0.28f);

            AudioSource ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = CreateAmbience();
            ambience.loop = true;
            ambience.volume = 0.20f;
            ambience.spatialBlend = 0;
            ambience.Play();
        }

        private void ResetRide()
        {
            foreach (GhostTarget target in targets.Where(item => item != null)) Destroy(target.gameObject);
            targets.Clear();
            aimLocks.Clear();
            reticles.Clear();
            scores[0] = scores[1] = 0;
            gameTime = 0;
            nextSpawnAt = 4.3f;
            bossSpawned = false;
            finished = false;
            paused = false;
            PositionCamera(0);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11)) Screen.fullScreen = !Screen.fullScreen;
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Space)) paused = !paused;
            if (finished && Input.GetKeyDown(KeyCode.R)) ResetRide();
            if (paused || finished) return;

            gameTime += Time.deltaTime;
            float rideTime = Mathf.Max(0, gameTime - CountdownSeconds);
            float distance = Mathf.Min(DarkRideWorld.TrackLength - 3f, rideTime * RideSpeed);
            PositionCamera(distance);
            if (gameTime < CountdownSeconds) return;

            UpdateTargets(rideTime, distance);
            UpdateAim();
            if (rideTime >= RideSeconds)
            {
                finished = true;
                reticles.Clear();
            }
        }

        private void PositionCamera(float z)
        {
            float x = DarkRideWorld.TrackCenter(z);
            float bounce = Mathf.Sin(Time.time * 4.4f) * 0.018f;
            Vector3 position = new Vector3(x, 1.72f + bounce, z);
            Vector3 look = new Vector3(DarkRideWorld.TrackCenter(z + 7f), 1.62f, z + 7f);
            rideCamera.transform.position = position;
            rideCamera.transform.rotation = Quaternion.Slerp(
                rideCamera.transform.rotation,
                Quaternion.LookRotation(look - position, Vector3.up),
                1f - Mathf.Exp(-7f * Time.deltaTime));
        }

        private void UpdateTargets(float rideTime, float distance)
        {
            targets.RemoveAll(item => item == null);
            foreach (GhostTarget target in targets.Where(item => item != null && item.transform.position.z < distance - 3f).ToArray())
            {
                targets.Remove(target);
                Destroy(target.gameObject);
            }

            if (rideTime >= nextSpawnAt && rideTime < 56f)
            {
                SpawnRegular(distance, rideTime);
                nextSpawnAt = rideTime + UnityEngine.Random.Range(2.4f, 3.5f);
            }

            if (!bossSpawned && rideTime >= 56f)
            {
                bossSpawned = true;
                float z = Mathf.Max(151f, distance + 24f);
                targets.Add(GhostTarget.Create(TargetKind.ConductorBoss,
                    new Vector3(DarkRideWorld.TrackCenter(z), 0.35f, z)));
            }
        }

        private void SpawnRegular(float distance, float rideTime)
        {
            float z = Mathf.Min(DarkRideWorld.TrackLength - 8f, distance + UnityEngine.Random.Range(18f, 27f));
            float lane = UnityEngine.Random.value < 0.25f
                ? UnityEngine.Random.Range(-1.1f, 1.1f)
                : UnityEngine.Random.Range(2.1f, 3.9f) * (UnityEngine.Random.value < 0.5f ? -1 : 1);
            TargetKind kind = rideTime < 23f || UnityEngine.Random.value < 0.42f ? TargetKind.Ghost : TargetKind.Zombie;
            float y = kind == TargetKind.Ghost ? UnityEngine.Random.Range(0.65f, 1.5f) : 0.28f;
            targets.Add(GhostTarget.Create(kind,
                new Vector3(DarkRideWorld.TrackCenter(z) + lane, y, z)));
        }

        private void UpdateAim()
        {
            IReadOnlyList<AimSample> samples = aimProvider.GetAimSamples();
            reticles.Clear();
            var seenHands = new HashSet<int>();
            foreach (AimSample sample in samples)
            {
                seenHands.Add(sample.HandId);
                Ray ray = rideCamera.ViewportPointToRay(new Vector3(sample.Position.x, 1f - sample.Position.y, 0));
                RaycastHit hit;
                GhostTarget target = null;
                if (Physics.Raycast(ray, out hit, 80f)) target = hit.collider.GetComponentInParent<GhostTarget>();

                AimLock aimLock;
                if (!aimLocks.TryGetValue(sample.HandId, out aimLock))
                {
                    aimLock = new AimLock();
                    aimLocks[sample.HandId] = aimLock;
                }

                float progress = 0;
                if (Time.time < aimLock.CooldownUntil)
                {
                    target = null;
                }
                else if (target == null)
                {
                    aimLock.Target = null;
                }
                else
                {
                    if (aimLock.Target != target)
                    {
                        aimLock.Target = target;
                        aimLock.StartedAt = Time.time;
                    }
                    progress = Mathf.Clamp01((Time.time - aimLock.StartedAt) / LockSeconds);
                    if (progress >= 1f)
                    {
                        int player = Mathf.Clamp(sample.PlayerIndex, 0, 1);
                        int points = target.Hit();
                        scores[player] += points;
                        effects.PlayOneShot(target.IsBoss ? bossSound : hitSound);
                        aimLock.Target = null;
                        aimLock.CooldownUntil = Time.time + 0.20f;
                        progress = 0;
                    }
                }

                reticles[sample.HandId] = new ReticleState
                {
                    PlayerIndex = sample.PlayerIndex,
                    Position = sample.Position,
                    Progress = progress,
                    OnTarget = target != null
                };
            }

            foreach (int handId in aimLocks.Keys.Where(id => !seenHands.Contains(id)).ToArray())
                aimLocks[handId].Target = null;
        }

        private void OnGUI()
        {
            EnsureStyles();
            float rideTime = Mathf.Max(0, gameTime - CountdownSeconds);
            float remaining = Mathf.Max(0, RideSeconds - rideTime);

            GUI.Box(new Rect(18, 16, 330, 78), string.Empty);
            GUI.Label(new Rect(34, 23, 300, 34), "SPÖKJAKTEN 3D", titleStyle);
            GUI.Label(new Rect(35, 58, 300, 26), inputStatus, smallStyle);

            GUI.Box(new Rect(Screen.width - 315, 16, 297, 78), string.Empty);
            GUI.Label(new Rect(Screen.width - 298, 23, 280, 32), "SPELARE 1   " + scores[0], hudStyle);
            GUI.Label(new Rect(Screen.width - 298, 57, 280, 26), "TID   " + Mathf.CeilToInt(remaining), smallStyle);
            if (reticles.Values.Any(item => item.PlayerIndex == 1))
                GUI.Label(new Rect(Screen.width - 298, 83, 280, 26), "SPELARE 2   " + scores[1], smallStyle);

            foreach (ReticleState reticle in reticles.Values)
            {
                float x = reticle.Position.x * Screen.width;
                float y = reticle.Position.y * Screen.height;
                Texture2D ring = reticle.PlayerIndex == 1 ? playerTwoRing : playerOneRing;
                Color previous = GUI.color;
                GUI.color = reticle.OnTarget ? Color.white : new Color(1, 1, 1, 0.72f);
                GUI.DrawTexture(new Rect(x - 34, y - 34, 68, 68), ring);
                GUI.color = previous;
                GUI.DrawTexture(new Rect(x - 34, y + 39, 68, 9), whiteTexture);
                GUI.DrawTexture(new Rect(x - 32, y + 41, 64 * reticle.Progress, 5), goldTexture);
            }

            GhostTarget boss = targets.FirstOrDefault(item => item != null && item.IsBoss);
            if (boss != null)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 190, Screen.height - 72, 380, 48), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 170, Screen.height - 66, 340, 24),
                    "ZOMBIE-KONDUKTÖREN", centerStyle);
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 155, Screen.height - 38, 310, 10), whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 153, Screen.height - 36,
                    306f * boss.Health / boss.MaxHealth, 6), goldTexture);
            }

            if (gameTime < CountdownSeconds)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 245, Screen.height * 0.5f - 92, 490, 184), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 220, Screen.height * 0.5f - 68, 440, 60),
                    Mathf.CeilToInt(CountdownSeconds - gameTime).ToString(), centerStyle);
                GUI.Label(new Rect(Screen.width * 0.5f - 220, Screen.height * 0.5f + 8, 440, 58),
                    "Håll handringen på ett lysande mål\ntills den gula mätaren fylls", centerStyle);
            }
            else if (rideTime < 9f)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 310, 112, 620, 48), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 295, 120, 590, 30),
                    "Vagnen kör själv – ni jagar spöken och zombies!", centerStyle);
            }

            if (paused)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 180, Screen.height * 0.5f - 60, 360, 120), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.5f - 36, 300, 72),
                    "PAUS\nMellanslag för att fortsätta", centerStyle);
            }
            else if (finished)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 255, Screen.height * 0.5f - 100, 510, 200), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 225, Screen.height * 0.5f - 76, 450, 150),
                    "BRA JAGAT!\nSpelare 1: " + scores[0]
                    + (scores[1] > 0 ? "   Spelare 2: " + scores[1] : string.Empty)
                    + "\nTryck R för en ny åktur", centerStyle);
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.76f, 0.91f, 1f) } };
            centerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                wordWrap = true
            };
        }

        private void CreateHudTextures()
        {
            playerOneRing = CreateRing(new Color(0.25f, 0.72f, 1f));
            playerTwoRing = CreateRing(new Color(1f, 0.35f, 0.62f));
            whiteTexture = SolidTexture(new Color(1, 1, 1, 0.88f));
            goldTexture = SolidTexture(new Color(1f, 0.72f, 0.08f));
        }

        private static Texture2D CreateRing(Color color)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                bool ring = distance > 23f && distance < 30f;
                bool cross = (Mathf.Abs(x - 31.5f) < 1.4f || Mathf.Abs(y - 31.5f) < 1.4f) && distance < 12f;
                texture.SetPixel(x, y, ring || cross ? color : Color.clear);
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D SolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - i / (float)length;
                samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateAmbience()
        {
            const int sampleRate = 22050;
            const int seconds = 4;
            float[] samples = new float[sampleRate * seconds];
            var random = new System.Random(731);
            float filteredNoise = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float noise = (float)(random.NextDouble() * 2 - 1);
                filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.015f);
                samples[i] = (Mathf.Sin(t * 43f * Mathf.PI * 2f) * 0.035f + filteredNoise * 0.06f);
            }
            AudioClip clip = AudioClip.Create("Spöktunnelns atmosfär", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (aimProvider != null) aimProvider.Dispose();
        }

        private sealed class AimLock
        {
            public GhostTarget Target;
            public float StartedAt;
            public float CooldownUntil;
        }

        private struct ReticleState
        {
            public int PlayerIndex;
            public Vector2 Position;
            public float Progress;
            public bool OnTarget;
        }
    }
}
