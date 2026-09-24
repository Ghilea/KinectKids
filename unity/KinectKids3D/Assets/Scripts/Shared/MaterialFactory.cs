using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Delad fabrik för runtime-material. Kapslar in valet av shader
    /// (Standard eller URP/Lit), en valfri material-mall från Resources, och
    /// de vanliga varianterna: enfärgat, texturerat och självlysande material.
    ///
    /// Implementationen flyttades hit från <see cref="DarkRideWorld"/> där den
    /// vuxit till en de-facto delad fabrik som redan används av båda spelen och
    /// alla prop-/mål-/scare-komponenter. Beteendet är oförändrat.
    /// </summary>
    public static class MaterialFactory
    {
        /// <summary>Enfärgat material med valfri metallic-nivå.</summary>
        public static Material Solid(Color color, float metallic = 0f)
        {
            Material template = Resources.Load<Material>("KinectKidsRuntimeStandard");
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = template != null ? new Material(template) : new Material(shader);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", metallic > 0 ? 0.78f : 0.2f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", metallic > 0 ? 0.78f : 0.2f);
            return material;
        }

        /// <summary>Texturerat material med tint och metallic-nivå.</summary>
        public static Material Textured(Color tint, Texture2D texture, float metallic)
        {
            Material material = Solid(tint, metallic);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTextureScale("_MainTex", new Vector2(1.15f, 1.15f));
            if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", new Vector2(1.15f, 1.15f));
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", metallic > 0 ? 0.42f : 0.05f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", metallic > 0 ? 0.42f : 0.05f);
            return material;
        }

        /// <summary>Självlysande (emissivt) material.</summary>
        public static Material Glow(Color color, float strength)
        {
            Material material = Solid(color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * strength);
            return material;
        }
    }
}
