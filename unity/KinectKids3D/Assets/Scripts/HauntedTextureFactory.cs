using UnityEngine;

namespace KinectKids3D
{
    public static class HauntedTextureFactory
    {
        private const int Size = 128;

        public static Texture2D DampStone(int seed, Color stone, Color mortar)
        {
            return Create("Fuktig slottssten", (x, y) =>
            {
                int row = y / 20;
                int shiftedX = x + (row % 2) * 17;
                bool joint = y % 20 < 3 || shiftedX % 34 < 3;
                if (joint) return mortar * (0.65f + Noise(x, y, seed) * 0.18f);
                float n = Noise(x, y, seed);
                float moss = Mathf.Clamp01((Noise(x / 2, y / 2, seed + 91) - 0.61f) * 3.4f);
                Color value = stone * (0.68f + n * 0.48f);
                return Color.Lerp(value, new Color(0.055f, 0.16f, 0.085f), moss * 0.72f);
            });
        }

        public static Texture2D OldWood(int seed)
        {
            return Create("Murket trä", (x, y) =>
            {
                bool seam = y % 32 < 3;
                float grain = Mathf.Sin((x + Noise(x, y, seed) * 18f) * 0.22f) * 0.5f + 0.5f;
                float n = Noise(x / 2, y, seed + 17);
                if (seam) return new Color(0.025f, 0.016f, 0.012f);
                return Color.Lerp(new Color(0.075f, 0.032f, 0.018f), new Color(0.29f, 0.115f, 0.045f),
                    grain * 0.40f + n * 0.24f);
            });
        }

        public static Texture2D RustedMetal(int seed)
        {
            return Create("Rostig järn", (x, y) =>
            {
                float metal = Noise(x * 2, y / 2, seed);
                float rust = Mathf.Clamp01((Noise(x / 3, y / 3, seed + 44) - 0.48f) * 2.6f);
                Color iron = Color.Lerp(new Color(0.075f, 0.085f, 0.09f), new Color(0.24f, 0.27f, 0.27f), metal);
                Color orange = Color.Lerp(new Color(0.18f, 0.045f, 0.012f), new Color(0.50f, 0.16f, 0.025f), metal);
                return Color.Lerp(iron, orange, rust * 0.78f);
            });
        }

        private static Texture2D Create(string name, System.Func<int, int, Color> pixel)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGB24, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 3
            };
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++) pixels[y * Size + x] = pixel(x, y);
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static float Noise(int x, int y, int seed)
        {
            return Mathf.PerlinNoise((x + seed * 13) * 0.057f, (y + seed * 29) * 0.057f);
        }
    }
}
