using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    // A solid, breathing silhouette behind Gast. The side and floor meshes grow
    // toward the camera; this surface keeps the distant corridor hidden.
    public sealed class RearMass : MonoBehaviour
    {
        private const int Columns = 64;
        private const int Rows = 28;
        private Mesh mesh;
        private Vector3[] vertices;
        private Color[] colors;
        private float width;
        private float height;

        public float Aggression { get; set; }

        public void Initialize(Material material, float corridorWidth, float corridorHeight)
        {
            width = corridorWidth;
            height = corridorHeight;
            mesh = LivingMassMesh.Create("Breathing rear mass", Columns, Rows,
                out vertices, out colors);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 19;
            Rebuild(0f);
        }

        private void Update()
        {
            if (mesh != null) Rebuild(Time.time);
        }

        private void Rebuild(float time)
        {
            for (int row = 0; row <= Rows; row++)
            for (int column = 0; column <= Columns; column++)
            {
                float u = column / (float)Columns;
                float v = row / (float)Rows;
                float y = -0.4f + v * (height + 1.5f);
                float side = u * 2f - 1f;
                float slow = Mathf.PerlinNoise(41f + y * 0.25f, time * 0.22f);
                float detail = Mathf.PerlinNoise(83f + y * 0.73f, time * 0.41f);
                float undulation = (slow - 0.5f) * 1.15f +
                    (detail - 0.5f) * 0.42f;
                float x = side * (width * 0.51f + undulation *
                    (0.65f + Aggression * 0.55f));

                // The center and the bottom are opaque. Only the outer several
                // metres dissolve, so the edge reads as a moving silhouette.
                float edge = Mathf.Abs(side);
                float feather = LivingMassMesh.Smooth(0.68f, 0.99f, edge);
                float tend = (slow - 0.5f) * 0.10f +
                    Mathf.Sin(y * 0.57f - time * 0.65f) * 0.025f;
                float alpha = 1f - LivingMassMesh.Smooth(0.69f + tend,
                    1.0f + tend, edge);
                float topFade = 1f - LivingMassMesh.Smooth(height - 2.8f,
                    height + 0.8f, y);
                alpha *= topFade;
                if (edge < 0.66f && y < height - 2.8f) alpha = 1f;

                int index = row * (Columns + 1) + column;
                vertices[index] = new Vector3(x, y, 2.3f +
                    (slow - 0.5f) * 0.18f);
                colors[index] = new Color(1f, 1f, 1f, alpha);
            }
            LivingMassMesh.Apply(mesh, vertices, colors);
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
