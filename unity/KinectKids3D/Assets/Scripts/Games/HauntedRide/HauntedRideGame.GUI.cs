using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D
{
    public sealed partial class HauntedRideGame
    {
        private void OnGUI()
        {
            EnsureStyles();
            if (flow == GameFlow.Menu)
            {
                DrawStartMenu();
                return;
            }
            if (flow == GameFlow.Calibration)
            {
                DrawCalibration();
                return;
            }
            float rideTime = Mathf.Max(0, gameTime - CountdownSeconds);

            GUI.Box(new Rect(18, 16, 430, 82), string.Empty);
            GUI.Label(new Rect(34, 23, 400, 34), "SPÖKJAKTEN", titleStyle);
            GUI.Label(new Rect(35, 58, Mathf.Max(400, Screen.width - 70), 42), inputStatus, smallStyle);

            GUI.Box(new Rect(Screen.width - 315, 16, 297, 126), string.Empty);
            GUI.Label(new Rect(Screen.width - 298, 23, 280, 32), "SPELARE 1   " + scores[0], hudStyle);
            GUI.Label(new Rect(Screen.width - 298, 57, 280, 26), bossBattle
                ? "SLUTBOSS – VAGNEN STÅR STILL"
                : "FÄRD   " + Mathf.RoundToInt(rideDistance / DarkRideWorld.TrackLength * 100f) + " %", smallStyle);
            GUI.Label(new Rect(Screen.width - 145, Screen.height - 30, 130, 22), "F11  HELSKÄRM", smallStyle);
            if (!paused && !finished && !gameOver
                && GUI.Button(new Rect(Screen.width - 170f, 150f, 150f, 44f), "☰ SPELMENY"))
                paused = true;
            if (requestedPlayers > 1)
                GUI.Label(new Rect(Screen.width - 298, 83, 280, 26), "SPELARE 2   " + scores[1], smallStyle);
            GUI.Label(new Rect(Screen.width - 298, 105, 280, 25), easyMode
                ? "SÄKER VAGN   COMBO " + Mathf.Max(combos[0], combos[1])
                : "VAGN " + new string('♥', wagonHealth) + "   COMBO " + Mathf.Max(combos[0], combos[1]), smallStyle);

            GUI.Label(new Rect(22, 102, 450, 24), easyMode
                ? "BARNLÄGE – OBEGRÄNSADE FÖRSÖK    RELIKER " + collectedRelics + "/" + TotalRelics
                : "LIV P1 " + new string('♥', lives[0])
                    + (requestedPlayers > 1 ? "    P2 " + new string('♥', lives[1]) : string.Empty)
                    + "    RELIKER " + collectedRelics + "/" + TotalRelics, smallStyle);

            if (showSettings)
            {
                GUI.Box(new Rect(18, 138, 390, 164), string.Empty);
                GUI.Label(new Rect(34, 148, 355, 145),
                    "INSTÄLLNINGAR / TEST\n"
                     + "F1: stäng   M: musik " + (musicMuted ? "av" : "på")
                     + "   [ ]: musikvolym " + Mathf.RoundToInt(musicVolume * 100f) + "%\n"
                     + "Effekter " + Mathf.RoundToInt(effectsVolume * 100f) + "%   Röst "
                     + Mathf.RoundToInt(voiceVolume * 100f) + "%\n"
                     + "- / +: ljusstyrka " + Mathf.RoundToInt(brightnessLevel * 100f) + "%\n"
                    + "F2: nästa kontrollpunkt   B: slutboss\n"
                    + "F11: helskärm   P/Mellanslag: paus", smallStyle);
            }

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
                string handLabel = reticle.IsRightHand
                    ? "HÖGER HAND – KASTA FRAMÅT"
                    : "VÄNSTER HAND – KASTA FRAMÅT";
                if (reticle.AttackBlocked) handLabel = "DUCKAR – ATTACK LÅST";
                GUI.Label(new Rect(x - 105, y + 51, 210, 22), handLabel, smallStyle);
            }

            if (currentHazard != null)
            {
                float gap = currentHazard.TrackZ - rideDistance;
                DrawMovementCue(currentHazard.Kind, Mathf.InverseLerp(QuickEventCueDistance, 0f, gap));
            }

            // Bossens riktningspil är en ren reaktionssignal. Visa den först
            // under den sista delen av kastet, när spelaren faktiskt ska röra sig.
            if (bossProjectile != null && bossProjectile.Progress >= 0.58f && !bossProjectile.Arrived)
            {
                DrawMovementCue(bossProjectile.Kind,
                    Mathf.InverseLerp(0.58f, 1f, bossProjectile.Progress));
            }

            DrawEnemyHealthBars();

            if (routeChoiceActive) DrawRouteChoice();

            if (Time.time < zoneTitleUntil)
            {
                float alpha = Mathf.Clamp01((zoneTitleUntil - Time.time) / 0.45f);
                Color old = GUI.color;
                GUI.color = new Color(1f, 0.82f, 0.58f, alpha);
                GUI.Label(new Rect(Screen.width * 0.5f - 330f, Screen.height * 0.18f, 660f, 54f),
                    zoneTitle, centerStyle);
                GUI.color = old;
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
                    bossPhase >= 2
                        ? (boss.IsTargetable ? "SVAG PUNKT – SKJUT!" : "KONDUKTÖREN ÄR SKYDDAD")
                        : "ZOMBIE-KONDUKTÖREN", centerStyle);
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 155, Screen.height - 38, 310, 10), whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width * 0.5f - 153, Screen.height - 36,
                    306f * boss.Health / boss.MaxHealth, 6), goldTexture);
            }

            if (flow == GameFlow.Countdown)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 285, Screen.height * 0.5f - 115, 570, 230), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 250, Screen.height * 0.5f - 91, 500, 58),
                    Mathf.CeilToInt(CountdownSeconds - gameTime).ToString(), centerStyle);
                GUI.Label(new Rect(Screen.width * 0.5f - 255, Screen.height * 0.5f - 20, 510, 105),
                    "GÖR ER REDO!\n" + (easyMode ? "BARNLÄGE" : "NORMALT LÄGE"), centerStyle);
            }
            else if (rideTime < 9f)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 330, 112, 660, 68), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 315, 118, 630, 54),
                    "Sikta och gör en snabb stöt framåt med valfri hand – ducka när pilarna visas!", centerStyle);
            }

            if (paused)
            {
                float menuX = Screen.width * 0.5f - 260f;
                float menuY = Screen.height * 0.5f - 205f;
                float buttonX = Screen.width * 0.5f - 175f;
                float buttonY = Screen.height * 0.5f - 55f;
                GUI.Box(new Rect(menuX, menuY, 520f, 410f), string.Empty);
                GUI.Label(new Rect(menuX + 30f, menuY + 24f, 460f, 92f),
                    "SPELMENY\nVad vill ni göra?", centerStyle);
                if (GUI.Button(new Rect(buttonX, buttonY, 350f, 56f), "FORTSÄTT SPELA"))
                    ExecuteSessionAction("continue");
                if (GUI.Button(new Rect(buttonX, buttonY + 70f, 350f, 56f), "STARTA OM SPELET"))
                    ExecuteSessionAction("restart");
                if (GUI.Button(new Rect(buttonX, buttonY + 140f, 350f, 56f), "TILL GEMENSAMMA MENYN"))
                    ExecuteSessionAction("home");
                DrawInterfaceReticles();
            }
            else if (finished)
            {
                int totalShots = shots[0] + shots[1];
                int totalHits = hits[0] + hits[1];
                int accuracy = totalShots > 0 ? Mathf.RoundToInt(totalHits * 100f / totalShots) : 0;
                GUI.Box(new Rect(Screen.width * 0.5f - 330, Screen.height * 0.5f - 250, 660, 500), string.Empty);
                GUI.Label(new Rect(Screen.width * 0.5f - 305, Screen.height * 0.5f - 232, 610, 310),
                    (gameOver ? "VAGNEN GICK SÖNDER" : "BRA JAGAT!")
                    + "\nBETYG: " + FinalGrade(accuracy)
                    + "\nPoäng: " + (scores[0] + scores[1])
                    + "   Besegrade: " + (kills[0] + kills[1])
                    + "   Träffsäkerhet: " + accuracy + "%"
                    + "\nBästa combo: " + Mathf.Max(maxCombos[0], maxCombos[1])
                    + "   Reliker: " + collectedRelics + "/" + TotalRelics
                    + "   Vagn: " + (easyMode ? "SÄKER" : wagonHealth.ToString())
                    + "\nREKORD  Poäng: " + PlayerPrefs.GetInt(PrefsKeys.HighScore, 0)
                    + "   Combo: " + PlayerPrefs.GetInt(PrefsKeys.BestCombo, 0)
                    + "   Reliker: " + PlayerPrefs.GetInt(PrefsKeys.BestRelics, 0)
                    + "\nHemligheter: " + secretsFound + "   Krossat: " + breakablesDestroyed
                    + "   Extra skräckhändelser: " + directorEvents
                    + "\nMEDALJER: " + MedalText(accuracy)
                    + "\nBästa antal medaljer: " + PlayerPrefs.GetInt(PrefsKeys.BestMedals, 0)
                    + "\nVälj vad ni vill göra här nedanför", centerStyle);
                float endButtonX = Screen.width * 0.5f - 175f;
                float endButtonY = Screen.height * 0.5f + 85f;
                if (GUI.Button(new Rect(endButtonX, endButtonY, 350f, 56f), "STARTA OM SPELET"))
                    ExecuteSessionAction("restart");
                if (GUI.Button(new Rect(endButtonX, endButtonY + 70f, 350f, 56f), "TILL GEMENSAMMA MENYN"))
                    ExecuteSessionAction("home");
                DrawInterfaceReticles();
            }
        }

        private void DrawStartMenu()
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), string.Empty);
            float width = Mathf.Min(680f, Screen.width - 60f);
            float height = Mathf.Min(720f, Screen.height - 50f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            GUI.Box(new Rect(x, y, width, height), string.Empty);
            GUI.Label(new Rect(x + 30f, y + 20f, width - 60f, 54f), "SPÖKJAKTEN", centerStyle);
            GUI.Label(new Rect(x + 35f, y + 70f, width - 70f, 42f),
                "Välj inställningar och kalibrera Kinect innan vagnen startar", centerStyle);

            float rowY = y + 128f;
            GUI.Label(new Rect(x + 48f, rowY, 180f, 30f), "SVÅRIGHET", hudStyle);
            if (GUI.Button(new Rect(x + 240f, rowY, 170f, 38f), "BARNLÄGE – SÄKERT" + (easyMode ? "  ✓" : string.Empty)))
            {
                easyMode = true;
                SavePreferences();
            }
            if (GUI.Button(new Rect(x + 420f, rowY, 170f, 38f), "NORMALT" + (!easyMode ? "  ✓" : string.Empty)))
            {
                easyMode = false;
                SavePreferences();
            }

            rowY += 55f;
            GUI.Label(new Rect(x + 48f, rowY, 180f, 30f), "SPELARE", hudStyle);
            if (GUI.Button(new Rect(x + 240f, rowY, 170f, 38f), "1 SPELARE" + (requestedPlayers == 1 ? "  ✓" : string.Empty)))
            {
                requestedPlayers = 1;
                SavePreferences();
            }
            if (GUI.Button(new Rect(x + 420f, rowY, 170f, 38f), "2 SPELARE" + (requestedPlayers == 2 ? "  ✓" : string.Empty)))
            {
                requestedPlayers = 2;
                SavePreferences();
            }

            rowY += 65f;
            DrawMenuSlider(x, width, ref rowY, "MUSIK", ref musicVolume, 0f, 1.2f, true);
            DrawMenuSlider(x, width, ref rowY, "LJUDEFFEKTER", ref effectsVolume, 0f, 1f, false);
            DrawMenuSlider(x, width, ref rowY, "SVENSK RÖST", ref voiceVolume, 0f, 1f, false);

            GUI.Label(new Rect(x + 48f, rowY, 190f, 30f), "LJUSSTYRKA", hudStyle);
            float newBrightness = GUI.HorizontalSlider(new Rect(x + 240f, rowY + 8f, 270f, 24f),
                brightnessLevel, 0.65f, 1.55f);
            GUI.Label(new Rect(x + 525f, rowY, 80f, 30f), Mathf.RoundToInt(newBrightness * 100f) + "%", smallStyle);
            if (Mathf.Abs(newBrightness - brightnessLevel) > 0.002f)
                AdjustBrightness(newBrightness - brightnessLevel);
            rowY += 48f;

            string fullscreenLabel = Screen.fullScreen ? "HELSKÄRM: PÅ" : "HELSKÄRM: AV";
            if (GUI.Button(new Rect(x + 185f, rowY, 310f, 40f), fullscreenLabel + "   (F11)"))
            {
                Screen.fullScreen = !Screen.fullScreen;
                SavePreferences();
            }

            float statusY = y + height - 176f;
            GUI.Label(new Rect(x + 45f, statusY, width - 90f, 58f), MenuInputStatus(), centeredSmallStyle);
            KinectAutoAimProvider automatic = aimProvider as KinectAutoAimProvider;
            if (automatic != null && !automatic.KinectConnected
                && GUI.Button(new Rect(x + 245f, statusY + 54f, 190f, 26f), "FÖRSÖK KINECT IGEN"))
                automatic.RetryNow();
            if (GUI.Button(new Rect(x + 45f, y + height - 92f, 170f, 58f), "HUVUDMENY"))
                LauncherReturnService.ReturnToLauncher();
            if (GUI.Button(new Rect(x + 225f, y + height - 92f, width - 270f, 58f), "KALIBRERA OCH STARTA"))
                BeginCalibration();
            DrawInterfaceReticles();
        }

        private string MenuInputStatus()
        {
            KinectAutoAimProvider automatic = aimProvider as KinectAutoAimProvider;
            if (automatic != null)
            {
                if (automatic.KinectConnected) return "KINECT ANSLUTEN – MENYN STYRS MED HÄNDERNA\n" + automatic.Status;
                return "KINECT STARTAR / ÅTERANSLUTER – MUSEN FUNGERAR UNDER TIDEN\n" + automatic.Status;
            }
            if (KinectInputActive()) return "KINECT ANSLUTEN\n" + aimProvider.Status;
            return "MUSLÄGE AKTIVT\n" + inputStatus;
        }

        private void DrawMenuSlider(float x, float width, ref float rowY, string label,
            ref float value, float minimum, float maximum, bool music)
        {
            GUI.Label(new Rect(x + 48f, rowY, 190f, 30f), label, hudStyle);
            float changed = GUI.HorizontalSlider(new Rect(x + 240f, rowY + 8f, 270f, 24f),
                value, minimum, maximum);
            GUI.Label(new Rect(x + 525f, rowY, 80f, 30f), Mathf.RoundToInt(changed * 100f) + "%", smallStyle);
            if (Mathf.Abs(changed - value) > 0.002f)
            {
                value = changed;
                if (music) musicMuted = false;
                ApplyAudioVolumes();
                SavePreferences();
            }
            rowY += 48f;
        }

        private void DrawCalibration()
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), string.Empty);
            float width = Mathf.Min(760f, Screen.width - 50f);
            float height = Mathf.Min(650f, Screen.height - 40f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            GUI.Box(new Rect(x, y, width, height), string.Empty);
            GUI.Label(new Rect(x + 30f, y + 18f, width - 60f, 48f), "KINECT-KALIBRERING", centerStyle);
            GUI.Label(new Rect(x + 45f, y + 68f, width - 90f, 58f),
                "Stå rakt och stilla tills mätaren är full. Sikta och gör sedan en snabb stöt framåt med varje hand.", centerStyle);

            float progress = Mathf.Clamp01(calibrationStableSeconds / 1.6f);
            GUI.DrawTexture(new Rect(x + 110f, y + 132f, width - 220f, 18f), whiteTexture);
            GUI.DrawTexture(new Rect(x + 113f, y + 135f, (width - 226f) * progress, 12f), goldTexture);
            GUI.Label(new Rect(x + 110f, y + 128f, width - 220f, 25f),
                Mathf.RoundToInt(progress * 100f) + "%", centeredSmallStyle);
            GUI.Label(new Rect(x + 80f, y + 154f, width - 160f, 30f),
                CalibrationProgressText(), centerStyle);

            float cardWidth = requestedPlayers == 1 ? width - 110f : (width - 135f) * 0.5f;
            for (int player = 0; player < requestedPlayers; player++)
            {
                float cardX = requestedPlayers == 1 ? x + 55f : x + 45f + player * (cardWidth + 45f);
                DrawCalibrationCard(player, new Rect(cardX, y + 195f, cardWidth, 245f));
            }

            DrawInterfaceReticles();

            GUI.Label(new Rect(x + 45f, y + 448f, width - 90f, 44f),
                "BÅDA HÄNDER: sikta stilla och slå snabbt framåt direkt från siktläget.", centeredSmallStyle);
            if (GUI.Button(new Rect(x + 45f, y + height - 72f, 150f, 42f), "TILLBAKA"))
                flow = GameFlow.Menu;
            GUI.enabled = calibrationTrackingStable;
            if (GUI.Button(new Rect(x + width - 315f, y + height - 78f, 270f, 52f),
                    calibrationTrackingStable ? "STARTA ÅKTUREN" : "VÄNTAR PÅ STABIL KINECT"))
                CompleteCalibration();
            GUI.enabled = true;
        }

        private void DrawCalibrationCard(int player, Rect card)
        {
            GUI.Box(card, string.Empty);
            bool body = currentPoses.ContainsKey(player);
            bool kinectInput = KinectInputActive();
            bool hands = calibrationVisibleHands[player] >= (kinectInput ? 2 : 1);
            string attackTests = kinectInput
                ? CalibrationMark(calibrationLeftCastPassed[player]) + " Vänster kast testat\n"
                    + CalibrationMark(calibrationRightCastPassed[player]) + " Höger kast testat\n"
                : CalibrationMark(calibrationCastPassed[player]) + " Musklick testat\n";
            GUI.Label(new Rect(card.x + 16f, card.y + 12f, card.width - 32f, 32f),
                "SPELARE " + (player + 1), hudStyle);
            GUI.Label(new Rect(card.x + 18f, card.y + 55f, card.width - 36f, 170f),
                CalibrationMark(body) + " Kropp hittad\n"
                + CalibrationMark(hands) + " Händer och sikte\n"
                + attackTests
                + CalibrationMark(calibrationDuckPassed[player]) + " Duckning testad\n"
                + CalibrationMark(calibrationLeanPassed[player]) + " Sidoväjning testad",
                centerStyle);
        }

        private void DrawInterfaceReticles()
        {
            foreach (ReticleState reticle in reticles.Values)
            {
                float x = reticle.Position.x * Screen.width;
                float y = reticle.Position.y * Screen.height;
                GUI.DrawTexture(new Rect(x - 27f, y - 27f, 54f, 54f),
                    reticle.PlayerIndex == 1 ? playerTwoRing : playerOneRing);
                GUI.DrawTexture(new Rect(x - 28f, y + 32f, 56f, 8f), whiteTexture);
                GUI.DrawTexture(new Rect(x - 26f, y + 34f, 52f * reticle.Progress, 4f), goldTexture);
            }
        }

        private string CalibrationProgressText()
        {
            if (calibrationTrackingStable) return "SPÅRNINGEN ÄR STABIL";
            bool kinectInput = KinectInputActive();
            for (int player = 0; player < requestedPlayers; player++)
            {
                if (!currentPoses.ContainsKey(player))
                    return "STÄLL SPELARE " + (player + 1) + " MITT FRAMFÖR KINECT";
                if (calibrationVisibleHands[player] < (kinectInput ? 2 : 1))
                    return "VISA BÅDA HÄNDERNA TYDLIGT";
            }
            if (calibrationStableSeconds > 0.05f)
                return "BRA – HÅLL ER STILLA  "
                    + Mathf.CeilToInt(1.6f - calibrationStableSeconds) + " s";
            return "STÅ RAKT OCH HÅLL ER STILLA";
        }

        private static string CalibrationMark(bool complete)
        {
            return complete ? "✓" : "•";
        }

        private void DrawEnemyHealthBars()
        {
            foreach (GhostTarget target in GhostTarget.ActiveTargets
                .Where(item => item.IsTargetable && !item.IsBoss && item.MaxHealth > 1))
            {
                Vector3 viewport = rideCamera.WorldToViewportPoint(target.transform.position + Vector3.up * 2.15f);
                if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) continue;
                float x = viewport.x * Screen.width;
                float y = (1f - viewport.y) * Screen.height;
                GUI.DrawTexture(new Rect(x - 31f, y, 62f, 7f), whiteTexture);
                GUI.DrawTexture(new Rect(x - 29f, y + 2f, 58f * target.Health / target.MaxHealth, 3f), goldTexture);
            }
        }

        private string FinalGrade(int accuracy)
        {
            if (gameOver) return "D";
            int value = scores[0] + scores[1] + collectedRelics * 100 + wagonHealth * 40 + accuracy * 3;
            return value >= 1900 ? "S" : value >= 1400 ? "A" : value >= 950 ? "B" : value >= 550 ? "C" : "D";
        }

        private int CountMedals()
        {
            int totalShots = shots[0] + shots[1];
            int accuracy = totalShots > 0 ? Mathf.RoundToInt((hits[0] + hits[1]) * 100f / totalShots) : 0;
            int count = 0;
            if (!gameOver && wagonDamageTaken == 0) count++;
            if (accuracy >= 70) count++;
            if (collectedRelics >= TotalRelics) count++;
            if (leftHandShots.Sum() >= 5 && rightHandShots.Sum() >= 5) count++;
            if (secretRouteUnlocked) count++;
            if (breakablesDestroyed >= 4) count++;
            return count;
        }

        private string MedalText(int accuracy)
        {
            List<string> medals = new List<string>();
            if (!gameOver && wagonDamageTaken == 0) medals.Add(easyMode ? "TRYGG FÄRD" : "OSKADD VAGN");
            if (accuracy >= 70) medals.Add("SKARPSKYTT");
            if (collectedRelics >= TotalRelics) medals.Add("RELIKJÄGARE");
            if (leftHandShots.Sum() >= 5 && rightHandShots.Sum() >= 5) medals.Add("DUBBELHAND");
            if (secretRouteUnlocked) medals.Add("HEMLIGHETSFUNNEN");
            if (breakablesDestroyed >= 4) medals.Add("KROSSARE");
            return medals.Count == 0 ? "INGA ÄNNU" : string.Join("  •  ", medals);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.76f, 0.91f, 1f) } };
            centeredSmallStyle = new GUIStyle(smallStyle)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
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
                secretRouteUnlocked ? "VÄLJ VÄG – HEMLIG VÄG UPPLÅST" : "VÄLJ VÄG – VÄNSTER ÄR LÅST", centerStyle);
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
            return HauntedTextureFactory.Solid(color);
        }
    }
}