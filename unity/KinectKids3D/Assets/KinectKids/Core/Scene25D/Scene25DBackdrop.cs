using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// A shared, reusable dark-storybook 2.5D backdrop. Any game can spawn one to
    /// get an instant layered environment (sky, far towers, mid pillars, side
    /// walls with warm lanterns, floor, foreground, drifting fog) built entirely
    /// from placeholder art and wired into a <see cref="ParallaxController"/>.
    ///
    /// The tint can be themed per game so each mode still feels distinct while
    /// sharing one consistent, cheap-to-produce visual language (new-way.md:
    /// "Hellre 20 bra återanvändbara delar ... än 100 unika objekt").
    /// </summary>
    public static class Scene25DBackdrop
    {
        public struct Theme
        {
            public Color sky;
            public Color structures;
            public Color walls;
            public Color accent;   // lantern / glow color
            public Color floor;
            public Color fog;

            public static Theme DarkStorybook => new Theme
            {
                sky = PlaceholderArt.NightBlue,
                structures = new Color(0.10f, 0.09f, 0.18f),
                walls = PlaceholderArt.DeepPurple,
                accent = PlaceholderArt.LanternOrange,
                floor = new Color(0.15f, 0.12f, 0.20f),
                fog = new Color(0.5f, 0.5f, 0.65f, 0.18f)
            };

            /// <summary>Tint the storybook theme toward a game's signature color.</summary>
            public static Theme Tinted(Color signature)
            {
                Theme t = DarkStorybook;
                t.sky = Color.Lerp(t.sky, signature, 0.20f);
                t.walls = Color.Lerp(t.walls, signature, 0.30f);
                t.accent = Color.Lerp(t.accent, signature, 0.25f);
                return t;
            }
        }

        /// <summary>Build the backdrop under <paramref name="parent"/> and return its controller.</summary>
        public static ParallaxController Build(Transform parent, Theme theme, bool lanterns = true)
        {
            GameObject worldGo = new GameObject("Scene25DBackdrop");
            worldGo.transform.SetParent(parent, false);
            var parallax = worldGo.AddComponent<ParallaxController>();

            var sky = NewLayer(worldGo.transform, "Sky", SceneBand.Sky, 0.05f);
            Quad(sky, PlaceholderArt.SolidBlock(), theme.sky, new Vector3(0, 2f, 0), new Vector3(48f, 26f, 1f), SceneBand.Sky, 0.1f);
            Quad(sky, PlaceholderArt.Glow(), new Color(0.6f, 0.7f, 0.9f, 0.5f), new Vector3(5f, 5f, 0), new Vector3(6f, 6f, 1f), SceneBand.Sky, 0.2f);

            var far = NewLayer(worldGo.transform, "FarBackground", SceneBand.FarBackground, 0.15f);
            for (int i = -4; i <= 4; i++)
                Quad(far, PlaceholderArt.SolidBlock(), theme.structures,
                    new Vector3(i * 4f, Random.Range(1f, 3f), 0),
                    new Vector3(2.2f, Random.Range(4f, 8f), 1f), SceneBand.FarBackground, 0.3f);

            var mid = NewLayer(worldGo.transform, "MidBackground", SceneBand.MidBackground, 0.35f);
            for (int i = -4; i <= 4; i++)
                Quad(mid, PlaceholderArt.SolidBlock(), PlaceholderArt.Shadow,
                    new Vector3(i * 3f, 0.5f, 0), new Vector3(1.0f, 6f, 1f), SceneBand.MidBackground, 0.4f);

            var leftEnv = NewLayer(worldGo.transform, "LeftEnvironment", SceneBand.LeftEnvironment, 0.6f);
            Quad(leftEnv, PlaceholderArt.SolidBlock(), theme.walls, new Vector3(-6.5f, 0f, 0), new Vector3(6f, 16f, 1f), SceneBand.LeftEnvironment, 0.5f);
            var rightEnv = NewLayer(worldGo.transform, "RightEnvironment", SceneBand.RightEnvironment, 0.6f);
            Quad(rightEnv, PlaceholderArt.SolidBlock(), theme.walls, new Vector3(6.5f, 0f, 0), new Vector3(6f, 16f, 1f), SceneBand.RightEnvironment, 0.5f);

            if (lanterns)
                for (int i = 0; i < 4; i++)
                {
                    float y = 3f - i * 1.6f;
                    Quad(leftEnv, PlaceholderArt.Glow(), theme.accent, new Vector3(-3.8f, y, -0.1f), new Vector3(1.4f, 1.4f, 1f), SceneBand.LeftEnvironment, 0.6f);
                    Quad(rightEnv, PlaceholderArt.Glow(), theme.accent, new Vector3(3.8f, y, -0.1f), new Vector3(1.4f, 1.4f, 1f), SceneBand.RightEnvironment, 0.6f);
                }

            var floor = NewLayer(worldGo.transform, "Floor", SceneBand.Floor, 0.5f);
            Quad(floor, PlaceholderArt.SolidBlock(), theme.floor, new Vector3(0, -4.5f, 0), new Vector3(48f, 6f, 1f), SceneBand.Floor, 0.5f);

            var fog = NewLayer(worldGo.transform, "Fog", SceneBand.Fog, 0.9f);
            fog.driftPerSecond = new Vector2(0.15f, 0f);
            Quad(fog, PlaceholderArt.Glow(), theme.fog, new Vector3(0, -2.5f, 0), new Vector3(26f, 8f, 1f), SceneBand.Fog, 0.5f);

            parallax.CollectLayers();
            return parallax;
        }

        /// <summary>Create an orthographic camera themed for a 2.5D scene.</summary>
        public static Camera BuildCamera(Transform parent, Color background, out ScriptedCamera scripted)
        {
            GameObject camGo = new GameObject("Scene25DCamera");
            camGo.transform.SetParent(parent, false);
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.transform.position = new Vector3(0f, 0f, -15f);
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();

            scripted = camGo.AddComponent<ScriptedCamera>();
            scripted.shots.Clear();
            scripted.shots.Add(new ScriptedCamera.Shot
            {
                name = "Default",
                position = new Vector3(0f, 0f, -15f),
                eulerAngles = Vector3.zero,
                orthographicSize = 5f,
                blendSeconds = 0.6f
            });
            return cam;
        }

        private static ParallaxLayer NewLayer(Transform parent, string name, SceneBand band, float hFactor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0, LayerSorting.BandZ(band));
            var layer = go.AddComponent<ParallaxLayer>();
            layer.depth = (int)band;
            layer.horizontalFactor = hFactor;
            return layer;
        }

        private static SpriteRenderer Quad(Component parent, Sprite sprite, Color color,
            Vector3 localPos, Vector3 localScale, SceneBand band, float depth01)
        {
            SpriteRenderer sr = PlaceholderArt.NewSpriteObject("piece", sprite, color, parent.transform, band, depth01);
            sr.transform.localPosition = localPos;
            sr.transform.localScale = localScale;
            return sr;
        }
    }
}
