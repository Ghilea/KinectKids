using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KinectKids3D
{
    public sealed class SpokjaktenGame : MonoBehaviour
    {
        private const float CountdownSeconds = 3f;
        private const float RideSpeed = 2.18f;
        private const float BossStopDistance = 344f;
        private const float QuickEventCueDistance = 4.0f;
        private const float QuickEventActionDistance = 3.55f;
        private const float QuickEventPassedDistance = -0.30f;
        private const float TargetGraceSeconds = 0.48f;
        private const float DwellShotSeconds = 0.58f;
        private static readonly float[] ScareDistances =
        {
            26f, 57f, 91f, 124f, 144f, 165f, 184f, 218f, 242f, 270f, 299f, 326f
        };
        private readonly List<GhostTarget> targets = new List<GhostTarget>();
        private readonly List<RideHazard> hazards = new List<RideHazard>();
        private readonly Dictionary<int, AimLock> aimLocks = new Dictionary<int, AimLock>();
        private readonly Dictionary<int, ReticleState> reticles = new Dictionary<int, ReticleState>();
        private readonly Dictionary<int, ActiveHandState> activeHands = new Dictionary<int, ActiveHandState>();
        private readonly Dictionary<int, HandActivity> handActivity = new Dictionary<int, HandActivity>();
        private readonly Dictionary<long, PoseCalibration> poseCalibrations = new Dictionary<long, PoseCalibration>();
        private readonly Dictionary<int, PoseState> currentPoses = new Dictionary<int, PoseState>();
        private readonly int[] scores = new int[2];
        private Camera rideCamera;
        private IAimProvider aimProvider;
        private float gameTime;
        private float rideDistance;
        private float nextSpawnAt;
        private bool bossSpawned;
        private bool bossBattle;
        private bool bossDefeated;
        private bool finished;
        private bool paused;
        private string inputStatus;
        private Texture2D playerOneRing;
        private Texture2D playerTwoRing;
        private Texture2D whiteTexture;
        private Texture2D goldTexture;
        private Texture2D movementArrow;
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
        private AudioSource ghostVoice;
        private AudioClip[] ghostSounds;
        private float nextGhostSoundAt;
        private RideHazard currentHazard;
        private float cameraShakeUntil;
        private float cameraDuck;
        private float cameraLean;
        private int nextScareIndex;
        private string actionMessage;
        private float actionMessageUntil;
        private BossProjectile bossProjectile;
        private float nextBossAttackAt;
        private int selectedRoute;
        private bool routeChoiceActive;
        private int routeCandidate;
        private float routeCandidateSince;
        private bool routeHazardsAdded;

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
            rideCamera.backgroundColor = new Color(0.0015f, 0.0025f, 0.006f);
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
                inputStatus = kinect.Status + " – ett sikte, valfri hand";
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
            AudioClip licensedMusic = Resources.Load<AudioClip>("Audio/CustomRideMusic");
            if (licensedMusic == null) licensedMusic = Resources.Load<AudioClip>("Audio/RideMusic");
            music.clip = licensedMusic != null ? licensedMusic : CreateRideMusic();
            music.loop = true;
            music.volume = licensedMusic != null ? 0.42f : 0.32f;
            music.spatialBlend = 0;
            music.Play();

            ghostVoice = gameObject.AddComponent<AudioSource>();
            ghostVoice.playOnAwake = false;
            ghostVoice.spatialBlend = 0f;
            ghostSounds = new[]
            {
                CreateGhostVoice("Avlägset spöke", 1.42f, 154f, 6201),
                CreateGhostVoice("Viskning i muren", 1.08f, 212f, 7712),
                CreateGhostVoice("Klagande vålnad", 1.78f, 118f, 8839),
                CreateEvilLaugh("Elakt skratt", 1.65f, 9917),
                CreateEvilLaugh("Kort häxskratt", 1.18f, 4471)
            };
        }

        private void ResetRide()
        {
            foreach (GhostTarget target in targets.Where(item => item != null)) Destroy(target.gameObject);
            foreach (RideHazard hazard in hazards.Where(item => item != null)) Destroy(hazard.gameObject);
            targets.Clear();
            hazards.Clear();
            aimLocks.Clear();
            reticles.Clear();
            activeHands.Clear();
            handActivity.Clear();
            poseCalibrations.Clear();
            currentPoses.Clear();
            scores[0] = scores[1] = 0;
            gameTime = 0;
            rideDistance = 0;
            nextSpawnAt = 4.3f;
            bossSpawned = false;
            bossBattle = false;
            bossDefeated = false;
            finished = false;
            paused = false;
            currentHazard = null;
            cameraShakeUntil = 0;
            cameraDuck = 0f;
            cameraLean = 0f;
            nextScareIndex = 0;
            nextGhostSoundAt = Time.time + UnityEngine.Random.Range(4.5f, 7.5f);
            actionMessage = string.Empty;
            actionMessageUntil = 0;
            if (bossProjectile != null) Destroy(bossProjectile.gameObject);
            bossProjectile = null;
            nextBossAttackAt = 0;
            selectedRoute = 0;
            routeChoiceActive = false;
            routeCandidate = 0;
            routeCandidateSince = 0f;
            routeHazardsAdded = false;
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
            if (Input.GetKeyDown(KeyCode.B) && !bossDefeated)
            {
                rideDistance = BossStopDistance;
                bossBattle = true;
            }
            if (paused || finished) return;

            gameTime += Time.deltaTime;
            float rideTime = Mathf.Max(0, gameTime - CountdownSeconds);
            UpdatePlayerPoses();
            if (gameTime < CountdownSeconds)
            {
                PositionCamera(0);
                return;
            }

            if (!bossBattle)
                rideDistance = Mathf.Min(DarkRideWorld.TrackLength - 3f, rideDistance + RideSpeed * Time.deltaTime);
            UpdateRouteChoice();
            if (!bossDefeated && rideDistance >= BossStopDistance)
            {
                rideDistance = BossStopDistance;
                bossBattle = true;
            }

            PositionCamera(rideDistance);
            UpdateTargets(rideTime, rideDistance);
            UpdateHazards(rideDistance);
            UpdateEnvironmentEvents();
            UpdateGhostAudio();
            UpdateAim();
            UpdateBossBattle();
            if (bossDefeated && rideDistance >= DarkRideWorld.TrackLength - 3f)
            {
                finished = true;
                reticles.Clear();
            }
        }

        private void PositionCamera(float z)
        {
            float x = DarkRideWorld.TrackCenter(z, selectedRoute);
            float bounce = Mathf.Sin(Time.time * 4.4f) * 0.018f;
            float targetDuck = 0f;
            float targetLean = 0f;
            foreach (PoseState pose in currentPoses.Values)
            {
                targetDuck = Mathf.Max(targetDuck, Mathf.Clamp01(pose.DuckAmount / 0.34f));
                float lean = Mathf.Clamp(pose.LeanAmount / 0.34f, -1f, 1f);
                if (Mathf.Abs(lean) > Mathf.Abs(targetLean)) targetLean = lean;
            }
            cameraDuck = Mathf.Lerp(cameraDuck, targetDuck, 1f - Mathf.Exp(-9f * Time.deltaTime));
            cameraLean = Mathf.Lerp(cameraLean, targetLean, 1f - Mathf.Exp(-8f * Time.deltaTime));
            x += cameraLean * 0.92f;
            if (Time.time < cameraShakeUntil)
            {
                x += UnityEngine.Random.Range(-0.12f, 0.12f);
                bounce += UnityEngine.Random.Range(-0.09f, 0.09f);
            }
            Vector3 position = new Vector3(x, 1.72f - cameraDuck * 0.68f + bounce, z);
            Vector3 look = new Vector3(DarkRideWorld.TrackCenter(z + 7f, selectedRoute) + cameraLean * 0.38f,
                1.62f - cameraDuck * 0.32f, z + 7f);
            Quaternion bodyMotion = Quaternion.LookRotation(look - position, Vector3.up)
                * Quaternion.Euler(cameraDuck * 3f, 0f, -cameraLean * 7.5f);
            rideCamera.transform.position = position;
            rideCamera.transform.rotation = Quaternion.Slerp(
                rideCamera.transform.rotation,
                bodyMotion,
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

            if (rideTime >= nextSpawnAt && distance < BossStopDistance - 13f && !routeChoiceActive)
            {
                SpawnRegular(distance, rideTime);
                nextSpawnAt = rideTime + UnityEngine.Random.Range(2.4f, 3.5f);
            }

            if (!bossSpawned && distance >= BossStopDistance)
            {
                bossSpawned = true;
                float z = BossStopDistance + 16f;
                targets.Add(GhostTarget.Create(TargetKind.ConductorBoss,
                    new Vector3(DarkRideWorld.TrackCenter(z, selectedRoute), 0.35f, z)));
                nextBossAttackAt = Time.time + 2.2f;
                actionMessage = "VAGNEN STANNAR – BESEGRA KONDUKTÖREN!";
                actionMessageUntil = Time.time + 2.4f;
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
                new Vector3(DarkRideWorld.TrackCenter(z, selectedRoute) + lane, y, z)));
        }

        private void UpdateRouteChoice()
        {
            if (selectedRoute != 0 || rideDistance < DarkRideWorld.BranchChoiceStart) return;
            routeChoiceActive = true;

            int candidate = 0;
            PoseState playerOne;
            if (currentPoses.TryGetValue(0, out playerOne))
            {
                if (playerOne.LeanAmount <= -0.13f) candidate = -1;
                else if (playerOne.LeanAmount >= 0.13f) candidate = 1;
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) candidate = -1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) candidate = 1;
            if (Input.GetMouseButtonDown(0))
            {
                SelectRoute(Input.mousePosition.x < Screen.width * 0.5f ? -1 : 1);
                return;
            }

            if (candidate != routeCandidate)
            {
                routeCandidate = candidate;
                routeCandidateSince = Time.time;
            }
            else if (candidate != 0 && Time.time - routeCandidateSince >= 0.32f)
            {
                SelectRoute(candidate);
                return;
            }

            if (rideDistance < DarkRideWorld.BranchSplitStart - 1.2f) return;
            ReticleState reticle;
            int automaticRoute = reticles.TryGetValue(0, out reticle)
                ? (reticle.Position.x < 0.5f ? -1 : 1)
                : (UnityEngine.Random.value < 0.5f ? -1 : 1);
            SelectRoute(automaticRoute);
        }

        private void SelectRoute(int route)
        {
            selectedRoute = route < 0 ? -1 : 1;
            routeChoiceActive = false;
            actionMessage = selectedRoute < 0 ? "VÄNSTRA HEMLIGA GÅNGEN!" : "HÖGRA HEMLIGA GÅNGEN!";
            actionMessageUntil = Time.time + 2.1f;
            effects.PlayOneShot(movementSuccessSound);

            if (routeHazardsAdded) return;
            routeHazardsAdded = true;
            if (selectedRoute < 0)
            {
                hazards.Add(RideHazard.Create(HazardKind.DodgeRight, 238f, selectedRoute));
                hazards.Add(RideHazard.Create(HazardKind.Duck, 272f, selectedRoute));
                hazards.Add(RideHazard.Create(HazardKind.DodgeLeft, 299f, selectedRoute));
            }
            else
            {
                hazards.Add(RideHazard.Create(HazardKind.Duck, 235f, selectedRoute));
                hazards.Add(RideHazard.Create(HazardKind.DodgeLeft, 266f, selectedRoute));
                hazards.Add(RideHazard.Create(HazardKind.DodgeRight, 296f, selectedRoute));
            }
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
                if (gap <= QuickEventCueDistance && gap >= QuickEventPassedDistance
                    && (currentHazard == null || gap < currentHazard.TrackZ - distance))
                    currentHazard = hazard;

                if (gap <= QuickEventActionDistance && gap >= QuickEventPassedDistance)
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

                if (gap >= QuickEventPassedDistance) continue;
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

        private void UpdateEnvironmentEvents()
        {
            if (nextScareIndex >= ScareDistances.Length || rideDistance < ScareDistances[nextScareIndex]) return;
            int side = UnityEngine.Random.value < 0.5f ? -1 : 1;
            nextScareIndex++;
            SideScare.Create(rideCamera.transform, side);
            effects.PlayOneShot(scareSound);
            PlayGhostSound(side, 0.82f);
            cameraShakeUntil = Mathf.Max(cameraShakeUntil, Time.time + 0.22f);
        }

        private void UpdateGhostAudio()
        {
            if (Time.time < nextGhostSoundAt || ghostVoice == null || ghostVoice.isPlaying) return;
            PlayGhostSound(UnityEngine.Random.value < 0.5f ? -1 : 1,
                UnityEngine.Random.Range(0.28f, 0.48f));
            nextGhostSoundAt = Time.time + UnityEngine.Random.Range(5.5f, 11.5f);
        }

        private void PlayGhostSound(int side, float volume)
        {
            if (ghostVoice == null || ghostSounds == null || ghostSounds.Length == 0) return;
            ghostVoice.clip = ghostSounds[UnityEngine.Random.Range(0, ghostSounds.Length)];
            ghostVoice.panStereo = side < 0 ? -0.72f : 0.72f;
            ghostVoice.pitch = UnityEngine.Random.Range(0.86f, 1.08f);
            ghostVoice.volume = volume;
            ghostVoice.Play();
        }

        private void UpdateAim()
        {
            IReadOnlyList<AimSample> samples = aimProvider.GetAimSamples();
            reticles.Clear();
            foreach (IGrouping<int, AimSample> group in samples.GroupBy(item => item.PlayerIndex))
            {
                int player = Mathf.Clamp(group.Key, 0, 1);
                List<AimSample> playerHands = group.ToList();
                foreach (AimSample hand in playerHands) UpdateHandActivity(hand);
                AimSample sample = SelectActiveHand(player, playerHands);

                Ray ray = rideCamera.ViewportPointToRay(new Vector3(sample.Position.x, 1f - sample.Position.y, 0));
                RaycastHit hit;
                GhostTarget target = null;
                if (Physics.Raycast(ray, out hit, 80f)) target = hit.collider.GetComponentInParent<GhostTarget>();

                AimLock aimLock;
                if (!aimLocks.TryGetValue(player, out aimLock))
                {
                    aimLock = new AimLock();
                    aimLocks[player] = aimLock;
                }
                if (target != null)
                {
                    aimLock.RecentTarget = target;
                    aimLock.LastTargetAt = Time.time;
                }

                if (target != null && target == aimLock.ChargeTarget)
                    aimLock.Charge += Time.deltaTime;
                else
                {
                    aimLock.ChargeTarget = target;
                    aimLock.Charge = 0f;
                }

                bool chargedShot = target != null && aimLock.Charge >= DwellShotSeconds;
                if ((sample.Fire || chargedShot) && Time.time >= aimLock.CooldownUntil)
                {
                    GhostTarget firedTarget = target;
                    if (firedTarget == null && Time.time - aimLock.LastTargetAt <= TargetGraceSeconds)
                        firedTarget = aimLock.RecentTarget;

                    Color boltColor = player == 1 ? new Color(1f, 0.28f, 0.62f) : new Color(0.22f, 0.75f, 1f);
                    Vector3 boltStart = rideCamera.transform.position + rideCamera.transform.forward * 0.65f;
                    Vector3 boltEnd = firedTarget != null
                        ? firedTarget.transform.position + Vector3.up
                        : ray.GetPoint(18f);
                    MagicBolt.Launch(boltStart, boltEnd, boltColor);
                    effects.PlayOneShot(castSound);
                    if (firedTarget != null)
                    {
                        bool wasBoss = firedTarget.IsBoss;
                        int points = firedTarget.Hit();
                        scores[player] += points;
                        effects.PlayOneShot(wasBoss ? bossSound : hitSound);
                        actionMessage = "+" + points;
                        actionMessageUntil = Time.time + 0.7f;
                        if (wasBoss && firedTarget.Health <= 0)
                        {
                            bossDefeated = true;
                            bossBattle = false;
                            if (bossProjectile != null) Destroy(bossProjectile.gameObject);
                            bossProjectile = null;
                            actionMessage = "KONDUKTÖREN ÄR BESEGRAD – VAGNEN KÖR VIDARE!";
                            actionMessageUntil = Time.time + 2.8f;
                        }
                    }
                    aimLock.RecentTarget = null;
                    aimLock.ChargeTarget = null;
                    aimLock.Charge = 0f;
                    aimLock.CooldownUntil = Time.time + 0.32f;
                }

                reticles[player] = new ReticleState
                {
                    PlayerIndex = player,
                    Position = sample.Position,
                    Progress = Mathf.Max(sample.GestureProgress, aimLock.Charge / DwellShotSeconds),
                    OnTarget = target != null,
                    IsRightHand = sample.HandId % 2 == 1
                };
            }
        }

        private void UpdateHandActivity(AimSample sample)
        {
            HandActivity activity;
            if (!handActivity.TryGetValue(sample.HandId, out activity))
            {
                activity = new HandActivity { LastPosition = sample.Position, LastAt = Time.time };
                handActivity[sample.HandId] = activity;
                return;
            }

            float dt = Mathf.Max(0.016f, Time.time - activity.LastAt);
            float movement = Vector2.Distance(sample.Position, activity.LastPosition) / dt;
            activity.Score = Mathf.Lerp(activity.Score, Mathf.Clamp01(movement * 0.65f), 0.24f);
            activity.LastPosition = sample.Position;
            activity.LastAt = Time.time;
        }

        private AimSample SelectActiveHand(int player, List<AimSample> hands)
        {
            ActiveHandState state;
            if (!activeHands.TryGetValue(player, out state))
            {
                AimSample preferred = hands.FirstOrDefault(item => item.HandId % 2 == 1);
                if (!hands.Any(item => item.HandId == preferred.HandId)) preferred = hands[0];
                state = new ActiveHandState { ActiveHandId = preferred.HandId };
                activeHands[player] = state;
            }

            AimSample active = hands.FirstOrDefault(item => item.HandId == state.ActiveHandId);
            if (!hands.Any(item => item.HandId == state.ActiveHandId))
            {
                active = hands[0];
                state.ActiveHandId = active.HandId;
            }

            AimSample challenger = hands
                .Where(item => item.HandId != active.HandId)
                .OrderByDescending(item => ActivityScore(item))
                .FirstOrDefault();
            bool hasChallenger = hands.Any(item => item.HandId == challenger.HandId && item.HandId != active.HandId);
            bool wantsSwitch = hasChallenger
                && (ActivityScore(challenger) > ActivityScore(active) + 0.14f
                    || challenger.GestureProgress > active.GestureProgress + 0.16f);
            if (wantsSwitch)
            {
                if (state.CandidateHandId != challenger.HandId)
                {
                    state.CandidateHandId = challenger.HandId;
                    state.CandidateSince = Time.time;
                }
                else if (Time.time - state.CandidateSince >= 0.55f)
                {
                    state.ActiveHandId = challenger.HandId;
                    state.CandidateHandId = -1;
                    active = challenger;
                }
            }
            else
            {
                state.CandidateHandId = -1;
            }

            return active;
        }

        private float ActivityScore(AimSample sample)
        {
            HandActivity activity;
            float movement = handActivity.TryGetValue(sample.HandId, out activity) ? activity.Score : 0f;
            return movement + sample.GestureProgress * 0.72f + (sample.Fire ? 1f : 0f);
        }

        private void UpdateBossBattle()
        {
            if (!bossBattle || bossDefeated) return;
            GhostTarget boss = targets.FirstOrDefault(item => item != null && item.IsBoss && item.Health > 0);
            if (boss == null) return;

            if (bossProjectile != null)
            {
                if (bossProjectile.Arrived) ResolveBossAttack();
                return;
            }

            if (Time.time < nextBossAttackAt) return;
            HazardKind kind = (HazardKind)UnityEngine.Random.Range(0, 3);
            Vector3 start = boss.transform.position + new Vector3(0f, 2.15f, -0.6f);
            Vector3 end = rideCamera.transform.position + rideCamera.transform.forward * 1.15f;
            bossProjectile = BossProjectile.Create(kind, start, end);
            nextBossAttackAt = Time.time + UnityEngine.Random.Range(3.0f, 4.0f);
            effects.PlayOneShot(scareSound);
        }

        private void ResolveBossAttack()
        {
            bool anyoneFailed = false;
            foreach (KeyValuePair<int, PoseState> pair in currentPoses)
            {
                bool succeeds = bossProjectile.Kind == HazardKind.Duck
                    ? pair.Value.DuckAmount >= 0.13f
                    : bossProjectile.Kind == HazardKind.DodgeLeft
                        ? pair.Value.LeanAmount <= -0.12f
                        : pair.Value.LeanAmount >= 0.12f;
                int player = Mathf.Clamp(pair.Key, 0, 1);
                if (succeeds)
                {
                    scores[player] += 35;
                    effects.PlayOneShot(movementSuccessSound);
                }
                else
                {
                    anyoneFailed = true;
                    scores[player] = Mathf.Max(0, scores[player] - 30);
                }
            }

            if (anyoneFailed || currentPoses.Count == 0)
            {
                effects.PlayOneShot(collisionSound);
                cameraShakeUntil = Time.time + 0.55f;
                actionMessage = "BOSSEN TRÄFFADE!  -30";
            }
            else
            {
                actionMessage = "SNYGGT UNDAN!  +35";
            }
            actionMessageUntil = Time.time + 1.2f;
            Destroy(bossProjectile.gameObject);
            bossProjectile = null;
        }

        private void OnGUI()
        {
            EnsureStyles();
            float rideTime = Mathf.Max(0, gameTime - CountdownSeconds);

            GUI.Box(new Rect(18, 16, 430, 82), string.Empty);
            GUI.Label(new Rect(34, 23, 400, 34), "SPÖKJAKTEN 3D", titleStyle);
            GUI.Label(new Rect(35, 58, Mathf.Max(400, Screen.width - 70), 42), inputStatus, smallStyle);

            GUI.Box(new Rect(Screen.width - 315, 16, 297, 78), string.Empty);
            GUI.Label(new Rect(Screen.width - 298, 23, 280, 32), "SPELARE 1   " + scores[0], hudStyle);
            GUI.Label(new Rect(Screen.width - 298, 57, 280, 26), bossBattle
                ? "SLUTBOSS – VAGNEN STÅR STILL"
                : "FÄRD   " + Mathf.RoundToInt(rideDistance / DarkRideWorld.TrackLength * 100f) + " %", smallStyle);
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
                GUI.Label(new Rect(x - 65, y + 51, 130, 22),
                    reticle.IsRightHand ? "HÖGER HAND" : "VÄNSTER HAND", smallStyle);
            }

            if (currentHazard != null)
            {
                float gap = currentHazard.TrackZ - rideDistance;
                DrawMovementCue(currentHazard.Kind, Mathf.InverseLerp(QuickEventCueDistance, 0f, gap));
            }

            if (bossProjectile != null && bossProjectile.Progress >= 0.32f)
            {
                DrawMovementCue(bossProjectile.Kind, bossProjectile.Progress);
            }

            if (routeChoiceActive) DrawRouteChoice();

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
                    "ETT SIKTE FÖLJER DIN AKTIVA HAND\nHåll på spöket eller knuffa handen lätt framåt!", centerStyle);
            }
            else if (rideTime < 9f)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 330, 112, 660, 68), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 315, 118, 630, 54),
                    "Håll siktet kort på spökena – ducka och väj när något kommer!", centerStyle);
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
            movementArrow = CreateArrowTexture();
        }

        private void DrawMovementCue(HazardKind kind, float urgency)
        {
            if (movementArrow == null) return;
            float rotation = kind == HazardKind.Duck ? 90f : kind == HazardKind.DodgeLeft ? 180f : 0f;
            Vector2 direction = kind == HazardKind.Duck ? Vector2.down
                : kind == HazardKind.DodgeLeft ? Vector2.left : Vector2.right;
            float wave = Mathf.Repeat(Time.time * 2.8f, 1f);
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            for (int i = 0; i < 3; i++)
            {
                float phase = Mathf.Repeat(wave + i * 0.24f, 1f);
                float size = Mathf.Lerp(76f, 112f, urgency) * (0.90f + phase * 0.10f);
                Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.25f)
                    + direction * (phase * 66f - 25f);
                GUI.color = new Color(1f, Mathf.Lerp(0.82f, 0.28f, urgency), 0.08f,
                    Mathf.Sin(phase * Mathf.PI) * 0.82f + 0.12f);
                GUIUtility.RotateAroundPivot(rotation, center);
                GUI.DrawTexture(new Rect(center.x - size * 0.5f, center.y - size * 0.35f,
                    size, size * 0.70f), movementArrow);
                GUI.matrix = oldMatrix;
            }
            GUI.color = oldColor;
            GUI.matrix = oldMatrix;
        }

        private void DrawRouteChoice()
        {
            float pulse = 0.65f + Mathf.Sin(Time.time * 5f) * 0.12f;
            GUI.Box(new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.14f, 300f, 50f), string.Empty);
            GUI.Label(new Rect(Screen.width * 0.5f - 140f, Screen.height * 0.145f, 280f, 40f),
                "VÄLJ VÄG", centerStyle);
            DrawRouteArrow(new Vector2(Screen.width * 0.27f, Screen.height * 0.35f), 180f, pulse,
                new Color(0.18f, 1f, 0.52f));
            DrawRouteArrow(new Vector2(Screen.width * 0.73f, Screen.height * 0.35f), 0f, pulse,
                new Color(0.74f, 0.22f, 1f));
        }

        private void DrawRouteArrow(Vector2 center, float rotation, float pulse, Color color)
        {
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(rotation, center);
            float width = 150f * pulse;
            GUI.DrawTexture(new Rect(center.x - width * 0.5f, center.y - 44f, width, 88f), movementArrow);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private static Texture2D CreateArrowTexture()
        {
            const int width = 96;
            const int height = 64;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool shaft = x >= 8 && x <= 57 && y >= 23 && y <= 40;
                int arrowX = x - 50;
                bool head = arrowX >= 0 && arrowX <= 40
                    && Mathf.Abs(y - 31) <= (40 - arrowX) * 0.72f;
                texture.SetPixel(x, y, shaft || head ? Color.white : Color.clear);
            }
            texture.Apply();
            return texture;
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

        private static AudioClip CreateGhostVoice(string clipName, float duration, float baseFrequency, int seed)
        {
            const int sampleRate = 22050;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(seed);
            float breath = 0f;
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = i / (float)length;
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                float wobble = Mathf.Sin(t * 3.1f + seed) * 18f + Mathf.Sin(t * 7.7f) * 6f;
                phase += (baseFrequency + wobble) / sampleRate * Mathf.PI * 2f;
                float noise = (float)random.NextDouble() * 2f - 1f;
                breath = Mathf.Lerp(breath, noise, 0.035f);
                float voice = Mathf.Sin(phase) * 0.55f
                    + Mathf.Sin(phase * 0.503f) * 0.24f
                    + breath * 0.34f;
                samples[i] = voice * envelope * (0.11f + Mathf.Sin(t * 5.2f) * 0.025f);
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateEvilLaugh(string clipName, float duration, int seed)
        {
            const int sampleRate = 22050;
            int length = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[length];
            var random = new System.Random(seed);
            float phase = 0f;
            float rasp = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = i / (float)length;
                float syllable = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 5.4f * Mathf.PI)), 1.7f);
                float envelope = Mathf.Sin(normalized * Mathf.PI) * (0.22f + syllable * 0.78f);
                float pitch = 132f + Mathf.Sin(t * 4.6f) * 31f + syllable * 42f;
                phase += pitch / sampleRate * Mathf.PI * 2f;
                float noise = (float)random.NextDouble() * 2f - 1f;
                rasp = Mathf.Lerp(rasp, noise, 0.08f);
                samples[i] = (Mathf.Sin(phase) * 0.12f + Mathf.Sin(phase * 0.51f) * 0.06f
                    + rasp * 0.025f) * envelope;
            }
            AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
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
            public GhostTarget ChargeTarget;
            public float LastTargetAt;
            public float CooldownUntil;
            public float Charge;
        }

        private sealed class ActiveHandState
        {
            public int ActiveHandId;
            public int CandidateHandId = -1;
            public float CandidateSince;
        }

        private sealed class HandActivity
        {
            public Vector2 LastPosition;
            public float LastAt;
            public float Score;
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
            public bool IsRightHand;
        }
    }
}
