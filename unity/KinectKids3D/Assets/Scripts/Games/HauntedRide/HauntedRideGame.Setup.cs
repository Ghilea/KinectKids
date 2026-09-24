using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D
{
    public sealed partial class HauntedRideGame
    {
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
            rideCamera.backgroundColor = new Color(0.003f, 0.0045f, 0.009f);
            rideCamera.fieldOfView = 63f;
            rideCamera.nearClipPlane = 0.06f;
            rideCamera.farClipPlane = 95f;
            rideCamera.allowHDR = true;
        }

        private void CreateInput()
        {
            if (global::KinectKids3D.Platform.KinectKidsInputManager.Instance != null)
            {
                aimProvider = new global::KinectKids3D.Platform.PlatformAimProvider();
                RefreshInputStatus();
                return;
            }
            var automatic = new KinectAutoAimProvider();
            automatic.Start();
            aimProvider = automatic;
            RefreshInputStatus();
        }

        private bool KinectInputActive()
        {
            KinectAutoAimProvider automatic = aimProvider as KinectAutoAimProvider;
            if (automatic != null) return automatic.KinectConnected;
            if (aimProvider is global::KinectKids3D.Platform.PlatformAimProvider)
                return global::KinectKids3D.Platform.KinectKidsInputManager.Instance != null
                    && global::KinectKids3D.Platform.KinectKidsInputManager.Instance.KinectConnected;
            return aimProvider is KinectBridgeAimProvider || aimProvider is KinectV1AimProvider;
        }

        private void RefreshInputStatus()
        {
            KinectAutoAimProvider automatic = aimProvider as KinectAutoAimProvider;
            if (automatic != null)
            {
                automatic.Tick();
                inputStatus = automatic.KinectConnected
                    ? automatic.Status + " – båda händerna har varsitt sikte och kastar framåt"
                    : automatic.Status + " – musen fungerar medan Kinect återansluter";
                return;
            }
            inputStatus = aimProvider != null ? aimProvider.Status : "Ingen inmatning hittades";
        }

        private void CreateAudio()
        {
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0;
            hitSounds = LoadClipSet("Audio/SFX/Impacts", "hit_",
                CreateTone("Träff", 640f, 0.10f, 0.20f));
            bossSounds = LoadNamedClips(CreateTone("Bossträff", 185f, 0.22f, 0.28f),
                "Audio/SFX/Impacts/horror_bass_01", "Audio/SFX/Impacts/horror_bass_02");
            castSounds = LoadClipSet("Audio/SFX/Magic", "magic_",
                CreateNoiseBurst("Magikast", 0.12f, 0.16f, 1101));
            movementSuccessSounds = LoadNamedClips(CreateTone("Undanmanöver", 880f, 0.22f, 0.20f),
                "Audio/SFX/Environment/success_bell");
            collisionSounds = LoadClipSet("Audio/SFX/Impacts", "slam_",
                CreateNoiseBurst("Krock", 0.34f, 0.26f, 9001));
            scareSounds = LoadNamedClips(CreateNoiseBurst("Överraskning", 0.25f, 0.11f, 4404),
                "Audio/SFX/Impacts/horror_high_01", "Audio/SFX/Impacts/horror_mid_01",
                "Audio/SFX/Environment/weird_01", "Audio/SFX/Environment/weird_03",
                "Audio/SFX/Environment/weird_05");
            hazardCueSounds = LoadNamedClips(CreateWarningSound(),
                "Audio/SFX/Environment/metal_03", "Audio/SFX/Environment/metal_08");
            chainRattleSounds = LoadNamedClips(CreateMetalRattle(),
                "Audio/SFX/Environment/chain_rattle", "Audio/SFX/Environment/metal_clank");
            batRushSounds = LoadNamedClips(CreateNoiseBurst("Fladdermöss", 0.72f, 0.14f, 7281),
                "Audio/SFX/Creatures/bat_wings");
            phantomSounds = LoadClipSet("Audio/SFX/Creatures", "ghost_moan_",
                CreateGhostVoice("Vålnad nära vagnen", 1.22f, 96f, 3108));

            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.clip = Resources.Load<AudioClip>("Audio/SFX/Ambience/ambient_horror") ?? CreateAmbience();
            ambienceSource.loop = true;
            ambienceSource.volume = 0.16f * effectsVolume;
            ambienceSource.spatialBlend = 0;
            ambienceSource.Play();

            musicSource = gameObject.AddComponent<AudioSource>();
            AudioClip licensedMusic = Resources.Load<AudioClip>("Audio/CustomRideMusic");
            if (licensedMusic == null) licensedMusic = Resources.Load<AudioClip>("Audio/RideMusic");
            musicSource.clip = licensedMusic != null ? licensedMusic : CreateRideMusic();
            musicSource.loop = true;
            musicSource.volume = (licensedMusic != null ? 1f : 0.62f) * musicVolume;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 0;
            musicSource.mute = false;
            musicSource.ignoreListenerPause = true;
            musicSource.bypassEffects = true;
            musicSource.bypassListenerEffects = true;
            musicSource.bypassReverbZones = true;
            AudioListener.pause = false;
            AudioListener.volume = 1f;
            StartCoroutine(StartMusicWhenReady(licensedMusic != null));

            ghostVoice = gameObject.AddComponent<AudioSource>();
            ghostVoice.playOnAwake = false;
            ghostVoice.spatialBlend = 0f;
            ghostSounds = phantomSounds;

            environmentVoice = gameObject.AddComponent<AudioSource>();
            environmentVoice.playOnAwake = false;
            environmentVoice.spatialBlend = 0f;

            announcerVoice = gameObject.AddComponent<AudioSource>();
            announcerVoice.playOnAwake = false;
            announcerVoice.spatialBlend = 0f;
            announcerVoice.volume = voiceVolume;
            announcerVoice.priority = 8;
            announcerVoice.bypassReverbZones = true;
            duckSuccessVoice = Resources.Load<AudioClip>("Audio/SFX/Voice/duck_success");
            dodgeSuccessVoice = Resources.Load<AudioClip>("Audio/SFX/Voice/dodge_success");
            bossSuccessVoice = Resources.Load<AudioClip>("Audio/SFX/Voice/boss_success");
            playerHitVoice = Resources.Load<AudioClip>("Audio/SFX/Voice/player_hit");
            wagonHitVoice = Resources.Load<AudioClip>("Audio/SFX/Voice/wagon_hit");
            ApplyAudioVolumes();
        }

        private void LoadPreferences()
        {
            easyMode = PlayerPrefs.GetInt(PrefsKeys.EasyMode, 1) != 0;
            requestedPlayers = Mathf.Clamp(PlayerPrefs.GetInt(PrefsKeys.Players, 1), 1, 2);
            musicVolume = Mathf.Clamp(PlayerPrefs.GetFloat(PrefsKeys.MusicVolume, 0.90f), 0f, 1.2f);
            musicMuted = PlayerPrefs.GetInt(PrefsKeys.MusicMuted, 0) != 0;
            effectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsKeys.EffectsVolume, 0.86f));
            voiceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsKeys.VoiceVolume, 1f));
            brightnessLevel = Mathf.Clamp(PlayerPrefs.GetFloat(PrefsKeys.Brightness, 1f), 0.65f, 1.55f);
            bool fullscreen = PlayerPrefs.GetInt(PrefsKeys.Fullscreen, Screen.fullScreen ? 1 : 0) != 0;
            if (Screen.fullScreen != fullscreen) Screen.fullScreen = fullscreen;
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetInt(PrefsKeys.EasyMode, easyMode ? 1 : 0);
            PlayerPrefs.SetInt(PrefsKeys.Players, requestedPlayers);
            PlayerPrefs.SetFloat(PrefsKeys.MusicVolume, musicVolume);
            PlayerPrefs.SetInt(PrefsKeys.MusicMuted, musicMuted ? 1 : 0);
            PlayerPrefs.SetFloat(PrefsKeys.EffectsVolume, effectsVolume);
            PlayerPrefs.SetFloat(PrefsKeys.VoiceVolume, voiceVolume);
            PlayerPrefs.SetFloat(PrefsKeys.Brightness, brightnessLevel);
            PlayerPrefs.SetInt(PrefsKeys.Fullscreen, Screen.fullScreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyInitialBrightness()
        {
            RenderSettings.ambientIntensity *= brightnessLevel;
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light != null && light.type != LightType.Directional)
                    light.intensity *= brightnessLevel;
        }

        private void ApplyAudioVolumes()
        {
            if (effects != null) effects.volume = effectsVolume;
            if (ghostVoice != null) ghostVoice.volume = effectsVolume;
            if (environmentVoice != null) environmentVoice.volume = effectsVolume;
            if (ambienceSource != null) ambienceSource.volume = 0.16f * effectsVolume;
            if (announcerVoice != null) announcerVoice.volume = voiceVolume;
        }

        private IEnumerator StartMusicWhenReady(bool importedMusic)
        {
            AudioClip clip = musicSource != null ? musicSource.clip : null;
            if (clip == null)
            {
                Debug.LogError("Spökjakten kunde inte skapa eller läsa in någon musik.");
                yield break;
            }

            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            float timeout = Time.realtimeSinceStartup + 8f;
            while (clip.loadState == AudioDataLoadState.Loading && Time.realtimeSinceStartup < timeout)
                yield return null;

            if (clip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogError("Musikfilen importerades men ljuddata kunde inte laddas: " + clip.name);
                yield break;
            }

            musicSource.Play();
            Debug.Log((importedMusic ? "Spökjakten spelar importerad musik: " : "Spökjakten spelar reservmusik: ")
                + clip.name + " | loadState=" + clip.loadState + " | playing=" + musicSource.isPlaying);
        }

        private void ResetRide()
        {
            // Listan innehåller dynamiska mål och reliker. Miljömonster byggs före
            // första ResetRide och får därför inte raderas här.
            foreach (GhostTarget target in targets.Where(item => item != null)) Destroy(target.gameObject);
            foreach (RideHazard hazard in hazards.Where(item => item != null)) Destroy(hazard.gameObject);
            targets.Clear();
            hazards.Clear();
            aimLocks.Clear();
            reticles.Clear();
            handFilters.Clear();
            poseCalibrations.Clear();
            currentPoses.Clear();
            scores[0] = scores[1] = 0;
            for (int player = 0; player < 2; player++)
            {
                lives[player] = easyMode ? 4 : 3;
                shots[player] = hits[player] = kills[player] = combos[player] = maxCombos[player] = 0;
                leftHandShots[player] = rightHandShots[player] = 0;
                rescueProgress[player] = 0;
                lastHitAt[player] = -10f;
            }
            wagonHealth = easyMode ? 7 : 5;
            startingWagonHealth = wagonHealth;
            wagonDamageTaken = 0;
            breakablesDestroyed = 0;
            secretsFound = 0;
            directorEvents = 0;
            secretRouteUnlocked = false;
            repairSigilSpawned = false;
            nextDirectorBeatAt = 7f;
            lastDirectorEncounter = -1;
            bossWeakPointUntil = 0f;
            collectedRelics = 0;
            nextZoneIndex = 0;
            nextMiniBossIndex = 0;
            zoneTitle = string.Empty;
            zoneTitleUntil = 0f;
            gameOver = false;
            recordSaved = false;
            bossPhase = 1;
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
            nextEncounterIndex = 0;
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
            routeCollectiblesAdded = false;
            RouteDoor.ResetAll();
            StalkingMonster.ResetStalker();
            hazards.Add(RideHazard.Create(HazardKind.Duck, 43f));
            hazards.Add(RideHazard.Create(HazardKind.DodgeLeft, 78f));
            hazards.Add(RideHazard.Create(HazardKind.DodgeRight, 111f));
            hazards.Add(RideHazard.Create(HazardKind.Duck, 143f));
            AddRelic(58f, 0, -2.8f, 0);
            AddRelic(121f, 0, 2.9f, 1);
            AddRelic(184f, 0, -2.4f, 2);
            targets.Add(SecretRouteSeal.Create(new Vector3(
                DarkRideWorld.TrackCenter(181f), 2.25f, 181f)));
            if (WagonDamageVisual.Instance != null)
                WagonDamageVisual.Instance.SetHealth(wagonHealth, startingWagonHealth, false);
            PositionCamera(0);
        }
    }
}