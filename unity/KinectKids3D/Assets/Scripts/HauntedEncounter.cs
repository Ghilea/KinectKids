using UnityEngine;

namespace KinectKids3D
{
    public enum HauntedEncounterKind
    {
        BatBurst,
        SwingingChain,
        PhantomFace
    }

    public sealed class HauntedEncounter : MonoBehaviour
    {
        private HauntedEncounterKind kind;
        private float startedAt;
        private int side;

        public static void Create(Transform cameraTransform, HauntedEncounterKind kind, int side)
        {
            GameObject root = new GameObject("Plötslig miljöhändelse - " + kind);
            root.transform.SetParent(cameraTransform, false);
            HauntedEncounter encounter = root.AddComponent<HauntedEncounter>();
            encounter.kind = kind;
            encounter.side = side < 0 ? -1 : 1;
            encounter.startedAt = Time.time;
            encounter.Build();
        }

        private void Update()
        {
            float age = Time.time - startedAt;
            if (kind == HauntedEncounterKind.BatBurst)
            {
                transform.localPosition = new Vector3(Mathf.Lerp(side * 5.5f, -side * 5.5f, age / 1.65f),
                    1.3f + Mathf.Sin(age * 11f) * 0.55f, Mathf.Lerp(7f, 2.8f, age / 1.65f));
            }
            else if (kind == HauntedEncounterKind.SwingingChain)
            {
                transform.localPosition = new Vector3(side * 2.7f, 2.9f, 4.6f);
                transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 8.5f) * 38f * Mathf.Clamp01(age * 3f));
            }
            else
            {
                float appear = age < 0.32f ? age / 0.32f : 1f - Mathf.Clamp01((age - 0.85f) / 0.55f);
                transform.localPosition = new Vector3(side * 2.3f, 0.15f, Mathf.Lerp(6.2f, 3.2f, Mathf.Clamp01(age)));
                transform.localScale = Vector3.one * Mathf.Max(0.02f, appear);
            }
            if (age > (kind == HauntedEncounterKind.SwingingChain ? 2.2f : 1.7f)) Destroy(gameObject);
        }

        private void Build()
        {
            Material black = DarkRideWorld.MaterialOf(new Color(0.008f, 0.006f, 0.012f));
            Material iron = DarkRideWorld.MaterialOf(new Color(0.10f, 0.09f, 0.08f), 0.75f);
            Material eye = DarkRideWorld.GlowMaterial(new Color(0.65f, 0.01f, 0.008f), 2.5f);
            if (kind == HauntedEncounterKind.BatBurst)
            {
                for (int i = 0; i < 8; i++)
                {
                    GameObject bat = new GameObject("Framrusande fladdermus");
                    bat.transform.SetParent(transform, false);
                    bat.transform.localPosition = new Vector3((i % 4) * 0.42f, (i / 4) * 0.45f, i * 0.18f);
                    Add(bat.transform, PrimitiveType.Sphere, "Kropp", Vector3.zero, new Vector3(0.16f, 0.12f, 0.30f), black);
                    Add(bat.transform, PrimitiveType.Cube, "Vingar", Vector3.zero, new Vector3(0.85f, 0.055f, 0.24f), black);
                    HauntedProp.Attach(bat, HauntedMotion.Flutter, 0.24f, 4.5f + i * 0.2f);
                }
            }
            else if (kind == HauntedEncounterKind.SwingingChain)
            {
                for (int i = 0; i < 12; i++)
                    Add(transform, PrimitiveType.Sphere, "Skramlande kedjelänk", new Vector3(0f, -i * 0.28f, 0f),
                        new Vector3(0.10f, 0.20f, 0.07f), iron);
            }
            else
            {
                Add(transform, PrimitiveType.Sphere, "Ansikte ur mörkret", new Vector3(0f, 1.7f, 0f),
                    new Vector3(0.92f, 1.12f, 0.25f), black);
                Add(transform, PrimitiveType.Sphere, "Vänster öga", new Vector3(-0.27f, 1.86f, -0.24f), Vector3.one * 0.13f, eye);
                Add(transform, PrimitiveType.Sphere, "Höger öga", new Vector3(0.27f, 1.86f, -0.24f), Vector3.one * 0.13f, eye);
                Add(transform, PrimitiveType.Sphere, "Gap", new Vector3(0f, 1.48f, -0.25f), new Vector3(0.35f, 0.22f, 0.08f), eye);
            }
        }

        private static void Add(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = new Material(material);
            Object.Destroy(part.GetComponent<Collider>());
        }
    }
}
