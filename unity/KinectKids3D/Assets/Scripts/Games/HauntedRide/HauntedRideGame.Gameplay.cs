using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D
{
    public sealed partial class HauntedRideGame
    {
        private void UpdateTargets(float rideTime, float distance)
        {
            targets.RemoveAll(item => item == null);
            foreach (GhostTarget target in targets.Where(item => item != null && item.transform.position.z < distance - 3f).ToArray())
            {
                targets.Remove(target);
                if (target.Health > 0 && target.GetComponent<CursedRelic>() == null
                    && target.GetComponent<BreakableProp>() == null
                    && target.GetComponent<HauntedIllusion>() == null
                    && target.GetComponent<SecretRouteSeal>() == null
                    && target.GetComponent<RepairSigil>() == null && !target.IsBoss)
                    DamageWagon(target.GetComponent<MiniBossTarget>() != null ? 2 : 1,
                        "ETT MONSTER NÅDDE VAGNEN!");
                Destroy(target.gameObject);
            }

            if (rideTime >= nextSpawnAt && distance < BossStopDistance - 13f && !routeChoiceActive)
            {
                SpawnRegular(distance, rideTime);
                float performance = Mathf.Clamp01((Mathf.Max(combos[0], combos[1]) - 3f) / 12f);
                float dangerRelief = wagonHealth <= 2 ? 1.15f : wagonHealth <= 3 ? 0.55f : 0f;
                float minimumDelay = easyMode ? 3.0f : Mathf.Lerp(2.65f, 2.05f, performance);
                float maximumDelay = easyMode ? 4.15f : Mathf.Lerp(3.65f, 2.85f, performance);
                nextSpawnAt = rideTime + UnityEngine.Random.Range(minimumDelay, maximumDelay) + dangerRelief;
            }

            if (nextMiniBossIndex < MiniBossDistances.Length
                && distance >= MiniBossDistances[nextMiniBossIndex])
            {
                SpawnMiniBoss(nextMiniBossIndex, distance);
                nextMiniBossIndex++;
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
            GhostTarget spawned = GhostTarget.Create(kind,
                new Vector3(DarkRideWorld.TrackCenter(z, selectedRoute) + lane, y, z));
            if (kind == TargetKind.Ghost && UnityEngine.Random.value < 0.16f)
            {
                spawned.gameObject.AddComponent<HauntedIllusion>();
                spawned.name = "Falsk spökillusion";
            }
            MonsterTelegraph.Attach(spawned, easyMode ? 17.5f : 15.5f, lane < 0f ? -1 : 1);
            targets.Add(spawned);
        }

        private void SpawnMiniBoss(int index, float distance)
        {
            MiniBossKind kind = (MiniBossKind)(index % 3);
            float z = Mathf.Min(BossStopDistance - 5f, distance + 18f);
            GhostTarget target = MiniBossTarget.Create(kind,
                new Vector3(DarkRideWorld.TrackCenter(z, selectedRoute), 0.25f, z));
            MonsterTelegraph.Attach(target, easyMode ? 20f : 18f, index % 2 == 0 ? -1 : 1);
            targets.Add(target);
            string name = kind == MiniBossKind.GiantSpider ? "JÄTTESPINDELN"
                : kind == MiniBossKind.Vampire ? "VAMPYREN" : "DEN BESATTA RUSTNINGEN";
            actionMessage = "MINIBOSS – " + name;
            actionMessageUntil = Time.time + 2.2f;
            PlayRandom(effects, scareSounds, 0.9f);
        }

        private void AddRelic(float z, int route, float lane, int variant)
        {
            GhostTarget relic = CursedRelic.Create(
                new Vector3(DarkRideWorld.TrackCenter(z, route) + lane, 1.25f, z), variant);
            targets.Add(relic);
        }

        public static void ReportMonsterEscape(int damage = 1)
        {
            if (Instance != null) Instance.DamageWagon(damage, "ETT MONSTER KOM UNDAN!");
        }

        public static void ReportThreatCue(int side)
        {
            if (Instance == null) return;
            if (Instance.environmentVoice != null) Instance.environmentVoice.panStereo = side < 0 ? -0.62f : 0.62f;
            PlayRandom(Instance.environmentVoice, Instance.hazardCueSounds, 0.42f);
            Instance.PlayGhostSound(side, 0.44f);
        }

        private void DamagePlayer(int player, int amount)
        {
            player = Mathf.Clamp(player, 0, 1);
            combos[player] = 0;
            if (easyMode)
            {
                scores[player] = Mathf.Max(0, scores[player] - 5);
                actionMessage = "NÄSTAN! FÖRSÖK IGEN – DU HAR OBEGRÄNSAT MED FÖRSÖK";
                actionMessageUntil = Time.time + 1.15f;
                return;
            }
            if (lives[player] > 0)
            {
                lives[player] = Mathf.Max(0, lives[player] - amount);
                PlayAnnouncement(playerHitVoice, true);
            }
            else DamageWagon(amount, "VAGNEN TOG SKADA!");
        }

        private void TryRescueTeammate(int rescuer)
        {
            int teammate = rescuer == 0 ? 1 : 0;
            if (lives[teammate] > 0 || (!currentPoses.ContainsKey(teammate)
                && !reticles.Values.Any(item => item.PlayerIndex == teammate))) return;
            rescueProgress[teammate]++;
            if (rescueProgress[teammate] < 3)
            {
                actionMessage = "RÄDDA SPELARE " + (teammate + 1) + "  " + rescueProgress[teammate] + "/3";
                actionMessageUntil = Time.time + 1f;
                return;
            }
            lives[teammate] = 1;
            rescueProgress[teammate] = 0;
            actionMessage = "SPELARE " + (teammate + 1) + " ÄR TILLBAKA!";
            actionMessageUntil = Time.time + 1.8f;
            PlayRandom(effects, movementSuccessSounds);
        }

        private int TriggerTrap(Vector3 position, int player)
        {
            int defeated = 0;
            foreach (GhostTarget victim in GhostTarget.ActiveTargets.Where(target =>
                         target.IsTargetable && !target.IsBoss
                         && target.GetComponent<TrapTrigger>() == null
                         && target.GetComponent<CursedRelic>() == null
                         && target.GetComponent<BreakableProp>() == null
                         && Vector3.Distance(target.transform.position, position) <= 9f).ToArray())
            {
                while (victim != null && victim.Health > 0) victim.Hit();
                defeated++;
            }
            if (defeated <= 0) return 15;
            kills[player] += defeated;
            for (int i = 0; i < defeated; i++) TryRescueTeammate(player);
            cameraShakeUntil = Time.time + 0.42f;
            PlayRandom(effects, collisionSounds, 1f);
            return defeated * 60;
        }

        private void DamageWagon(int amount, string message)
        {
            if (finished || gameOver) return;
            if (easyMode)
            {
                combos[0] = combos[1] = 0;
                cameraShakeUntil = Mathf.Max(cameraShakeUntil, Time.time + 0.16f);
                actionMessage = "NÄSTAN – DEN SÄKRA VAGNEN KÖR VIDARE!";
                actionMessageUntil = Time.time + 1.15f;
                PlayRandom(effects, collisionSounds, 0.28f);
                return;
            }
            wagonHealth = Mathf.Max(0, wagonHealth - amount);
            wagonDamageTaken += amount;
            combos[0] = combos[1] = 0;
            cameraShakeUntil = Mathf.Max(cameraShakeUntil, Time.time + 0.45f);
            actionMessage = message + "  VAGN " + wagonHealth;
            actionMessageUntil = Time.time + 1.35f;
            PlayRandom(effects, collisionSounds, 0.85f);
            PlayAnnouncement(wagonHitVoice, true);
            if (WagonDamageVisual.Instance != null)
                WagonDamageVisual.Instance.SetHealth(wagonHealth, startingWagonHealth, true);
            if (!repairSigilSpawned && wagonHealth > 0 && wagonHealth <= startingWagonHealth - 2)
                SpawnRepairSigil();
            if (wagonHealth > 0) return;
            gameOver = true;
            finished = true;
            reticles.Clear();
            actionMessage = "VAGNEN ÄR FÖRSTÖRD";
            actionMessageUntil = float.PositiveInfinity;
        }

        private void SpawnRepairSigil()
        {
            repairSigilSpawned = true;
            float z = Mathf.Min(BossStopDistance - 8f, rideDistance + 19f);
            float lane = UnityEngine.Random.value < 0.5f ? -2.7f : 2.7f;
            GhostTarget repair = RepairSigil.Create(new Vector3(
                DarkRideWorld.TrackCenter(z, selectedRoute) + lane, 1.65f, z));
            targets.Add(repair);
            actionMessage = "ETT REPARATIONSSIGILL HAR VAKNAT!";
            actionMessageUntil = Time.time + 1.7f;
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
            ReticleState reticle = reticles.Values.FirstOrDefault(item => item.PlayerIndex == 0);
            int automaticRoute = reticles.Values.Any(item => item.PlayerIndex == 0)
                ? (reticle.Position.x < 0.5f ? -1 : 1)
                : (UnityEngine.Random.value < 0.5f ? -1 : 1);
            SelectRoute(automaticRoute);
        }

        private void SelectRoute(int route)
        {
            if (route < 0 && !secretRouteUnlocked)
            {
                route = 1;
                actionMessage = "SIGILLET HÖLL DEN HEMLIGA DÖRREN STÄNGD – HÖGER VÄG!";
                actionMessageUntil = Time.time + 2.4f;
            }
            selectedRoute = route < 0 ? -1 : 1;
            routeChoiceActive = false;
            if (selectedRoute < 0)
            {
                actionMessage = "DEN HEMLIGA GÅNGEN ÄR ÖPPEN!";
                actionMessageUntil = Time.time + 2.1f;
            }
            else if (secretRouteUnlocked)
            {
                actionMessage = "HÖGRA FÖRBANNADE GÅNGEN!";
                actionMessageUntil = Time.time + 2.1f;
            }
            PlayRandom(effects, movementSuccessSounds);
            RouteDoor.OpenRoute(selectedRoute);

            if (!routeCollectiblesAdded)
            {
                routeCollectiblesAdded = true;
                AddRelic(238f, selectedRoute, selectedRoute < 0 ? 2.8f : -2.8f, 3);
                AddRelic(281f, selectedRoute, selectedRoute < 0 ? -2.7f : 2.7f, 4);
                AddRelic(329f, 0, 2.5f, 5);
            }

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
                if (pose.PlayerIndex < 0 || pose.PlayerIndex >= requestedPlayers) continue;
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

                if (flow == GameFlow.Calibration)
                {
                    float headDifference = pose.HeadY - calibration.StandingHeadY;
                    if (headDifference >= -0.06f && headDifference <= 0.12f)
                        calibration.StandingHeadY = Mathf.Lerp(
                            calibration.StandingHeadY, pose.HeadY, 0.08f);
                }
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

                if (gap <= QuickEventCueDistance && gap >= QuickEventPassedDistance && hazard.Reveal())
                    PlayRandom(effects, hazardCueSounds, 0.78f);

                if (gap <= QuickEventActionDistance && gap >= QuickEventPassedDistance)
                {
                    foreach (KeyValuePair<int, PoseState> pair in currentPoses)
                    {
                        bool succeeds = hazard.Kind == HazardKind.Duck
                            ? pair.Value.DuckAmount >= (easyMode ? 0.16f : 0.20f)
                            : hazard.Kind == HazardKind.DodgeLeft
                                ? pair.Value.LeanAmount <= (easyMode ? -0.14f : -0.17f)
                                : pair.Value.LeanAmount >= (easyMode ? 0.14f : 0.17f);
                        if (!succeeds || !hazard.MarkSuccess(pair.Key)) continue;
                        int player = Mathf.Clamp(pair.Key, 0, 1);
                        scores[player] += 40;
                        PlayRandom(effects, movementSuccessSounds);
                        if (hazard.TryClaimPraise())
                            PlayAnnouncement(hazard.Kind == HazardKind.Duck
                                ? duckSuccessVoice : dodgeSuccessVoice);
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
                    PlayRandom(effects, collisionSounds);
                    foreach (int player in currentPoses.Keys.ToArray())
                        if (!hazard.HasSucceeded(player)) DamagePlayer(Mathf.Clamp(player, 0, 1), 1);
                    if (currentPoses.Count == 0) DamageWagon(1, "INGEN UNDVIKNING – VAGNEN SKADAS!");
                    cameraShakeUntil = Time.time + 0.55f;
                }
            }
        }

        private void UpdateEnvironmentEvents()
        {
            if (nextScareIndex < ScareDistances.Length && rideDistance >= ScareDistances[nextScareIndex])
            {
                int side = UnityEngine.Random.value < 0.5f ? -1 : 1;
                nextScareIndex++;
                SideScare.Create(rideCamera.transform, side);
                PlayRandom(effects, scareSounds);
                PlayGhostSound(side, 0.82f);
                cameraShakeUntil = Mathf.Max(cameraShakeUntil, Time.time + 0.22f);
            }

            if (nextEncounterIndex < EncounterDistances.Length
                && rideDistance >= EncounterDistances[nextEncounterIndex])
            {
                int encounterSide = UnityEngine.Random.value < 0.5f ? -1 : 1;
                HauntedEncounterKind kind = NextDirectorEncounter();
                nextEncounterIndex++;
                TriggerEncounter(kind, encounterSide);
            }

            UpdateScareDirector();
        }

        private void UpdateScareDirector()
        {
            float rideTime = Mathf.Max(0f, gameTime - CountdownSeconds);
            if (rideTime < nextDirectorBeatAt || bossBattle || routeChoiceActive) return;
            int livingThreats = GhostTarget.ActiveTargets.Count(target => target.IsTargetable
                && target.GetComponent<CursedRelic>() == null
                && target.GetComponent<BreakableProp>() == null);
            float performance = Mathf.Clamp01((Mathf.Max(maxCombos[0], maxCombos[1]) - 2f) / 12f);
            float relief = wagonHealth <= 2 ? 4f : 0f;
            nextDirectorBeatAt = rideTime + UnityEngine.Random.Range(8.0f, 12.5f)
                - performance * 2.0f + relief;
            if (livingThreats >= (easyMode ? 2 : 3)) return;

            int side = UnityEngine.Random.value < 0.5f ? -1 : 1;
            directorEvents++;
            float roll = UnityEngine.Random.value;
            if (roll < 0.24f)
            {
                // Ett falsklarm bryter mönstret utan att alltid visa ett monster.
                PlayRandom(environmentVoice, chainRattleSounds, 0.46f);
                PlayGhostSound(-side, 0.30f);
                StartCoroutine(PulseNearbyTorches(1.15f));
            }
            else if (roll < 0.48f)
            {
                StartCoroutine(DirectorScareChain(side));
            }
            else
            {
                TriggerEncounter(NextDirectorEncounter(), side);
            }
        }

        private HauntedEncounterKind NextDirectorEncounter()
        {
            int choice = UnityEngine.Random.Range(0, 3);
            if (choice == lastDirectorEncounter) choice = (choice + UnityEngine.Random.Range(1, 3)) % 3;
            lastDirectorEncounter = choice;
            return (HauntedEncounterKind)choice;
        }

        private void TriggerEncounter(HauntedEncounterKind kind, int side)
        {
            HauntedEncounter.Create(rideCamera.transform, kind, side);
            AudioClip sound = RandomClip(kind == HauntedEncounterKind.BatBurst
                ? batRushSounds
                : kind == HauntedEncounterKind.SwingingChain ? chainRattleSounds : phantomSounds);
            if (environmentVoice == null || sound == null) return;
            environmentVoice.clip = sound;
            environmentVoice.panStereo = side * 0.58f;
            environmentVoice.volume = 0.74f * effectsVolume;
            environmentVoice.pitch = UnityEngine.Random.Range(0.92f, 1.06f);
            environmentVoice.Play();
        }

        private IEnumerator DirectorScareChain(int side)
        {
            PlayGhostSound(side, 0.34f);
            yield return PulseNearbyTorches(0.72f);
            TriggerEncounter(HauntedEncounterKind.PhantomFace, -side);
            yield return new WaitForSeconds(0.48f);
            PlayRandom(environmentVoice, scareSounds, 0.62f);
        }

        private IEnumerator PulseNearbyTorches(float duration)
        {
            Light[] torches = FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(light => light != null
                    && light.name.IndexOf("Fackelljus", StringComparison.OrdinalIgnoreCase) >= 0
                    && Vector3.Distance(light.transform.position, rideCamera.transform.position) < 24f)
                .ToArray();
            float[] intensities = torches.Select(light => light.intensity).ToArray();
            float end = Time.time + duration;
            while (Time.time < end)
            {
                bool on = Mathf.FloorToInt((end - Time.time) * 12f) % 2 == 0;
                for (int i = 0; i < torches.Length; i++)
                    if (torches[i] != null) torches[i].intensity = on ? intensities[i] : 0.03f;
                yield return null;
            }
            for (int i = 0; i < torches.Length; i++)
                if (torches[i] != null) torches[i].intensity = intensities[i];
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
            ghostVoice.volume = volume * effectsVolume;
            ghostVoice.Play();
        }
    }
}