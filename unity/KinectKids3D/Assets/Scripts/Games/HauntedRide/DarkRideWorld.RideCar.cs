using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class DarkRideWorld
    {
        private void BuildRideCar(Transform cameraTransform)
        {
            GameObject car = new GameObject("Spökvagn");
            // Vagnen har sin egen stabila världsposition. Bara spelarens huvud/
            // kamera ska sjunka vid en duckning; annars ser det ut som om hela
            // vagnen faller genom golvet och vagnskanten försvinner ur bild.
            car.AddComponent<RideCarFollower>().Configure(cameraTransform);

            // En komplett vagnskorg behövs eftersom duckningen flyttar ned huvudet
            // och då visar mer av insidan. Tidigare fanns bara den övre framkanten,
            // vilket gjorde att vagnen såg ihålig ut under den.
            CreateChildCube(car.transform, "Vagnens träbotten", new Vector3(0f, -1.40f, 0.20f),
                new Vector3(3.05f, 0.18f, 3.65f), wood);
            CreateChildCube(car.transform, "Solid vagnfront", new Vector3(0f, -1.10f, 1.48f),
                new Vector3(3.16f, 0.82f, 0.34f), wood);
            CreateChildCube(car.transform, "Vänster vagnsida", new Vector3(-1.48f, -1.08f, 0.18f),
                new Vector3(0.22f, 0.84f, 3.30f), wood);
            CreateChildCube(car.transform, "Höger vagnsida", new Vector3(1.48f, -1.08f, 0.18f),
                new Vector3(0.22f, 0.84f, 3.30f), wood);
            CreateChildCube(car.transform, "Vagnens övre framkant", new Vector3(0f, -0.72f, 1.39f),
                new Vector3(3.28f, 0.24f, 0.54f), wood);
            CreateChildCube(car.transform, "Främre järnband", new Vector3(0f, -1.12f, 1.30f),
                new Vector3(3.20f, 0.10f, 0.08f), rail);
            CreateChildCube(car.transform, "Vänster kantbeslag", new Vector3(-1.58f, -0.83f, 0.25f),
                new Vector3(0.10f, 0.13f, 2.75f), rail);
            CreateChildCube(car.transform, "Höger kantbeslag", new Vector3(1.58f, -0.83f, 0.25f),
                new Vector3(0.10f, 0.13f, 2.75f), rail);
            CreateChildCube(car.transform, "Vänster lykta", new Vector3(-1.2f, -0.58f, 1.08f), new Vector3(0.22f, 0.22f, 0.22f), amberGlow);
            CreateChildCube(car.transform, "Höger lykta", new Vector3(1.2f, -0.58f, 1.08f), new Vector3(0.22f, 0.22f, 0.22f), amberGlow);
            GameObject lanternLight = new GameObject("Vagnens svaga lyktljus");
            lanternLight.transform.SetParent(car.transform, false);
            lanternLight.transform.localPosition = new Vector3(0f, -0.48f, 1.1f);
            Light light = lanternLight.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.24f, 0.035f);
            light.intensity = 0.65f;
            light.range = 6.5f;
            light.spotAngle = 58f;
            light.shadows = LightShadows.None;
            HauntedProp.Attach(lanternLight, HauntedMotion.Flicker, 0f, 8.2f);
            WagonDamageVisual.Attach(car);
        }

        private GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            value.name = name;
            value.transform.SetParent(root, false);
            value.transform.position = position;
            value.transform.localScale = scale;
            value.transform.rotation = rotation ?? Quaternion.identity;
            value.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(value.GetComponent<Collider>());
            return value;
        }

        private GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 position,
            Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(root, false);
            value.transform.position = position;
            value.transform.localScale = scale;
            value.transform.rotation = rotation ?? Quaternion.identity;
            value.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(value.GetComponent<Collider>());
            return value;
        }

        private static void CreateChildCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Cube);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localScale = scale;
            value.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(value.GetComponent<Collider>());
        }

        private static void CreateChildPrimitive(Transform parent, PrimitiveType type, string name,
            Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject value = GameObject.CreatePrimitive(type);
            value.name = name;
            value.transform.SetParent(parent, false);
            value.transform.localPosition = position;
            value.transform.localScale = scale;
            value.transform.localRotation = rotation ?? Quaternion.identity;
            value.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(value.GetComponent<Collider>());
        }

        private GameObject CreateSphere(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject value = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            value.name = name;
            value.transform.SetParent(root, false);
            value.transform.position = position;
            value.transform.localScale = scale;
            value.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(value.GetComponent<Collider>());
            return value;
        }
    }
}