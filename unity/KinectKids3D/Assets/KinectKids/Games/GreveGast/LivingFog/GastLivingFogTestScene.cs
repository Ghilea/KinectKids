using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    public sealed class GastLivingFogTestScene : MonoBehaviour
    {
        public Shader massShader;

        private void Start()
        {
            if (massShader == null)
                massShader = Shader.Find("KinectKids/Greve Living Mass");
            if (massShader == null || !massShader.isSupported)
            {
                Debug.LogError("Greve Living Mass shader is missing or unsupported.");
                return;
            }

            RenderSettings.fog = false;
            RenderSettings.ambientLight = new Color(0.18f, 0.21f, 0.29f);
            BuildCamera();
            BuildRoom();

            GameObject root = new GameObject("GastLivingFog");
            GastLivingFog mass = root.AddComponent<GastLivingFog>();
            mass.Initialize(massShader, 12f, 6f);
            root.AddComponent<GastFogController>();
        }

        private void BuildCamera()
        {
            GameObject go = new GameObject("Living mass test camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 3.0f, -10.5f);
            go.transform.LookAt(new Vector3(0f, 2.5f, -0.7f));
            Camera camera = go.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.095f, 1f);
            if (FindFirstObjectByType<AudioListener>() == null)
                go.AddComponent<AudioListener>();
        }

        private void BuildRoom()
        {
            Shader lit = Shader.Find("Standard");
            Material wall = new Material(lit) { color = new Color(0.34f, 0.39f, 0.52f) };
            Material floor = new Material(lit) { color = new Color(0.22f, 0.26f, 0.36f) };
            Box("Single wall", new Vector3(0f, 3f, 0f),
                new Vector3(12f, 6f, 0.3f), wall);
            Box("Floor with infected edge", new Vector3(0f, -0.15f, -5f),
                new Vector3(12f, 0.3f, 10f), floor);

            GameObject lightGo = new GameObject("Cold inspection light");
            lightGo.transform.position = new Vector3(-2.8f, 4.4f, -5f);
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.65f, 0.76f, 1f);
            light.range = 19f;
            light.intensity = 2.1f;
        }

        private static void Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
