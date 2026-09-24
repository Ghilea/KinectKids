using UnityEngine;

namespace KinectKids3D
{
    public enum MiniBossKind
    {
        GiantSpider,
        Vampire,
        PossessedArmor
    }

    public sealed class MiniBossTarget : MonoBehaviour
    {
        public MiniBossKind Kind { get; private set; }

        public static GhostTarget Create(MiniBossKind kind, Vector3 position)
        {
            GameObject root = new GameObject("Miniboss - " + kind);
            root.transform.position = position;
            MiniBossTarget marker = root.AddComponent<MiniBossTarget>();
            marker.Kind = kind;
            string path = kind == MiniBossKind.GiantSpider ? "Models/Quaternius/Spider"
                : kind == MiniBossKind.Vampire ? "Models/KenneyGraveyard/character-vampire"
                : "Models/QuaterniusKnight/KnightCharacter";
            Quaternion rotation = kind == MiniBossKind.GiantSpider
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.Euler(0f, 180f, 0f);
            GameObject model = ImportedModelFactory.Create(path, root.transform,
                "Animerad " + kind, new Vector3(0f, 1.55f, 0f), 4.1f, rotation,
                "attack", "walk", "idle", "move");
            if (model == null)
            {
                Material body = DarkRideWorld.MaterialOf(new Color(0.16f, 0.035f, 0.075f), 0.25f);
                AddFallback(root.transform, body);
            }
            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = kind == MiniBossKind.PossessedArmor
                ? new Color(0.15f, 0.42f, 1f) : new Color(0.82f, 0.025f, 0.035f);
            light.intensity = 2.4f;
            light.range = 5.5f;
            light.shadows = LightShadows.None;
            return GhostTarget.AttachExisting(root, 6, new Vector3(0f, 1.55f, 0f), 3.7f, 1.22f);
        }

        private static void AddFallback(Transform parent, Material material)
        {
            GameObject body = PrimitiveBuilder.Create(PrimitiveType.Capsule, "Minibossens reservkropp",
                parent, new Vector3(0f, 1.5f, 0f), new Vector3(1.35f, 1.55f, 1.15f));
            body.GetComponent<Renderer>().material = new Material(material);
        }
    }
}
