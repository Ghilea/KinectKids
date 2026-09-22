using UnityEngine;
using KinectKids.Scene25D;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// Builds the layered 2.5D environment for the chase, following the layer
    /// list in new-way.md (floor, left/right environment, mid, background,
    /// foreground, fog, light FX). Everything is placeholder art parented under
    /// <see cref="ParallaxLayer"/>s so final illustrations can replace any layer
    /// without touching the chase logic.
    /// </summary>
    public static class ChaseEnvironmentBuilder
    {
        public static ParallaxController Build(Transform parent)
        {
            GameObject worldGo = new GameObject("ChaseWorld");
            worldGo.transform.SetParent(parent, false);
            var parallax = worldGo.AddComponent<ParallaxController>();

            // Sky / far background: slow, mostly still.
            var sky = NewLayer(worldGo.transform, "Sky", SceneBand.Sky, depth: 8f, hFactor: 0.05f);
            Quad(sky, PlaceholderArt.SolidBlock(), PlaceholderArt.NightBlue,
                new Vector3(0, 2f, 0), new Vector3(40f, 24f, 1f), SceneBand.Sky, 0.1f);
            // A cold moon glow.
            Quad(sky, PlaceholderArt.Glow(), new Color(0.6f, 0.7f, 0.9f, 0.5f),
                new Vector3(4f, 5f, 0), new Vector3(6f, 6f, 1f), SceneBand.Sky, 0.2f);

            // Far background silhouette (castle towers).
            var far = NewLayer(worldGo.transform, "FarBackground", SceneBand.FarBackground, depth: 6f, hFactor: 0.15f);
            for (int i = -3; i <= 3; i++)
            {
                Quad(far, PlaceholderArt.SolidBlock(), new Color(0.10f, 0.09f, 0.18f),
                    new Vector3(i * 4f, Random.Range(1f, 3f), 0),
                    new Vector3(2.2f, Random.Range(4f, 8f), 1f), SceneBand.FarBackground, 0.3f);
            }

            // Mid background (arches / pillars) with more parallax.
            var mid = NewLayer(worldGo.transform, "MidBackground", SceneBand.MidBackground, depth: 4f, hFactor: 0.35f);
            for (int i = -3; i <= 3; i++)
            {
                Quad(mid, PlaceholderArt.SolidBlock(), PlaceholderArt.Shadow,
                    new Vector3(i * 3f, 0.5f, 0), new Vector3(1.0f, 6f, 1f), SceneBand.MidBackground, 0.4f);
            }

            // Left & right environment walls, strong parallax (converging toward horizon).
            var leftEnv = NewLayer(worldGo.transform, "LeftEnvironment", SceneBand.LeftEnvironment, depth: 2f, hFactor: 0.6f);
            Quad(leftEnv, PlaceholderArt.SolidBlock(), PlaceholderArt.DeepPurple,
                new Vector3(-6.5f, 0f, 0), new Vector3(6f, 16f, 1f), SceneBand.LeftEnvironment, 0.5f);
            var rightEnv = NewLayer(worldGo.transform, "RightEnvironment", SceneBand.RightEnvironment, depth: 2f, hFactor: 0.6f);
            Quad(rightEnv, PlaceholderArt.SolidBlock(), PlaceholderArt.DeepPurple,
                new Vector3(6.5f, 0f, 0), new Vector3(6f, 16f, 1f), SceneBand.RightEnvironment, 0.5f);

            // Lanterns for warm accents (new-way.md palette).
            for (int i = 0; i < 4; i++)
            {
                float y = 3f - i * 1.6f;
                Quad(leftEnv, PlaceholderArt.Glow(), PlaceholderArt.LanternOrange,
                    new Vector3(-3.8f, y, -0.1f), new Vector3(1.4f, 1.4f, 1f), SceneBand.LeftEnvironment, 0.6f);
                Quad(rightEnv, PlaceholderArt.Glow(), PlaceholderArt.LanternOrange,
                    new Vector3(3.8f, y, -0.1f), new Vector3(1.4f, 1.4f, 1f), SceneBand.RightEnvironment, 0.6f);
            }

            // Floor.
            var floor = NewLayer(worldGo.transform, "Floor", SceneBand.Floor, depth: 1f, hFactor: 0.5f);
            Quad(floor, PlaceholderArt.SolidBlock(), new Color(0.15f, 0.12f, 0.20f),
                new Vector3(0, -4.5f, 0), new Vector3(40f, 6f, 1f), SceneBand.Floor, 0.5f);

            // Foreground detail that whips past fastest.
            var fg = NewLayer(worldGo.transform, "Foreground", SceneBand.Foreground, depth: 0.2f, hFactor: 1.4f);
            for (int i = -2; i <= 2; i++)
            {
                Quad(fg, PlaceholderArt.SolidBlock(), new Color(0.05f, 0.04f, 0.09f),
                    new Vector3(i * 8f, -3.5f, 0), new Vector3(1.6f, 4f, 1f), SceneBand.Foreground, 0.8f);
            }

            // Fog layer with slow idle drift.
            var fog = NewLayer(worldGo.transform, "Fog", SceneBand.Fog, depth: 0.5f, hFactor: 0.9f);
            fog.driftPerSecond = new Vector2(0.15f, 0f);
            var fogSr = Quad(fog, PlaceholderArt.Glow(), new Color(0.5f, 0.5f, 0.65f, 0.18f),
                new Vector3(0, -2.5f, 0), new Vector3(24f, 8f, 1f), SceneBand.Fog, 0.5f);
            fogSr.name = "FogSheet";

            parallax.CollectLayers();
            return parallax;
        }

        private static ParallaxLayer NewLayer(Transform parent, string name, SceneBand band,
            float depth, float hFactor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0, LayerSorting.BandZ(band));
            var layer = go.AddComponent<ParallaxLayer>();
            layer.depth = depth;
            layer.horizontalFactor = hFactor;
            layer.verticalFactor = 0f;
            return layer;
        }

        private static SpriteRenderer Quad(Component parent, Sprite sprite, Color color,
            Vector3 localPos, Vector3 localScale, SceneBand band, float depth01)
        {
            SpriteRenderer sr = PlaceholderArt.NewSpriteObject("piece", sprite, color,
                parent.transform, band, depth01);
            sr.transform.localPosition = localPos;
            sr.transform.localScale = localScale;
            return sr;
        }
    }
}
