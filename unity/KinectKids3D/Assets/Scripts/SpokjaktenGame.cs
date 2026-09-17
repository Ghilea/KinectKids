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
        private const float TargetGraceSeconds = 0.48f;
        private readonly List<GhostTarget> targets = new List<GhostTarget>();
        private readonly List<RideHazard> hazards = new List<RideHazard>();
        private readonly Dictionary<int, AimLock> aimLocks = new Dictionary<int, AimLock>();
        private readonly Dictionary<int, ReticleState> reticles = new Dictionary<int, ReticleState>();
        private readonly Dictionary<long, PoseCalibration> poseCalibrations = new Dictionary<long, PoseCalibration>();
        private readonly Dictionary<int, PoseState> currentPoses = new Dictionary<int, PoseState>();
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
        private AudioClip castSound;
        private AudioClip movementSuccessSound;
        private AudioClip collisionSound;
        private AudioClip scareSound;
        private RideHazard currentHazard;
        private float cameraShakeUntil;
        private int nextScareIndex;
        private string actionMessage;
        private float actionMessageUntil;

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
            var kinect = new KinectBridgeAimProvider();
            if (kinect.TryStart())
            {
                aimProvider = kinect;
                inputStatus = kinect.Status + " – sikta, dra tillbaka och kasta";
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
            castSound = CreateNoiseBurst("Magikast", 0.12f, 0.16f, 1101);
            movementSuccessSound = CreateTone("Undanmanöver", 880f, 0.22f, 0.20f);
            collisionSound = CreateNoiseBurst("Krock", 0.34f, 0.26f, 9001);
            scareSound = CreateNoiseBurst("Överraskning", 0.25f, 0.11f, 4404);

            AudioSource ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = CreateAmbience();
            ambience.loop = true;
            ambience.volume = 0.20f;
            ambience.spatialBlend = 0;
            ambience.Play();

            AudioSource music = gameObject.AddComponent<AudioSource>();
            AudioClip licensedMusic = Resources.Load<AudioClip>("Audio/RideMusic");
            music.clip = licensedMusic != null ? licensedMusic : CreateRideMusic();
            music.loop = true;
            music.volume = licensedMusic != null ? 0.42f : 0.32f;
            music.spatialBlend = 0;
            music.Play();
        }

        private void ResetRide()
        {
            foreach (GhostTarget target in targets.Where(item => item != null)) Destroy(target.gameObject);
            foreach (RideHazard hazard in hazards.Where(item => item != null)) Destroy(hazard.gameObject);
            targets.Clear();
            hazards.Clear();
            aimLocks.Clear();
            reticles.Clear();
            poseCalibrations.Clear();
            currentPoses.Clear();
            scores[0] = scores[1] = 0;
            gameTime = 0;
            nextSpawnAt = 4.3f;
            bossSpawned = false;
            finished = false;
            paused = false;
            currentHazard = null;
            cameraShakeUntil = 0;
            nextScareIndex = 0;
            actionMessage = string.Empty;
            actionMessageUntil = 0;
            hazards.Add(RideHazard.Create(HazardKind.Duck, 43f));
            hazards.Add(RideHazard.Create(HazardKind.DodgeLeft, 78f));
            hazards.Add(RideHazard.Create(HazardKind.DodgeRight, 111f));
            hazards.Add(RideHazard.Create(HazardKind.Duck, 143f));
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
            UpdatePlayerPoses();
            PositionCamera(distance);
            if (gameTime < CountdownSeconds) return;

            UpdateTargets(rideTime, distance);
            UpdateHazards(distance);
            UpdateEnvironmentEvents(rideTime);
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
            if (Time.time < cameraShakeUntil)
            {
                x += UnityEngine.Random.Range(-0.12f, 0.12f);
                bounce += UnityEngine.Random.Range(-0.09f, 0.09f);
            }
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

        private void UpdatePlayerPoses()
        {
            currentPoses.Clear();
            foreach (PlayerPose pose in aimProvider.GetPlayerPoses())
            {
                PoseCalibration calibration;
                if (!poseCalibrations.TryGetValue(pose.TrackingId, out calibration))
                {
                    calibration = new PoseCalibration
                    {
                        CenterX = pose.CenterX,
                        StandingHeadY = pose.HeadY
                    };
                    poseCalibrations[pose.TrackingId] = calibration;
                }

                calibration.StandingHeadY = Mathf.Max(calibration.StandingHeadY, pose.HeadY);
                if (currentHazard == null)
                    calibration.CenterX = Mathf.Lerp(calibration.CenterX, pose.CenterX, 0.035f);

                currentPoses[pose.PlayerIndex] = new PoseState
                {
                    DuckAmount = calibration.StandingHeadY - pose.HeadY,
                    LeanAmount = pose.CenterX - calibration.CenterX
                };
            }
        }

        private void UpdateHazards(float distance)
        {
            currentHazard = null;
            foreach (RideHazard hazard in hazards.Where(item => item != null && !item.Resolved))
            {
                float gap = hazard.TrackZ - distance;
                if (gap <= 11f && gap >= -1.25f && (currentHazard == null || gap < currentHazard.TrackZ - distance))
                    currentHazard = hazard;

                if (gap <= 5.2f && gap >= -0.45f)
                {
                    foreach (KeyValuePair<int, PoseState> pair in currentPoses)
                    {
                        bool succeeds = hazard.Kind == HazardKind.Duck
                            ? pair.Value.DuckAmount >= 0.20f
                            : hazard.Kind == HazardKind.DodgeLeft
                                ? pair.Value.LeanAmount <= -0.17f
                                : pair.Value.LeanAmount >= 0.17f;
                        if (!succeeds || !hazard.MarkSuccess(pair.Key)) continue;
                        int player = Mathf.Clamp(pair.Key, 0, 1);
                        scores[player] += 40;
                        effects.PlayOneShot(movementSuccessSound);
                        actionMessage = "SNYGGT, SPELARE " + (player + 1) + "!  +40";
                        actionMessageUntil = Time.time + 1.25f;
                    }
                }

                if (gap >= -1.25f) continue;
                bool everyoneSucceeded = true;
                foreach (int player in currentPoses.Keys.ToArray())
                {
                    if (hazard.HasSucceeded(player)) continue;
                    everyoneSucceeded = false;
                    scores[Mathf.Clamp(player, 0, 1)] = Mathf.Max(0, scores[Mathf.Clamp(player, 0, 1)] - 25);
                }
                hazard.Resolved = true;
                hazard.ResolveVisual(everyoneSucceeded);
                if (!everyoneSucceeded)
                {
                    effects.PlayOneShot(collisionSound);
                    cameraShakeUntil = Time.time + 0.55f;
                    actionMessage = "OJ!  -25";
                    actionMessageUntil = Time.time + 1.1f;
                }
            }
        }

        private void UpdateEnvironmentEvents(float rideTime)
        {
            float[] scareTimes = { 11.5f, 27.5f, 44f, 62f };
            if (nextScareIndex >= scareTimes.Length || rideTime < scareTimes[nextScareIndex]) return;
            nextScareIndex++;
            effects.PlayOneShot(scareSound);
            cameraShakeUntil = Mathf.Max(cameraShakeUntil, Time.time + 0.16f);
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
                if (target != null)
                {
                    aimLock.RecentTarget = target;
                    aimLock.LastTargetAt = Time.time;
                }

                if (sample.Fire && Time.time >= aimLock.CooldownUntil)
                {
                    GhostTarget firedTarget = target;
                    if (firedTarget == null && Time.time - aimLock.LastTargetAt <= TargetGraceSeconds)
                        firedTarget = aimLock.RecentTarget;

                    int player = Mathf.Clamp(sample.PlayerIndex, 0, 1);
                    Color boltColor = player == 1 ? new Color(1f, 0.28f, 0.62f) : new Color(0.22f, 0.75f, 1f);
                    Vector3 boltStart = rideCamera.transform.position + rideCamera.transform.forward * 0.65f;
                    Vector3 boltEnd = firedTarget != null
                        ? firedTarget.transform.position + Vector3.up
                        : ray.GetPoint(18f);
                    MagicBolt.Launch(boltStart, boltEnd, boltColor);
                    effects.PlayOneShot(castSound);
                    if (firedTarget != null)
                    {
                        int points = firedTarget.Hit();
                        scores[player] += points;
                        effects.PlayOneShot(firedTarget.IsBoss ? bossSound : hitSound);
                        actionMessage = "+" + points;
                        actionMessageUntil = Time.time + 0.7f;
                    }
                    aimLock.RecentTarget = null;
                    aimLock.CooldownUntil = Time.time + 0.28f;
                }

                reticles[sample.HandId] = new ReticleState
                {
                    PlayerIndex = sample.PlayerIndex,
                    Position = sample.Position,
                    Progress = sample.GestureProgress,
                    OnTarget = target != null
                };
            }

            foreach (int handId in aimLocks.Keys.Where(id => !seenHands.Contains(id)).ToArray())
                aimLocks[handId].RecentTarget = null;
        }

        private void OnGUI()
        {
            EnsureStyles();
            float rideTime = Mathf.Max(0, gameTime - CountdownSeconds);
            float remaining = Mathf.Max(0, RideSeconds - rideTime);

            GUI.Box(new Rect(18, 16, 430, 82), string.Empty);
            GUI.Label(new Rect(34, 23, 400, 34), "SPÖKJAKTEN 3D", titleStyle);
            GUI.Label(new Rect(35, 58, Mathf.Max(400, Screen.width - 70), 42), inputStatus, smallStyle);

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

            if (currentHazard != null)
            {
                float gap = currentHazard.TrackZ - Mathf.Min(DarkRideWorld.TrackLength - 3f, rideTime * RideSpeed);
                float pulse = 1f + Mathf.Sin(Time.time * 9f) * 0.06f;
                float width = 470f * pulse;
                GUI.Box(new Rect(Screen.width * 0.5f - width * 0.5f, 104, width, 76), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 220, 114, 440, 52),
                    currentHazard.Instruction + "\n" + (gap > 5.2f ? "GÖR DIG REDO" : "NU!"), centerStyle);
            }

            if (Time.time < actionMessageUntil)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 190, Screen.height * 0.34f, 380, 54),
                    actionMessage, centerStyle);
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
                    "SIKTA PÅ ETT SPÖKE\nDra handen bakåt och kasta den framåt!", centerStyle);
            }
            else if (rideTime < 9f)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 330, 112, 660, 68), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 315, 118, 630, 54),
                    "Kasta magi på spökena – ducka och väj när något kommer mot vagnen!", centerStyle);
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

        private static AudioClip CreateRideMusic()
        {
            const int sampleRate = 22050;
            const int seconds = 16;
            float[] samples = new float[sampleRate * seconds];
            float[] melody = { 220f, 261.63f, 293.66f, 329.63f, 293.66f, 261.63f, 246.94f, 196f };
            float[] bass = { 55f, 65.41f, 49f, 55f };
            var random = new System.Random(1313);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                int melodyStep = Mathf.FloorToInt(t * 2f) % melody.Length;
                int bassStep = Mathf.FloorToInt(t / 4f) % bass.Length;
                float notePhase = (t * 2f) % 1f;
                float noteEnvelope = Mathf.Clamp01(1f - notePhase * 1.15f);
                float arpeggio = Mathf.Sin(t * melody[melodyStep] * Mathf.PI * 2f) * noteEnvelope * 0.055f;
                float bell = Mathf.Sin(t * melody[melodyStep] * 2f * Mathf.PI * 2f) * noteEnvelope * 0.014f;
                float lowDrone = Mathf.Sin(t * bass[bassStep] * Mathf.PI * 2f) * 0.052f;
                float beatPhase = (t * 2f) % 1f;
                float beat = beatPhase < 0.055f
                    ? ((float)random.NextDouble() * 2f - 1f) * (1f - beatPhase / 0.055f) * 0.055f
                    : 0f;
                float swell = Mathf.Sin(t * Mathf.PI / 4f) * 0.018f;
                samples[i] = Mathf.Clamp(arpeggio + bell + lowDrone + beat + swell, -0.35f, 0.35f);
            }
            AudioClip clip = AudioClip.Create("Spökjaktens musik", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateNoiseBurst(string clipName, float duration, float volume, int seed)
        {
            const int sampleRate = 22050;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(seed);
            float filtered = 0f;
            for (int i = 0; i < length; i++)
            {
                float envelope = Mathf.Pow(1f - i / (float)length, 2f);
                float noise = (float)random.NextDouble() * 2f - 1f;
                filtered = Mathf.Lerp(filtered, noise, 0.16f);
                samples[i] = filtered * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (aimProvider != null) aimProvider.Dispose();
        }

        private sealed class AimLock
        {
            public GhostTarget RecentTarget;
            public float LastTargetAt;
            public float CooldownUntil;
        }

        private sealed class PoseCalibration
        {
            public float CenterX;
            public float StandingHeadY;
        }

        private struct PoseState
        {
            public float DuckAmount;
            public float LeanAmount;
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
