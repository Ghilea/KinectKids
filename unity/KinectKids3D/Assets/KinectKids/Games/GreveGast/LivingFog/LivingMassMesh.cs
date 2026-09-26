using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    internal static class LivingMassMesh
    {
        public static Mesh Create(string name, int columns, int rows, out Vector3[] vertices,
            out Color[] colors)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            vertices = new Vector3[(columns + 1) * (rows + 1)];
            colors = new Color[vertices.Length];
            int[] triangles = new int[columns * rows * 6];
            int index = 0;
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int a = y * (columns + 1) + x;
                int b = a + columns + 1;
                triangles[index++] = a;
                triangles[index++] = b;
                triangles[index++] = a + 1;
                triangles[index++] = a + 1;
                triangles[index++] = b;
                triangles[index++] = b + 1;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            return mesh;
        }

        public static void Apply(Mesh mesh, Vector3[] vertices, Color[] colors)
        {
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }

        public static float Smooth(float start, float end, float value)
        {
            float t = Mathf.Clamp01((value - start) / (end - start));
            return t * t * (3f - 2f * t);
        }

        public static float Wave(float x, float time, float seed)
        {
            float large = Mathf.PerlinNoise(seed + x * 0.38f, time * 0.22f);
            float detail = Mathf.PerlinNoise(seed + 19f + x * 1.05f, time * 0.44f);
            return large * 0.72f + detail * 0.28f;
        }

        public static Material Material(Shader shader, Color body, Color edge, float opacity = 1f)
        {
            Material material = new Material(shader);
            material.SetColor("_Color", body);
            material.SetColor("_RimColor", edge);
            material.SetFloat("_Opacity", opacity);
            return material;
        }
    }
}
