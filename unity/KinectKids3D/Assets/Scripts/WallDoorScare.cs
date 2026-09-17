using System.Linq;
using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// En riktig dörr i sidoväggen. Varelsen bakom dörren är helt dold tills
    /// vagnen är nära; därefter öppnas dörren och armar/kropp skjuts ut.
    /// </summary>
    public sealed class WallDoorScare : MonoBehaviour
    {
        private Transform door;
        private Transform creature;
        private Renderer[] creatureRenderers;
        private Quaternion doorClosed;
        private Quaternion doorOpen;
        private Vector3 hiddenPosition;
        private Vector3 lungePosition;
        private float trackZ;
        private float phase;
        private bool revealed;
        private AudioSource scareAudio;
        private AudioClip[] revealSounds;
        private AudioClip[] creatureSounds;
        private GhostTarget shootable;

        public static WallDoorScare Create(float z, int side, int route,
            Material wood, Material stone, Material metal)
        {
            GameObject root = new GameObject("Väggdörr med gömd varelse");
            root.transform.position = new Vector3(DarkRideWorld.TrackCenter(z, route), 0f, z);
            root.transform.rotation = DarkRideWorld.TrackRotation(z, route);
            WallDoorScare scare = root.AddComponent<WallDoorScare>();
            scare.trackZ = z;
            scare.phase = Random.value * Mathf.PI * 2f;
            scare.Build(side < 0 ? -1 : 1, wood, stone, metal);
            return scare;
        }

        private void Build(int side, Material wood, Material stone, Material metal)
        {
            scareAudio = gameObject.AddComponent<AudioSource>();
            scareAudio.playOnAwake = false;
            scareAudio.spatialBlend = 0.82f;
            scareAudio.minDistance = 2.5f;
            scareAudio.maxDistance = 24f;
            scareAudio.rolloffMode = AudioRolloffMode.Linear;
            revealSounds = new[]
            {
                Resources.Load<AudioClip>("Audio/SFX/Doors/door_creak_open"),
                Resources.Load<AudioClip>("Audio/SFX/Doors/floor_creak_01"),
                Resources.Load<AudioClip>("Audio/SFX/Doors/floor_creak_02"),
                Resources.Load<AudioClip>("Audio/SFX/Doors/floor_creak_03")
            }.Where(clip => clip != null).ToArray();
            creatureSounds = Resources.LoadAll<AudioClip>("Audio/SFX/Creatures")
                .Where(clip => clip != null && clip.name.StartsWith("ghost_moan_"))
                .ToArray();

            // Dörren sitter på en riktig gångjärnspunkt i öppningens bakkant.
            // Då svänger hela bladet undan i stället för att rotera mitt i muren.
            door = new GameObject("Gångjärn för sidodörr").transform;
            door.SetParent(transform, false);
            door.localPosition = new Vector3(side * 5.38f, 0f, 1.72f);
            doorClosed = Quaternion.identity;
            doorOpen = Quaternion.Euler(0f, -side * 96f, 0f);
            AddPart(door, "Massivt träblad", new Vector3(0f, 1.85f, -1.72f),
                new Vector3(0.24f, 3.70f, 3.38f), wood);
            AddPart(door, "Övre järnband", new Vector3(-side * 0.14f, 2.85f, -1.72f),
                new Vector3(0.10f, 0.16f, 2.85f), metal);
            AddPart(door, "Nedre järnband", new Vector3(-side * 0.14f, 0.85f, -1.72f),
                new Vector3(0.10f, 0.16f, 2.85f), metal);

            // En liten faktisk nisch bakom hålet gör att spelaren ser in i
            // mörkret, i stället för att mötas av den obrutna korridorväggen.
            AddPart(transform, "Nischens bakvägg", new Vector3(side * 7.05f, 2.0f, 0f),
                new Vector3(0.25f, 4.0f, 3.75f), stone);
            AddPart(transform, "Nischens främre kant", new Vector3(side * 5.42f, 3.92f, 0f),
                new Vector3(0.48f, 0.42f, 4.15f), stone);
            AddPart(transform, "Nischens vänstra sida", new Vector3(side * 6.20f, 2.0f, -1.88f),
                new Vector3(1.75f, 4.0f, 0.28f), stone);
            AddPart(transform, "Nischens högra sida", new Vector3(side * 6.20f, 2.0f, 1.88f),
                new Vector3(1.75f, 4.0f, 0.28f), stone);
            AddPart(transform, "Dörrpost fram", new Vector3(side * 5.28f, 1.95f, -1.88f),
                new Vector3(0.48f, 3.9f, 0.34f), metal);
            AddPart(transform, "Dörrpost bak", new Vector3(side * 5.28f, 1.95f, 1.88f),
                new Vector3(0.48f, 3.9f, 0.34f), metal);

            GameObject recessLightObject = new GameObject("Svagt ljus inne i dörrnischen");
            recessLightObject.transform.SetParent(transform, false);
            recessLightObject.transform.localPosition = new Vector3(side * 6.55f, 2.15f, 0f);
            Light recessLight = recessLightObject.AddComponent<Light>();
            recessLight.type = LightType.Point;
            recessLight.color = new Color(0.78f, 0.045f, 0.018f);
            recessLight.intensity = 1.35f;
            recessLight.range = 4.2f;
            recessLight.shadows = LightShadows.None;

            GameObject holder = new GameObject("Armar och monster bakom dörren");
            holder.transform.SetParent(transform, false);
            creature = holder.transform;
            hiddenPosition = new Vector3(side * 6.55f, 0.12f, 0.15f);
            lungePosition = new Vector3(side * 3.55f, 0.16f, -0.48f);
            creature.localPosition = hiddenPosition;

            string[] monsters =
            {
                "Models/KenneyGraveyard/character-zombie",
                "Models/KenneyGraveyard/character-skeleton",
                "Models/KenneyGraveyard/character-vampire",
                "Models/CuteMonsters/Cthulhu",
                "Models/CuteMonsters/Demon"
            };
            string path = monsters[Random.Range(0, monsters.Length)];
            ImportedModelFactory.Create(path, creature, "Varelsen som griper ur dörren",
                new Vector3(0f, 1.28f, 0f), 2.75f, Quaternion.Euler(0f, 180f, 0f),
                "attack", "punch", "walk", "idle");
            creatureRenderers = creature.GetComponentsInChildren<Renderer>(true);
            shootable = GhostTarget.AttachExisting(holder, 2, new Vector3(0f, 1.25f, 0f),
                2.75f, 0.82f);
            shootable.SetTargetable(false);
            SetCreatureVisible(false);
        }

        private void Update()
        {
            if (Camera.main == null) return;
            float gap = trackZ - Camera.main.transform.position.z;
            float amount = 0f;
            if (gap <= 8f && gap >= 1.2f)
                amount = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8f, 2.8f, gap));
            else if (gap < 1.2f && gap >= -3f)
                amount = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.2f, -3f, gap));

            if (!revealed && amount > 0.02f)
            {
                revealed = true;
                SetCreatureVisible(true);
                if (shootable != null) shootable.SetTargetable(true);
                PlayRevealSounds();
            }

            if (door != null) door.localRotation = Quaternion.Slerp(doorClosed, doorOpen, amount);
            if (creature != null)
            {
                creature.localPosition = Vector3.Lerp(hiddenPosition, lungePosition, amount)
                    + Vector3.up * Mathf.Sin(Time.time * 10f + phase) * 0.055f * amount;
                creature.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Sin(Time.time * 7f + phase) * 7f * amount);
            }

            if (revealed && gap < -3.2f)
            {
                SetCreatureVisible(false);
                enabled = false;
            }
        }

        private void PlayRevealSounds()
        {
            if (scareAudio == null) return;
            scareAudio.pitch = Random.Range(0.92f, 1.04f);
            if (revealSounds != null && revealSounds.Length > 0)
                scareAudio.PlayOneShot(revealSounds[Random.Range(0, revealSounds.Length)], 0.72f);
            if (creatureSounds != null && creatureSounds.Length > 0)
                scareAudio.PlayOneShot(creatureSounds[Random.Range(0, creatureSounds.Length)], 0.62f);
        }

        private void SetCreatureVisible(bool visible)
        {
            if (creatureRenderers == null) return;
            foreach (Renderer renderer in creatureRenderers) renderer.enabled = visible;
        }

        private static void AddPart(Transform parent, string partName, Vector3 position,
            Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.Destroy(part.GetComponent<Collider>());
        }
    }
}
