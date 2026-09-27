using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Games.GreveGast
{
    /// <summary>Sprites cut at runtime from the five original environment sheets.</summary>
    internal static class GreveEnvironmentSheets
    {
        private const string Root = "GreveChase/Sheets/environment_";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite CastleWindow => Cut("castle_window", 1, 950, 0, 265, 425);
        public static Sprite KitchenHearth => Cut("kitchen_hearth", 2, 0, 0, 515, 350);
        public static Sprite Staircase => Cut("staircase", 3, 0, 0, 340, 670);
        public static Sprite CellarArch => Cut("cellar_arch", 3, 550, 0, 310, 320);

        public static Sprite[] Cauldron => Sequence("cauldron", 2, 500, 150, 122, 676, 795, 915);
        public static Sprite[] SwingingAxe => Sequence("axe", 4, 0, 235, 145, 0, 145, 290, 435, 580);
        public static Sprite[] Choir => Sequence("choir", 5, 0, 225, 170, 715, 885, 1065, 1240);

        public static Sprite[] Portrait
        {
            get
            {
                return new[]
                {
                    Cut("portrait_0", 5, 492, 220, 185, 250),
                    Cut("portrait_1", 5, 677, 220, 190, 250),
                    Cut("portrait_2", 5, 868, 220, 185, 250),
                    Cut("portrait_3", 5, 1053, 220, 380, 250)
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

        private static Sprite Cut(string name, int sheet, int left, int top, int width, int height)
        {
            if (Cache.TryGetValue(name, out Sprite sprite)) return sprite;
            Texture2D texture = Resources.Load<Texture2D>(Root + sheet);
            if (texture == null) return null;
            // The atlas coordinates are measured from the upper left; Unity's
            // Sprite.Create rectangle starts at the lower left.
            Rect rect = new Rect(left, texture.height - top - height, width, height);
            sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = name;
            Cache[name] = sprite;
            return sprite;
        }
    }
}
