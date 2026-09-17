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
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(
                8f + Mathf.Sin(Time.time * 2.5f + phase) * 12f, 0f, 8f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(
                8f - Mathf.Sin(Time.time * 2.5f + phase) * 12f, 0f, -8f);
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
            if (!IsBoss)
            {
                string[] ghostModels =
                {
                    "Models/Quaternius/Ghost",
                    "Models/CuteMonsters/Ghost",
                    "Models/KenneyGraveyard/character-ghost"
                };
                string[] monsterModels =
                {
                    "Models/KenneyGraveyard/character-zombie",
                    "Models/KenneyGraveyard/character-skeleton",
                    "Models/KenneyGraveyard/character-vampire",
                    "Models/CuteMonsters/Demon",
                    "Models/CuteMonsters/GreenDemon",
                    "Models/CuteMonsters/Cthulhu",
                    "Models/CuteMonsters/Cyclops",
                    "Models/CuteMonsters/Skull",
                    "Models/CuteMonsters/Yeti"
                };
                string[] choices = Kind == TargetKind.Ghost ? ghostModels : monsterModels;
                string resourcePath = choices[Random.Range(0, choices.Length)];
                GameObject importedGhost = ImportedModelFactory.Create(
                    resourcePath, transform, Kind == TargetKind.Ghost
                        ? "Animerad spökvariant" : "Animerad monstervariant",
                    new Vector3(0f, 1.05f, 0f), 2.65f, Quaternion.Euler(0f, 180f, 0f),
                    "idle", "flying", "walk", "move", "attack");
                if (importedGhost != null)
                {
                    RegisterRenderers(importedGhost);
                    return;
                }
            }

            Color skin = Kind == TargetKind.Ghost
                ? new Color(0.68f, 0.73f, 0.71f)
                : new Color(0.58f, 0.66f, 0.44f);
            Material skinMaterial = Kind == TargetKind.Ghost
                ? DarkRideWorld.TexturedMaterial(skin, HauntedTextureFactory.GhostCloth(113), 0f)
                : DarkRideWorld.TexturedMaterial(skin, HauntedTextureFactory.RottenSkin(127), 0f);
            Material clothes = DarkRideWorld.TexturedMaterial(
                IsBoss ? new Color(0.24f, 0.29f, 0.43f) : new Color(0.38f, 0.24f, 0.34f),
                HauntedTextureFactory.TatteredCloth(IsBoss ? 166 : 151), 0f);
            Material eye = DarkRideWorld.GlowMaterial(new Color(0.88f, 0.22f, 0.025f), 1.7f);
            Material mouth = DarkRideWorld.MaterialOf(new Color(0.055f, 0.012f, 0.014f));
            Material bone = DarkRideWorld.MaterialOf(new Color(0.70f, 0.65f, 0.48f));
            Material blackIron = DarkRideWorld.MaterialOf(new Color(0.055f, 0.065f, 0.075f), 0.78f);
            float scale = IsBoss ? 1.38f : 1f;

            if (Kind == TargetKind.Ghost)
            {
                GameObject shroud = LowPolyMeshFactory.CreateTatteredBody(transform, "Vågig spöksvepning",
                    skinMaterial, 1.58f, 0.43f, 0.73f, 0.88f, 210 + Mathf.RoundToInt(phase * 10f));
                RegisterRenderer(shroud);
                AddPrimitive(PrimitiveType.Sphere, "Spökhuva", new Vector3(0, 1.72f, 0),
                    new Vector3(0.78f, 0.82f, 0.66f), skinMaterial);
                AddPrimitive(PrimitiveType.Sphere, "Huvans mörker", new Vector3(0, 1.69f, -0.52f),
                    new Vector3(0.53f, 0.57f, 0.16f), mouth);
                AddEye(-0.20f, 1.84f, eye, scale);
                AddEye(0.20f, 1.84f, eye, scale);
                AddPrimitive(PrimitiveType.Sphere, "Gapande mun", new Vector3(0, 1.53f, -0.64f),
                    new Vector3(0.27f, 0.19f, 0.08f), mouth);
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject upperArm = LowPolyMeshFactory.CreateTaperedLimb(transform, "Svepande spökarm",
                        new Vector3(side * 0.52f, 1.28f, 0f), new Vector3(side * 1.08f, 1.06f, -0.08f),
                        0.19f, 0.13f, skinMaterial);
                    RegisterRenderer(upperArm);
                    GameObject forearm = LowPolyMeshFactory.CreateTaperedLimb(transform, "Spökhand",
                        new Vector3(side * 1.08f, 1.06f, -0.08f), new Vector3(side * 1.38f, 0.92f, -0.22f),
                        0.13f, 0.025f, skinMaterial);
                    RegisterRenderer(forearm);
                }
            }
            else
            {
                GameObject torso = LowPolyMeshFactory.CreateTatteredBody(transform, "Formad trasig rock", clothes,
                    1.72f * scale, 0.42f * scale, 0.72f * scale, 0.58f * scale,
                    IsBoss ? 303 : 286 + Mathf.RoundToInt(phase * 8f));
                RegisterRenderer(torso);
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
                leftArm = CreateZombieArm(-1, scale, skinMaterial);
                rightArm = CreateZombieArm(1, scale, skinMaterial);
                CreateZombieLeg(-1, scale, clothes);
                CreateZombieLeg(1, scale, clothes);
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

        private Transform CreateZombieArm(int side, float scale, Material material)
        {
            GameObject pivot = new GameObject(side < 0 ? "Vänster axel" : "Höger axel");
            pivot.transform.SetParent(transform, false);
            pivot.transform.localPosition = new Vector3(side * 0.46f * scale, 1.43f * scale, -0.02f);

            Vector3 elbow = new Vector3(side * 0.42f * scale, -0.34f * scale, -0.13f);
            Vector3 hand = new Vector3(side * 0.68f * scale, -0.69f * scale, -0.39f);
            GameObject upper = LowPolyMeshFactory.CreateTaperedLimb(pivot.transform, "Benig överarm",
                Vector3.zero, elbow, 0.16f * scale, 0.12f * scale, material);
            GameObject lower = LowPolyMeshFactory.CreateTaperedLimb(pivot.transform, "Benig underarm",
                elbow, hand, 0.12f * scale, 0.055f * scale, material);
            RegisterRenderer(upper);
            RegisterRenderer(lower);
            return pivot.transform;
        }

        private void CreateZombieLeg(int side, float scale, Material material)
        {
            Vector3 hip = new Vector3(side * 0.24f * scale, 0.72f * scale, 0.04f);
            Vector3 knee = new Vector3(side * 0.30f * scale, 0.17f * scale, -0.02f);
            Vector3 ankle = new Vector3(side * 0.28f * scale, -0.35f, -0.08f);
            GameObject upper = LowPolyMeshFactory.CreateTaperedLimb(transform, "Trasigt byxben",
                hip, knee, 0.22f * scale, 0.16f * scale, material);
            GameObject lower = LowPolyMeshFactory.CreateTaperedLimb(transform, "Trasigt byxben",
                knee, ankle, 0.16f * scale, 0.11f * scale, material);
            RegisterRenderer(upper);
            RegisterRenderer(lower);
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

        private void RegisterRenderer(GameObject model)
        {
            Renderer renderer = model.GetComponent<Renderer>();
            if (renderer == null) return;
            renderers.Add(renderer);
            baseColors.Add(renderer.material.color);
        }

        private void RegisterRenderers(GameObject model)
        {
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderers.Add(renderer);
                baseColors.Add(renderer.material.color);
            }
        }
    }
}
