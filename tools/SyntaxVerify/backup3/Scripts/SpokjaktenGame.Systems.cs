using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D
{
    public sealed partial class SpokjaktenGame
    {
        private void ReloadRide()
        {
            SavePreferences();
            enabled = false;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        private void UpdateDynamicMusic()
        {
            if (musicSource == null) return;
            float targetPitch = bossBattle ? 1f + bossPhase * 0.025f
                : Mathf.Max(combos[0], combos[1]) >= 10 ? 1.035f : 1f;
            musicSource.pitch = Mathf.Lerp(musicSource.pitch, targetPitch, Time.unscaledDeltaTime * 2f);
            float voiceDuck = announcerVoice != null && announcerVoice.isPlaying ? 0.48f : 1f;
            float targetVolume = musicMuted ? 0f
                : musicVolume * (bossBattle ? 1.08f : 0.92f) * voiceDuck;
            musicSource.volume = Mathf.Lerp(musicSource.volume, targetVolume, Time.unscaledDeltaTime * 4f);
        }

        private void AdjustBrightness(float change)
        {
            float previous = brightnessLevel;
            brightnessLevel = Mathf.Clamp(brightnessLevel + change, 0.65f, 1.55f);
            float ratio = brightnessLevel / previous;
            RenderSettings.ambientIntensity *= ratio;
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light != null && light.type != LightType.Directional) light.intensity *= ratio;
            actionMessage = "LJUSSTYRKA " + Mathf.RoundToInt(brightnessLevel * 100f) + "%";
            actionMessageUntil = Time.time + 1f;
            SavePreferences();
        }

        private void JumpToNextCheckpoint()
        {
            float[] checkpoints = { 48f, 106f, 148f, 189f, 230f, 308f, BossStopDistance };
            float next = checkpoints.FirstOrDefault(value => value > rideDistance + 2f);
            if (next <= 0f) return;
            rideDistance = next;
            if (rideDistance >= DarkRideWorld.BranchChoiceStart && selectedRoute == 0) SelectRoute(-1);
            actionMessage = "TESTHOPP " + Mathf.RoundToInt(rideDistance) + " m";
            actionMessageUntil = Time.time + 1.2f;
        }

        private void SaveRecord()
        {
            if (recordSaved) return;
            recordSaved = true;
            int totalScore = scores[0] + scores[1];
            PlayerPrefs.SetInt(PrefsKeys.HighScore,
                Mathf.Max(totalScore, PlayerPrefs.GetInt(PrefsKeys.HighScore, 0)));
            PlayerPrefs.SetInt(PrefsKeys.BestCombo,
                Mathf.Max(Mathf.Max(maxCombos[0], maxCombos[1]), PlayerPrefs.GetInt(PrefsKeys.BestCombo, 0)));
            PlayerPrefs.SetInt(PrefsKeys.BestRelics,
                Mathf.Max(collectedRelics, PlayerPrefs.GetInt(PrefsKeys.BestRelics, 0)));
            PlayerPrefs.SetInt(PrefsKeys.BestMedals,
                Mathf.Max(CountMedals(), PlayerPrefs.GetInt(PrefsKeys.BestMedals, 0)));
            PlayerPrefs.Save();
        }

        private void UpdateZoneStory()
        {
            if (nextZoneIndex >= ZoneDistances.Length || rideDistance < ZoneDistances[nextZoneIndex]) return;
            zoneTitle = ZoneTitles[nextZoneIndex];
            zoneTitleUntil = Time.time + 2.8f;
            nextZoneIndex++;
            PlayRandom(effects, doorCreakSoundsForZone(), 0.36f);
        }

        private AudioClip[] doorCreakSoundsForZone()
        {
            return LoadNamedClips(null, "Audio/SFX/Doors/floor_creak_01", "Audio/SFX/Doors/floor_creak_02");
        }

        private void SetDifficulty(bool easy)
        {
            easyMode = easy;
            for (int player = 0; player < 2; player++) lives[player] = easy ? 4 : 3;
            wagonHealth = easy ? 7 : 5;
            startingWagonHealth = wagonHealth;
            wagonDamageTaken = 0;
            if (WagonDamageVisual.Instance != null)
                WagonDamageVisual.Instance.SetHealth(wagonHealth, startingWagonHealth, false);
            actionMessage = easy
                ? "BARNLÄGE – INGA LIV OCH VAGNEN KAN INTE FÖRSTÖRAS"
                : "NORMALT LÄGE";
            actionMessageUntil = Time.time + 1.4f;
            SavePreferences();
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
    }
}