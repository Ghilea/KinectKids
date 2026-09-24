using UnityEngine;

namespace KinectKids3D
{
    /// <summary>Ett dolt bonusmål som ger extra poäng och räknas på slutskärmen.</summary>
    public sealed class CursedRelic : MonoBehaviour
    {
        private Vector3 basePosition;
        private float phase;

        public static GhostTarget Create(Vector3 position, int variant)
        {
            GameObject root = new GameObject("Förbannad relik " + (variant + 1));
            root.transform.position = position;
            CursedRelic relic = root.AddComponent<CursedRelic>();
            relic.basePosition = position;
            relic.phase = variant * 1.17f;
            relic.Build(variant);
            return GhostTarget.AttachExisting(root, 1, Vector3.zero, 1.15f, 0.55f, TargetKind.Ghost);
        }

        private void Update()
        {
            transform.position = basePosition + Vector3.up * Mathf.Sin(Time.time * 2.4f + phase) * 0.16f;
            transform.Rotate(0f, 52f * Time.deltaTime, 0f, Space.Self);
        }

        private void Build(int variant)
        {
            Color color = variant % 3 == 0 ? new Color(0.12f, 0.92f, 0.42f)
                : variant % 3 == 1 ? new Color(0.72f, 0.10f, 1f)
                : new Color(1f, 0.42f, 0.04f);
            Material glow = DarkRideWorld.GlowMaterial(color, 3.4f);
            Material dark = DarkRideWorld.MaterialOf(new Color(0.035f, 0.025f, 0.045f), 0.55f);
            Add(PrimitiveType.Cylinder, "Relikens metallring", Vector3.zero,
                new Vector3(0.52f, 0.10f, 0.52f), dark, Quaternion.Euler(90f, 0f, 0f));
            Add(PrimitiveType.Sphere, "Relikens förbannade kärna", Vector3.zero,
                Vector3.one * 0.48f, glow, Quaternion.identity);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Add(PrimitiveType.Cube, "Svävande runbit",
                    new Vector3(Mathf.Cos(angle) * 0.68f, Mathf.Sin(angle * 2f) * 0.12f,
                        Mathf.Sin(angle) * 0.68f),
                    new Vector3(0.16f, 0.42f, 0.12f), glow,
                    Quaternion.Euler(i * 18f, i * 90f, i * 25f));
            }

            LightFactory.AddPoint(gameObject, color, 2.2f, 4.2f);
        }

        private void Add(PrimitiveType type, string partName, Vector3 position, Vector3 scale,
            Material material, Quaternion rotation)
        {
            GameObject part = PrimitiveBuilder.Create(type, partName, transform, position, scale, rotation);
            part.GetComponent<Renderer>().material = new Material(material);
        }
    }
}
