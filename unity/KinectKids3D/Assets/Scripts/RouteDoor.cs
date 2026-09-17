using UnityEngine;

namespace KinectKids3D
{
    public sealed class RouteDoor : MonoBehaviour
    {
        private Transform leftLeaf;
        private Transform rightLeaf;
        private Quaternion leftClosed;
        private Quaternion rightClosed;
        private float openAmount;
        private bool opening;
        private bool automatic;
        private float trackZ;

        public int Route { get; private set; }

        public static RouteDoor Create(float z, int route, Material wood, Material metal, Material glow,
            bool automatic = false)
        {
            GameObject root = new GameObject(route < 0 ? "Vänster vägport" : "Höger vägport");
            root.transform.position = new Vector3(DarkRideWorld.TrackCenter(z, route), 0f, z);
            root.transform.rotation = DarkRideWorld.TrackRotation(z, route);
            RouteDoor door = root.AddComponent<RouteDoor>();
            door.Route = route;
            door.automatic = automatic;
            door.trackZ = z;
            door.Build(wood, metal, glow);
            return door;
        }

        public static void OpenRoute(int selectedRoute)
        {
            foreach (RouteDoor door in Object.FindObjectsByType<RouteDoor>(FindObjectsSortMode.None))
                if (!door.automatic) door.opening = door.Route == selectedRoute;
        }

        private void Update()
        {
            if (automatic && Camera.main != null)
                opening = trackZ - Camera.main.transform.position.z <= 11f;
            float target = opening ? 1f : 0f;
            openAmount = Mathf.MoveTowards(openAmount, target, Time.deltaTime * 0.62f);
            float eased = openAmount * openAmount * (3f - 2f * openAmount);
            leftLeaf.localRotation = leftClosed * Quaternion.Euler(0f, -102f * eased, 0f);
            rightLeaf.localRotation = rightClosed * Quaternion.Euler(0f, 102f * eased, 0f);
        }

        private void Build(Material wood, Material metal, Material glow)
        {
            GameObject importedPortal = ImportedModelFactory.Create(
                "Models/KayKit/wall_doorway", transform, "Importerad slottsportal",
                new Vector3(0f, 2.55f, 0.30f), 9.55f, Quaternion.Euler(0f, 180f, 0f));
            if (importedPortal == null)
            {
                AddPart(transform, "Dörrpost vänster", new Vector3(-4.45f, 2.55f, 0f), new Vector3(0.65f, 5.25f, 0.70f), metal);
                AddPart(transform, "Dörrpost höger", new Vector3(4.45f, 2.55f, 0f), new Vector3(0.65f, 5.25f, 0.70f), metal);
                AddPart(transform, "Dörröverstycke", new Vector3(0f, 5.0f, 0f), new Vector3(9.55f, 0.62f, 0.70f), metal);
            }

            leftLeaf = new GameObject("Vänster dörrblad").transform;
            leftLeaf.SetParent(transform, false);
            leftLeaf.localPosition = new Vector3(-4.05f, 0f, 0f);
            rightLeaf = new GameObject("Höger dörrblad").transform;
            rightLeaf.SetParent(transform, false);
            rightLeaf.localPosition = new Vector3(4.05f, 0f, 0f);
            leftClosed = leftLeaf.localRotation;
            rightClosed = rightLeaf.localRotation;

            GameObject importedLeft = ImportedModelFactory.Create(
                "Models/KenneyGraveyard/crypt-door", leftLeaf, "Vänster riktig kryptdörr",
                new Vector3(2.02f, 2.55f, 0f), 4.75f, Quaternion.identity);
            GameObject importedRight = ImportedModelFactory.Create(
                "Models/KenneyGraveyard/crypt-door", rightLeaf, "Höger riktig kryptdörr",
                new Vector3(-2.02f, 2.55f, 0f), 4.75f, Quaternion.Euler(0f, 180f, 0f));
            if (importedLeft == null || importedRight == null)
            {
                if (importedLeft != null) Destroy(importedLeft);
                if (importedRight != null) Destroy(importedRight);
                AddPart(leftLeaf, "Massiv vänsterdörr", new Vector3(2.02f, 2.55f, 0f), new Vector3(4.0f, 4.82f, 0.34f), wood);
                AddPart(rightLeaf, "Massiv högerdörr", new Vector3(-2.02f, 2.55f, 0f), new Vector3(4.0f, 4.82f, 0.34f), wood);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Transform leaf = side < 0 ? leftLeaf : rightLeaf;
                AddPart(leaf, "Rostigt dörrband", new Vector3(-side * 2.02f, 3.7f, -0.22f), new Vector3(3.55f, 0.16f, 0.12f), metal);
                AddPart(leaf, "Rostigt dörrband", new Vector3(-side * 2.02f, 1.35f, -0.22f), new Vector3(3.55f, 0.16f, 0.12f), metal);
                AddPart(leaf, "Lysande dörrsymbol", new Vector3(-side * 2.02f, 2.55f, -0.24f), Vector3.one * 0.30f, glow, PrimitiveType.Sphere);
            }
        }

        private static void AddPart(Transform parent, string name, Vector3 position, Vector3 scale,
            Material material, PrimitiveType type = PrimitiveType.Cube)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(part.GetComponent<Collider>());
        }
    }
}
