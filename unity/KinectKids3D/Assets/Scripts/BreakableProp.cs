using UnityEngine;

namespace KinectKids3D
{
    public enum BreakablePropKind
    {
        Urn,
        Crate,
        Portrait
    }

    /// <summary>Miljöföremål som kan skjutas sönder för en liten poängbonus.</summary>
    public sealed class BreakableProp : MonoBehaviour
    {
        public static void Create(float z, float localX, int route, BreakablePropKind kind)
        {
            GameObject root = new GameObject("Förstörbart föremål - " + kind);
            root.transform.position = new Vector3(DarkRideWorld.TrackCenter(z, route) + localX,
                kind == BreakablePropKind.Portrait ? 2.45f : 0.45f, z);
            root.transform.rotation = DarkRideWorld.TrackRotation(z, route);
            root.AddComponent<BreakableProp>().Build(kind);
            Vector3 center = kind == BreakablePropKind.Portrait ? Vector3.zero : new Vector3(0f, 0.35f, 0f);
            GhostTarget target = GhostTarget.AttachExisting(root, 1, center,
                kind == BreakablePropKind.Portrait ? 1.9f : 1.25f,
                kind == BreakablePropKind.Portrait ? 0.72f : 0.58f, TargetKind.Ghost);
            root.AddComponent<TrackScareTarget>().Configure(z, target, false);
        }

        private void Build(BreakablePropKind kind)
        {
            Material wood = DarkRideWorld.TexturedMaterial(new Color(0.27f, 0.13f, 0.055f),
                HauntedTextureFactory.OldWood(2100 + (int)kind), 0f);
            Material ceramic = DarkRideWorld.MaterialOf(new Color(0.18f, 0.22f, 0.21f), 0.18f);
            Material glow = DarkRideWorld.GlowMaterial(new Color(0.52f, 0.04f, 0.72f), 1.6f);
            if (kind == BreakablePropKind.Urn)
            {
                Add(PrimitiveType.Sphere, "Sprucken urna", Vector3.zero,
                    new Vector3(0.62f, 0.86f, 0.62f), ceramic);
                Add(PrimitiveType.Cylinder, "Urnans kant", new Vector3(0f, 0.50f, 0f),
                    new Vector3(0.42f, 0.12f, 0.42f), glow);
            }
            else if (kind == BreakablePropKind.Crate)
            {
                Add(PrimitiveType.Cube, "Murken låda", Vector3.zero, Vector3.one * 1.05f, wood);
                Add(PrimitiveType.Cube, "Lådans järnband", new Vector3(0f, 0f, -0.54f),
                    new Vector3(0.18f, 1.08f, 0.08f), ceramic);
            }
            else
            {
                Add(PrimitiveType.Cube, "Förbannad tavelram", Vector3.zero,
                    new Vector3(0.18f, 2.05f, 1.42f), wood);
                Add(PrimitiveType.Sphere, "Ansikte i tavlan", new Vector3(-0.11f, 0f, 0f),
                    new Vector3(0.10f, 0.72f, 0.55f), glow);
            }
        }

        private void Add(PrimitiveType type, string partName, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = new Material(material);
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
    }
}
