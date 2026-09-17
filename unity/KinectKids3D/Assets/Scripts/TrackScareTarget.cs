using UnityEngine;

namespace KinectKids3D
{
    /// <summary>Aktiverar ett miljömonster först när vagnen är nära och straffar ett missat mål.</summary>
    public sealed class TrackScareTarget : MonoBehaviour
    {
        private GhostTarget target;
        private float trackZ;
        private bool activated;

        public void Configure(float z, GhostTarget shootable)
        {
            trackZ = z;
            target = shootable;
            if (target != null) target.SetTargetable(false);
        }

        private void Update()
        {
            if (Camera.main == null || target == null) return;
            float gap = trackZ - Camera.main.transform.position.z;
            if (!activated && gap <= 24f && gap >= -2f)
            {
                activated = true;
                target.SetTargetable(true);
            }
            if (gap >= -4f) return;
            if (target.Health > 0) SpokjaktenGame.ReportMonsterEscape();
            Destroy(gameObject);
        }
    }
}
