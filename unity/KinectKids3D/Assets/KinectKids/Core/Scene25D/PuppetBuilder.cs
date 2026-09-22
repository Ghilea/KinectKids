using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// Assembles placeholder <see cref="PuppetRig"/> characters from separate
    /// silhouette layers, exactly the body-part split new-way.md lists for the
    /// player and Greve Gast. Final sprite layers can later be dropped into the
    /// same part names without changing gameplay or pose code.
    /// </summary>
    public static class PuppetBuilder
    {
        /// <summary>
        /// Player seen from the front (runs toward camera). Layers: shadow, legs,
        /// hips, torso, arms, head, hair. Poses: run, duckDown, jumpUp, leanLeft,
        /// leanRight, idle.
        /// </summary>
        public static PuppetRig BuildPlayer(Transform parent)
        {
            GameObject root = new GameObject("Player");
            if (parent != null) root.transform.SetParent(parent, false);
            var rig = root.AddComponent<PuppetRig>();
            rig.idleBob = 0.03f;

            Color skin = new Color(0.95f, 0.78f, 0.55f);
            Color shirt = new Color(0.20f, 0.55f, 0.85f);
            Color pants = new Color(0.25f, 0.22f, 0.35f);
            Color hair = new Color(0.30f, 0.18f, 0.10f);

            AddLayer(rig, "shadow", PlaceholderArt.Glow(), new Color(0, 0, 0, 0.4f),
                new Vector3(0, -1.55f, 0), new Vector3(1.4f, 0.4f, 1f), SceneBand.Actors, 0.1f);
            AddLayer(rig, "legLeft", PlaceholderArt.Capsule(40, 90), pants,
                new Vector3(-0.22f, -1.0f, 0), Vector3.one * 0.01f, SceneBand.Actors, 0.45f);
            AddLayer(rig, "legRight", PlaceholderArt.Capsule(40, 90), pants,
                new Vector3(0.22f, -1.0f, 0), Vector3.one * 0.01f, SceneBand.Actors, 0.45f);
            AddLayer(rig, "torso", PlaceholderArt.Capsule(90, 120), shirt,
                new Vector3(0, -0.1f, 0), Vector3.one * 0.011f, SceneBand.Actors, 0.5f);
            AddLayer(rig, "armLeft", PlaceholderArt.Capsule(30, 90), shirt,
                new Vector3(-0.55f, -0.1f, 0), Vector3.one * 0.009f, SceneBand.Actors, 0.55f);
            AddLayer(rig, "armRight", PlaceholderArt.Capsule(30, 90), shirt,
                new Vector3(0.55f, -0.1f, 0), Vector3.one * 0.009f, SceneBand.Actors, 0.55f);
            AddLayer(rig, "head", PlaceholderArt.Capsule(70, 80), skin,
                new Vector3(0, 0.85f, 0), Vector3.one * 0.009f, SceneBand.Actors, 0.6f);
            AddLayer(rig, "hair", PlaceholderArt.Capsule(74, 50), hair,
                new Vector3(0, 1.05f, 0), Vector3.one * 0.009f, SceneBand.Actors, 0.62f);

            rig.AddPose(new PuppetPose("idle"));
            rig.AddPose(new PuppetPose("run")
                .Set("legLeft", new Vector2(0.05f, 0.1f), 20f)
                .Set("legRight", new Vector2(-0.05f, 0.1f), -20f)
                .Set("armLeft", new Vector2(0.05f, 0.05f), -25f)
                .Set("armRight", new Vector2(-0.05f, 0.05f), 25f));
            rig.AddPose(new PuppetPose("duck")
                .Set("torso", new Vector2(0, -0.5f), 0f, 0.8f)
                .Set("head", new Vector2(0, -0.55f))
                .Set("hair", new Vector2(0, -0.55f)));
            rig.AddPose(new PuppetPose("jump")
                .Set("legLeft", new Vector2(0.05f, 0.35f), 10f)
                .Set("legRight", new Vector2(-0.05f, 0.35f), -10f)
                .Set("armLeft", new Vector2(-0.05f, 0.35f), -45f)
                .Set("armRight", new Vector2(0.05f, 0.35f), 45f));
            rig.AddPose(new PuppetPose("left")
                .Set("torso", new Vector2(-0.35f, 0), 12f)
                .Set("head", new Vector2(-0.4f, 0), 12f)
                .Set("hair", new Vector2(-0.4f, 0), 12f));
            rig.AddPose(new PuppetPose("right")
                .Set("torso", new Vector2(0.35f, 0), -12f)
                .Set("head", new Vector2(0.4f, 0), -12f)
                .Set("hair", new Vector2(0.4f, 0), -12f));
            rig.SetPose("idle", true);
            return rig;
        }

        /// <summary>
        /// Greve Gast as a floating ghost puppet: hat, head, collar, cloak body,
        /// two long arms, wispy tail. Poses: chase, reach, threaten, stunned.
        /// </summary>
        public static PuppetRig BuildGreveGast(Transform parent)
        {
            GameObject root = new GameObject("GreveGast");
            if (parent != null) root.transform.SetParent(parent, false);
            var rig = root.AddComponent<PuppetRig>();
            rig.idleBob = 0.12f;
            rig.idleBobSpeed = 1.6f;

            Color cloak = PlaceholderArt.DeepPurple;
            Color darker = new Color(0.12f, 0.08f, 0.18f);
            Color glow = PlaceholderArt.GhostGlow;

            AddLayer(rig, "tail", PlaceholderArt.Glow(), new Color(glow.r, glow.g, glow.b, 0.35f),
                new Vector3(0, -1.6f, 0), new Vector3(1.6f, 1.2f, 1f), SceneBand.Actors, 0.3f);
            AddLayer(rig, "cloak", PlaceholderArt.GhostSilhouette(), cloak,
                new Vector3(0, 0, 0), Vector3.one * 0.02f, SceneBand.Actors, 0.5f);
            AddLayer(rig, "armLeft", PlaceholderArt.Capsule(26, 110), darker,
                new Vector3(-0.9f, 0.1f, 0), Vector3.one * 0.008f, SceneBand.Actors, 0.55f);
            AddLayer(rig, "armRight", PlaceholderArt.Capsule(26, 110), darker,
                new Vector3(0.9f, 0.1f, 0), Vector3.one * 0.008f, SceneBand.Actors, 0.55f);
            // Glowing eyes as a small overlay on the silhouette.
            AddLayer(rig, "eyes", PlaceholderArt.Glow(), PlaceholderArt.LanternOrange,
                new Vector3(0, 1.15f, 0), new Vector3(0.5f, 0.18f, 1f), SceneBand.Actors, 0.65f);

            rig.AddPose(new PuppetPose("idle"));
            rig.AddPose(new PuppetPose("chase")
                .Set("armLeft", new Vector2(0.1f, 0.1f), 15f)
                .Set("armRight", new Vector2(-0.1f, 0.1f), -15f));
            rig.AddPose(new PuppetPose("reach")
                .Set("armLeft", new Vector2(0.35f, 0.5f), 55f)
                .Set("armRight", new Vector2(-0.35f, 0.5f), -55f)
                .Set("cloak", new Vector2(0, 0.15f), 0f, 1.05f));
            rig.AddPose(new PuppetPose("threaten")
                .Set("armLeft", new Vector2(0.2f, 0.7f), 80f)
                .Set("armRight", new Vector2(-0.2f, 0.7f), -80f)
                .Set("eyes", new Vector2(0, 0.05f), 0f, 1.3f));
            rig.AddPose(new PuppetPose("stunned")
                .Set("cloak", new Vector2(0, -0.2f), 15f, 0.9f)
                .Set("armLeft", new Vector2(-0.2f, -0.3f), -40f)
                .Set("armRight", new Vector2(0.2f, -0.3f), 40f)
                .Set("eyes", Vector2.zero, 0f, 0.4f));
            rig.SetPose("idle", true);
            return rig;
        }

        private static void AddLayer(PuppetRig rig, string name, Sprite sprite, Color color,
            Vector3 localPos, Vector3 localScale, SceneBand band, float depth01)
        {
            SpriteRenderer sr = PlaceholderArt.NewSpriteObject(name, sprite, color, rig.transform, band, depth01);
            sr.transform.localPosition = localPos;
            sr.transform.localScale = localScale;
            rig.AddPart(name, sr);
        }
    }
}
