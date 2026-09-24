using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class DarkRideWorld
    {
        public static Material MaterialOf(Color color, float metallic = 0f)
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