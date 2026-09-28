using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Games.GreveGast
{
    /// <summary>Sprites cut at runtime from the supplied environment sheets.</summary>
    internal static class GreveEnvironmentSheets
    {
        private const string Root = "GreveChase/Sheets/environment_";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite CastleWindow => Cut("castle_window", 1, 950, 0, 265, 425);
        public static Sprite KitchenHearth => Cut("kitchen_hearth", 2, 0, 0, 515, 350);
        public static Sprite Staircase => Cut("staircase", 3, 0, 0, 340, 670);
        public static Sprite CellarArch => Cut("cellar_arch", 3, 550, 0, 310, 320);
        public static Sprite EntranceDebris => Cut("entrance_debris", 4, 0, 925, 245, 155);
        public static Sprite TransparentWindow => CutFrom("transparent_window",
            "GreveChase/Sheets/environment_windows", 30, 15, 285, 1015);
        public static Sprite WallTorch => CutFrom("wall_torch", "GreveChase/Sheets/environment_walls_1",
            0, 0, 100, 270);
        public static Sprite WallBanner => CutFrom("wall_banner", "GreveChase/Sheets/environment_walls_1",
            0, 270, 115, 310);
        public static Sprite WallGargoyle => CutFrom("wall_gargoyle", "GreveChase/Sheets/environment_walls_1",
            1015, 605, 170, 285, true);
        public static Sprite WallPlaque => CutFrom("wall_plaque", "GreveChase/Sheets/environment_walls_1",
            1040, 320, 130, 200);
        public static Sprite WallCobweb => CutFrom("wall_cobweb", "GreveChase/Sheets/environment_walls_1",
            930, 890, 145, 190);

        public static Sprite[] Cauldron => Sequence("cauldron", 2, 500, 150, 122, 676, 795, 915);
        public static Sprite[] SwingingAxe => IsolatedSequence("axe", 4, 0, 235, 145, 0, 145, 290, 435, 580);
        public static Sprite[] Choir => Sequence("choir", 5, 0, 225, 170, 715, 885, 1065, 1240);

        public static Sprite[] Portrait
        {
            get
            {
                return new[]
                {
                    Cut("portrait_0", 5, 492, 220, 185, 250, true),
                    Cut("portrait_1", 5, 677, 220, 190, 250, true),
                    Cut("portrait_2", 5, 868, 220, 185, 250, true),
                    Cut("portrait_3", 5, 1053, 220, 380, 250, true)
                };
            }
        }

        private static Sprite[] Sequence(string name, int sheet, int top, int height,
            int width, params int[] lefts)
        {
            Sprite[] frames = new Sprite[lefts.Length];
            for (int i = 0; i < lefts.Length; i++)
                frames[i] = Cut(name + "_" + i, sheet, lefts[i], top, width, height);
            return frames;
        }

        private static Sprite[] IsolatedSequence(string name, int sheet, int top, int height,
            int width, params int[] lefts)
        {
            Sprite[] frames = new Sprite[lefts.Length];
            for (int i = 0; i < lefts.Length; i++)
                frames[i] = Cut(name + "_" + i, sheet, lefts[i], top, width, height, true);
            return frames;
        }

        private static Sprite Cut(string name, int sheet, int left, int top, int width, int height,
            bool isolate = false)
        {
            return CutFrom(name, Root + sheet, left, top, width, height, isolate);
        }

        private static Sprite CutFrom(string name, string resource, int left, int top,
            int width, int height, bool isolate = false)
        {
            if (Cache.TryGetValue(name, out Sprite sprite)) return sprite;
            Texture2D texture = Resources.Load<Texture2D>(resource);
            if (texture == null) return null;
            // The atlas coordinates are measured from the upper left; Unity's
            // Sprite.Create rectangle starts at the lower left.
            Rect rect = new Rect(left, texture.height - top - height, width, height);
            if (isolate)
            {
                texture = ExtractMainIsland(texture, (int)rect.x, (int)rect.y, width, height);
                rect = new Rect(0, 0, width, height);
            }
            sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = name;
            Cache[name] = sprite;
            return sprite;
        }

        private static Texture2D ExtractMainIsland(Texture2D source, int left, int bottom,
            int width, int height)
        {
            Color[] pixels = source.GetPixels(left, bottom, width, height);
            int count = pixels.Length;
            bool[] visited = new bool[count];
            int[] queue = new int[count];
            int[] largest = null;
            int largestCount = 0;
            for (int start = 0; start < count; start++)
            {
                if (visited[start] || pixels[start].a < 0.16f) continue;
                visited[start] = true;
                int head = 0;
                int tail = 1;
                queue[0] = start;
                while (head < tail)
                {
                    int pixel = queue[head++];
                    int x = pixel % width;
                    int y = pixel / width;
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int neighbor = ny * width + nx;
                        if (visited[neighbor] || pixels[neighbor].a < 0.16f) continue;
                        visited[neighbor] = true;
                        queue[tail++] = neighbor;
                    }
                }
                if (tail <= largestCount) continue;
                largestCount = tail;
                largest = new int[tail];
                System.Array.Copy(queue, largest, tail);
            }
            Color[] cleaned = largest == null ? pixels : new Color[count];
            for (int i = 0; largest != null && i < largest.Length; i++)
            {
                int pixel = largest[i];
                int x = pixel % width;
                int y = pixel / width;
                // Preserve the soft antialiased edge around the chosen object.
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    int neighbor = ny * width + nx;
                    cleaned[neighbor] = pixels[neighbor];
                }
            }
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.filterMode = source.filterMode;
            result.wrapMode = TextureWrapMode.Clamp;
            result.SetPixels(cleaned);
            result.Apply(false, true);
            return result;
        }
    }
}
