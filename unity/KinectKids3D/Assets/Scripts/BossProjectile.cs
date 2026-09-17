using UnityEngine;

namespace KinectKids3D
{
    public sealed class BossProjectile : MonoBehaviour
    {
        private Vector3 start;
        private Vector3 end;
        private float startedAt;
        private float duration;
        private Transform visual;

        public HazardKind Kind { get; private set; }
        public float Progress { get; private set; }
        public bool Arrived { get; private set; }
        public string Instruction => Kind == HazardKind.Duck
            ? "DUCKA!"
            : Kind == HazardKind.DodgeLeft ? "VÄJ ÅT VÄNSTER!" : "VÄJ ÅT HÖGER!";

        public static BossProjectile Create(HazardKind kind, Vector3 start, Vector3 end)
        {
            GameObject root = new GameObject("Bosskast - " + kind);
            BossProjectile projectile = root.AddComponent<BossProjectile>();
            projectile.Kind = kind;
            projectile.start = start;
            projectile.end = end;
            projectile.duration = 2.25f;
            projectile.startedAt = Time.time;
            projectile.Build();
            return projectile;
        }

        private void Update()
        {
            if (Arrived) return;
            Progress = Mathf.Clamp01((Time.time - startedAt) / duration);
            float eased = Progress * Progress * (3f - 2f * Progress);
            Vector3 arc = Vector3.up * Mathf.Sin(Progress * Mathf.PI) * 2.2f;
            transform.position = Vector3.Lerp(start, end, eased) + arc;
            transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 1.28f, eased);
            transform.Rotate(95f * Time.deltaTime, 145f * Time.deltaTime, 65f * Time.deltaTime, Space.Self);
            if (visual != null)
                visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 13f) * 0.08f, 0f);
            if (Progress >= 1f) Arrived = true;
        }

        private void Build()
        {
            Material rock = DarkRideWorld.MaterialOf(new Color(0.09f, 0.075f, 0.065f), 0.08f);
            Material glow = DarkRideWorld.GlowMaterial(
                Kind == HazardKind.Duck ? new Color(1f, 0.20f, 0.025f) : new Color(0.62f, 0.08f, 1f), 3.2f);
            GameObject skull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skull.name = "Förbannad stenskalle";
            skull.transform.SetParent(transform, false);
            skull.transform.localScale = new Vector3(1.0f, 0.82f, 0.78f);
            skull.GetComponent<Renderer>().material = rock;
            Destroy(skull.GetComponent<Collider>());
            visual = skull.transform;

            AddPart(PrimitiveType.Sphere, "Vänster glödande öga", new Vector3(-0.24f, 0.10f, -0.38f),
                Vector3.one * 0.18f, glow);
            AddPart(PrimitiveType.Sphere, "Höger glödande öga", new Vector3(0.24f, 0.10f, -0.38f),
                Vector3.one * 0.18f, glow);
            AddPart(PrimitiveType.Cube, "Gap", new Vector3(0f, -0.27f, -0.40f),
                new Vector3(0.46f, 0.17f, 0.12f), glow);
            for (int i = 0; i < 7; i++)
            {
                float angle = i / 7f * Mathf.PI * 2f;
                AddPart(PrimitiveType.Capsule, "Magisk svans", new Vector3(Mathf.Cos(angle) * 0.58f,
                    Mathf.Sin(angle) * 0.48f, 0.34f), new Vector3(0.10f, 0.34f, 0.10f), glow,
                    Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg));
            }
            Light light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Kind == HazardKind.Duck ? new Color(1f, 0.16f, 0.02f) : new Color(0.54f, 0.05f, 1f);
            light.range = 5.5f;
            light.intensity = 3.1f;
        }

        private void AddPart(PrimitiveType type, string partName, Vector3 position, Vector3 scale,
            Material material, Quaternion? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = rotation ?? Quaternion.identity;
            part.GetComponent<Renderer>().material = new Material(material);
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
    }
}
