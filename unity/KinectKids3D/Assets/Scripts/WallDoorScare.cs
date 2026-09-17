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

        public static WallDoorScare Create(float z, int side, int route)
        {
            GameObject root = new GameObject("Väggdörr med gömd varelse");
            root.transform.position = new Vector3(DarkRideWorld.TrackCenter(z, route), 0f, z);
            root.transform.rotation = DarkRideWorld.TrackRotation(z, route);
            WallDoorScare scare = root.AddComponent<WallDoorScare>();
            scare.trackZ = z;
            scare.phase = Random.value * Mathf.PI * 2f;
            scare.Build(side < 0 ? -1 : 1);
            return scare;
        }

        private void Build(int side)
        {
            GameObject doorModel = ImportedModelFactory.Create(
                "Models/KenneyGraveyard/crypt-door", transform, "Stängd kryptdörr",
                new Vector3(side * 5.20f, 1.72f, 0f), 3.45f,
                Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f));
            if (doorModel != null)
            {
                door = doorModel.transform;
                doorClosed = door.localRotation;
                doorOpen = doorClosed * Quaternion.Euler(0f, side * 78f, 0f);
            }

            GameObject holder = new GameObject("Armar och monster bakom dörren");
            holder.transform.SetParent(transform, false);
            creature = holder.transform;
            hiddenPosition = new Vector3(side * 5.55f, 0.12f, 0.15f);
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

        private void SetCreatureVisible(bool visible)
        {
            if (creatureRenderers == null) return;
            foreach (Renderer renderer in creatureRenderers) renderer.enabled = visible;
        }
    }
}
