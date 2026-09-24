using UnityEngine;

namespace KinectKids3D
{
    /// <summary>Ett spöke som först endast syns i den mörka spegelytan.</summary>
    public sealed class MirrorScare : MonoBehaviour
    {
        private float trackZ;
        private Renderer[] apparitionRenderers;
        private GhostTarget target;
        private bool revealed;
        private float targetableSince = -1f;

        public static void Create(float z, int side, int route)
        {
            GameObject root = new GameObject("Hemsökt spegel");
            root.transform.position = new Vector3(DarkRideWorld.TrackCenter(z, route), 0f, z);
            root.transform.rotation = DarkRideWorld.TrackRotation(z, route);
            MirrorScare mirror = root.AddComponent<MirrorScare>();
            mirror.trackZ = z;
            mirror.Build(side < 0 ? -1 : 1);
        }

        private void Build(int side)
        {
            Material frame = DarkRideWorld.TexturedMaterial(new Color(0.25f, 0.12f, 0.04f),
                HauntedTextureFactory.OldWood(2501 + side), 0.15f);
            Material glass = DarkRideWorld.MaterialOf(new Color(0.035f, 0.07f, 0.10f), 0.92f);
            float x = side * 5.25f;
            Add("Svart spegelyta", new Vector3(x, 2.65f, 0f), new Vector3(0.10f, 2.45f, 1.75f), glass);
            Add("Övre spegelram", new Vector3(x - side * 0.08f, 3.95f, 0f), new Vector3(0.22f, 0.18f, 2.05f), frame);
            Add("Nedre spegelram", new Vector3(x - side * 0.08f, 1.35f, 0f), new Vector3(0.22f, 0.18f, 2.05f), frame);
            Add("Främre spegelram", new Vector3(x - side * 0.08f, 2.65f, -1.0f), new Vector3(0.22f, 2.75f, 0.18f), frame);
            Add("Bakre spegelram", new Vector3(x - side * 0.08f, 2.65f, 1.0f), new Vector3(0.22f, 2.75f, 0.18f), frame);

            GameObject apparition = ImportedModelFactory.Create(
                "Models/CuteMonsters/Ghost", transform, "Spöket i spegeln",
                new Vector3(x - side * 0.28f, 2.55f, 0f), 2.05f,
                Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f), "idle", "flying", "move");
            if (apparition == null) return;
            apparitionRenderers = apparition.GetComponentsInChildren<Renderer>(true);
            target = GhostTarget.AttachExisting(gameObject, 2,
                new Vector3(x - side * 0.28f, 2.55f, 0f), 2.5f, 0.85f, TargetKind.Ghost);
            target.SetTargetable(false);
            SetApparition(false);
        }

        private void Update()
        {
            if (Camera.main == null || target == null) return;
            float gap = trackZ - Camera.main.transform.position.z;
            if (!revealed && gap <= 17f && gap >= -1f)
            {
                revealed = true;
                targetableSince = Time.time;
                SetApparition(true);
                target.SetTargetable(true);
            }
            if (!revealed || gap >= -2f) return;
            if (target.Health > 0 && Time.time - targetableSince >= 2.25f)
                HauntedRideGame.ReportMonsterEscape();
            Destroy(gameObject);
        }

        private void SetApparition(bool visible)
        {
            if (apparitionRenderers == null) return;
            foreach (Renderer renderer in apparitionRenderers)
                if (renderer != null) renderer.enabled = visible;
        }

        private void Add(string partName, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = PrimitiveBuilder.Create(PrimitiveType.Cube, partName, transform, position, scale);
            part.GetComponent<Renderer>().material = new Material(material);
        }
    }
}
