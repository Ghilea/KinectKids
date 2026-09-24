using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class DarkRideWorld
    {
        private void CreateArch(float z, Material glow, int route = 0)
        {
            float center = TrackCenter(z, route);
            Quaternion rotation = TrackRotation(z, route);
            CreateCube("Valv vänster", new Vector3(center - 4.4f, 2.3f, z), new Vector3(0.75f, 4.8f, 0.8f), stone, rotation);
            CreateCube("Valv höger", new Vector3(center + 4.4f, 2.3f, z), new Vector3(0.75f, 4.8f, 0.8f), stone, rotation);
            CreateCube("Valv över", new Vector3(center, 4.65f, z), new Vector3(9.4f, 0.6f, 0.8f), stone, rotation);
            CreateCube("Valvjärn vänster", new Vector3(center - 3.6f, 4.45f, z - 0.45f),
                new Vector3(0.18f, 0.18f, 0.24f), rail);
            CreateCube("Valvjärn höger", new Vector3(center + 3.6f, 4.45f, z - 0.45f),
                new Vector3(0.18f, 0.18f, 0.24f), rail);
        }

        private void CreateCastleGate(float z, int route = 0)
        {
            float center = TrackCenter(z, route);
            Quaternion rotation = TrackRotation(z, route);
            CreateCube("Slottsport vänster", new Vector3(center - 4.55f, 2.55f, z),
                new Vector3(1.35f, 5.3f, 1.25f), stone, rotation);
            CreateCube("Slottsport höger", new Vector3(center + 4.55f, 2.55f, z),
                new Vector3(1.35f, 5.3f, 1.25f), stone, rotation);
            CreateCube("Slottsport valv", new Vector3(center, 4.88f, z),
                new Vector3(10.2f, 1.0f, 1.25f), stone, rotation);
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
                CreateCube("Tinn", new Vector3(center + side * (3.65f + i * 0.52f), 5.75f, z),
                    new Vector3(0.38f, 0.72f, 1.10f), darkStone, rotation);
            CreateTorch(z - 0.7f, -1, route);
            CreateTorch(z - 0.7f, 1, route);
        }

        private void CreateBattlements(float z)
        {
            float center = TrackCenter(z);
            Quaternion rotation = TrackRotation(z);
            for (int side = -1; side <= 1; side += 2)
            {
                CreateCube("Borgmur", new Vector3(center + side * 5.2f, 2.35f, z),
                    new Vector3(1.05f, 4.9f, 7.8f), stone, rotation);
                for (int i = -2; i <= 2; i++)
                    CreateCube("Murtinne", new Vector3(center + side * 5.2f, 5.15f, z + i * 1.45f),
                        new Vector3(1.18f, 0.85f, 0.72f), darkStone, rotation);
            }
        }

        private void CreateCastleTower(float z, int side)
        {
            float x = TrackCenter(z) + side * 4.45f;
            GameObject tower = CreatePrimitive(PrimitiveType.Cylinder, "Runt slottstorn",
                new Vector3(x, 2.2f, z), new Vector3(2.15f, 2.35f, 2.15f), stone);
            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                CreateCube("Torntinne", new Vector3(x + Mathf.Cos(angle) * 1.55f, 5.05f,
                    z + Mathf.Sin(angle) * 1.55f), new Vector3(0.55f, 0.85f, 0.55f), darkStone,
                    Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f));
            }
        }

        private void CreateTorch(float z, int side, int route = 0)
        {
            float x = TrackCenter(z, route) + side * 5.15f;
            Vector3 position = new Vector3(x, 2.75f, z);
            GameObject importedTorch = ImportedModelFactory.Create(
                "Models/KayKit/torch_mounted", root, "Importerad väggfackla",
                position + new Vector3(-side * 0.10f, -0.22f, 0f), 1.25f,
                Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f));
            if (importedTorch == null)
            {
                CreateCube("Fackelhållare", position + new Vector3(-side * 0.18f, -0.28f, 0),
                    new Vector3(0.12f, 0.75f, 0.12f), rail,
                    Quaternion.Euler(0f, 0f, side * 20f));
                CreatePrimitive(PrimitiveType.Cylinder, "Fackelskål", position,
                    new Vector3(0.30f, 0.12f, 0.30f), rail);
            }
            GameObject flame = CreateSphere("Fackellåga", position + Vector3.up * 0.33f,
                new Vector3(0.28f, 0.55f, 0.28f), amberGlow);
            CreateSphere("Fackelkärna", position + Vector3.up * 0.28f,
                new Vector3(0.14f, 0.31f, 0.14f), GlowMaterial(new Color(1f, 0.78f, 0.18f), 3.4f));
            HauntedProp.Attach(flame, HauntedMotion.Flicker, 0.06f, Random.Range(9f, 13f));
            GameObject lightObject = new GameObject("Fackelljus");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.position = position + Vector3.up * 0.28f;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.30f, 0.055f);
            light.intensity = 5.3f;
            light.range = 9.4f;
            light.shadows = LightShadows.None;
            HauntedProp.Attach(lightObject, HauntedMotion.Flicker, 0f, Random.Range(7f, 11f));
        }

        private void CreateArmor(float z, int side, int route = 0)
        {
            GameObject armor = new GameObject("Kuslig riddarrustning");
            armor.transform.SetParent(root, false);
            armor.transform.position = new Vector3(TrackCenter(z, route) + side * 4.25f, 0.16f, z);
            armor.transform.rotation = TrackRotation(z, route) * Quaternion.Euler(0f, 180f + side * 12f, 0f);
            GameObject importedArmor = ImportedModelFactory.Create(
                "Models/QuaterniusKnight/KnightCharacter", armor.transform, "Animerad hemsökt riddarrustning",
                new Vector3(0f, 1.45f, 0f), 3.05f, Quaternion.identity, "idle", "stand");
            if (importedArmor != null)
            {
                HauntedProp.Attach(armor, HauntedMotion.Bob, 0.025f, 0.72f);
                return;
            }
            Material blackIron = MaterialOf(new Color(0.075f, 0.085f, 0.095f), 0.88f);
            Material edge = MaterialOf(new Color(0.27f, 0.24f, 0.19f), 0.74f);
            Material eye = GlowMaterial(new Color(0.82f, 0.018f, 0.008f), 3f);
            CreateChildPrimitive(armor.transform, PrimitiveType.Capsule, "Bröstplåt",
                new Vector3(0, 1.55f, 0), new Vector3(0.82f, 0.78f, 0.48f), blackIron);
            CreateChildPrimitive(armor.transform, PrimitiveType.Sphere, "Hjälm",
                new Vector3(0, 2.42f, 0), new Vector3(0.62f, 0.66f, 0.58f), blackIron);
            CreateChildPrimitive(armor.transform, PrimitiveType.Cube, "Visir",
                new Vector3(0, 2.38f, -0.51f), new Vector3(0.70f, 0.16f, 0.12f), edge);
            CreateChildPrimitive(armor.transform, PrimitiveType.Sphere, "Glödande springa",
                new Vector3(0, 2.39f, -0.59f), new Vector3(0.35f, 0.055f, 0.055f), eye);
            for (int armSide = -1; armSide <= 1; armSide += 2)
            {
                CreateChildPrimitive(armor.transform, PrimitiveType.Sphere, "Axelplåt",
                    new Vector3(armSide * 0.68f, 1.82f, 0), new Vector3(0.42f, 0.32f, 0.48f), edge);
                CreateChildPrimitive(armor.transform, PrimitiveType.Capsule, "Pansararm",
                    new Vector3(armSide * 0.73f, 1.23f, 0), new Vector3(0.27f, 0.56f, 0.27f), blackIron,
                    Quaternion.Euler(0f, 0f, armSide * 8f));
                CreateChildPrimitive(armor.transform, PrimitiveType.Capsule, "Pansarben",
                    new Vector3(armSide * 0.28f, 0.52f, 0), new Vector3(0.31f, 0.65f, 0.33f), blackIron);
                CreateChildPrimitive(armor.transform, PrimitiveType.Cube, "Järnsko",
                    new Vector3(armSide * 0.28f, 0.04f, -0.18f), new Vector3(0.38f, 0.22f, 0.62f), edge);
            }
            CreateChildPrimitive(armor.transform, PrimitiveType.Capsule, "Hillebard",
                new Vector3(-0.95f, 1.4f, 0), new Vector3(0.10f, 1.65f, 0.10f), wood);
            CreateChildPrimitive(armor.transform, PrimitiveType.Cube, "Hillebardsblad",
                new Vector3(-0.95f, 2.95f, 0), new Vector3(0.58f, 0.42f, 0.10f), edge,
                Quaternion.Euler(0f, 0f, -28f));
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
            light.intensity = 3.2f;
            light.range = 10f;
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
                GameObject importedBat = ImportedModelFactory.Create(
                    "Models/Quaternius/Bat", bat.transform, "Animerad fladdermus",
                    Vector3.zero, 0.82f, Quaternion.Euler(0f, 180f, 0f), "fly", "flying", "idle");
                if (importedBat == null)
                {
                    CreateChildCube(bat.transform, "Kropp", Vector3.zero, new Vector3(0.18f, 0.16f, 0.34f), batMaterial);
                    CreateChildCube(bat.transform, "Vänster vinge", new Vector3(-0.28f, 0f, 0f),
                        new Vector3(0.48f, 0.06f, 0.22f), batMaterial);
                    CreateChildCube(bat.transform, "Höger vinge", new Vector3(0.28f, 0f, 0f),
                        new Vector3(0.48f, 0.06f, 0.22f), batMaterial);
                }
                HauntedProp.Attach(bat, HauntedMotion.Flutter, 0.35f + i * 0.025f, 2.7f + i * 0.3f);
            }
            HauntedProp.Attach(swarm, HauntedMotion.Flutter, 1.15f, 0.75f);
            GhostTarget target = GhostTarget.AttachExisting(swarm, 2,
                new Vector3(0f, 0.25f, 0.55f), 1.9f, 1.55f);
            swarm.AddComponent<TrackScareTarget>().Configure(z, target);
        }

        private void CreateCeilingSpider(float z, int side, int route)
        {
            GameObject spider = new GameObject("Takspindel som kan skjutas");
            spider.transform.SetParent(root, false);
            spider.transform.position = new Vector3(TrackCenter(z, route) + side * 2.8f, 4.45f, z);
            GameObject imported = ImportedModelFactory.Create(
                "Models/Quaternius/Spider", spider.transform, "Animerad jättespindel",
                Vector3.zero, 1.75f, Quaternion.Euler(180f, 0f, 0f), "walk", "attack", "idle");
            if (imported == null)
            {
                Material body = MaterialOf(new Color(0.055f, 0.025f, 0.065f), 0.25f);
                CreateChildPrimitive(spider.transform, PrimitiveType.Sphere, "Spindelkropp", Vector3.zero,
                    new Vector3(0.72f, 0.38f, 0.92f), body);
                for (int leg = -1; leg <= 1; leg += 2)
                for (int row = 0; row < 4; row++)
                    CreateChildPrimitive(spider.transform, PrimitiveType.Capsule, "Spindelben",
                        new Vector3(leg * 0.62f, 0f, -0.48f + row * 0.32f),
                        new Vector3(0.10f, 0.65f, 0.10f), body,
                        Quaternion.Euler(0f, 0f, leg * 68f));
            }
            HauntedProp.Attach(spider, HauntedMotion.Flutter, 0.12f, 2.2f);
            GhostTarget target = GhostTarget.AttachExisting(spider, 2, Vector3.zero, 1.8f, 0.95f);
            spider.AddComponent<TrackScareTarget>().Configure(z, target);
        }

        private void CreateGrabbingHands(float z, float localX, int route)
        {
            GameObject hands = new GameObject("Händer ur golvet som kan skjutas");
            hands.transform.SetParent(root, false);
            hands.transform.position = new Vector3(TrackCenter(z, route) + localX, 0.05f, z);
            Material skin = TexturedMaterial(new Color(0.34f, 0.42f, 0.31f),
                HauntedTextureFactory.RottenSkin(1700 + Mathf.RoundToInt(z)), 0f);
            for (int side = -1; side <= 1; side += 2)
            {
                CreateChildPrimitive(hands.transform, PrimitiveType.Capsule, "Arm ur golvet",
                    new Vector3(side * 0.34f, 0.65f, 0f), new Vector3(0.18f, 0.72f, 0.18f), skin,
                    Quaternion.Euler(0f, 0f, side * 13f));
                CreateChildPrimitive(hands.transform, PrimitiveType.Sphere, "Gripande hand",
                    new Vector3(side * 0.48f, 1.28f, -0.08f), new Vector3(0.34f, 0.25f, 0.22f), skin);
            }
            HauntedProp.Attach(hands, HauntedMotion.Bob, 0.12f, 2.6f);
            GhostTarget target = GhostTarget.AttachExisting(hands, 1,
                new Vector3(0f, 0.72f, 0f), 1.75f, 0.72f);
            hands.AddComponent<TrackScareTarget>().Configure(z, target);
        }

        private void CreateCobweb(float z, int side, int route = 0)
        {
            float x = TrackCenter(z, route) + side * 5.42f;
            Vector3 corner = new Vector3(x, 4.85f, z);
            CreateBeam("Spindelväv kant", corner, corner + new Vector3(-side * 1.75f, 0, 0), 0.018f, cobweb);
            CreateBeam("Spindelväv kant", corner, corner + new Vector3(0, -1.70f, 0), 0.018f, cobweb);
            for (int i = 1; i <= 4; i++)
            {
                float t = i / 5f;
                CreateBeam("Spindelväv tråd", corner,
                    corner + new Vector3(-side * 1.72f * t, -1.68f * (1f - t), -0.03f), 0.009f, cobweb);
            }
        }

        private void CreateMist(float z, int route)
        {
            GameObject mist = new GameObject("Krypande slottsdimma");
            mist.SetActive(false);
            mist.transform.SetParent(root, false);
            mist.transform.position = new Vector3(TrackCenter(z, route), 0.18f, z);
            ParticleSystem particles = mist.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.duration = 7f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5.5f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.2f, 5.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.11f, 0.14f, 0.15f, 0.018f),
                new Color(0.24f, 0.28f, 0.27f, 0.060f));
            main.maxParticles = 72;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 7.5f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8.5f, 0.30f, 8f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            // Unity validerar varje tilldelning direkt. Alla tre måste därför ligga i
            // samma kurvläge även under konfigurationen; Noise-modulen ger variationen.
            velocity.x = new ParticleSystem.MinMaxCurve(0.035f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.006f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.025f);

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            noise.frequency = 0.18f;
            noise.scrollSpeed = 0.08f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            // Utgå från Unitys eget partikelmaterial. Den tidigare Shader.Find-
            // lösningen kunde strippas ur en release-build och gav då en stor
            // skrikrosa fyrkant i korridoren.
            Material baseMaterial = renderer.sharedMaterial;
            Shader shader = baseMaterial != null ? baseMaterial.shader : null;
            if (shader == null || !shader.isSupported || shader.name == "Hidden/InternalErrorShader")
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null || !shader.isSupported)
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null || !shader.isSupported)
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (baseMaterial != null || shader != null)
            {
                renderer.material = baseMaterial != null && baseMaterial.shader != null && baseMaterial.shader.isSupported
                    ? new Material(baseMaterial)
                    : new Material(shader);
                Material mistMaterial = renderer.material;
                Texture2D texture = GetSoftMistTexture();
                if (mistMaterial.HasProperty("_MainTex")) mistMaterial.SetTexture("_MainTex", texture);
                if (mistMaterial.HasProperty("_BaseMap")) mistMaterial.SetTexture("_BaseMap", texture);
                if (mistMaterial.HasProperty("_Color"))
                    mistMaterial.SetColor("_Color", new Color(0.20f, 0.24f, 0.24f, 0.10f));
            }
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = -2;
            mist.SetActive(true);
            particles.Play();
        }

        private GameObject CreateGravestone(float z, int side, int route, int variant)
        {
            string[] models =
            {
                "Models/KenneyGraveyard/gravestone-bevel",
                "Models/KenneyGraveyard/gravestone-broken",
                "Models/KenneyGraveyard/gravestone-cross-large",
                "Models/KenneyGraveyard/gravestone-decorative",
                "Models/KenneyGraveyard/gravestone-roof",
                "Models/KenneyGraveyard/gravestone-round",
                "Models/KenneyGraveyard/gravestone-wide"
            };
            string path = models[Mathf.Abs(variant) % models.Length];
            GameObject grave = ImportedModelFactory.Create(path, root, "Importerad gravsten",
                new Vector3(TrackCenter(z, route) + side * 3.55f, 0.72f, z), 1.65f,
                TrackRotation(z, route) * Quaternion.Euler(0f, side * 12f, side * 4f));
            return grave != null ? grave : CreateCube("Reservgravsten",
                new Vector3(TrackCenter(z, route) + side * 3.55f, 0.55f, z),
                new Vector3(0.85f, 1.15f, 0.28f), stone);
        }

        private void CreateGraveyardProp(string path, float z, int side, int route, float size, float yaw)
        {
            ImportedModelFactory.Create(path, root, "Importerad skräckdekor",
                new Vector3(TrackCenter(z, route) + side * 4.15f, size * 0.42f, z), size,
                TrackRotation(z, route) * Quaternion.Euler(0f, yaw + (side < 0 ? 180f : 0f), 0f));
        }

        private static Texture2D GetSoftMistTexture()
        {
            if (softMistTexture != null) return softMistTexture;
            const int size = 64;
            softMistTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Mjuk procedurdimma",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var random = new System.Random(1947);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float edge = 1f - Mathf.SmoothStep(0.16f, 0.98f, distance);
                float cloud = 0.80f + (float)random.NextDouble() * 0.20f;
                float alpha = Mathf.Clamp01(edge * cloud);
                softMistTexture.SetPixel(x, y, new Color(0.72f, 0.79f, 0.80f, alpha));
            }
            softMistTexture.Apply(false, true);
            return softMistTexture;
        }

        private void CreateWatchingPortrait(float z, int side, int route = 0)
        {
            float x = TrackCenter(z, route) + side * 5.42f;
            CreateCube("Gammalt porträtt", new Vector3(x, 2.65f, z), new Vector3(0.14f, 2.15f, 1.45f), wood);
            Material eye = GlowMaterial(new Color(0.80f, 0.025f, 0.018f), 2.7f);
            float inward = -side * 0.095f;
            CreateSphere("Vakande öga", new Vector3(x + inward, 2.83f, z - 0.22f), Vector3.one * 0.10f, eye)
                .AddComponent<WatchingEye>();
            CreateSphere("Vakande öga", new Vector3(x + inward, 2.83f, z + 0.22f), Vector3.one * 0.10f, eye)
                .AddComponent<WatchingEye>();
        }

        private void CreateHangingChain(float z, float localX, int route = 0)
        {
            float x = TrackCenter(z, route) + localX;
            GameObject chain = new GameObject("Skjutbar rostig takkedja");
            chain.transform.SetParent(root, false);
            chain.transform.position = new Vector3(x, 5.12f, z);
            for (int i = 0; i < 8; i++)
            {
                CreateChildPrimitive(chain.transform, PrimitiveType.Sphere, "Rostig kedjelänk",
                    new Vector3(0f, -i * 0.32f, 0f), new Vector3(0.13f, 0.22f, 0.08f), rail,
                    Quaternion.Euler(i % 2 == 0 ? 0 : 90, 0, 0));
            }
            chain.AddComponent<TrapTrigger>();
            HauntedProp.Attach(chain, HauntedMotion.Swing, 22f, 1.9f);
            GhostTarget target = GhostTarget.AttachExisting(chain, 1,
                new Vector3(0f, -1.05f, 0f), 2.65f, 0.42f, TargetKind.Ghost);
            chain.AddComponent<TrackScareTarget>().Configure(z, target, false);
        }

        private void CreateBeam(string name, Vector3 from, Vector3 to, float width, Material material)
        {
            Vector3 delta = to - from;
            GameObject beam = CreateCube(name, (from + to) * 0.5f, new Vector3(width, width, delta.magnitude), material);
            beam.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        private void CreateSign(float z, string text, Material material)
        {
            GameObject sign = CreateCube(text, new Vector3(TrackCenter(z), 3.5f, z), new Vector3(5.6f, 1.05f, 0.25f), darkStone);
            for (int i = 0; i < 9; i++)
                CreateSphere("Skyltljus", sign.transform.position + new Vector3(-2.1f + i * 0.52f, 0, -0.17f), Vector3.one * 0.11f, material);
        }
    }
}