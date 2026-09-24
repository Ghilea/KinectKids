using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectKids3D
{
    public sealed partial class HauntedRideGame
    {
        private void UpdateBossBattle()
        {
            if (!bossBattle || bossDefeated) return;
            GhostTarget boss = targets.FirstOrDefault(item => item != null && item.IsBoss && item.Health > 0);
            if (boss == null) return;

            if (bossPhase >= 2 && bossWeakPointUntil > 0f
                && Time.time >= bossWeakPointUntil && boss.IsTargetable)
            {
                boss.SetTargetable(false);
                bossWeakPointUntil = 0f;
            }

            if (bossProjectile != null)
            {
                if (bossProjectile.Arrived) ResolveBossAttack();
                return;
            }

            if (Time.time < nextBossAttackAt) return;
            HazardKind kind = (HazardKind)UnityEngine.Random.Range(0, 3);
            Vector3 start = boss.transform.position + new Vector3(0f, 2.15f, -0.6f);
            Vector3 end = rideCamera.transform.position + rideCamera.transform.forward * 1.15f;
            bossProjectile = BossProjectile.Create(kind, start, end, easyMode);
            if (bossPhase >= 2) boss.SetTargetable(false);
            if (bossPhase >= 2)
            {
                Vector3 shifted = boss.transform.position;
                shifted.x = DarkRideWorld.TrackCenter(shifted.z) + UnityEngine.Random.Range(-3.2f, 3.2f);
                boss.transform.position = shifted;
            }
            float delay = bossPhase == 1 ? UnityEngine.Random.Range(3.0f, 4.0f)
                : bossPhase == 2 ? UnityEngine.Random.Range(2.4f, 3.2f)
                : UnityEngine.Random.Range(1.8f, 2.6f);
            nextBossAttackAt = Time.time + delay;
            PlayRandom(effects, scareSounds);
        }

        private void UpdateBossPhase(GhostTarget boss)
        {
            int newPhase = boss.Health <= 5 ? 3 : boss.Health <= 10 ? 2 : 1;
            if (newPhase <= bossPhase) return;
            bossPhase = newPhase;
            boss.SetTargetable(false);
            bossWeakPointUntil = 0f;
            actionMessage = newPhase == 2
                ? "FAS 2 – KONDUKTÖREN KALLAR PÅ DE DÖDA!"
                : "FAS 3 – ALLA LJUS SLOCKNAR!";
            actionMessageUntil = Time.time + 2.4f;
            PlayRandom(effects, scareSounds, 1f);
            SummonBossMinions(newPhase);
            if (newPhase == 3) StartCoroutine(BossBlackout());
        }

        private void SummonBossMinions(int phase)
        {
            int count = phase == 2 ? 2 : 3;
            for (int i = 0; i < count; i++)
            {
                float lane = (i - (count - 1) * 0.5f) * 2.25f;
                float z = BossStopDistance + 10.5f + (i % 2) * 2f;
                TargetKind kind = i % 2 == 0 ? TargetKind.Ghost : TargetKind.Zombie;
                targets.Add(GhostTarget.Create(kind,
                    new Vector3(DarkRideWorld.TrackCenter(z) + lane, kind == TargetKind.Ghost ? 1f : 0.3f, z)));
            }
        }

        private IEnumerator BossBlackout()
        {
            Light[] torches = FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(light => light != null && light.name.IndexOf("Fackelljus", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            foreach (Light torch in torches) torch.enabled = false;
            yield return new WaitForSeconds(2.8f);
            foreach (Light torch in torches)
                if (torch != null) torch.enabled = true;
        }

        private void ResolveBossAttack()
        {
            bool anyoneFailed = false;
            foreach (KeyValuePair<int, PoseState> pair in currentPoses)
            {
                bool succeeds = bossProjectile.Kind == HazardKind.Duck
                    ? pair.Value.DuckAmount >= (easyMode ? 0.14f : 0.18f)
                    : bossProjectile.Kind == HazardKind.DodgeLeft
                        ? pair.Value.LeanAmount <= (easyMode ? -0.12f : -0.15f)
                        : pair.Value.LeanAmount >= (easyMode ? 0.12f : 0.15f);
                int player = Mathf.Clamp(pair.Key, 0, 1);
                if (succeeds)
                {
                    scores[player] += 35;
                    PlayRandom(effects, movementSuccessSounds);
                }
                else
                {
                    anyoneFailed = true;
                    scores[player] = Mathf.Max(0, scores[player] - 30);
                    DamagePlayer(player, 1);
                }
            }

            if (anyoneFailed || currentPoses.Count == 0)
            {
                if (currentPoses.Count == 0) DamageWagon(1, "BOSSEN TRÄFFADE VAGNEN!");
                PlayRandom(effects, collisionSounds);
                cameraShakeUntil = Time.time + 0.55f;
            }
            else
            {
                PlayAnnouncement(bossSuccessVoice);
                GhostTarget boss = targets.FirstOrDefault(item => item != null && item.IsBoss && item.Health > 0);
                if (boss != null)
                {
                    boss.SetTargetable(true);
                    bossWeakPointUntil = Time.time + (easyMode ? 3.4f : 2.7f);
                    actionMessage = "SVAG PUNKT ÖPPEN – ATTACKERA!";
                    actionMessageUntil = bossWeakPointUntil;
                    nextBossAttackAt = Mathf.Max(nextBossAttackAt, bossWeakPointUntil + 0.35f);
                }
            }
            Destroy(bossProjectile.gameObject);
            bossProjectile = null;
        }

        private void PlayAnnouncement(AudioClip clip, bool interrupt = false)
        {
            if (announcerVoice == null || clip == null) return;
            if (!interrupt && (announcerVoice.isPlaying || Time.time < nextAnnouncementAt)) return;
            if (interrupt) announcerVoice.Stop();
            announcerVoice.clip = clip;
            announcerVoice.pitch = 1f;
            announcerVoice.Play();
            nextAnnouncementAt = Time.time + 0.30f;
        }
    }
}