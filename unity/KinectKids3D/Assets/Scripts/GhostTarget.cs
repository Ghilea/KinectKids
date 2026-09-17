using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public enum TargetKind
    {
        Ghost,
        Zombie,
        ConductorBoss
    }

    public sealed class GhostTarget : MonoBehaviour
    {
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Color> baseColors = new List<Color>();
        private float baseY;
        private float phase;
        private bool defeated;
        private Transform leftArm;
        private Transform rightArm;

        public TargetKind Kind { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public bool IsBoss => Kind == TargetKind.ConductorBoss;

        public static GhostTarget Create(TargetKind kind, Vector3 position)
        {
            GameObject root = new GameObject(kind == TargetKind.ConductorBoss ? "Zombie-konduktören" : kind.ToString());
            root.transform.position = position;
            GhostTarget target = root.AddComponent<GhostTarget>();
            target.Kind = kind;
            target.MaxHealth = kind == TargetKind.ConductorBoss ? 16 : kind == TargetKind.Zombie ? 2 : 1;
            target.Health = target.MaxHealth;
            target.baseY = position.y;
            target.phase = Random.value * Mathf.PI * 2f;
            target.BuildModel();
            CapsuleCollider hitArea = root.AddComponent<CapsuleCollider>();
            hitArea.center = new Vector3(0, kind == TargetKind.ConductorBoss ? 1.65f : 1.05f, 0);
            hitArea.height = kind == TargetKind.ConductorBoss ? 3.8f : 2.55f;
            hitArea.radius = kind == TargetKind.ConductorBoss ? 1.15f : 0.75f;
            return target;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * (IsBoss ? 1.6f : 2.4f) + phase) * (IsBoss ? 0.11f : 0.18f);
            transform.position = new Vector3(transform.position.x, baseY + bob, transform.position.z);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(58f + Mathf.Sin(Time.time * 2.5f + phase) * 13f, 0, 68f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(58f - Mathf.Sin(Time.time * 2.5f + phase) * 13f, 0, -68f);
        }

        public int Hit()
        {
            if (defeated) return 0;
            Health--;
            StopAllCoroutines();
            StartCoroutine(HitFlash());
            int points = IsBoss ? 25 : 15;
            if (Health <= 0)
            {
                defeated = true;
                Collider hitArea = GetComponent<Collider>();
                if (hitArea != null) hitArea.enabled = false;
                points += IsBoss ? 250 : 35;
                StartCoroutine(Defeat());
            }
            return points;
        }

        private IEnumerator HitFlash()
        {
            for (int i = 0; i < renderers.Count; i++) renderers[i].material.color = Color.white;
            transform.localScale = Vector3.one * 1.12f;
            yield return new WaitForSeconds(0.11f);
            for (int i = 0; i < renderers.Count; i++) renderers[i].material.color = baseColors[i];
            transform.localScale = Vector3.one;
        }

        private IEnumerator Defeat()
        {
            float time = 0;
            Vector3 start = transform.localScale;
            while (time < 0.38f)
            {
                time += Time.deltaTime;
                transform.localScale = Vector3.Lerp(start, Vector3.zero, time / 0.38f);
                transform.Rotate(0, 480f * Time.deltaTime, 0);
                yield return null;
            }
            Destroy(gameObject);
        }

        private void BuildModel()
        {
            Color skin = Kind == TargetKind.Ghost
                ? new Color(0.72f, 0.88f, 1f)
                : new Color(0.26f, 0.80f, 0.55f);
            Material skinMaterial = DarkRideWorld.GlowMaterial(skin, Kind == TargetKind.Ghost ? 1.5f : 0.55f);
            Material clothes = DarkRideWorld.MaterialOf(IsBoss ? new Color(0.05f, 0.10f, 0.27f) : new Color(0.18f, 0.12f, 0.30f));
            Material eye = DarkRideWorld.GlowMaterial(new Color(1f, 0.52f, 0.04f), 3f);
            Material mouth = DarkRideWorld.MaterialOf(new Color(0.055f, 0.012f, 0.014f));
            Material bone = DarkRideWorld.MaterialOf(new Color(0.70f, 0.65f, 0.48f));
            Material blackIron = DarkRideWorld.MaterialOf(new Color(0.055f, 0.065f, 0.075f), 0.78f);
            float scale = IsBoss ? 1.38f : 1f;

            if (Kind == TargetKind.Ghost)
            {
                AddPrimitive(PrimitiveType.Sphere, "Spökhuvud", new Vector3(0, 1.75f, 0), new Vector3(0.9f, 0.9f, 0.72f), skinMaterial);
                AddPrimitive(PrimitiveType.Capsule, "Spökkropp", new Vector3(0, 0.82f, 0), new Vector3(0.82f, 1.05f, 0.66f), skinMaterial);
                AddEye(-0.20f, 1.84f, eye, scale);
                AddEye(0.20f, 1.84f, eye, scale);
                AddPrimitive(PrimitiveType.Sphere, "Gapande mun", new Vector3(0, 1.55f, -0.62f),
                    new Vector3(0.34f, 0.22f, 0.10f), mouth);
                for (int side = -1; side <= 1; side += 2)
                {
                    AddPrimitive(PrimitiveType.Capsule, "Spökarm", new Vector3(side * 0.78f, 1.05f, -0.05f),
                        new Vector3(0.20f, 0.72f, 0.20f), skinMaterial,
                        Quaternion.Euler(62f, 0f, side * 58f));
                    AddPrimitive(PrimitiveType.Sphere, "Spöksvans", new Vector3(side * 0.35f, -0.08f, 0),
                        new Vector3(0.38f, 0.62f, 0.36f), skinMaterial);
                }
            }
            else
            {
                AddPrimitive(PrimitiveType.Capsule, "Kropp", new Vector3(0, 0.92f * scale, 0), new Vector3(0.88f, 1.08f * scale, 0.64f), clothes);
                AddPrimitive(PrimitiveType.Sphere, "Huvud", new Vector3(0, 1.86f * scale, 0), new Vector3(0.87f, 0.92f, 0.76f), skinMaterial);
                AddEye(-0.20f * scale, 1.94f * scale, eye, scale);
                AddEye(0.20f * scale, 1.94f * scale, eye, scale);
                AddPrimitive(PrimitiveType.Sphere, "Insjunken näsa", new Vector3(0, 1.82f * scale, -0.68f),
                    new Vector3(0.17f, 0.24f, 0.18f), skinMaterial);
                AddPrimitive(PrimitiveType.Cube, "Mörkt gap", new Vector3(0, 1.62f * scale, -0.66f),
                    new Vector3(0.44f, 0.18f, 0.10f), mouth);
                for (int tooth = -1; tooth <= 1; tooth += 2)
                    AddPrimitive(PrimitiveType.Cube, "Trasig tand", new Vector3(tooth * 0.11f, 1.69f * scale, -0.73f),
                        new Vector3(0.075f, 0.12f, 0.06f), bone,
                        Quaternion.Euler(0f, 0f, tooth * 9f));
                leftArm = AddPrimitive(PrimitiveType.Capsule, "Vänster arm", new Vector3(-0.68f * scale, 1.22f * scale, -0.18f), new Vector3(0.28f, 0.72f * scale, 0.28f), skinMaterial).transform;
                rightArm = AddPrimitive(PrimitiveType.Capsule, "Höger arm", new Vector3(0.68f * scale, 1.22f * scale, -0.18f), new Vector3(0.28f, 0.72f * scale, 0.28f), skinMaterial).transform;
                AddPrimitive(PrimitiveType.Cube, "Trasig rock vänster", new Vector3(-0.29f * scale, 0.62f * scale, -0.42f),
                    new Vector3(0.50f, 0.72f * scale, 0.12f), clothes, Quaternion.Euler(0f, 0f, -7f));
                AddPrimitive(PrimitiveType.Cube, "Trasig rock höger", new Vector3(0.29f * scale, 0.59f * scale, -0.42f),
                    new Vector3(0.50f, 0.68f * scale, 0.12f), clothes, Quaternion.Euler(0f, 0f, 9f));
                AddPrimitive(PrimitiveType.Capsule, "Vänster ben", new Vector3(-0.27f * scale, 0.02f, 0), new Vector3(0.34f, 0.64f * scale, 0.34f), clothes);
                AddPrimitive(PrimitiveType.Capsule, "Höger ben", new Vector3(0.27f * scale, 0.02f, 0), new Vector3(0.34f, 0.64f * scale, 0.34f), clothes);
                AddPrimitive(PrimitiveType.Cube, "Vänster känga", new Vector3(-0.28f * scale, -0.40f, -0.18f),
                    new Vector3(0.38f, 0.25f, 0.62f), blackIron);
                AddPrimitive(PrimitiveType.Cube, "Höger känga", new Vector3(0.28f * scale, -0.40f, -0.18f),
                    new Vector3(0.38f, 0.25f, 0.62f), blackIron);
                if (IsBoss)
                {
                    AddPrimitive(PrimitiveType.Cylinder, "Konduktörsmössa", new Vector3(0, 2.50f, 0), new Vector3(0.75f, 0.13f, 0.75f), clothes);
                    AddPrimitive(PrimitiveType.Cube, "Mösskärm", new Vector3(0, 2.38f, -0.37f), new Vector3(0.88f, 0.09f, 0.40f), clothes);
                    AddPrimitive(PrimitiveType.Sphere, "Mössmärke", new Vector3(0, 2.51f, -0.67f), Vector3.one * 0.16f, eye);
                    AddPrimitive(PrimitiveType.Cube, "Järnskärp", new Vector3(0, 0.93f, -0.52f),
                        new Vector3(1.18f, 0.18f, 0.14f), blackIron);
                    for (int button = 0; button < 4; button++)
                        AddPrimitive(PrimitiveType.Sphere, "Mässingsknapp", new Vector3(0, 1.70f - button * 0.27f, -0.69f),
                            Vector3.one * 0.095f, eye);
                    AddPrimitive(PrimitiveType.Cube, "Sliten mantel", new Vector3(0, 1.15f, 0.42f),
                        new Vector3(1.35f, 1.95f, 0.16f), clothes,
                        Quaternion.Euler(-8f, 0f, 0f));
                    AddPrimitive(PrimitiveType.Capsule, "Förbannad stav", new Vector3(1.20f, 1.10f, -0.05f),
                        new Vector3(0.12f, 1.55f, 0.12f), blackIron);
                    AddPrimitive(PrimitiveType.Sphere, "Stavens glöd", new Vector3(1.20f, 2.58f, -0.05f),
                        Vector3.one * 0.30f, eye);
                }
            }
        }

        private void AddEye(float x, float y, Material material, float scale)
        {
            AddPrimitive(PrimitiveType.Sphere, "Öga", new Vector3(x, y, -0.39f * scale), Vector3.one * 0.16f * scale, material);
        }

        private GameObject AddPrimitive(PrimitiveType type, string objectName, Vector3 localPosition,
            Vector3 localScale, Material material, Quaternion? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = rotation ?? Quaternion.identity;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.material = new Material(material);
            renderers.Add(renderer);
            baseColors.Add(renderer.material.color);
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return part;
        }
    }
}
