using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    public sealed class GastLivingFog : MonoBehaviour
    {
        private Material bodyMaterial;
        private Material smokeMaterial;
        private EdgeMass edgeMass;
        private RearMass rearMass;
        private FloorMass floorMass;
        private TendrilSpawner tendrilSpawner;
        private WispsSmoke wisps;

        public void Initialize(Shader shader, float wallWidth, float wallHeight,
            bool corridorMode = false)
        {
            bodyMaterial = LivingMassMesh.Material(shader,
                new Color(0.002f, 0.002f, 0.006f, 1f),
                new Color(0.025f, 0.018f, 0.042f, 1f));
            smokeMaterial = LivingMassMesh.Material(shader,
                new Color(0.008f, 0.009f, 0.018f, 1f),
                new Color(0.052f, 0.058f, 0.086f, 1f), 0.38f);

            edgeMass = AddPart<EdgeMass>("EdgeMass");
            edgeMass.Initialize(bodyMaterial, wallWidth, wallHeight, corridorMode);
            if (corridorMode)
            {
                rearMass = AddPart<RearMass>("RearMass");
                rearMass.Initialize(bodyMaterial, wallWidth, wallHeight);
            }
            floorMass = AddPart<FloorMass>("FloorMass");
            floorMass.Initialize(bodyMaterial, wallWidth, corridorMode);
            tendrilSpawner = AddPart<TendrilSpawner>("TendrilSpawner");
            tendrilSpawner.Initialize(bodyMaterial, wallWidth, wallHeight, corridorMode);
            wisps = AddPart<WispsSmoke>("Wisps/Smoke");
            wisps.Initialize(smokeMaterial, wallWidth);
        }

        public void SetAggression(float value)
        {
            value = Mathf.Clamp01(value);
            if (edgeMass != null) edgeMass.Aggression = value;
            if (rearMass != null) rearMass.Aggression = value;
            if (floorMass != null) floorMass.Aggression = value;
            if (tendrilSpawner != null) tendrilSpawner.SetAggression(value);
            if (wisps != null) wisps.Aggression = value;
        }

        private T AddPart<T>(string name) where T : Component
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(transform, false);
            return part.AddComponent<T>();
        }

        private void OnDestroy()
        {
            if (bodyMaterial != null) Destroy(bodyMaterial);
            if (smokeMaterial != null) Destroy(smokeMaterial);
        }
    }
}
