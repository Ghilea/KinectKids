using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class DarkRideWorld
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
            RenderSettings.fogColor = new Color(0.0025f, 0.0035f, 0.007f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.056f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.0035f, 0.0042f, 0.007f);
            RenderSettings.ambientEquatorColor = new Color(0.0022f, 0.0028f, 0.0042f);
            RenderSettings.ambientGroundColor = new Color(0.0008f, 0.0009f, 0.0014f);
            RenderSettings.ambientIntensity = 0.18f;
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
                if (z >= 218f && z < 280f)
                {
                    CreateCorridorSection(z, -1);
                    CreateCorridorSection(z, 1);
                }
                else if (z >= BranchSplitStart && z <= BranchJoinEnd)
                    CreateForkHallSection(z);
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
            CreateSideWall(center, z, rotation, route, -1);
            CreateSideWall(center, z, rotation, route, 1);
            CreateCube("Tak", new Vector3(center, 5.65f, z), new Vector3(12f, 0.5f, 8.1f), darkStone, rotation);
            CreateMasonryFacing(center, z, rotation, zone, route);
        }

        private void CreateForkHallSection(float z)
        {
            float center = TrackCenter(z);
            float routeOffset = Mathf.Abs(TrackCenter(z, 1) - center);
            float halfWidth = 6f + routeOffset;
            Quaternion rotation = TrackRotation(z);
            CreateCube("Vägskälets öppna golv", new Vector3(center, -0.22f, z),
                new Vector3(halfWidth * 2f, 0.45f, 8f), road, rotation);
            CreateCube("Vägskälets vänstra yttervägg", new Vector3(center - halfWidth, 2.8f, z),
                new Vector3(0.6f, 6f, 8.1f), darkStone, rotation);
            CreateCube("Vägskälets högra yttervägg", new Vector3(center + halfWidth, 2.8f, z),
                new Vector3(0.6f, 6f, 8.1f), darkStone, rotation);
            CreateCube("Vägskälets tak", new Vector3(center, 5.65f, z),
                new Vector3(halfWidth * 2f, 0.5f, 8.1f), darkStone, rotation);
        }

        private void CreateSideWall(float center, float z, Quaternion rotation, int route, int side)
        {
            float openingLocalZ;
            if (!TryGetWallOpening(z, route, side, out openingLocalZ))
            {
                CreateCube(side < 0 ? "Vänster murkärna" : "Höger murkärna",
                    new Vector3(center + side * 5.8f, 2.8f, z),
                    new Vector3(0.6f, 6f, 8.1f), darkStone, rotation);
                return;
            }

            GameObject section = new GameObject("Mur med riktig dörröppning");
            section.transform.SetParent(root, false);
            section.transform.position = new Vector3(center, 0f, z);
            section.transform.rotation = rotation;
            const float sectionStart = -4.05f;
            const float sectionEnd = 4.05f;
            float openingStart = Mathf.Clamp(openingLocalZ - 1.85f, sectionStart, sectionEnd);
            float openingEnd = Mathf.Clamp(openingLocalZ + 1.85f, sectionStart, sectionEnd);
            AddWallSpan(section.transform, side, sectionStart, openingStart);
            AddWallSpan(section.transform, side, openingEnd, sectionEnd);
            CreateChildPrimitive(section.transform, PrimitiveType.Cube, "Stenvalv över dörrhålet",
                new Vector3(side * 5.8f, 5.25f, (openingStart + openingEnd) * 0.5f),
                new Vector3(0.6f, 1.1f, openingEnd - openingStart), darkStone, Quaternion.identity);
        }

        private void AddWallSpan(Transform section, int side, float start, float end)
        {
            float length = end - start;
            if (length <= 0.05f) return;
            CreateChildPrimitive(section, PrimitiveType.Cube, "Mur bredvid dörrhålet",
                new Vector3(side * 5.8f, 2.8f, (start + end) * 0.5f),
                new Vector3(0.6f, 6f, length), darkStone, Quaternion.identity);
        }

        private static bool TryGetWallOpening(float sectionZ, int route, int side, out float localZ)
        {
            float[] openingZ = { 38f, 94f, 126f, 174f, 250f, 279f, 327f };
            int[] openingRoute = { 0, 0, 0, 0, -1, 1, 0 };
            int[] openingSide = { -1, 1, -1, 1, -1, 1, 1 };
            for (int i = 0; i < openingZ.Length; i++)
            {
                if (route != openingRoute[i] || side != openingSide[i]
                    || Mathf.Abs(sectionZ - openingZ[i]) > 3.75f) continue;
                localZ = openingZ[i] - sectionZ;
                return true;
            }
            localZ = 0f;
            return false;
        }

        private void CreateMasonryFacing(float center, float z, Quaternion rotation, Material material, int route)
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
                float openingLocalZ;
                if (rowIndex < 3 && TryGetWallOpening(z, route, side, out openingLocalZ)
                    && Mathf.Abs(localZ - openingLocalZ) < 2.35f) continue;
                CreateChildPrimitive(section.transform, PrimitiveType.Cube, "Enskilt stenblock",
                    new Vector3(side * 5.46f, y, localZ), new Vector3(0.34f, 1.25f, depth), material,
                    Quaternion.Euler(0f, 0f, side * (((rowIndex + column) % 3) - 1) * 0.55f));
            }
        }
    }
}