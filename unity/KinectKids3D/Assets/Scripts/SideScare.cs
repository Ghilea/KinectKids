using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Ett riktigt 3D-överraskningsmoment som kastar sig in från sidan av vagnen
    /// och försvinner igen. Objektet följer kameran så att överraskningen fungerar
    /// likadant genom både raka och kurvande delar av rälsen.
    /// </summary>
    public sealed class SideScare : MonoBehaviour
    {
        private float startedAt;
        private int side;
        private Vector3 start;
        private Vector3 lunge;

        public static SideScare Create(Transform cameraTransform, int side)
        {
            GameObject root = new GameObject(side < 0
                ? "Något kastar sig fram från vänster"
                : "Något kastar sig fram från höger");
            root.transform.SetParent(cameraTransform, false);
            SideScare scare = root.AddComponent<SideScare>();
            scare.side = side < 0 ? -1 : 1;
            scare.start = new Vector3(scare.side * 4.8f, -0.18f, 6.8f);
            scare.lunge = new Vector3(scare.side * 1.48f, -0.12f, 3.15f);
            scare.startedAt = Time.time;
            scare.BuildModel();
            return scare;
        }

        private void Update()
        {
            float age = Time.time - startedAt;
            float amount;
            if (age < 0.34f)
                amount = Mathf.SmoothStep(0f, 1f, age / 0.34f);
            else if (age < 0.82f)
                amount = 1f + Mathf.Sin(age * 20f) * 0.025f;
            else
                amount = 1f - Mathf.SmoothStep(0f, 1f, (age - 0.82f) / 0.62f);

            transform.localPosition = Vector3.Lerp(start, lunge, amount)
                + Vector3.up * Mathf.Sin(age * 13f) * 0.07f;
            transform.localRotation = Quaternion.Euler(
                Mathf.Sin(age * 9f) * 4f,
                -side * (12f + amount * 18f),
                -side * (7f + Mathf.Sin(age * 15f) * 4f));
            transform.localScale = Vector3.one * Mathf.Lerp(0.66f, 1.08f, amount);
            if (age >= 1.48f) Destroy(gameObject);
        }

        private void BuildModel()
        {
            Material cloth = DarkRideWorld.TexturedMaterial(new Color(0.31f, 0.25f, 0.29f),
                HauntedTextureFactory.TatteredCloth(811 + side), 0f);
            Material skin = DarkRideWorld.TexturedMaterial(new Color(0.40f, 0.45f, 0.34f),
                HauntedTextureFactory.RottenSkin(919 + side), 0f);
            Material cavity = DarkRideWorld.MaterialOf(new Color(0.012f, 0.004f, 0.006f));
            Material eye = DarkRideWorld.GlowMaterial(new Color(0.85f, 0.018f, 0.006f), 1.8f);

            Register(LowPolyMeshFactory.CreateTatteredBody(transform, "Trasig kropp", cloth,
                2.05f, 0.39f, 0.76f, 0.68f, 712 + side));
            AddPart(PrimitiveType.Sphere, "Sned huva", new Vector3(0f, 2.12f, 0f),
                new Vector3(0.82f, 0.88f, 0.72f), cloth);
            AddPart(PrimitiveType.Sphere, "Mörkt ansikte", new Vector3(0f, 2.08f, -0.57f),
                new Vector3(0.57f, 0.64f, 0.16f), cavity);
            AddPart(PrimitiveType.Sphere, "Vänster öga", new Vector3(-0.22f, 2.20f, -0.68f),
                Vector3.one * 0.12f, eye);
            AddPart(PrimitiveType.Sphere, "Höger öga", new Vector3(0.22f, 2.20f, -0.68f),
                Vector3.one * 0.12f, eye);
            AddPart(PrimitiveType.Sphere, "Gap", new Vector3(0f, 1.89f, -0.69f),
                new Vector3(0.28f, 0.20f, 0.07f), cavity);

            for (int armSide = -1; armSide <= 1; armSide += 2)
            {
                Vector3 shoulder = new Vector3(armSide * 0.52f, 1.55f, -0.05f);
                Vector3 elbow = new Vector3(armSide * 0.98f, 1.28f, -0.38f);
                Vector3 claw = new Vector3(armSide * 1.24f, 1.10f, -0.92f);
                Register(LowPolyMeshFactory.CreateTaperedLimb(transform, "Gripande arm",
                    shoulder, elbow, 0.18f, 0.12f, skin));
                Register(LowPolyMeshFactory.CreateTaperedLimb(transform, "Gripande underarm",
                    elbow, claw, 0.12f, 0.035f, skin));
            }
        }

        private static void Register(GameObject model)
        {
            Renderer renderer = model.GetComponent<Renderer>();
            if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        private void AddPart(PrimitiveType type, string partName, Vector3 position,
            Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = new Material(material);
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
    }
}
