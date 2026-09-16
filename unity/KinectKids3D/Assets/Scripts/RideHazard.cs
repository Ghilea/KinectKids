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
        private Vector3 basePosition;
        private Transform movingPart;
        private float phase;

        public HazardKind Kind { get; private set; }
        public float TrackZ { get; private set; }
        public bool Resolved { get; set; }
        public string Instruction => Kind == HazardKind.Duck
            ? "DUCKA!"
            : Kind == HazardKind.DodgeLeft ? "VÄJ ÅT VÄNSTER!" : "VÄJ ÅT HÖGER!";

        public static RideHazard Create(HazardKind kind, float z)
        {
            GameObject root = new GameObject("Kroppshinder - " + kind);
            root.transform.position = new Vector3(DarkRideWorld.TrackCenter(z), 0f, z);
            root.transform.rotation = DarkRideWorld.TrackRotation(z);
            RideHazard hazard = root.AddComponent<RideHazard>();
            hazard.Kind = kind;
            hazard.TrackZ = z;
            hazard.basePosition = root.transform.position;
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
                item.material.SetColor("_EmissionColor", color * 2.2f);
            }
        }

        private void Update()
        {
            if (movingPart == null) return;
            if (Kind == HazardKind.Duck)
            {
                movingPart.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Sin(Time.time * 2.7f + phase) * 9f);
            }
            else
            {
                float direction = Kind == HazardKind.DodgeLeft ? 1f : -1f;
                movingPart.localPosition = new Vector3(
                    direction * (0.7f + Mathf.Sin(Time.time * 2.1f + phase) * 0.42f),
                    1.15f + Mathf.Sin(Time.time * 3.4f + phase) * 0.13f,
                    0f);
                movingPart.localRotation = Quaternion.Euler(0f, Time.time * 75f * direction, 0f);
            }
        }

        private void Build()
        {
            Material warning = DarkRideWorld.GlowMaterial(new Color(1f, 0.18f, 0.04f), 2.7f);
            Material ghost = DarkRideWorld.GlowMaterial(new Color(0.3f, 0.85f, 1f), 1.8f);
            if (Kind == HazardKind.Duck)
            {
                GameObject pivot = new GameObject("Svängande spökbom");
                pivot.transform.SetParent(transform, false);
                pivot.transform.localPosition = new Vector3(0f, 2.05f, 0f);
                movingPart = pivot.transform;
                AddPrimitive(pivot.transform, PrimitiveType.Cube, "Lysande bom", Vector3.zero,
                    new Vector3(4.2f, 0.28f, 0.38f), warning);
                AddPrimitive(pivot.transform, PrimitiveType.Sphere, "Spöklykta vänster",
                    new Vector3(-2.05f, 0f, 0f), Vector3.one * 0.52f, ghost);
                AddPrimitive(pivot.transform, PrimitiveType.Sphere, "Spöklykta höger",
                    new Vector3(2.05f, 0f, 0f), Vector3.one * 0.52f, ghost);
            }
            else
            {
                GameObject attacker = new GameObject("Anflygande spöke");
                attacker.transform.SetParent(transform, false);
                movingPart = attacker.transform;
                AddPrimitive(attacker.transform, PrimitiveType.Sphere, "Huvud", new Vector3(0f, 0.55f, 0f),
                    new Vector3(1.05f, 0.9f, 0.72f), ghost);
                AddPrimitive(attacker.transform, PrimitiveType.Capsule, "Kropp", new Vector3(0f, -0.28f, 0f),
                    new Vector3(0.95f, 0.85f, 0.65f), ghost);
                AddPrimitive(attacker.transform, PrimitiveType.Sphere, "Varningsöga", new Vector3(0f, 0.6f, -0.38f),
                    Vector3.one * 0.25f, warning);
            }
        }

        private static void AddPrimitive(Transform parent, PrimitiveType type, string name,
            Vector3 position, Vector3 scale, Material material)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().material = new Material(material);
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
    }
}
