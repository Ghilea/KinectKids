using UnityEngine;

namespace KinectKids3D
{
    public sealed class MagicBolt : MonoBehaviour
    {
        private Vector3 start;
        private Vector3 end;
        private float startedAt;
        private const float TravelSeconds = 0.18f;

        public static void Launch(Vector3 start, Vector3 end, Color color)
        {
            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = "Kastad spökmagi";
            bolt.transform.localScale = Vector3.one * 0.16f;
            Collider collider = bolt.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            bolt.GetComponent<Renderer>().material = DarkRideWorld.GlowMaterial(color, 4f);
            MagicBolt motion = bolt.AddComponent<MagicBolt>();
            motion.start = start;
            motion.end = end;
            motion.startedAt = Time.time;
            bolt.transform.position = start;

            Light light = bolt.AddComponent<Light>();
            light.color = color;
            light.intensity = 2.5f;
            light.range = 3.5f;
            light.shadows = LightShadows.None;
        }

        public static void CreateImpact(Vector3 position, bool boss)
        {
            GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impact.name = boss ? "Kraftig bossträff" : "Magisk träffblixt";
            impact.transform.position = position;
            Collider collider = impact.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Color color = boss ? new Color(1f, 0.18f, 0.025f) : new Color(0.25f, 0.82f, 1f);
            impact.GetComponent<Renderer>().material = DarkRideWorld.GlowMaterial(color, 5f);
            MagicImpact animation = impact.AddComponent<MagicImpact>();
            animation.Configure(boss ? 0.75f : 0.48f);
            Light light = impact.AddComponent<Light>();
            light.color = color;
            light.intensity = boss ? 4.5f : 3.2f;
            light.range = boss ? 6f : 4f;
            light.shadows = LightShadows.None;
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.time - startedAt) / TravelSeconds);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            transform.position = Vector3.Lerp(start, end, eased)
                                 + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.18f;
            transform.localScale = Vector3.one * Mathf.Lerp(0.14f, 0.32f, t);
            if (t >= 1f) Destroy(gameObject);
        }
    }

    public sealed class MagicImpact : MonoBehaviour
    {
        private float startedAt;
        private float size;

        public void Configure(float maximumSize)
        {
            startedAt = Time.time;
            size = maximumSize;
            transform.localScale = Vector3.one * 0.05f;
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.time - startedAt) / 0.22f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.05f, size, Mathf.Sin(t * Mathf.PI));
            Light light = GetComponent<Light>();
            if (light != null) light.intensity *= 1f - Time.deltaTime * 7f;
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
