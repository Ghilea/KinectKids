using UnityEngine;

namespace KinectKids3D
{
    public sealed class DarkRideWorld
    {
        public const float TrackLength = 178f;
        private readonly Transform root;
        private readonly Material stone;
        private readonly Material darkStone;
        private readonly Material wood;
        private readonly Material rail;
        private readonly Material purpleGlow;
        private readonly Material greenGlow;
        private readonly Material amberGlow;

        public DarkRideWorld(Transform parent)
        {
            root = new GameObject("3D Dark Ride World").transform;
            root.SetParent(parent, false);
            stone = MaterialOf(new Color(0.13f, 0.15f, 0.22f));
            darkStone = MaterialOf(new Color(0.045f, 0.055f, 0.09f));
            wood = MaterialOf(new Color(0.20f, 0.10f, 0.055f));
            rail = MaterialOf(new Color(0.23f, 0.28f, 0.32f), 0.82f);
            purpleGlow = GlowMaterial(new Color(0.52f, 0.08f, 0.95f), 2.3f);
            greenGlow = GlowMaterial(new Color(0.05f, 1f, 0.46f), 2.1f);
            amberGlow = GlowMaterial(new Color(1f, 0.34f, 0.045f), 2.2f);
        }

        public void Build(Camera rideCamera)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.012f, 0.018f, 0.045f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.023f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.055f, 0.075f, 0.16f);
            RenderSettings.ambientEquatorColor = new Color(0.035f, 0.055f, 0.09f);
            RenderSettings.ambientGroundColor = new Color(0.015f, 0.018f, 0.025f);

            BuildTrack();
            BuildEntranceHall();
            BuildCrypt();
            BuildMonsterWorkshop();
            BuildFinalHall();
            BuildRideCar(rideCamera.transform);
        }

        public static float TrackCenter(float z)
        {
            return Mathf.Sin(z * 0.045f) * 2.5f + Mathf.Sin(z * 0.105f) * 0.65f;
        }

        public static Quaternion TrackRotation(float z)
        {
            float dx = TrackCenter(z + 0.5f) - TrackCenter(z - 0.5f);
            return Quaternion.Euler(0, Mathf.Atan2(dx, 1f) * Mathf.Rad2Deg, 0);
        }

        private void BuildTrack()
        {
            for (float z = 0; z <= TrackLength; z += 1.5f)
            {
                float center = TrackCenter(z);
                Quaternion rotation = TrackRotation(z);
                CreateCube("Sliper", new Vector3(center, 0.04f, z), new Vector3(2.7f, 0.12f, 0.25f), wood, rotation);
                CreateCube("Vänster räls", new Vector3(center - 0.72f, 0.18f, z), new Vector3(0.11f, 0.15f, 1.65f), rail, rotation);
                CreateCube("Höger räls", new Vector3(center + 0.72f, 0.18f, z), new Vector3(0.11f, 0.15f, 1.65f), rail, rotation);
            }

            for (float z = 3; z < TrackLength; z += 7.5f)
            {
                float center = TrackCenter(z);
                Quaternion rotation = TrackRotation(z);
                Material zone = z < 48 ? stone : z < 105 ? darkStone : stone;
                CreateCube("Golv", new Vector3(center, -0.22f, z), new Vector3(12f, 0.45f, 8f), zone, rotation);
                CreateCube("Vänster vägg", new Vector3(center - 5.8f, 2.8f, z), new Vector3(0.6f, 6f, 8.1f), zone, rotation);
                CreateCube("Höger vägg", new Vector3(center + 5.8f, 2.8f, z), new Vector3(0.6f, 6f, 8.1f), zone, rotation);
                CreateCube("Tak", new Vector3(center, 5.65f, z), new Vector3(12f, 0.5f, 8.1f), darkStone, rotation);
            }
        }

        private void BuildEntranceHall()
        {
            for (float z = 5; z < 48; z += 9f)
            {
                CreateArch(z, purpleGlow);
                CreateLamp(z + 2.5f, -4.6f, new Color(0.50f, 0.08f, 1f));
                CreateLamp(z + 5.5f, 4.6f, new Color(1f, 0.25f, 0.04f));
            }
            CreateSign(14f, "SPÖKJAKTEN 3D", purpleGlow);
            CreateFloatingOrb(32f, -3.4f, 1.5f, purpleGlow);
            CreateFloatingOrb(38f, 3.1f, 2.2f, greenGlow);
            CreateBatSwarm(25f, -2.6f, 3.35f);
            CreateBatSwarm(44f, 2.4f, 2.75f);
        }

        private void BuildCrypt()
        {
            for (float z = 52; z < 105; z += 8f)
            {
                CreateArch(z, greenGlow);
                float center = TrackCenter(z);
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject grave = CreateCube("Lysande gravsten", new Vector3(center + side * 3.5f, 0.55f, z + 2f),
                        new Vector3(0.85f, 1.15f, 0.28f), stone, Quaternion.Euler(0, side * 12f, side * 5f));
                    CreateSphere("Spökeld", grave.transform.position + Vector3.up * 0.95f, Vector3.one * 0.24f, greenGlow);
                }
            }
            CreateFloatingOrb(71f, -2.8f, 3.1f, purpleGlow);
            CreateFloatingOrb(92f, 2.9f, 2.4f, greenGlow);
            CreateBatSwarm(63f, 2.2f, 3.2f);
        }

        private void BuildMonsterWorkshop()
        {
            for (float z = 108; z < 146; z += 9f)
            {
                float center = TrackCenter(z);
                CreateCube("Rör vänster", new Vector3(center - 4.3f, 2.1f, z), new Vector3(0.35f, 4.2f, 0.35f), rail);
                CreateCube("Rör höger", new Vector3(center + 4.3f, 2.1f, z + 3f), new Vector3(0.35f, 4.2f, 0.35f), rail);
                GameObject leftBubble = CreateSphere("Giftbubbla", new Vector3(center - 3.3f, 0.6f, z + 2f), Vector3.one * 0.55f, greenGlow);
                GameObject rightBubble = CreateSphere("Giftbubbla", new Vector3(center + 3.5f, 1.2f, z + 5f), Vector3.one * 0.38f, purpleGlow);
                HauntedProp.Attach(leftBubble, HauntedMotion.Bob, 0.22f, 2.1f);
                HauntedProp.Attach(rightBubble, HauntedMotion.Bob, 0.18f, 2.8f);
                CreateLamp(z + 4f, z % 18 < 1 ? -4.6f : 4.6f, new Color(0.1f, 1f, 0.35f));
            }
            CreateBatSwarm(128f, -2.3f, 3.65f);
        }

        private void BuildFinalHall()
        {
            for (float z = 148; z < TrackLength; z += 6f) CreateArch(z, amberGlow);
            float endX = TrackCenter(173f);
            CreateCube("Bossportal", new Vector3(endX, 2.7f, 174f), new Vector3(9f, 5.4f, 0.7f), darkStone);
            CreateSphere("Portal vänster", new Vector3(endX - 3f, 2.5f, 173.5f), Vector3.one * 0.7f, amberGlow);
            CreateSphere("Portal höger", new Vector3(endX + 3f, 2.5f, 173.5f), Vector3.one * 0.7f, amberGlow);
        }

        private void CreateArch(float z, Material glow)
        {
            float center = TrackCenter(z);
            Quaternion rotation = TrackRotation(z);
            CreateCube("Valv vänster", new Vector3(center - 4.4f, 2.3f, z), new Vector3(0.75f, 4.8f, 0.8f), stone, rotation);
            CreateCube("Valv höger", new Vector3(center + 4.4f, 2.3f, z), new Vector3(0.75f, 4.8f, 0.8f), stone, rotation);
            CreateCube("Valv över", new Vector3(center, 4.65f, z), new Vector3(9.4f, 0.6f, 0.8f), stone, rotation);
            CreateSphere("Blacklight vänster", new Vector3(center - 3.6f, 4.45f, z - 0.45f), Vector3.one * 0.18f, glow);
            CreateSphere("Blacklight höger", new Vector3(center + 3.6f, 4.45f, z - 0.45f), Vector3.one * 0.18f, glow);
        }

        private void CreateLamp(float z, float localX, Color color)
        {
            Vector3 position = new Vector3(TrackCenter(z) + localX, 3.25f, z);
            CreateSphere("Lampa", position, Vector3.one * 0.22f, GlowMaterial(color, 2.4f));
            GameObject lamp = new GameObject("Punktljus");
            lamp.transform.SetParent(root, false);
            lamp.transform.position = position;
            Light light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 2.6f;
            light.range = 9f;
            light.shadows = LightShadows.None;
            HauntedProp.Attach(lamp, HauntedMotion.Flicker, 0f, Random.Range(6.5f, 10.5f));
        }

        private void CreateFloatingOrb(float z, float localX, float y, Material material)
        {
            Vector3 position = new Vector3(TrackCenter(z) + localX, y, z);
            GameObject orb = CreateSphere("Svävande spökljus", position, Vector3.one * 0.44f, material);
            HauntedProp.Attach(orb, HauntedMotion.Bob, 0.32f, Random.Range(1.4f, 2.4f));
        }

        private void CreateBatSwarm(float z, float localX, float y)
        {
            GameObject swarm = new GameObject("Flygande fladdermöss");
            swarm.transform.SetParent(root, false);
            swarm.transform.position = new Vector3(TrackCenter(z) + localX, y, z);
            Material batMaterial = GlowMaterial(new Color(0.22f, 0.08f, 0.38f), 1.15f);
            for (int i = 0; i < 5; i++)
            {
                GameObject bat = new GameObject("Fladdermus " + (i + 1));
                bat.transform.SetParent(swarm.transform, false);
                bat.transform.localPosition = new Vector3((i - 2) * 0.55f, (i % 2) * 0.35f, i * 0.28f);
                CreateChildCube(bat.transform, "Kropp", Vector3.zero, new Vector3(0.18f, 0.16f, 0.34f), batMaterial);
                CreateChildCube(bat.transform, "Vänster vinge", new Vector3(-0.28f, 0f, 0f),
                    new Vector3(0.48f, 0.06f, 0.22f), batMaterial);
                CreateChildCube(bat.transform, "Höger vinge", new Vector3(0.28f, 0f, 0f),
                    new Vector3(0.48f, 0.06f, 0.22f), batMaterial);
                HauntedProp.Attach(bat, HauntedMotion.Flutter, 0.35f + i * 0.025f, 2.7f + i * 0.3f);
            }
            HauntedProp.Attach(swarm, HauntedMotion.Flutter, 1.15f, 0.75f);
        }

        private void CreateSign(float z, string text, Material material)
        {
            GameObject sign = CreateCube(text, new Vector3(TrackCenter(z), 3.5f, z), new Vector3(5.6f, 1.05f, 0.25f), darkStone);
            for (int i = 0; i < 9; i++)
                CreateSphere("Skyltljus", sign.transform.position + new Vector3(-2.1f + i * 0.52f, 0, -0.17f), Vector3.one * 0.11f, material);
        }

        private void BuildRideCar(Transform cameraTransform)
        {
            GameObject car = new GameObject("Spökvagn");
            car.transform.SetParent(cameraTransform, false);
            CreateChildCube(car.transform, "Vagnkant", new Vector3(0, -0.85f, 1.35f), new Vector3(3.1f, 0.42f, 0.55f), wood);
            CreateChildCube(car.transform, "Vänster lykta", new Vector3(-1.2f, -0.58f, 1.08f), new Vector3(0.22f, 0.22f, 0.22f), amberGlow);
            CreateChildCube(car.transform, "Höger lykta", new Vector3(1.2f, -0.58f, 1.08f), new Vector3(0.22f, 0.22f, 0.22f), amberGlow);
        }

        private GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            value.name = name;
            value.transform.SetParent(root, false);
            value.transform.position = position;
            value.transform.localScale = scale;
            value.transform.rotation = rotation ?? Quaternion.identity;
            value.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(value.GetComponent<Collider>());
            return value;
        }

        private static void CreateChildCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localScale = scale;
            value.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(value.GetComponent<Collider>());
        }

        private GameObject CreateSphere(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            value.name = name;
            value.transform.SetParent(root, false);
            value.transform.position = position;
            value.transform.localScale = scale;
            value.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(value.GetComponent<Collider>());
            return value;
        }

        public static Material MaterialOf(Color color, float metallic = 0f)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", metallic > 0 ? 0.78f : 0.2f);
            return material;
        }

        public static Material GlowMaterial(Color color, float strength)
        {
            Material material = MaterialOf(color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * strength);
            return material;
        }
    }
}
