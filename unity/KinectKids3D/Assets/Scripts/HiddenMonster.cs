using UnityEngine;

namespace KinectKids3D
{
    public enum HiddenMonsterKind
    {
        Portrait,
        Cabinet,
        Tomb
    }

    /// <summary>
    /// Miljömonster som först ser ut som en del av dekoren och sedan tittar eller
    /// kastar sig fram när vagnen är nära. De blir skjutbara först när de visar sig.
    /// </summary>
    public sealed class HiddenMonster : MonoBehaviour
    {
        private Transform monster;
        private Vector3 hiddenPosition;
        private Vector3 revealedPosition;
        private float trackZ;
        private float phase;
        private bool woke;
        private bool escaped;
        private GhostTarget shootable;
        private float targetableSince = -1f;

        public static HiddenMonster Create(float z, int side, int route, HiddenMonsterKind kind)
        {
            GameObject root = new GameObject("Gömt miljömonster - " + kind);
            root.transform.position = new Vector3(DarkRideWorld.TrackCenter(z, route), 0f, z);
            root.transform.rotation = DarkRideWorld.TrackRotation(z, route);
            HiddenMonster hidden = root.AddComponent<HiddenMonster>();
            hidden.trackZ = z;
            hidden.phase = Random.value * Mathf.PI * 2f;
            hidden.Build(side < 0 ? -1 : 1, kind);
            return hidden;
        }

        private void Update()
        {
            if (monster == null || Camera.main == null) return;
            float gap = trackZ - Camera.main.transform.position.z;
            float reveal = 0f;
            if (gap <= 13f && gap >= 1.2f)
                reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(13f, 5.2f, gap));
            else if (gap < 1.2f && gap >= -1.8f)
                reveal = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.2f, -1.8f, gap));

            if (reveal > 0.12f)
            {
                woke = true;
                if (targetableSince < 0f) targetableSince = Time.time;
                if (shootable != null) shootable.SetTargetable(true);
            }
            monster.localPosition = Vector3.Lerp(hiddenPosition, revealedPosition, reveal)
                + Vector3.up * Mathf.Sin(Time.time * 8f + phase) * 0.035f * reveal;
            monster.localRotation = Quaternion.Euler(
                Mathf.Sin(Time.time * 5f + phase) * 4f * reveal,
                180f,
                Mathf.Sin(Time.time * 9f + phase) * 7f * reveal);
            if (woke && gap < -2.2f)
            {
                if (!escaped && shootable != null && shootable.Health > 0
                    && Time.time - targetableSince >= 2.25f)
                {
                    escaped = true;
                    SpokjaktenGame.ReportMonsterEscape();
                }
                enabled = false;
            }
        }

        private void Build(int side, HiddenMonsterKind kind)
        {
            Material wood = DarkRideWorld.TexturedMaterial(new Color(0.28f, 0.16f, 0.08f),
                HauntedTextureFactory.OldWood(430 + (int)kind), 0f);
            Material stone = DarkRideWorld.TexturedMaterial(new Color(0.50f, 0.52f, 0.51f),
                HauntedTextureFactory.DampStone(520 + (int)kind,
                    new Color(0.22f, 0.24f, 0.23f), new Color(0.025f, 0.03f, 0.03f)), 0.03f);
            Material cloth = DarkRideWorld.TexturedMaterial(new Color(0.25f, 0.20f, 0.24f),
                HauntedTextureFactory.TatteredCloth(610 + side + (int)kind), 0f);
            Material face = DarkRideWorld.MaterialOf(new Color(0.025f, 0.018f, 0.018f));
            Material eye = DarkRideWorld.GlowMaterial(new Color(0.80f, 0.018f, 0.006f), 1.6f);

            float coverX = side * 4.60f;
            if (kind == HiddenMonsterKind.Portrait)
            {
                AddPart(transform, PrimitiveType.Cube, "Tung porträttram",
                    new Vector3(coverX, 2.55f, 0f), new Vector3(0.28f, 2.45f, 1.75f), wood);
                AddPart(transform, PrimitiveType.Cube, "Svart tavelyta",
                    new Vector3(coverX - side * 0.18f, 2.55f, 0f), new Vector3(0.08f, 1.93f, 1.27f), face);
            }
            else if (kind == HiddenMonsterKind.Cabinet)
            {
                AddPart(transform, PrimitiveType.Cube, "Murkna skåpet",
                    new Vector3(coverX, 1.35f, 0f), new Vector3(1.20f, 2.70f, 1.65f), wood);
                AddPart(transform, PrimitiveType.Cube, "Skåpsdörr på glänt",
                    new Vector3(coverX - side * 0.72f, 1.35f, -0.35f),
                    new Vector3(0.12f, 2.48f, 1.32f), wood,
                    Quaternion.Euler(0f, side * 24f, 0f));
            }
            else
            {
                AddPart(transform, PrimitiveType.Cube, "Sprucken stensarkofag",
                    new Vector3(coverX, 0.62f, 0f), new Vector3(2.15f, 1.15f, 1.35f), stone);
                AddPart(transform, PrimitiveType.Cube, "Förskjutet lock",
                    new Vector3(coverX - side * 0.30f, 1.28f, 0f), new Vector3(2.20f, 0.20f, 1.40f), stone,
                    Quaternion.Euler(0f, side * 7f, side * 10f));
            }

            GameObject body = new GameObject("Varelsen bakom föremålet");
            body.transform.SetParent(transform, false);
            monster = body.transform;
            hiddenPosition = new Vector3(side * 5.22f, kind == HiddenMonsterKind.Tomb ? 0.20f : 1.05f, 0.18f);
            revealedPosition = new Vector3(side * 3.45f, kind == HiddenMonsterKind.Tomb ? 0.34f : 0.92f, -0.48f);
            monster.localPosition = hiddenPosition;

            LowPolyMeshFactory.CreateTatteredBody(monster, "Hopkrupen trasig kropp", cloth,
                1.55f, 0.34f, 0.61f, 0.55f, 700 + side + (int)kind);
            AddPart(monster, PrimitiveType.Sphere, "Varelsens huvud", new Vector3(0f, 1.65f, 0f),
                new Vector3(0.69f, 0.76f, 0.62f), cloth);
            AddPart(monster, PrimitiveType.Sphere, "Ansiktshåla", new Vector3(0f, 1.61f, -0.49f),
                new Vector3(0.45f, 0.48f, 0.12f), face);
            AddPart(monster, PrimitiveType.Sphere, "Vänster öga", new Vector3(-0.17f, 1.72f, -0.57f),
                Vector3.one * 0.09f, eye);
            AddPart(monster, PrimitiveType.Sphere, "Höger öga", new Vector3(0.17f, 1.72f, -0.57f),
                Vector3.one * 0.09f, eye);
            for (int armSide = -1; armSide <= 1; armSide += 2)
                LowPolyMeshFactory.CreateTaperedLimb(monster, "Lång griparm",
                    new Vector3(armSide * 0.40f, 1.26f, 0f),
                    new Vector3(armSide * 0.82f, 0.82f, -0.72f), 0.13f, 0.025f, cloth);
            shootable = GhostTarget.AttachExisting(body, 2, new Vector3(0f, 1.05f, -0.12f),
                2.55f, 0.78f);
            shootable.SetTargetable(false);
        }

        private static GameObject AddPart(Transform parent, PrimitiveType type, string name,
            Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.transform.localRotation = rotation ?? Quaternion.identity;
            part.GetComponent<Renderer>().material = new Material(material);
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            return part;
        }
    }
}
