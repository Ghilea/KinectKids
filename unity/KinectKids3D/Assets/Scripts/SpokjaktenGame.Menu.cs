using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D
{
    public sealed partial class SpokjaktenGame
    {
        private void BeginCalibration()
        {
            activeHandSelector.Reset();
            poseCalibrations.Clear();
            currentPoses.Clear();
            reticles.Clear();
            handFilters.Clear();
            calibrationStableSeconds = 0f;
            calibrationUnstableSeconds = 0f;
            calibrationTrackingStable = false;
            interfaceDwellAction = null;
            interfaceDwellStartedAt = 0f;
            for (int player = 0; player < 2; player++)
            {
                calibrationCastPassed[player] = false;
                calibrationLeftCastPassed[player] = false;
                calibrationRightCastPassed[player] = false;
                calibrationDuckPassed[player] = false;
                calibrationLeanPassed[player] = false;
                calibrationDuckTime[player] = 0f;
                calibrationLeanTime[player] = 0f;
            }
            flow = GameFlow.Calibration;
            SavePreferences();
        }

        private void UpdateMenuAim()
        {
            IReadOnlyList<AimSample> samples = aimProvider.GetAimSamples();
            reticles.Clear();
            foreach (Light reticleLight in reticleLights.Values)
                if (reticleLight != null) reticleLight.enabled = false;
            foreach (AimSample rawSample in samples)
            {
                int player = Mathf.Clamp(rawSample.PlayerIndex, 0, 1);
                if (player >= requestedPlayers) continue;
                AimSample sample = SmoothHand(rawSample);
                Ray ray = rideCamera.ViewportPointToRay(
                    new Vector3(sample.Position.x, 1f - sample.Position.y, 0f));
                UpdateReticleLight(sample.HandId, player, ray);
                reticles[sample.HandId] = new ReticleState
                {
                    PlayerIndex = player,
                    Position = sample.Position,
                    IsRightHand = sample.HandId % 2 == 1,
                };
            }
            UpdateInterfaceDwell(false);
        }

        private void UpdateCalibrationDwell()
        {
            UpdateInterfaceDwell(true);
        }

        private void UpdateInterfaceDwell(bool calibration)
        {
            string hovered = null;
            foreach (ReticleState reticle in reticles.Values)
            {
                string action = calibration
                    ? CalibrationActionAt(reticle.Position)
                    : MenuActionAt(reticle.Position);
                if (!string.IsNullOrEmpty(action))
                {
                    hovered = action;
                    break;
                }
            }

            if (hovered != interfaceDwellAction)
            {
                interfaceDwellAction = hovered;
                interfaceDwellStartedAt = Time.time;
            }
            float progress = string.IsNullOrEmpty(hovered)
                ? 0f : Mathf.Clamp01((Time.time - interfaceDwellStartedAt) / 0.85f);
            foreach (int key in reticles.Keys.ToArray())
            {
                ReticleState state = reticles[key];
                string action = calibration
                    ? CalibrationActionAt(state.Position)
                    : MenuActionAt(state.Position);
                state.Progress = action == hovered ? progress : 0f;
                reticles[key] = state;
            }
            if (progress < 1f) return;

            interfaceDwellAction = null;
            interfaceDwellStartedAt = Time.time;
            if (calibration)
            {
                if (hovered == "back") flow = GameFlow.Menu;
                else if (hovered == "start" && calibrationTrackingStable) CompleteCalibration();
            }
            else
            {
                ExecuteMenuAction(hovered);
            }
        }

        private string MenuActionAt(Vector2 normalizedPosition)
        {
            float width = Mathf.Min(680f, Screen.width - 60f);
            float height = Mathf.Min(720f, Screen.height - 50f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            float rowY = y + 128f;
            Vector2 point = new Vector2(normalizedPosition.x * Screen.width,
                normalizedPosition.y * Screen.height);
            if (new Rect(x + 240f, rowY, 170f, 38f).Contains(point)) return "easy";
            if (new Rect(x + 420f, rowY, 170f, 38f).Contains(point)) return "normal";
            rowY += 55f;
            if (new Rect(x + 240f, rowY, 170f, 38f).Contains(point)) return "one";
            if (new Rect(x + 420f, rowY, 170f, 38f).Contains(point)) return "two";
            rowY = y + 440f;
            if (new Rect(x + 185f, rowY, 310f, 40f).Contains(point)) return "fullscreen";
            KinectAutoAimProvider automatic = aimProvider as KinectAutoAimProvider;
            float statusY = y + height - 176f;
            if (automatic != null && !automatic.KinectConnected
                && new Rect(x + 245f, statusY + 54f, 190f, 26f).Contains(point)) return "retry-kinect";
            if (new Rect(x + 45f, y + height - 92f, 170f, 58f).Contains(point)) return "launcher";
            if (new Rect(x + 225f, y + height - 92f, width - 270f, 58f).Contains(point)) return "calibrate";
            return null;
        }

        private string CalibrationActionAt(Vector2 normalizedPosition)
        {
            float width = Mathf.Min(760f, Screen.width - 50f);
            float height = Mathf.Min(650f, Screen.height - 40f);
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            Vector2 point = new Vector2(normalizedPosition.x * Screen.width,
                normalizedPosition.y * Screen.height);
            if (new Rect(x + 45f, y + height - 72f, 150f, 42f).Contains(point)) return "back";
            if (calibrationTrackingStable
                && new Rect(x + width - 315f, y + height - 78f, 270f, 52f).Contains(point)) return "start";
            return null;
        }

        private void ExecuteMenuAction(string action)
        {
            if (action == "easy") easyMode = true;
            else if (action == "normal") easyMode = false;
            else if (action == "one") requestedPlayers = 1;
            else if (action == "two") requestedPlayers = 2;
            else if (action == "fullscreen") Screen.fullScreen = !Screen.fullScreen;
            else if (action == "retry-kinect")
            {
                KinectAutoAimProvider automatic = aimProvider as KinectAutoAimProvider;
                if (automatic != null) automatic.RetryNow();
                return;
            }
            else if (action == "calibrate")
            {
                BeginCalibration();
                return;
            }
            else if (action == "launcher")
            {
                LauncherReturnService.ReturnToLauncher();
                return;
            }
            SavePreferences();
        }

        private void UpdateCalibration()
        {
            UpdatePlayerPoses();
            IReadOnlyList<AimSample> samples = aimProvider.GetAimSamples();
            reticles.Clear();
            foreach (Light reticleLight in reticleLights.Values)
                if (reticleLight != null) reticleLight.enabled = false;

            int[] handCounts = new int[2];
            foreach (AimSample rawSample in samples)
            {
                AimSample sample = SmoothHand(rawSample);
                int player = Mathf.Clamp(sample.PlayerIndex, 0, 1);
                if (player >= requestedPlayers) continue;
                handCounts[player]++;
                if (sample.Fire)
                {
                    calibrationCastPassed[player] = true;
                    if (sample.HandId % 2 == 1) calibrationRightCastPassed[player] = true;
                    else calibrationLeftCastPassed[player] = true;
                }
                if (!reticles.ContainsKey(sample.HandId))
                {
                    Ray ray = rideCamera.ViewportPointToRay(
                        new Vector3(sample.Position.x, 1f - sample.Position.y, 0f));
                    UpdateReticleLight(sample.HandId, player, ray);
                    reticles[sample.HandId] = new ReticleState
                    {
                        PlayerIndex = player,
                        Position = sample.Position,
                        Progress = sample.GestureProgress,
                        IsRightHand = sample.HandId % 2 == 1,
                    };
                }
            }

            calibrationVisibleHands[0] = handCounts[0];
            calibrationVisibleHands[1] = handCounts[1];

            bool neutralPose = currentPoses.Count >= requestedPlayers;
            bool handsFound = true;
            bool kinectInput = KinectInputActive();
            for (int player = 0; player < requestedPlayers; player++)
            {
                PoseState pose;
                if (!currentPoses.TryGetValue(player, out pose))
                {
                    neutralPose = false;
                    handsFound = false;
                    continue;
                }
                calibrationDuckTime[player] = pose.DuckAmount >= 0.15f
                    ? calibrationDuckTime[player] + Time.deltaTime
                    : Mathf.Max(0f, calibrationDuckTime[player] - Time.deltaTime * 2f);
                calibrationLeanTime[player] = Mathf.Abs(pose.LeanAmount) >= 0.14f
                    ? calibrationLeanTime[player] + Time.deltaTime
                    : Mathf.Max(0f, calibrationLeanTime[player] - Time.deltaTime * 2f);
                if (calibrationDuckTime[player] >= 0.30f) calibrationDuckPassed[player] = true;
                if (calibrationLeanTime[player] >= 0.30f) calibrationLeanPassed[player] = true;
                if (pose.DuckAmount > 0.08f || Mathf.Abs(pose.LeanAmount) > 0.11f)
                    neutralPose = false;
                if (handCounts[player] < (kinectInput ? 2 : 1)) handsFound = false;
            }

            if (!calibrationTrackingStable)
            {
                if (neutralPose && handsFound)
                {
                    calibrationStableSeconds += Time.deltaTime;
                    calibrationUnstableSeconds = 0f;
                }
                else
                {
                    calibrationUnstableSeconds += Time.deltaTime;
                    if (calibrationUnstableSeconds > 0.45f)
                        calibrationStableSeconds = Mathf.Max(0f,
                            calibrationStableSeconds - Time.deltaTime * 0.35f);
                }
                if (calibrationStableSeconds >= 1.6f) calibrationTrackingStable = true;
            }
        }

        private void CompleteCalibration()
        {
            activeHandSelector.Reset();
            SetDifficulty(easyMode);
            gameTime = 0f;
            paused = false;
            reticles.Clear();
            actionMessage = string.Empty;
            actionMessageUntil = 0f;
            flow = GameFlow.Countdown;
            SavePreferences();
        }
    }
}