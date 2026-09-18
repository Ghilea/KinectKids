using System.Linq;
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
        private bool routeSelected;
        private float trackZ;
        private AudioSource doorAudio;
        private AudioClip[] doorSounds;

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
                if (!door.automatic) door.routeSelected = door.Route == selectedRoute;
        }

        public static void ResetAll()
        {
            foreach (RouteDoor door in Object.FindObjectsByType<RouteDoor>(FindObjectsSortMode.None))
                door.ResetDoor();
        }

        private void Update()
        {
            if (!opening && Camera.main != null && (automatic || routeSelected)
                && trackZ - Camera.main.transform.position.z <= 12f)
                BeginOpening();
            float target = opening ? 1f : 0f;
            openAmount = Mathf.MoveTowards(openAmount, target, Time.deltaTime * 0.62f);
            float eased = openAmount * openAmount * (3f - 2f * openAmount);
            leftLeaf.localRotation = leftClosed * Quaternion.Euler(0f, -102f * eased, 0f);
            rightLeaf.localRotation = rightClosed * Quaternion.Euler(0f, 102f * eased, 0f);
        }

        private void Build(Material wood, Material metal, Material glow)
        {
            doorAudio = gameObject.AddComponent<AudioSource>();
            doorAudio.playOnAwake = false;
            doorAudio.spatialBlend = 0.72f;
            doorAudio.minDistance = 3f;
            doorAudio.maxDistance = 28f;
            doorAudio.rolloffMode = AudioRolloffMode.Linear;
            doorSounds = new[]
            {
                Resources.Load<AudioClip>("Audio/SFX/Doors/door_creak_open"),
                Resources.Load<AudioClip>("Audio/SFX/Doors/door_open"),
                Resources.Load<AudioClip>("Audio/SFX/Doors/grind_stone")
            }.Where(clip => clip != null).ToArray();

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

            // Dörrbladen byggs utan en importerad dörranimation. Kryptmodellen
            // innehöll ett loopande klipp som slogs mot gångjärnsstyrningen och
            // fick bladen att flaxa fram och tillbaka.
            AddPart(leftLeaf, "Tydligt vänster dörrblad av trä", new Vector3(2.02f, 2.55f, -0.20f),
                new Vector3(4.0f, 4.82f, 0.24f), wood);
            AddPart(rightLeaf, "Tydligt höger dörrblad av trä", new Vector3(-2.02f, 2.55f, -0.20f),
                new Vector3(4.0f, 4.82f, 0.24f), wood);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform leaf = side < 0 ? leftLeaf : rightLeaf;
                AddPart(leaf, "Rostigt dörrband", new Vector3(-side * 2.02f, 3.7f, -0.22f), new Vector3(3.55f, 0.16f, 0.12f), metal);
                AddPart(leaf, "Rostigt dörrband", new Vector3(-side * 2.02f, 1.35f, -0.22f), new Vector3(3.55f, 0.16f, 0.12f), metal);
                AddPart(leaf, "Lysande dörrsymbol", new Vector3(-side * 2.02f, 2.55f, -0.24f), Vector3.one * 0.30f, glow, PrimitiveType.Sphere);
            }
        }

        private void BeginOpening()
        {
            if (opening) return;
            opening = true;
            if (doorAudio == null || doorSounds == null || doorSounds.Length == 0) return;
            doorAudio.pitch = Random.Range(0.92f, 1.04f);
            doorAudio.PlayOneShot(doorSounds[Random.Range(0, doorSounds.Length)],
                0.88f * SpokjaktenGame.CurrentEffectsVolume);
        }

        private void ResetDoor()
        {
            routeSelected = false;
            opening = false;
            openAmount = 0f;
            if (leftLeaf != null) leftLeaf.localRotation = leftClosed;
            if (rightLeaf != null) rightLeaf.localRotation = rightClosed;
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
