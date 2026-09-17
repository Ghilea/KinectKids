using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public enum HazardKind
    {
        Duck,
        DodgeLeft,
        DodgeRight
    }

    public sealed class RideHazard : MonoBehaviour
    {
        private readonly HashSet<int> successfulPlayers = new HashSet<int>();
        private Transform movingPart;
        private Transform tether;
        private float phase;
        private int wallSide;

        public HazardKind Kind { get; private set; }
        public float TrackZ { get; private set; }
        public bool Resolved { get; set; }
        public string Instruction => Kind == HazardKind.Duck
            ? "DUCKA FÖR SPINDELN!"
            : Kind == HazardKind.DodgeLeft
                ? "VÄJ VÄNSTER – KLO UR VÄGGEN!"
                : "VÄJ HÖGER – KLO UR VÄGGEN!";

        public static RideHazard Create(HazardKind kind, float z, int route = 0)
        {
            GameObject root = new GameObject("Kroppshinder - " + kind);
            root.transform.position = new Vector3(DarkRideWorld.TrackCenter(z, route), 0f, z);
            root.transform.rotation = DarkRideWorld.TrackRotation(z, route);
            RideHazard hazard = root.AddComponent<RideHazard>();
            hazard.Kind = kind;
            hazard.TrackZ = z;
            hazard.phase = Random.value * Mathf.PI * 2f;
            hazard.Build();
            return hazard;
        }

        public bool HasSucceeded(int playerIndex) => successfulPlayers.Contains(playerIndex);

        public bool MarkSuccess(int playerIndex)
        {
            return successfulPlayers.Add(playerIndex);
        }

        public void ResolveVisual(bool success)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            Color color = success ? new Color(0.1f, 1f, 0.42f) : new Color(1f, 0.12f, 0.08f);
            foreach (Renderer item in renderers)
            {
                item.material.EnableKeyword("_EMISSION");
                item.material.SetColor("_EmissionColor", color * 0.35f);
            }
        }

        private void Update()
        {
            if (movingPart == null) return;
            Camera camera = Camera.main;
            float gap = camera != null ? TrackZ - camera.transform.position.z : 20f;
            float approach = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((4.6f - gap) / 3.6f));
            if (Kind == HazardKind.Duck)
            {
                float spiderY = Mathf.Lerp(5.05f, 1.82f, approach);
                movingPart.localPosition = new Vector3(Mathf.Sin(Time.time * 1.7f + phase) * 0.22f,
                    spiderY, 0f);
                movingPart.localRotation = Quaternion.Euler(0f,
                    Mathf.Sin(Time.time * 1.2f + phase) * 12f,
                    Mathf.Sin(Time.time * 2.1f + phase) * 5f);
                if (tether != null)
                {
                    float length = 5.45f - spiderY;
                    tether.localPosition = new Vector3(0f, spiderY + length * 0.5f, 0f);
                    tether.localScale = new Vector3(0.025f, length * 0.5f, 0.025f);
                }
            }
            else
            {
                movingPart.localPosition = new Vector3(
                    wallSide * Mathf.Lerp(4.75f, 0.72f, approach),
                    1.38f + Mathf.Sin(Time.time * 3.4f + phase) * 0.08f,
                    0f);
                movingPart.localRotation = Quaternion.Euler(
                    Mathf.Sin(Time.time * 4f + phase) * 4f,
                    wallSide > 0 ? -90f : 90f,
                    Mathf.Sin(Time.time * 2.7f + phase) * 7f);
            }
        }

        private void Build()
        {
            Material eye = DarkRideWorld.GlowMaterial(new Color(0.78f, 0.025f, 0.012f), 1.9f);
            Material chitin = DarkRideWorld.TexturedMaterial(new Color(0.20f, 0.18f, 0.16f),
                HauntedTextureFactory.RustedMetal(77), 0.22f);
            Material stone = DarkRideWorld.TexturedMaterial(new Color(0.55f, 0.57f, 0.58f),
                HauntedTextureFactory.DampStone(92, new Color(0.25f, 0.27f, 0.28f),
                    new Color(0.025f, 0.028f, 0.030f)), 0.04f);
            Material web = DarkRideWorld.MaterialOf(new Color(0.34f, 0.35f, 0.34f));
            if (Kind == HazardKind.Duck)
            {
                GameObject thread = AddPrimitive(transform, PrimitiveType.Cylinder, "Lång spindeltråd",
                    new Vector3(0f, 5.35f, 0f), new Vector3(0.025f, 0.10f, 0.025f), web);
                tether = thread.transform;
                GameObject spider = new GameObject("Nedfirad slottsöspindel");
                spider.transform.SetParent(transform, false);
                movingPart = spider.transform;
                AddPrimitive(spider.transform, PrimitiveType.Sphere, "Spindelbakkropp",
                    new Vector3(0f, 0.08f, 0.20f), new Vector3(0.82f, 0.58f, 1.05f), chitin);
                AddPrimitive(spider.transform, PrimitiveType.Sphere, "Spindelhuvud",
                    new Vector3(0f, 0.02f, -0.62f), new Vector3(0.58f, 0.48f, 0.58f), chitin);
                for (int side = -1; side <= 1; side += 2)
                for (int leg = 0; leg < 4; leg++)
                {
                    float z = -0.44f + leg * 0.30f;
                    Vector3 hip = new Vector3(side * 0.38f, 0.05f, z);
                    Vector3 knee = new Vector3(side * (1.00f + leg * 0.08f), 0.34f - leg * 0.10f, z + 0.08f);
                    Vector3 foot = new Vector3(side * (1.70f - leg * 0.08f), -0.46f, z + 0.17f);
                    LowPolyMeshFactory.CreateTaperedLimb(spider.transform, "Spindelben", hip, knee,
                        0.105f, 0.075f, chitin);
                    LowPolyMeshFactory.CreateTaperedLimb(spider.transform, "Spindelben", knee, foot,
                        0.075f, 0.025f, chitin);
                }
                for (int eyeIndex = -1; eyeIndex <= 1; eyeIndex += 2)
                    AddPrimitive(spider.transform, PrimitiveType.Sphere, "Rött spindelöga",
                        new Vector3(eyeIndex * 0.18f, 0.10f, -1.12f), Vector3.one * 0.105f, eye);
                LowPolyMeshFactory.CreateTaperedLimb(spider.transform, "Vänster giftklo",
                    new Vector3(-0.20f, -0.12f, -0.92f), new Vector3(-0.34f, -0.40f, -1.25f),
                    0.09f, 0.025f, chitin);
                LowPolyMeshFactory.CreateTaperedLimb(spider.transform, "Höger giftklo",
                    new Vector3(0.20f, -0.12f, -0.92f), new Vector3(0.34f, -0.40f, -1.25f),
                    0.09f, 0.025f, chitin);
            }
            else
            {
                wallSide = Kind == HazardKind.DodgeLeft ? 1 : -1;
                AddPrimitive(transform, PrimitiveType.Cube, "Sprucken muröppning",
                    new Vector3(wallSide * 5.20f, 1.55f, 0f), new Vector3(0.32f, 2.75f, 2.45f), stone);
                GameObject attacker = new GameObject("Gargoyle som slår ur väggen");
                attacker.transform.SetParent(transform, false);
                movingPart = attacker.transform;
                AddPrimitive(attacker.transform, PrimitiveType.Sphere, "Gargoylehuvud", Vector3.zero,
                    new Vector3(0.72f, 0.64f, 0.68f), stone);
                AddPrimitive(attacker.transform, PrimitiveType.Cube, "Gapande stenkäke", new Vector3(0f, -0.28f, -0.37f),
                    new Vector3(0.56f, 0.23f, 0.43f), stone,
                    Quaternion.Euler(-13f, 0f, 0f));
                AddPrimitive(attacker.transform, PrimitiveType.Capsule, "Horn vänster", new Vector3(-0.38f, 0.48f, 0f),
                    new Vector3(0.13f, 0.46f, 0.13f), stone, Quaternion.Euler(0f, 0f, -35f));
                AddPrimitive(attacker.transform, PrimitiveType.Capsule, "Horn höger", new Vector3(0.38f, 0.48f, 0f),
                    new Vector3(0.13f, 0.46f, 0.13f), stone, Quaternion.Euler(0f, 0f, 35f));
                AddPrimitive(attacker.transform, PrimitiveType.Sphere, "Glödande öga vänster",
                    new Vector3(-0.22f, 0.10f, -0.58f), Vector3.one * 0.12f, eye);
                AddPrimitive(attacker.transform, PrimitiveType.Sphere, "Glödande öga höger",
                    new Vector3(0.22f, 0.10f, -0.58f), Vector3.one * 0.12f, eye);
                Vector3 shoulder = new Vector3(0f, -0.08f, 0.35f);
                Vector3 elbow = new Vector3(0f, -0.18f, -0.92f);
                Vector3 claw = new Vector3(0f, -0.24f, -2.05f);
                LowPolyMeshFactory.CreateTaperedLimb(attacker.transform, "Stenarm", shoulder, elbow,
                    0.34f, 0.24f, stone);
                LowPolyMeshFactory.CreateTaperedLimb(attacker.transform, "Lång stenklo", elbow, claw,
                    0.24f, 0.075f, stone);
                for (int finger = -1; finger <= 1; finger++)
                    LowPolyMeshFactory.CreateTaperedLimb(attacker.transform, "Klofingrar", claw,
                        claw + new Vector3(finger * 0.22f, -0.18f, -0.40f), 0.07f, 0.015f, stone);
            }
        }

        private static GameObject AddPrimitive(Transform parent, PrimitiveType type, string name,
            Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.transform.localRotation = rotation ?? Quaternion.identity;
            item.GetComponent<Renderer>().material = new Material(material);
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return item;
        }
    }
}
