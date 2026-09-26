using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    public sealed class EdgeMass : MonoBehaviour
    {
        private const int Columns = 44;
        private const int Rows = 7;
        private readonly Mesh[] meshes = new Mesh[5];
        private readonly Vector3[][] vertices = new Vector3[5][];
        private readonly Color[][] colors = new Color[5][];
        private float width;
        private float height;
        private int firstPart;
        private int partCount;
        private bool ready;

        public float Aggression { get; set; }

        public void Initialize(Material material, float wallWidth, float wallHeight,
            bool corridorMode = false)
        {
            ready = false;
            width = wallWidth;
            height = wallHeight;
            firstPart = corridorMode ? 3 : 0;
            partCount = corridorMode ? 5 : 3;
            for (int part = firstPart; part < partCount; part++)
            {
                string name = part == 0 ? "Wall bottom" : part == 1 ? "Left infected edge" :
                    part == 2 ? "Right infected edge" : part == 3 ? "Left wall body" :
                    "Right wall body";
                GameObject surface = new GameObject(name);
                surface.transform.SetParent(transform, false);
                meshes[part] = LivingMassMesh.Create(surface.name, Columns, Rows,
                    out vertices[part], out colors[part]);
                surface.AddComponent<MeshFilter>().sharedMesh = meshes[part];
                MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 20;
            }
            ready = true;
            Rebuild(0f);
        }

        private void Update()
        {
            if (ready && RestoreMeshData()) Rebuild(Time.time);
        }

        private bool RestoreMeshData()
        {
            // Unity can keep this component and its child MeshFilters during a
            // script reload while recreating the readonly managed arrays.
            // Recover those arrays before the next animated vertex update.
            for (int part = firstPart; part < partCount; part++)
            {
                if (meshes[part] != null && vertices[part] != null &&
                    colors[part] != null) continue;

                int childIndex = part - firstPart;
                if (childIndex >= transform.childCount) return false;
                MeshFilter filter = transform.GetChild(childIndex).GetComponent<MeshFilter>();
                if (filter == null) return false;

                Mesh mesh = filter.sharedMesh;
                if (mesh == null || mesh.vertexCount != (Columns + 1) * (Rows + 1))
                {
                    mesh = LivingMassMesh.Create(filter.gameObject.name, Columns, Rows,
                        out vertices[part], out colors[part]);
                    filter.sharedMesh = mesh;
                }
                else
                {
                    vertices[part] = mesh.vertices;
                    colors[part] = mesh.colors;
                    if (colors[part].Length != vertices[part].Length)
                        colors[part] = new Color[vertices[part].Length];
                }
                meshes[part] = mesh;
            }
            return true;
        }

        private void Rebuild(float time)
        {
            for (int part = firstPart; part < partCount; part++)
            {
                Vector3[] points = vertices[part];
                Color[] shades = colors[part];
                for (int row = 0; row <= Rows; row++)
                for (int column = 0; column <= Columns; column++)
                {
                    float u = column / (float)Columns;
                    float v = row / (float)Rows;
                    float x;
                    float y;
                    float alpha;
                    if (part == 0)
                    {
                        x = (u - 0.5f) * width;
                        float corner = LivingMassMesh.Smooth(0.50f, 0.95f,
                            Mathf.Abs(x) / (width * 0.5f));
                        float crest = 1.45f + Aggression * 0.75f + corner * 1.05f +
                            LivingMassMesh.Wave(x, time, 3.1f) * 1.25f +
                            Mathf.Sin(x * 1.12f + time * 0.7f) * 0.24f;
                        y = Mathf.Min(height - 0.15f, crest) * v;
                        alpha = 1f - LivingMassMesh.Smooth(0.54f, 1f, v);
                    }
                    else if (part < 3)
                    {
                        y = u * height;
                        float spread = 0.9f + Aggression * 0.65f +
                            LivingMassMesh.Wave(y, time, part * 7.8f) * 1.15f +
                            Mathf.Sin(y * 1.5f - time * 0.63f + part) * 0.18f;
                        x = (part == 1 ? -1f : 1f) *
                            (width * 0.5f - spread * v);
                        alpha = (1f - LivingMassMesh.Smooth(0.42f, 1f, v)) *
                            (1f - LivingMassMesh.Smooth(height - 0.55f, height, y));
                    }
                    else
                    {
                        float side = part == 3 ? -1f : 1f;
                        float z = -8f + u * 18f;
                        float invasion = LivingMassMesh.Smooth(0f, 0.58f, u);
                        float crest = height * (0.16f + invasion *
                            (0.42f + Aggression * 0.12f)) +
                            LivingMassMesh.Wave(z, time, part * 11.4f) * height * 0.15f;
                        x = side * (width * 0.5f - 0.25f);
                        y = Mathf.Min(height - 0.1f, crest) * v;
                        alpha = (1f - LivingMassMesh.Smooth(0.55f, 1f, v)) *
                            LivingMassMesh.Smooth(0f, 0.58f, u) *
                            (1f - LivingMassMesh.Smooth(0.82f, 1f, u));
                        int wallIndex = row * (Columns + 1) + column;
                        points[wallIndex] = new Vector3(x, y, z);
                        shades[wallIndex] = new Color(1f, 1f, 1f, alpha);
                        continue;
                    }
                    int index = row * (Columns + 1) + column;
                    points[index] = new Vector3(x, y, -0.19f - part * 0.012f);
                    shades[index] = new Color(1f, 1f, 1f, alpha);
                }
                LivingMassMesh.Apply(meshes[part], points, shades);
            }
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh);
        }
    }
}
