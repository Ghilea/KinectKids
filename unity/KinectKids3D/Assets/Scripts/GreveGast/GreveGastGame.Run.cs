using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class GreveGastGame
    {
        private void AnimateCharacters()
        {
            bool neutral = Mathf.Abs(body.HorizontalDelta) < 0.075f;
            if (neutral)
            {
                if (laneNeutralSince < 0f) laneNeutralSince = Time.unscaledTime;
                if (Time.unscaledTime - laneNeutralSince >= 0.16f) laneGestureArmed = true;
            }
            else laneNeutralSince = -1f;

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                MoveOneLane(-1);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                MoveOneLane(1);
            else if (laneGestureArmed && Time.unscaledTime >= laneChangeCooldownUntil
                     && body.HorizontalDelta < -0.16f)
            {
                laneGestureArmed = false;
                MoveOneLane(-1);
            }
            else if (laneGestureArmed && Time.unscaledTime >= laneChangeCooldownUntil
                     && body.HorizontalDelta > 0.16f)
            {
                laneGestureArmed = false;
                MoveOneLane(1);
            }
            // Kameran tittar tillbaka langs banan och speglar varlds-X.
            // Negativ kroppslutning ska fortfarande synas som vanster pa skarmen.
            float targetX = -currentLane * LaneSpacing;
            float runBob = body.Current == GreveGastAction.Run
                ? Mathf.Abs(Mathf.Sin(song.SongTime * 10f)) * 0.09f : 0f;
            float jumpProgress = (Time.time - jumpStartedAt) / JumpVisualDuration;
            float jumpHeight = jumpProgress >= 0f && jumpProgress <= 1f
                ? Mathf.Sin(jumpProgress * Mathf.PI) * 2.65f : 0f;
            float targetY = jumpHeight > 0f ? jumpHeight
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
            if (skeletonLeftArm != null)
            {
                skeletonLeftArm.localRotation = playerLeftArm.localRotation;
                skeletonRightArm.localRotation = playerRightArm.localRotation;
                skeletonLeftLeg.localRotation = playerLeftLeg.localRotation;
                skeletonRightLeg.localRotation = playerRightLeg.localRotation;
            }

            // Spelaren springer mot kameran. Jagarfiguren ligger bakom och far
            // aldrig lagga sig mellan kameran och spelarfiguren.
            float lyricApproach = song.SongTime < scriptedApproachUntil ? 10f : 0f;
            float greveZ = Mathf.Min(-4.5f, Mathf.Lerp(-58f, -5.5f, chase) + lyricApproach);
            Vector3 target = new Vector3(Mathf.Sin(song.SongTime * 1.4f) * 1.3f,
                chaserGroundY + Mathf.Sin(song.SongTime * 2.2f) * 0.10f, greveZ);
            greve.position = Vector3.Lerp(greve.position, target, 1f - Mathf.Exp(-3f * Time.deltaTime));
            if (drawnGreveAnimation != null) drawnGreveAnimation.SetChaseDistance(chase);
        }

        private void ScrollCorridor()
        {
            float corridorLength = corridor.Count * 12f;
            float backLimit = 18f - (corridor.Count - 1) * 12f;
            for (int i = 0; i < corridor.Count; i++)
            {
                Transform segment = corridor[i];
                // Banmarkeringarna kommer fran nederkanten och flyttas upp mot
                // spelarens position. En ny del matas in fran forgrunden.
                segment.position += Vector3.back * (CorridorSpeed * (1f + runSpeedBoost * 0.62f) * Time.deltaTime);
                // Ateranvand delen tillrackligt langt bakom kameran sa att de
                // tre vagarna alltid fortsatter hela vagen ned till bildkanten.
                if (segment.position.z < backLimit)
                    segment.position += Vector3.forward * corridorLength;
            }
        }

        private void MoveOneLane(int direction)
        {
            if (Time.unscaledTime < laneChangeCooldownUntil) return;
            direction = direction < 0 ? -1 : 1;
            currentLane = Mathf.Clamp(currentLane + direction, -1, 1);
            laneChangeCooldownUntil = Time.unscaledTime + 0.42f;
            laneGestureArmed = false;
            laneNeutralSince = -1f;
        }

        private void TriggerCaught()
        {
            if (caught) return;
            caught = true;
            chase = 1f;
            runSpeedBoost = 0f;
            catchReleaseAt = Time.unscaledTime + 2.8f;
            PlayChaserCatch();
            catchPausedSong = song != null && song.IsPlaying;
            if (catchPausedSong) song.TogglePause();
        }

        private void ReleaseFromCatch()
        {
            caught = false;
            chase = 0.56f;
            currentLane = 0;
            laneGestureArmed = false;
            laneNeutralSince = Time.unscaledTime;
            if (catchPausedSong && song != null) song.TogglePause();
            catchPausedSong = false;
            SetChaserRunning(true);
        }

        private void UpdateCaughtVisual()
        {
            if (greve == null) return;
            Vector3 target = new Vector3(0f, chaserGroundY, -4.5f);
            greve.position = Vector3.Lerp(greve.position, target,
                1f - Mathf.Exp(-5.5f * Time.unscaledDeltaTime));
        }

        private void OnWarning(GreveGastCue cue)
        {
            GreveGastAction action = ParseAction(cue.action);
            if (action != GreveGastAction.None)
            {
                if (cue.time < song.GameplayStartTime || song.SongTime < song.GameplayStartTime) return;
                warningSymbol = Symbol(action);
                warningUntil = cue.time + cue.duration;
                effects.PlayOneShot(warningTone);
                SpawnObstacle(cue, action);
                if (chase > 0.68f) PlayChaserReach();
            }
            else if (cue.kind == "scare" && cue.time >= song.GameplayStartTime)
            {
                warningUntil = cue.time + 0.35f;
                warningSymbol = "!";
            }
        }

        private void OnCue(GreveGastCue cue)
        {
            GreveGastAction action = ParseAction(cue.action);
            if (action != GreveGastAction.None && cue.time >= song.GameplayStartTime)
            {
                if (action == GreveGastAction.Run) BeginRunChallenge(cue);
                else pending.Add(new PendingAction { Cue = cue });
            }
            if (greveAnimation != null || drawnGreveAnimation != null)
            {
                switch (cue.kind)
                {
                    case "gast_run":
                        if (cue.time >= song.GameplayStartTime) SetChaserRunning(true);
                        break;
                    case "gast_closer":
                        if (cue.time >= song.GameplayStartTime)
                        {
                            SetChaserRunning(true);
                            scriptedApproachUntil = song.SongTime + Mathf.Max(2f, cue.duration);
                        }
                        break;
                    case "gast_stumble":
                        PlayChaserStumble();
                        break;
                    case "gast_dance":
                        PlayChaserDance();
                        break;
                    case "gast_reach":
                        PlayChaserReach();
                        break;
                    case "gast_catch":
                        PlayChaserCatch();
                        break;
                    case "gast_laugh":
                        PlayChaserLaugh();
                        break;
                    case "gast_surprise":
                        PlayChaserSurprise();
                        break;
                }
            }
            if (cue.kind == "reveal") PlayChaserSurprise();
            if ((cue.kind == "reveal" || cue.kind == "scare")
                && cue.time >= song.GameplayStartTime)
            {
                chase = Mathf.Clamp01(chase + 0.07f);
                if (cue.kind == "scare") PlayChaserStumble();
                if (cue.kind == "scare") SpawnAtmosphereScare(cue);
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
                    PlayChaserStumble();
                }
                else if (now > item.Cue.time + item.Cue.duration)
                {
                    item.Resolved = true;
                    chase = Mathf.Clamp01(chase + 0.12f);
                    feedback = "!";
                    feedbackColor = new Color(1f, 0.34f, 0.28f);
                    feedbackUntil = now + 0.75f;
                    if (chase > 0.9f) PlayChaserCatch();
                    else PlayChaserReach();
                }
            }
        }

        private void BeginRunChallenge(GreveGastCue cue)
        {
            activeRunUntil = Mathf.Max(activeRunUntil, cue.time + Mathf.Max(1.5f, cue.duration));
            warningSymbol = "⚡";
            warningUntil = activeRunUntil;
            SetChaserRunning(true);
        }

        private void UpdateRunChallenge()
        {
            if (song.SongTime > activeRunUntil)
            {
                runSpeedBoost = Mathf.MoveTowards(runSpeedBoost, 0f, Time.deltaTime * 1.8f);
                return;
            }

            bool keyboardRun = Input.GetKey(KeyCode.W);
            float effort = keyboardRun ? 1f : body.RunEnergy;
            bool running = effort > 0.38f || body.Current == GreveGastAction.Run;
            if (running)
            {
                float effectiveEffort = Mathf.Max(0.52f, effort);
                runSpeedBoost = Mathf.MoveTowards(runSpeedBoost, effectiveEffort, Time.deltaTime * 2.8f);
                chase = Mathf.Clamp01(chase - Time.deltaTime * (0.055f + effectiveEffort * 0.070f));
            }
            else
            {
                runSpeedBoost = Mathf.MoveTowards(runSpeedBoost, 0f, Time.deltaTime * 2.2f);
                chase = Mathf.Clamp01(chase + Time.deltaTime * 0.072f);
            }
        }

        private void SpawnObstacle(GreveGastCue cue, GreveGastAction action)
        {
            GameObject root = new GameObject("Hinder " + cue.id);
            if (action == GreveGastAction.Jump)
            {
                jumpObstacleCounter++;
                if ((jumpObstacleCounter & 1) == 0)
                {
                    Cube("Golvgrop", root.transform, new Vector3(0, 0.035f, 0), new Vector3(23.5f, 0.06f, 4.2f), pit);
                    Cube("Trasig framkant", root.transform, new Vector3(0, 0.10f, 2.08f), new Vector3(23.5f, 0.18f, 0.24f), stone);
                    Cube("Trasig bakkant", root.transform, new Vector3(0, 0.10f, -2.08f), new Vector3(23.5f, 0.18f, 0.24f), stone);
                    for (int lane = -1; lane <= 1; lane++)
                        ImportedModelFactory.Create("Models/KenneyGraveyard/rocks-tall", root.transform,
                            "Rasade stenar vid grop", new Vector3(lane * LaneSpacing, 0.30f, 2.0f),
                            1.25f, Quaternion.Euler(0f, lane * 17f, 0f));
                }
                else
                {
                    Cube("Nedfallen takbjalk", root.transform, new Vector3(0, 0.48f, 0),
                        new Vector3(23.5f, 0.72f, 0.85f), hauntedWood);
                    for (int lane = -1; lane <= 1; lane++)
                        ImportedModelFactory.Create("Models/KenneyGraveyard/debris-wood", root.transform,
                            "Trasigt virke", new Vector3(lane * LaneSpacing, 0.42f, 0.15f),
                            1.6f, Quaternion.Euler(0f, lane * 21f, 0f));
                }
            }
            else if (action == GreveGastAction.Duck)
            {
                Cube("Hangande slottsbjalk", root.transform, new Vector3(0, 2.22f, 0),
                    new Vector3(23.5f, 0.72f, 1.0f), hauntedWood);
                for (int x = -10; x <= 10; x += 10)
                    Cube("Rostig kedja", root.transform, new Vector3(x, 5.3f, 0),
                        new Vector3(0.13f, 5.8f, 0.13f), rustedIron);
                ImportedModelFactory.Create("Models/KenneyGraveyard/lantern-candle", root.transform,
                    "Svangande lykta", new Vector3(0f, 1.62f, 0f), 1.15f,
                    Quaternion.Euler(0f, 180f, 0f));
            }
            else if (action == GreveGastAction.Left)
            {
                // Skarmens vanstra fil ar world +X eftersom kameran ser bakat.
                AddLaneBarricade(root.transform, 0f, 0);
                AddLaneBarricade(root.transform, -LaneSpacing, 1);
            }
            else if (action == GreveGastAction.Right)
            {
                AddLaneBarricade(root.transform, 0f, 2);
                AddLaneBarricade(root.transform, LaneSpacing, 3);
            }
            else
                return;
            root.transform.position = new Vector3(0f, 0f, ObstacleStartZ);
            if (drawingPowers != null)
            {
                global::GreveGast2D.GreveGastDrawnPower power = action == GreveGastAction.Jump
                    && root.transform.Find("Golvgrop") != null
                    ? global::GreveGast2D.GreveGastDrawnPower.DrawHole
                    : action == GreveGastAction.Duck
                        ? global::GreveGast2D.GreveGastDrawnPower.DrawWall
                        : global::GreveGast2D.GreveGastDrawnPower.DrawObstacle;
                drawingPowers.DrawIntoWorld(root, greve, power);
            }
            float remaining = Mathf.Max(0.5f, cue.time - song.SongTime);
            obstacles.Add(new Obstacle
            {
                Cue = cue,
                Object = root,
                Speed = (ObstacleStartZ - PlayerZ) / remaining
            });
        }

        private void AddLaneBarricade(Transform parent, float x, int variant)
        {
            string path;
            float size;
            switch (variant % 4)
            {
                case 0:
                    path = "Models/KenneyGraveyard/coffin-old";
                    size = 3.9f;
                    break;
                case 1:
                    path = "Models/KenneyGraveyard/gravestone-cross-large";
                    size = 3.7f;
                    break;
                case 2:
                    path = "Models/KenneyGraveyard/gravestone-debris";
                    size = 3.5f;
                    break;
                default:
                    path = "Models/KenneyGraveyard/rocks-tall";
                    size = 3.8f;
                    break;
            }
            ImportedModelFactory.Create(path, parent, "Slottsbråte som blockerar filen",
                new Vector3(x, 1.35f, 0f), size,
                Quaternion.Euler(0f, 180f + variant * 13f, 0f));
            Cube("Korslagda plankor A", parent, new Vector3(x, 1.25f, -0.15f),
                new Vector3(4.8f, 0.28f, 0.34f), hauntedWood).transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            Cube("Korslagda plankor B", parent, new Vector3(x, 1.25f, -0.12f),
                new Vector3(4.8f, 0.28f, 0.34f), hauntedWood).transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
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
            if (ambience != null)
            {
                if (paused) ambience.Pause(); else ambience.UnPause();
            }
        }
    }
}