using UnityEngine;

namespace KinectKids3D
{
    public static class LowPolyMeshFactory
    {
        public static GameObject CreateTatteredBody(Transform parent, string name, Material material,
            float height, float topRadius, float shoulderRadius, float bottomRadius, int seed)
        {
            const int segments = 14;
            float[] levels = { 0f, 0.18f, 0.53f, 0.78f, 1f };
            float[] radii = { bottomRadius, bottomRadius * 0.92f, shoulderRadius * 0.82f, shoulderRadius, topRadius };
            Vector3[] vertices = new Vector3[levels.Length * segments + 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            var random = new System.Random(seed);
            for (int ring = 0; ring < levels.Length; ring++)
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = segment / (float)segments * Mathf.PI * 2f;
                float ragged = ring == 0 ? (float)random.NextDouble() * 0.22f : 0f;
                float radius = radii[ring] * (1f + Mathf.Sin(angle * 3f + seed) * 0.045f);
                int index = ring * segments + segment;
                vertices[index] = new Vector3(Mathf.Cos(angle) * radius,
                    levels[ring] * height + ragged, Mathf.Sin(angle) * radius);
                uvs[index] = new Vector2(segment / (float)segments, levels[ring]);
            }

            int bottom = levels.Length * segments;
            int top = bottom + 1;
            vertices[bottom] = Vector3.zero;
            vertices[top] = Vector3.up * height;
            var triangles = new int[(levels.Length - 1) * segments * 6 + segments * 6];
            int t = 0;
            for (int ring = 0; ring < levels.Length - 1; ring++)
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                int a = ring * segments + segment;
                int b = ring * segments + next;
                int c = (ring + 1) * segments + segment;
                int d = (ring + 1) * segments + next;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                triangles[t++] = bottom; triangles[t++] = next; triangles[t++] = segment;
                int a = (levels.Length - 1) * segments + segment;
                int b = (levels.Length - 1) * segments + next;
                triangles[t++] = top; triangles[t++] = a; triangles[t++] = b;
            }

            Mesh mesh = new Mesh { name = name + " Mesh" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GameObject model = new GameObject(name);
            model.transform.SetParent(parent, false);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().material = new Material(material);
            return model;
        }

        public static GameObject CreateTaperedLimb(Transform parent, string name, Vector3 from,
            Vector3 to, float startWidth, float endWidth, Material material)
        {
            const int sides = 7;
            Vector3 direction = to - from;
            Vector3 forward = direction.normalized;
            Vector3 right = Vector3.Cross(forward, Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.9f
                ? Vector3.forward : Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, forward).normalized;
            Vector3[] vertices = new Vector3[sides * 2];
            int[] triangles = new int[sides * 6];
            for (int i = 0; i < sides; i++)
            {
                float angle = i / (float)sides * Mathf.PI * 2f;
                Vector3 radial = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                vertices[i] = from + radial * startWidth;
                vertices[sides + i] = to + radial * endWidth;
                int next = (i + 1) % sides;
                int ti = i * 6;
                triangles[ti] = i;
                triangles[ti + 1] = sides + i;
                triangles[ti + 2] = next;
                triangles[ti + 3] = next;
                triangles[ti + 4] = sides + i;
                triangles[ti + 5] = sides + next;
            }
            Mesh mesh = new Mesh { name = name + " Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GameObject limb = new GameObject(name);
            limb.transform.SetParent(parent, false);
            limb.AddComponent<MeshFilter>().sharedMesh = mesh;
            limb.AddComponent<MeshRenderer>().material = new Material(material);
            return limb;
        }
    }
}
