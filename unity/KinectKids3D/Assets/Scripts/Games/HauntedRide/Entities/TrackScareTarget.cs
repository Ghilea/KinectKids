using UnityEngine;

namespace KinectKids3D
{
    /// <summary>Aktiverar ett miljömonster först när vagnen är nära och straffar ett missat mål.</summary>
    public sealed class TrackScareTarget : MonoBehaviour
    {
        private GhostTarget target;
        private float trackZ;
        private bool activated;
        private bool punishIfMissed;
        private float activatedAt;

        public void Configure(float z, GhostTarget shootable, bool punish = true)
        {
            trackZ = z;
            target = shootable;
            punishIfMissed = punish;
            if (target != null) target.SetTargetable(false);
        }

        private void Update()
        {
            if (Camera.main == null || target == null) return;
            float gap = trackZ - Camera.main.transform.position.z;
            if (!activated && gap <= 28f && gap >= -1f)
            {
                activated = true;
                activatedAt = Time.time;
                target.SetTargetable(true);
            }
            if (gap >= -2f) return;
            if (punishIfMissed && target.Health > 0 && activated
                && Time.time - activatedAt >= 2.25f)
                HauntedRideGame.ReportMonsterEscape();
            Destroy(gameObject);
        }
    }
}
