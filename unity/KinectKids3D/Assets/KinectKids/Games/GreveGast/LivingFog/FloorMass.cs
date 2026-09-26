using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    public sealed class FloorMass : MonoBehaviour
    {
        private const int Columns = 48;
        private const int Rows = 12;
        private Mesh mesh;
        private Vector3[] vertices;
        private Color[] colors;
        private float width;
        private float reachMultiplier = 1f;
        private bool corridor;

        public float Aggression { get; set; }

        public void Initialize(Material material, float wallWidth, bool corridorMode = false)
        {
            width = wallWidth;
            corridor = corridorMode;
            reachMultiplier = corridorMode ? 2.2f : 1f;
            mesh = LivingMassMesh.Create("Liquid floor body", Columns, Rows,
                out vertices, out colors);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 20;
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
                float x = (u - 0.5f) * width;
                float wave = LivingMassMesh.Wave(x, time, 17.5f);
                float reach = 1.85f + Aggression * 1.65f + wave * 3.1f +
                    Mathf.Sin(x * 0.82f + time * 0.51f) * 0.55f;
                float z = -0.19f - reach * reachMultiplier * v;
                float lift = Mathf.Sin(v * Mathf.PI) *
                    (0.12f + LivingMassMesh.Wave(x + v * 3f, time, 31f) * 0.23f);
                float alpha = 1f - LivingMassMesh.Smooth(0.57f, 1f, v);
                if (corridor)
                {
                    float wallBank = LivingMassMesh.Smooth(0.16f, 0.76f,
                        Mathf.Abs(x) / (width * 0.5f));
                    float distantCenter = (1f - LivingMassMesh.Smooth(0.08f, 0.57f, v)) * 0.88f;
                    alpha *= Mathf.Max(wallBank, distantCenter);
                }
                int index = row * (Columns + 1) + column;
                vertices[index] = new Vector3(x, 0.045f + lift, z);
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
