using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// Generates simple, stylised placeholder sprites at runtime so a 2.5D scene
    /// is fully playable before final art exists (new-way.md: "Använd
    /// placeholder-grafik där slutliga assets ännu inte finns").
    ///
    /// Everything here is intentionally throw-away: solid silhouettes and soft
    /// gradients in the dark-storybook palette. Real PNG sprites can replace any
    /// of these without touching gameplay, because scenes reference sprites
    /// through this factory or through swappable fields, not by baked pixels.
    /// </summary>
    public static class PlaceholderArt
    {
        // Dark storybook palette from new-way.md.
        public static readonly Color NightBlue = new Color(0.12f, 0.11f, 0.24f);
        public static readonly Color DeepPurple = new Color(0.20f, 0.13f, 0.30f);
        public static readonly Color Shadow = new Color(0.06f, 0.05f, 0.12f);
        public static readonly Color LanternOrange = new Color(1.00f, 0.62f, 0.20f);
        public static readonly Color LanternYellow = new Color(1.00f, 0.85f, 0.45f);
        public static readonly Color GhostGlow = new Color(0.55f, 0.85f, 0.95f);
        public static readonly Color BoneWhite = new Color(0.92f, 0.90f, 0.82f);

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>A soft 1x1 sprite tinted by SpriteRenderer.color. Cheapest primitive.</summary>
        public static Sprite SolidBlock()
        {
            const string key = "solid";
            if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "ph_solid",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            Color[] px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            s = Make(tex, "ph_solid");
            cache[key] = s;
            return s;
        }

        /// <summary>A soft radial glow, for lanterns, fog cores and ghost light.</summary>
        public static Sprite Glow(int size = 128)
        {
            string key = "glow" + size;
            if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
            Texture2D tex = NewTex(size, "ph_glow");
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            s = Make(tex, "ph_glow");
            cache[key] = s;
            return s;
        }

        /// <summary>A rounded upright silhouette, useful for characters and props.</summary>
        public static Sprite Capsule(int w = 96, int h = 160)
        {
            string key = $"cap{w}x{h}";
            if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
            Texture2D tex = NewTex(w, h, "ph_capsule");
            float rx = w * 0.5f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x - rx) / rx;
                bool inside;
                float cap = rx;
                if (y < cap) inside = (x - rx) * (x - rx) + (y - cap) * (y - cap) <= rx * rx;
                else if (y > h - cap) inside = (x - rx) * (x - rx) + (y - (h - cap)) * (y - (h - cap)) <= rx * rx;
                else inside = Mathf.Abs(nx) <= 1f;
                tex.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
            }
            tex.Apply();
            s = Make(tex, "ph_capsule");
            cache[key] = s;
            return s;
        }

        /// <summary>A hat-and-shoulders ghost silhouette for a Greve-Gast placeholder.</summary>
        public static Sprite GhostSilhouette(int w = 140, int h = 200)
        {
            string key = $"ghost{w}x{h}";
            if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
            Texture2D tex = NewTex(w, h, "ph_ghost");
            float cx = w * 0.5f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fy = (float)y / h;      // 0 bottom, 1 top
                float dx = Mathf.Abs(x - cx);
                bool inside = false;

                if (fy > 0.82f) // tall hat
                    inside = dx < w * 0.14f;
                else if (fy > 0.74f) // brim
                    inside = dx < w * 0.30f;
                else if (fy > 0.55f) // head
                    inside = dx < w * 0.20f;
                else if (fy > 0.42f) // pointed collar
                    inside = dx < Mathf.Lerp(w * 0.42f, w * 0.20f, (fy - 0.42f) / 0.13f);
                else // wispy flaring ghost body (no legs)
                {
                    float wobble = Mathf.Sin(fy * 30f) * w * 0.05f;
                    float bodyHalf = Mathf.Lerp(w * 0.10f, w * 0.42f, 1f - fy / 0.42f) + wobble;
                    inside = dx < bodyHalf && Random.value > fy * 0.15f; // frayed bottom
                }
                tex.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
            }
            tex.Apply();
            s = Make(tex, "ph_ghost");
            cache[key] = s;
            return s;
        }

        /// <summary>A horizontal beam/plank silhouette (low beam, planks, banners).</summary>
        public static Sprite Beam(int w = 256, int h = 48)
        {
            string key = $"beam{w}x{h}";
            if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
            Texture2D tex = NewTex(w, h, "ph_beam");
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            s = Make(tex, "ph_beam");
            cache[key] = s;
            return s;
        }

        // --- helpers ---

        private static Texture2D NewTex(int size, string name) => NewTex(size, size, name);

        private static Texture2D NewTex(int w, int h, string name)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            return tex;
        }

        private static Sprite Make(Texture2D tex, string name)
        {
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>Create a GameObject with a SpriteRenderer using a placeholder sprite.</summary>
        public static SpriteRenderer NewSpriteObject(string name, Sprite sprite, Color color,
            Transform parent, SceneBand band, float depth01 = 0.5f)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            LayerSorting.Apply(sr, band, depth01);
            return sr;
        }
    }
}
