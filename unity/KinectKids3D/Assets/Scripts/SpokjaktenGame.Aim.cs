using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D
{
    public sealed partial class SpokjaktenGame
    {
        private void UpdateAim()
        {
            IReadOnlyList<AimSample> samples = aimProvider.GetAimSamples();
            reticles.Clear();
            foreach (Light reticleLight in reticleLights.Values)
                if (reticleLight != null) reticleLight.enabled = false;
            foreach (AimSample rawSample in activeHandSelector.Select(samples, requestedPlayers))
            {
                int player = Mathf.Clamp(rawSample.PlayerIndex, 0, 1);
                if (player >= requestedPlayers) continue;
                AimSample sample = SmoothHand(rawSample);
                Ray ray = rideCamera.ViewportPointToRay(new Vector3(sample.Position.x, 1f - sample.Position.y, 0));
                UpdateReticleLight(sample.HandId, player, ray);
                GhostTarget target = FindAimTarget(ray, sample.Position);
                PoseState playerPose;
                bool attackBlocked = currentPoses.TryGetValue(player, out playerPose)
                    && playerPose.DuckAmount >= 0.15f;

                AimLock aimLock;
                if (!aimLocks.TryGetValue(sample.HandId, out aimLock))
                {
                    aimLock = new AimLock();
                    aimLocks[sample.HandId] = aimLock;
                }
                if (target != null && !attackBlocked)
                {
                    aimLock.RecentTarget = target;
                    aimLock.LastTargetAt = Time.time;
                }

                if (attackBlocked)
                {
                    // En duckning är en ren undanmanöver: ingen gammal dwell-
                    // laddning eller mållåsning får ge ett skott på vägen upp.
                    aimLock.RecentTarget = null;
                }

                // Varje hand siktar och kastar självständigt. Kinect kräver
                // ett riktigt framåtkast; att bara hålla siktet på ett monster
                // avfyrar aldrig automatiskt.
                if (!attackBlocked && sample.Fire
                    && Time.time >= aimLock.CooldownUntil)
                {
                    GhostTarget firedTarget = target;
                    if (firedTarget == null && Time.time - aimLock.LastTargetAt <= TargetGraceSeconds)
                        firedTarget = aimLock.RecentTarget;

                    bool rightHand = sample.HandId % 2 == 1;
                    Color boltColor = player == 1
                        ? (rightHand ? new Color(1f, 0.25f, 0.58f) : new Color(1f, 0.50f, 0.20f))
                        : (rightHand ? new Color(0.20f, 0.68f, 1f) : new Color(0.18f, 1f, 0.78f));
                    Vector3 boltStart = ray.origin + ray.direction * 0.78f;
                    Vector3 boltEnd = firedTarget != null
                        ? firedTarget.transform.position + Vector3.up
                        : ray.GetPoint(18f);
                    MagicBolt.Launch(boltStart, boltEnd, boltColor);
                    PlayRandom(effects, castSounds, 0.92f);
                    shots[player]++;
                    if (rightHand) rightHandShots[player]++;
                    else leftHandShots[player]++;
                    if (firedTarget != null)
                    {
                        bool wasBoss = firedTarget.IsBoss;
                        bool wasRelic = firedTarget.GetComponent<CursedRelic>() != null;
                        bool wasBreakable = firedTarget.GetComponent<BreakableProp>() != null;
                        bool wasIllusion = firedTarget.GetComponent<HauntedIllusion>() != null;
                        bool wasTrap = firedTarget.GetComponent<TrapTrigger>() != null;
                        bool wasSecretSeal = firedTarget.GetComponent<SecretRouteSeal>() != null;
                        bool wasRepairSigil = firedTarget.GetComponent<RepairSigil>() != null;
                        hits[player]++;
                        combos[player] = Time.time - lastHitAt[player] <= 2.6f ? combos[player] + 1 : 1;
                        lastHitAt[player] = Time.time;
                        maxCombos[player] = Mathf.Max(maxCombos[player], combos[player]);
                        int multiplier = Mathf.Clamp(1 + combos[player] / 5, 1, 3);
                        int points = firedTarget.Hit() * multiplier;
                        bool killedNow = firedTarget.Health <= 0;
                        if (wasIllusion) points = 0;
                        if (killedNow && !wasBreakable && !wasIllusion && !wasTrap
                            && !wasRelic && !wasSecretSeal && !wasRepairSigil)
                        {
                            kills[player]++;
                            TryRescueTeammate(player);
                        }
                        if (wasRelic && killedNow)
                        {
                            collectedRelics++;
                            points += 100;
                            PlayRandom(effects, movementSuccessSounds, 0.9f);
                        }
                        if (wasBreakable && killedNow) points += 25;
                        if (wasTrap && killedNow) points += TriggerTrap(firedTarget.transform.position, player);
                        if (wasBreakable && killedNow) breakablesDestroyed++;
                        if (wasSecretSeal && killedNow)
                        {
                            secretRouteUnlocked = true;
                            secretsFound++;
                            points += 175;
                            actionMessage = "HEMLIG VÄG UPPLÅST!";
                            actionMessageUntil = Time.time + 2.2f;
                        }
                        if (wasRepairSigil && killedNow)
                        {
                            wagonHealth = Mathf.Min(startingWagonHealth, wagonHealth + 1);
                            points += 90;
                            actionMessage = "VAGNEN REPARERAD!  VAGN " + wagonHealth;
                            actionMessageUntil = Time.time + 2.0f;
                            if (WagonDamageVisual.Instance != null)
                                WagonDamageVisual.Instance.SetHealth(wagonHealth, startingWagonHealth, false);
                            PlayRandom(effects, movementSuccessSounds, 1f);
                        }
                        scores[player] += points;
                        PlayRandom(effects, wasBoss ? bossSounds : hitSounds, wasBoss ? 0.95f : 0.82f);
                        if (!wasSecretSeal && !wasRepairSigil)
                            actionMessage = (wasRelic && killedNow ? "FÖRBANNAD RELIK!  "
                                : wasBreakable && killedNow ? "KROSSAT!  "
                                : wasIllusion ? "ILLUSION!  "
                                : wasTrap && killedNow ? "FÄLLA UTLÖST!  " : string.Empty)
                                + "+" + points + (multiplier > 1 ? "  x" + multiplier : string.Empty);
                        if (!wasSecretSeal && !wasRepairSigil) actionMessageUntil = Time.time + 0.7f;
                        if (wasBoss && !killedNow) UpdateBossPhase(firedTarget);
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
                    else combos[player] = 0;
                    aimLock.RecentTarget = null;
                    aimLock.CooldownUntil = Time.time + 0.32f;
                }

                reticles[sample.HandId] = new ReticleState
                {
                    PlayerIndex = player,
                    Position = sample.Position,
                    Progress = attackBlocked ? 0f : sample.GestureProgress,
                    OnTarget = target != null,
                    AttackBlocked = attackBlocked,
                    IsRightHand = sample.HandId % 2 == 1
                };
            }
        }

        private void UpdateSessionAim()
        {
            IReadOnlyList<AimSample> samples = aimProvider.GetAimSamples();
            reticles.Clear();
            foreach (Light reticleLight in reticleLights.Values)
                if (reticleLight != null) reticleLight.enabled = false;
            foreach (AimSample rawSample in activeHandSelector.Select(samples, requestedPlayers))
            {
                AimSample sample = SmoothHand(rawSample);
                reticles[sample.HandId] = new ReticleState
                {
                    PlayerIndex = Mathf.Clamp(sample.PlayerIndex, 0, 1),
                    Position = sample.Position,
                    IsRightHand = sample.HandId % 2 == 1
                };
            }
        }

        private void UpdateSessionDwell()
        {
            string hovered = null;
            foreach (ReticleState reticle in reticles.Values)
            {
                string action = SessionActionAt(reticle.Position);
                if (string.IsNullOrEmpty(action)) continue;
                hovered = action;
                break;
            }

            if (string.IsNullOrEmpty(hovered) && string.IsNullOrEmpty(sessionDwellAction)) return;

            if (hovered != sessionDwellAction)
            {
                sessionDwellAction = hovered;
                sessionDwellStartedAt = Time.unscaledTime;
            }
            float progress = string.IsNullOrEmpty(hovered)
                ? 0f : Mathf.Clamp01((Time.unscaledTime - sessionDwellStartedAt) / 0.9f);
            foreach (int key in reticles.Keys.ToArray())
            {
                ReticleState state = reticles[key];
                state.Progress = SessionActionAt(state.Position) == hovered ? progress : 0f;
                reticles[key] = state;
            }
            if (progress < 1f) return;

            sessionDwellAction = null;
            sessionDwellStartedAt = Time.unscaledTime;
            ExecuteSessionAction(hovered);
        }

        private string SessionActionAt(Vector2 normalizedPosition)
        {
            Vector2 point = new Vector2(normalizedPosition.x * Screen.width,
                normalizedPosition.y * Screen.height);
            if (!paused && !finished && !gameOver)
                return new Rect(Screen.width - 170f, 150f, 150f, 44f).Contains(point) ? "open" : null;

            float x = Screen.width * 0.5f - 175f;
            float y = paused ? Screen.height * 0.5f - 55f : Screen.height * 0.5f + 85f;
            if (new Rect(x, y, 350f, 56f).Contains(point)) return paused ? "continue" : "restart";
            if (new Rect(x, y + 70f, 350f, 56f).Contains(point)) return paused ? "restart" : "home";
            if (paused && new Rect(x, y + 140f, 350f, 56f).Contains(point)) return "home";
            return null;
        }

        private void ExecuteSessionAction(string action)
        {
            if (action == "open") paused = true;
            else if (action == "continue") paused = false;
            else if (action == "restart") ReloadRide();
            else if (action == "home") LauncherReturnService.ReturnToLauncher();
        }

        private AimSample SmoothHand(AimSample sample)
        {
            HandFilterState state;
            if (!handFilters.TryGetValue(sample.HandId, out state))
            {
                state = new HandFilterState
                {
                    Position = sample.Position,
                    LastRaw = sample.Position
                };
                handFilters[sample.HandId] = state;
                return sample;
            }

            Vector2 raw = new Vector2(Mathf.Clamp(sample.Position.x, 0.025f, 0.975f),
                Mathf.Clamp(sample.Position.y, 0.025f, 0.975f));
            float rawJump = Vector2.Distance(raw, state.LastRaw);
            state.LastRaw = raw;

            // Begränsa enskilda Kinect-spikar. Stora riktiga handrörelser
            // kommer fortfarande ikapp över flera bildrutor i stället för att
            // teleportera siktet tvärs över skärmen.
            Vector2 delta = raw - state.Position;
            const float maximumStep = 0.115f;
            if (delta.magnitude > maximumStep)
                raw = state.Position + delta.normalized * maximumStep;

            float distance = Vector2.Distance(state.Position, raw);
            if (distance < 0.0045f) raw = state.Position;
            float smoothTime = Mathf.Lerp(0.15f, 0.075f,
                Mathf.InverseLerp(0.015f, 0.14f, distance));
            if (rawJump > 0.22f) smoothTime = Mathf.Max(smoothTime, 0.13f);
            state.Position = Vector2.SmoothDamp(state.Position, raw, ref state.Velocity,
                smoothTime, 1.65f, Mathf.Max(0.001f, Time.deltaTime));
            sample.Position = state.Position;
            return sample;
        }

        private GhostTarget FindAimTarget(Ray ray, Vector2 handPosition)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 80f))
            {
                GhostTarget direct = hit.collider.GetComponentInParent<GhostTarget>();
                if (direct != null) return direct;
            }

            Vector2 viewportAim = new Vector2(handPosition.x, 1f - handPosition.y);
            GhostTarget best = null;
            float bestDistance = easyMode ? 0.12f : 0.085f;
            foreach (GhostTarget candidate in GhostTarget.ActiveTargets
                .Where(item => item.Health > 0 && item.IsTargetable))
            {
                Vector3 viewport = rideCamera.WorldToViewportPoint(candidate.transform.position + Vector3.up * 1.05f);
                if (viewport.z <= 0f) continue;
                float distance = Vector2.Distance(viewportAim, new Vector2(viewport.x, viewport.y));
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = candidate;
            }
            return best;
        }

        private void UpdateReticleLight(int lightKey, int player, Ray ray)
        {
            Light flashlight;
            if (!reticleLights.TryGetValue(lightKey, out flashlight) || flashlight == null)
            {
                GameObject lightObject = new GameObject("Siktesficklampa spelare " + (player + 1));
                lightObject.transform.SetParent(rideCamera.transform, false);
                flashlight = lightObject.AddComponent<Light>();
                flashlight.type = LightType.Spot;
                flashlight.color = player == 1
                    ? new Color(1f, 0.62f, 0.74f)
                    : new Color(0.70f, 0.86f, 1f);
                flashlight.intensity = 5.25f * brightnessLevel;
                flashlight.range = 30f;
                flashlight.spotAngle = 38f;
                flashlight.innerSpotAngle = 22f;
                flashlight.shadows = LightShadows.None;
                reticleLights[lightKey] = flashlight;
            }

            flashlight.enabled = true;
            flashlight.transform.position = ray.origin + rideCamera.transform.forward * 0.16f;
            flashlight.transform.rotation = Quaternion.LookRotation(ray.direction, rideCamera.transform.up);
        }
    }
}