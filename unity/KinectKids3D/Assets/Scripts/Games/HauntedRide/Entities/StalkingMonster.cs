using UnityEngine;

namespace KinectKids3D
{
    /// <summary>Samma förföljare återkommer närmare vagnen tills spelarna besegrar den.</summary>
    public sealed class StalkingMonster : MonoBehaviour
    {
        private static bool banished;
        private float trackZ;
        private GhostTarget target;
        private bool activated;
        private float activatedAt;

        public static void ResetStalker()
        {
            banished = false;
        }

        public static void Create(float z, int side, int route, int appearance)
        {
            GameObject root = new GameObject("Den återkommande förföljaren " + appearance);
            root.transform.position = new Vector3(DarkRideWorld.TrackCenter(z, route) + side * (4.2f - appearance * 0.35f),
                0.25f, z);
            root.transform.rotation = DarkRideWorld.TrackRotation(z, route);
            StalkingMonster stalker = root.AddComponent<StalkingMonster>();
            stalker.trackZ = z;
            GameObject model = ImportedModelFactory.Create(
                "Models/CuteMonsters/Demon", root.transform, "Förföljaren",
                new Vector3(0f, 1.35f, 0f), 2.9f + appearance * 0.22f,
                Quaternion.Euler(0f, 180f, 0f), "walk", "attack", "idle");
            if (model == null) return;
            stalker.target = GhostTarget.AttachExisting(root, 2 + appearance,
                new Vector3(0f, 1.35f, 0f), 3.0f, 0.92f);
            stalker.target.SetTargetable(false);
        }

        private void Update()
        {
            if (banished)
            {
                Destroy(gameObject);
                return;
            }
            if (Camera.main == null || target == null) return;
            float gap = trackZ - Camera.main.transform.position.z;
            if (!activated && gap <= 28f && gap >= -1f)
            {
                activated = true;
                activatedAt = Time.time;
                target.SetTargetable(true);
            }
            if (gap >= -2f) return;
            if (target.Health > 0 && Time.time - activatedAt >= 2.25f)
                HauntedRideGame.ReportMonsterEscape();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (target != null && target.Health <= 0) banished = true;
        }
    }
}
