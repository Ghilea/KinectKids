using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    public sealed class WispsSmoke : MonoBehaviour
    {
        private const int Ribbons = 5;
        private const int Segments = 25;
        private Mesh mesh;
        private Vector3[] vertices;
        private Color[] colors;
        private float width;

        public float Aggression { get; set; }

        public void Initialize(Material material, float wallWidth)
        {
            width = wallWidth;
            mesh = new Mesh { name = "Continuous smoke ribbons" };
            mesh.MarkDynamic();
            vertices = new Vector3[Ribbons * (Segments + 1) * 2];
            colors = new Color[vertices.Length];
            int[] triangles = new int[Ribbons * Segments * 6];
            int cursor = 0;
            for (int ribbon = 0; ribbon < Ribbons; ribbon++)
            for (int segment = 0; segment < Segments; segment++)
            {
                int a = (ribbon * (Segments + 1) + segment) * 2;
                triangles[cursor++] = a;
                triangles[cursor++] = a + 2;
                triangles[cursor++] = a + 1;
                triangles[cursor++] = a + 1;
                triangles[cursor++] = a + 2;
                triangles[cursor++] = a + 3;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 22;
            Rebuild(0f);
        }

        private void Update()
        {
            if (mesh != null) Rebuild(Time.time);
        }

        private void Rebuild(float time)
        {
            for (int ribbon = 0; ribbon < Ribbons; ribbon++)
            for (int segment = 0; segment <= Segments; segment++)
            {
                float u = segment / (float)Segments;
                float side = ribbon % 2 == 0 ? -1f : 1f;
                float anchor = side * (width * 0.5f - 0.28f);
                float curl = Mathf.Sin(u * 7f - time * (0.54f + ribbon * 0.08f) + ribbon) *
                    (0.12f + u * 0.24f);
                float x = Mathf.Lerp(anchor, anchor - side * (2.2f + Aggression), u) + curl;
                float z = -0.28f - ribbon * 0.12f - u * (1.5f + ribbon * 0.24f);
                float y = 0.28f + ribbon * 0.10f +
                    Mathf.Sin(u * 9f + time * 0.63f + ribbon * 2f) * 0.19f +
                    Mathf.PerlinNoise(ribbon * 9f + u * 3f, time * 0.29f) * 0.28f;
                float halfHeight = (0.22f + ribbon * 0.035f) * Mathf.Sin(u * Mathf.PI);
                float alpha = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 1.4f) * 0.50f;
                int index = (ribbon * (Segments + 1) + segment) * 2;
                vertices[index] = new Vector3(x, y - halfHeight, z);
                vertices[index + 1] = new Vector3(x, y + halfHeight, z);
                colors[index] = new Color(1f, 1f, 1f, alpha);
                colors[index + 1] = new Color(1f, 1f, 1f, alpha * 0.46f);
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
