using UnityEngine;

namespace KinectKids3D
{
    public sealed class DarkRideWorld
    {
        public const float TrackLength = 378f;
        public const float BranchChoiceStart = 190f;
        public const float BranchSplitStart = 202f;
        public const float BranchJoinEnd = 307f;
        private readonly Transform root;
        private readonly Material stone;
        private readonly Material darkStone;
        private readonly Material road;
        private readonly Material wood;
        private readonly Material rail;
        private readonly Material purpleGlow;
        private readonly Material greenGlow;
        private readonly Material amberGlow;
        private readonly Material cobweb;
        private static Texture2D softMistTexture;

        public DarkRideWorld(Transform parent)
        {
            root = new GameObject("3D Dark Ride World").transform;
            Texture2D castleStone = Resources.Load<Texture2D>("Textures/CastleStone");
            Texture2D castleRoad = Resources.Load<Texture2D>("Textures/CastleRoad");
            if (castleStone != null) castleStone.wrapMode = TextureWrapMode.Repeat;
            if (castleRoad != null) castleRoad.wrapMode = TextureWrapMode.Repeat;
            root.SetParent(parent, false);
            stone = TexturedMaterial(new Color(0.58f, 0.60f, 0.63f),
                castleStone != null ? castleStone : HauntedTextureFactory.DampStone(8,
                    new Color(0.22f, 0.24f, 0.25f), new Color(0.028f, 0.033f, 0.038f)), 0.08f);
            darkStone = TexturedMaterial(new Color(0.19f, 0.21f, 0.24f),
                castleStone != null ? castleStone : HauntedTextureFactory.DampStone(31,
                    new Color(0.13f, 0.15f, 0.17f), new Color(0.016f, 0.022f, 0.030f)), 0.05f);
            road = TexturedMaterial(new Color(0.46f, 0.44f, 0.40f),
                castleRoad != null ? castleRoad : HauntedTextureFactory.DampStone(51,
                    new Color(0.26f, 0.27f, 0.25f), new Color(0.035f, 0.040f, 0.037f)), 0.03f);
            wood = TexturedMaterial(new Color(0.30f, 0.18f, 0.10f), HauntedTextureFactory.OldWood(17), 0f);
            rail = TexturedMaterial(new Color(0.42f, 0.43f, 0.42f), HauntedTextureFactory.RustedMetal(23), 0.72f);
            purpleGlow = GlowMaterial(new Color(0.25f, 0.025f, 0.38f), 0.42f);
            greenGlow = GlowMaterial(new Color(0.025f, 0.31f, 0.12f), 0.38f);
            amberGlow = GlowMaterial(new Color(1f, 0.34f, 0.045f), 2.2f);
            cobweb = GlowMaterial(new Color(0.24f, 0.28f, 0.30f), 0.32f);
        }

        public void Build(Camera rideCamera)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.0005f, 0.0007f, 0.0015f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.064f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.00035f, 0.00042f, 0.0007f);
            RenderSettings.ambientEquatorColor = new Color(0.00022f, 0.00028f, 0.00042f);
            RenderSettings.ambientGroundColor = new Color(0.00008f, 0.00009f, 0.00014f);
            RenderSettings.ambientIntensity = 0.08f;
            RenderSettings.reflectionIntensity = 0f;

            BuildTrack();
            BuildEntranceHall();
            BuildCrypt();
            BuildCastleCourtyard();
            BuildHauntedGallery();
            BuildForkedPassages();
            BuildForgottenDungeon();
            BuildFinalHall();
            BuildRideCar(rideCamera.transform);
        }

        public static float TrackCenter(float z)
        {
            return TrackCenter(z, 0);
        }

        public static Quaternion TrackRotation(float z)
        {
            return TrackRotation(z, 0);
        }

        public static float TrackCenter(float z, int route)
        {
            float center = Mathf.Sin(z * 0.034f) * 3.25f
                + Mathf.Sin(z * 0.081f) * 1.15f
                + Mathf.Sin(z * 0.014f) * 0.85f;
            if (route == 0) return center;
            float split = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(BranchSplitStart, 229f, z));
            float join = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(280f, BranchJoinEnd, z));
            return center + Mathf.Clamp(route, -1, 1) * 8.2f * split * join;
        }

        public static Quaternion TrackRotation(float z, int route)
        {
            float dx = TrackCenter(z + 0.5f, route) - TrackCenter(z - 0.5f, route);
            return Quaternion.Euler(0, Mathf.Atan2(dx, 1f) * Mathf.Rad2Deg, 0);
        }

        private void BuildTrack()
        {
            for (float z = 0; z <= TrackLength; z += 1.5f)
            {
                if (z >= BranchSplitStart && z <= BranchJoinEnd)
                {
                    CreateRails(z, -1);
                    CreateRails(z, 1);
                }
                else CreateRails(z, 0);
            }

            for (float z = 3; z < TrackLength; z += 7.5f)
            {
                if (z >= BranchSplitStart && z <= BranchJoinEnd)
                {
                    CreateCorridorSection(z, -1);
                    CreateCorridorSection(z, 1);
                }
                else CreateCorridorSection(z, 0);
            }
        }

        private void CreateRails(float z, int route)
        {
            float center = TrackCenter(z, route);
            Quaternion rotation = TrackRotation(z, route);
            CreateCube("Sliper", new Vector3(center, 0.04f, z), new Vector3(2.7f, 0.12f, 0.25f), wood, rotation);
            CreateCube("Vänster räls", new Vector3(center - 0.72f, 0.18f, z), new Vector3(0.11f, 0.15f, 1.65f), rail, rotation);
            CreateCube("Höger räls", new Vector3(center + 0.72f, 0.18f, z), new Vector3(0.11f, 0.15f, 1.65f), rail, rotation);
        }

        private void CreateCorridorSection(float z, int route)
        {
            float center = TrackCenter(z, route);
            Quaternion rotation = TrackRotation(z, route);
            Material zone = z < 48f || z > 330f ? stone : darkStone;
            CreateCube("Kullerstensväg", new Vector3(center, -0.22f, z), new Vector3(12f, 0.45f, 8f), road, rotation);
            CreateCube("Vänster murkärna", new Vector3(center - 5.8f, 2.8f, z), new Vector3(0.6f, 6f, 8.1f), darkStone, rotation);
            CreateCube("Höger murkärna", new Vector3(center + 5.8f, 2.8f, z), new Vector3(0.6f, 6f, 8.1f), darkStone, rotation);
            CreateCube("Tak", new Vector3(center, 5.65f, z), new Vector3(12f, 0.5f, 8.1f), darkStone, rotation);
            CreateMasonryFacing(center, z, rotation, zone);
        }

        private void CreateMasonryFacing(float center, float z, Quaternion rotation, Material material)
        {
            GameObject section = new GameObject("Modellerad slottsmur");
            section.transform.SetParent(root, false);
            section.transform.position = new Vector3(center, 0f, z);
            section.transform.rotation = rotation;
            for (int side = -1; side <= 1; side += 2)
            for (int rowIndex = 0; rowIndex < 4; rowIndex++)
            for (int column = 0; column < 4; column++)
            {
                float stagger = rowIndex % 2 == 0 ? 0f : 0.52f;
                float depth = 1.82f + ((rowIndex + column) % 3) * 0.08f;
                float y = 0.72f + rowIndex * 1.40f;
                float localZ = -3.0f + column * 1.95f + stagger;
                CreateChildPrimitive(section.transform, PrimitiveType.Cube, "Enskilt stenblock",
                    new Vector3(side * 5.46f, y, localZ), new Vector3(0.34f, 1.25f, depth), material,
                    Quaternion.Euler(0f, 0f, side * (((rowIndex + column) % 3) - 1) * 0.55f));
            }
        }

        private void BuildEntranceHall()
        {
            for (float z = 5; z < 48; z += 9f)
            {
                CreateArch(z, amberGlow);
                CreateTorch(z + 2.2f, -1);
                CreateTorch(z + 6.0f, 1);
            }
            CreateCastleGate(8f);
            CreateSign(14f, "SPÖKJAKTEN 3D", amberGlow);
            CreateArmor(22f, -1);
            CreateArmor(31f, 1);
            CreateBatSwarm(25f, -2.6f, 3.35f);
            CreateBatSwarm(44f, 2.4f, 2.75f);
            CreateCobweb(20f, -1);
            CreateWatchingPortrait(36f, 1);
        }

        private void BuildCrypt()
        {
            for (float z = 52; z < 105; z += 8f)
            {
                CreateArch(z, greenGlow);
                CreateTorch(z + 2.6f, z % 16f < 1f ? -1 : 1);
                float center = TrackCenter(z);
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject grave = CreateCube("Lysande gravsten", new Vector3(center + side * 3.5f, 0.55f, z + 2f),
                        new Vector3(0.85f, 1.15f, 0.28f), stone, Quaternion.Euler(0, side * 12f, side * 5f));
                    CreateSphere("Spökeld", grave.transform.position + Vector3.up * 0.95f, Vector3.one * 0.24f, greenGlow);
                }
            }
            CreateBatSwarm(63f, 2.2f, 3.2f);
            CreateCobweb(59f, 1);
            CreateCobweb(89f, -1);
            CreateWatchingPortrait(77f, -1);
            CreateArmor(84f, 1);
            CreateCastleGate(103f);
        }

        private void BuildCastleCourtyard()
        {
            for (float z = 108; z < 146; z += 9f)
            {
                CreateBattlements(z);
                CreateTorch(z + 2.4f, -1);
                CreateTorch(z + 5.8f, 1);
            }
            CreateCastleGate(109f);
            CreateCastleTower(118f, -1);
            CreateCastleTower(132f, 1);
            CreateArmor(116f, 1);
            CreateArmor(137f, -1);
            CreateBatSwarm(128f, -2.3f, 3.65f);
            CreateHangingChain(119f, -3.7f);
            CreateHangingChain(138f, 3.8f);
        }

        private void BuildHauntedGallery()
        {
            CreateCastleGate(149f);
            for (float z = 154f; z < 199f; z += 10f)
            {
                CreateArch(z, purpleGlow);
                if (((int)z / 10) % 2 == 0) CreateTorch(z + 2.4f, -1);
                CreateWatchingPortrait(z + 4.2f, ((int)z / 10) % 2 == 0 ? 1 : -1);
            }
            CreateArmor(166f, -1);
            CreateArmor(183f, 1);
            HiddenMonster.Create(161f, 1, 0, HiddenMonsterKind.Portrait);
            HiddenMonster.Create(179f, -1, 0, HiddenMonsterKind.Cabinet);
            HiddenMonster.Create(196f, 1, 0, HiddenMonsterKind.Portrait);
            CreateMist(158f, 0);
            CreateMist(188f, 0);
        }

        private void BuildForkedPassages()
        {
            CreateCastleGate(201f);
            float forkX = TrackCenter(205f);
            CreateSphere("Vänster vägvisare", new Vector3(forkX - 2.4f, 2.5f, 205f),
                Vector3.one * 0.34f, greenGlow);
            CreateSphere("Höger vägvisare", new Vector3(forkX + 2.4f, 2.5f, 205f),
                Vector3.one * 0.34f, purpleGlow);

            RouteDoor.Create(218f, -1, wood, rail, greenGlow).transform.SetParent(root, true);
            RouteDoor.Create(218f, 1, wood, rail, purpleGlow).transform.SetParent(root, true);

            for (int route = -1; route <= 1; route += 2)
            {
                for (float z = 214f; z < 300f; z += 11f)
                {
                    CreateArch(z, route < 0 ? greenGlow : purpleGlow, route);
                    if (((int)z / 11) % 2 == 0) CreateTorch(z + 2.3f, route < 0 ? -1 : 1, route);
                }
                CreateArmor(228f, route < 0 ? -1 : 1, route);
                CreateCobweb(246f, route < 0 ? 1 : -1, route);
                CreateHangingChain(266f, route < 0 ? -3.6f : 3.6f, route);
                CreateWatchingPortrait(284f, route < 0 ? -1 : 1, route);
                HiddenMonster.Create(235f, route < 0 ? 1 : -1, route, HiddenMonsterKind.Cabinet);
                HiddenMonster.Create(258f, route < 0 ? -1 : 1, route, HiddenMonsterKind.Tomb);
                HiddenMonster.Create(289f, route < 0 ? 1 : -1, route, HiddenMonsterKind.Portrait);
                CreateMist(222f, route);
                CreateMist(274f, route);
            }
        }

        private void BuildForgottenDungeon()
        {
            CreateCastleGate(310f);
            for (float z = 313f; z < 333f; z += 7f)
            {
                CreateArch(z, greenGlow);
                CreateHangingChain(z + 2.5f, ((int)z % 2 == 0 ? -3.7f : 3.7f));
            }
            CreateCobweb(318f, -1);
            CreateCobweb(329f, 1);
            HiddenMonster.Create(321f, -1, 0, HiddenMonsterKind.Tomb);
            CreateMist(315f, 0);
            CreateMist(328f, 0);
        }

        private void BuildFinalHall()
        {
            CreateCastleGate(334f);
            for (float z = 337f; z < TrackLength; z += 6f)
            {
                CreateArch(z, amberGlow);
                CreateTorch(z + 2.2f, z % 12f < 1f ? -1 : 1);
            }
            CreateArmor(342f, -1);
            CreateArmor(342f, 1);
            float endZ = TrackLength - 4f;
            float endX = TrackCenter(endZ);
            CreateCube("Bossportal", new Vector3(endX, 2.7f, endZ), new Vector3(9f, 5.4f, 0.7f), darkStone);
            CreateSphere("Portal vänster", new Vector3(endX - 3f, 2.5f, endZ - 0.5f), Vector3.one * 0.7f, amberGlow);
            CreateSphere("Portal höger", new Vector3(endX + 3f, 2.5f, endZ - 0.5f), Vector3.one * 0.7f, amberGlow);
            CreateCobweb(352f, 1);
            CreateMist(340f, 0);
            CreateMist(365f, 0);
        }

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
            CreateCube("Fackelhållare", position + new Vector3(-side * 0.18f, -0.28f, 0),
                new Vector3(0.12f, 0.75f, 0.12f), rail,
                Quaternion.Euler(0f, 0f, side * 20f));
            CreatePrimitive(PrimitiveType.Cylinder, "Fackelskål", position,
                new Vector3(0.30f, 0.12f, 0.30f), rail);
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
            light.intensity = 2.45f;
            light.range = 5.6f;
            light.shadows = LightShadows.None;
            HauntedProp.Attach(lightObject, HauntedMotion.Flicker, 0f, Random.Range(7f, 11f));
        }

        private void CreateArmor(float z, int side, int route = 0)
        {
            GameObject armor = new GameObject("Kuslig riddarrustning");
            armor.transform.SetParent(root, false);
            armor.transform.position = new Vector3(TrackCenter(z, route) + side * 4.25f, 0.16f, z);
            armor.transform.rotation = TrackRotation(z, route) * Quaternion.Euler(0f, 180f + side * 12f, 0f);
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
            velocity.x = new ParticleSystem.MinMaxCurve(-0.10f, 0.10f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            noise.frequency = 0.18f;
            noise.scrollSpeed = 0.08f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            if (shader != null)
            {
                renderer.material = new Material(shader);
                Material mistMaterial = renderer.material;
                Texture2D texture = GetSoftMistTexture();
                if (mistMaterial.HasProperty("_MainTex")) mistMaterial.SetTexture("_MainTex", texture);
                if (mistMaterial.HasProperty("_BaseMap")) mistMaterial.SetTexture("_BaseMap", texture);
                if (mistMaterial.HasProperty("_Color"))
                    mistMaterial.SetColor("_Color", new Color(0.20f, 0.24f, 0.24f, 0.10f));
                if (mistMaterial.HasProperty("_ZWrite")) mistMaterial.SetFloat("_ZWrite", 0f);
                if (mistMaterial.HasProperty("_SrcBlend"))
                    mistMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mistMaterial.HasProperty("_DstBlend"))
                    mistMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mistMaterial.EnableKeyword("_ALPHABLEND_ON");
                mistMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = -2;
            mist.SetActive(true);
            particles.Play();
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
            CreateSphere("Vakande öga", new Vector3(x + inward, 2.83f, z - 0.22f), Vector3.one * 0.10f, eye);
            CreateSphere("Vakande öga", new Vector3(x + inward, 2.83f, z + 0.22f), Vector3.one * 0.10f, eye);
        }

        private void CreateHangingChain(float z, float localX, int route = 0)
        {
            float x = TrackCenter(z, route) + localX;
            for (int i = 0; i < 8; i++)
            {
                GameObject link = CreateSphere("Rostig kedjelänk", new Vector3(x, 5.12f - i * 0.32f, z),
                    new Vector3(0.13f, 0.22f, 0.08f), rail);
                link.transform.rotation = Quaternion.Euler(i % 2 == 0 ? 0 : 90, 0, 0);
            }
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

        private void BuildRideCar(Transform cameraTransform)
        {
            GameObject car = new GameObject("Spökvagn");
            car.transform.SetParent(cameraTransform, false);
            CreateChildCube(car.transform, "Vagnkant", new Vector3(0, -0.85f, 1.35f), new Vector3(3.1f, 0.42f, 0.55f), wood);
            CreateChildCube(car.transform, "Vänster lykta", new Vector3(-1.2f, -0.58f, 1.08f), new Vector3(0.22f, 0.22f, 0.22f), amberGlow);
            CreateChildCube(car.transform, "Höger lykta", new Vector3(1.2f, -0.58f, 1.08f), new Vector3(0.22f, 0.22f, 0.22f), amberGlow);
            GameObject lanternLight = new GameObject("Vagnens svaga lyktljus");
            lanternLight.transform.SetParent(cameraTransform, false);
            lanternLight.transform.localPosition = new Vector3(0f, -0.48f, 1.1f);
            Light light = lanternLight.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.24f, 0.035f);
            light.intensity = 0.22f;
            light.range = 3.8f;
            light.spotAngle = 58f;
            light.shadows = LightShadows.None;
            HauntedProp.Attach(lanternLight, HauntedMotion.Flicker, 0f, 8.2f);
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

        private GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position,
            Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject value = GameObject.CreatePrimitive(type);
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

        private static void CreateChildPrimitive(Transform parent, PrimitiveType type, string name,
            Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localScale = scale;
            value.transform.localRotation = rotation ?? Quaternion.identity;
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
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = new Material(shader);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", metallic > 0 ? 0.78f : 0.2f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", metallic > 0 ? 0.78f : 0.2f);
            return material;
        }

        public static Material TexturedMaterial(Color tint, Texture2D texture, float metallic)
        {
            Material material = MaterialOf(tint, metallic);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTextureScale("_MainTex", new Vector2(1.15f, 1.15f));
            if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", new Vector2(1.15f, 1.15f));
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", metallic > 0 ? 0.42f : 0.05f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", metallic > 0 ? 0.42f : 0.05f);
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
