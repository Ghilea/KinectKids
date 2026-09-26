using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    public sealed class ProceduralTendril : MonoBehaviour
    {
        private const int ControlPoints = 13;
        private const int MeshRings = (ControlPoints - 1) * 3 + 1;
        private const int Sides = 9;
        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private Vector3[] vertices;
        private readonly Vector3[] control = new Vector3[ControlPoints];
        private Color[] colors;
        private Vector3 root;
        private Vector3 direction;
        private float length;
        private float thickness;
        private float amplitude;
        private float cycle;
        private float clock;
        private float seed;
        private bool floorOrigin;

        public float Aggression { get; set; }

        public void Initialize(Material material)
        {
            mesh = new Mesh { name = "Living tendril tube" };
            mesh.MarkDynamic();
            vertices = new Vector3[MeshRings * Sides];
            colors = new Color[vertices.Length];
            int[] triangles = new int[(MeshRings - 1) * Sides * 6];
            int cursor = 0;
            for (int ring = 0; ring < MeshRings - 1; ring++)
            for (int side = 0; side < Sides; side++)
            {
                int a = ring * Sides + side;
                int b = ring * Sides + (side + 1) % Sides;
                int c = (ring + 1) * Sides + side;
                int d = (ring + 1) * Sides + (side + 1) % Sides;
                triangles[cursor++] = a;
                triangles[cursor++] = c;
                triangles[cursor++] = b;
                triangles[cursor++] = b;
                triangles[cursor++] = c;
                triangles[cursor++] = d;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = 21;
        }

        public void Configure(Vector3 start, Vector3 travel, float baseLength,
            float baseThickness, float bend, float cycleSeconds, float phaseSeconds,
            float noiseSeed, bool fromFloor)
        {
            root = start;
            direction = travel.normalized;
            length = baseLength;
            thickness = baseThickness;
            amplitude = bend;
            cycle = cycleSeconds;
            clock = phaseSeconds;
            seed = noiseSeed;
            floorOrigin = fromFloor;
        }

        private void Update()
        {
            if (mesh == null || cycle <= 0f) return;
            clock += Time.deltaTime * (1f + Aggression * 0.95f);
            float phase = Mathf.Repeat(clock, cycle) / cycle;
            float extension = phase < 0.30f
                ? LivingMassMesh.Smooth(0f, 0.30f, phase)
                : phase < 0.78f ? 1f
                : 1f - LivingMassMesh.Smooth(0.78f, 1f, phase);
            meshRenderer.enabled = extension > 0.10f;
            if (!meshRenderer.enabled) return;

            float time = Time.time;
            Vector3 sideways = Vector3.Cross(direction, Vector3.up).normalized;
            if (sideways.sqrMagnitude < 0.001f) sideways = Vector3.right;
            for (int point = 0; point < ControlPoints; point++)
                control[point] = Point(point / (float)(ControlPoints - 1) * extension,
                    time, sideways);

            for (int ring = 0; ring < MeshRings; ring++)
            {
                float fraction = ring / (float)(MeshRings - 1);
                float along = fraction * extension;
                Vector3 center = Spline(fraction);
                Vector3 before = Spline(Mathf.Max(0f, fraction - 0.015f));
                Vector3 after = Spline(Mathf.Min(1f, fraction + 0.015f));
                Vector3 tangent = (after - before).normalized;
                if (tangent.sqrMagnitude < 0.001f) tangent = direction;
                Vector3 normal = Vector3.Cross(tangent, Vector3.up).normalized;
                if (normal.sqrMagnitude < 0.001f) normal = Vector3.right;
                Vector3 binormal = Vector3.Cross(tangent, normal).normalized;
                float pulse = 1f + Mathf.Sin(time * 3.2f + seed + along * 8f) * 0.17f;
                float growthThickness = LivingMassMesh.Smooth(0f, 0.55f, extension);
                float radius = thickness * (1f + Aggression * 0.48f) * pulse *
                    (0.35f + growthThickness * 0.65f) *
                    (1f - Mathf.Pow(fraction, 1.65f) * 0.94f);
                for (int side = 0; side < Sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / Sides;
                    int index = ring * Sides + side;
                    vertices[index] = center +
                        (normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle)) * radius;
                    colors[index] = new Color(1f, 1f, 1f, 1f);
                }
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }

        private Vector3 Point(float u, float time, Vector3 sideways)
        {
            float reach = length * (1f + Aggression * 0.72f);
            float life = Mathf.Sin(u * Mathf.PI * 0.88f);
            float slow = Mathf.PerlinNoise(seed + u * 1.9f, time * 0.48f) - 0.5f;
            float detail = Mathf.PerlinNoise(seed + 14f + u * 3.8f, time * 0.73f) - 0.5f;
            float coil = Mathf.Sin(u * 6.8f + time * 1.1f + seed);
            float bend = amplitude * (1f + Aggression * 0.65f) * life;
            Vector3 drift = sideways * (slow * 1.65f + coil * 0.76f) * bend +
                Vector3.up * (detail * 1.35f +
                    Mathf.Cos(u * 5.8f - time * 0.86f + seed) * 0.58f) * bend;
            if (floorOrigin) drift.y += Mathf.Sin(u * Mathf.PI) *
                (1.0f + Aggression * 0.65f);
            return root + direction * reach * u + drift;
        }

        private Vector3 Spline(float fraction)
        {
            float position = Mathf.Clamp01(fraction) * (ControlPoints - 1);
            int index = Mathf.Min(ControlPoints - 2, Mathf.FloorToInt(position));
            float t = position - index;
            Vector3 a = control[Mathf.Max(0, index - 1)];
            Vector3 b = control[index];
            Vector3 c = control[index + 1];
            Vector3 d = control[Mathf.Min(ControlPoints - 1, index + 2)];
            return 0.5f * ((2f * b) + (-a + c) * t +
                (2f * a - 5f * b + 4f * c - d) * t * t +
                (-a + 3f * b - 3f * c + d) * t * t * t);
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
