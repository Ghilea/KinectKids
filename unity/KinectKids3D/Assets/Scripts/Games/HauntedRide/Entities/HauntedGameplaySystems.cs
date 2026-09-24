using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    /// <summary>Ger porträttögon en liten men tydlig rörelse mot spelaren.</summary>
    public sealed class WatchingEye : MonoBehaviour
    {
        private Vector3 home;

        private void Start()
        {
            home = transform.position;
        }

        private void LateUpdate()
        {
            if (Camera.main == null) return;
            Vector3 direction = Camera.main.transform.position - home;
            direction.y = Mathf.Clamp(direction.y, -1f, 1f);
            direction.x = Mathf.Clamp(direction.x, -1f, 1f);
            direction.z = Mathf.Clamp(direction.z, -1f, 1f);
            transform.position = Vector3.Lerp(transform.position,
                home + direction.normalized * 0.055f, 1f - Mathf.Exp(-7f * Time.deltaTime));
        }
    }

    /// <summary>
    /// Håller fienden dold tills dess varningssignal har spelats. Spelaren ser
    /// först ett pulserande sken och får därefter ett tydligt skottfönster.
    /// </summary>
    public sealed class MonsterTelegraph : MonoBehaviour
    {
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private GhostTarget target;
        private Light warningLight;
        private float revealDistance;
        private int side;
        private bool warned;
        private bool revealed;

        public static void Attach(GhostTarget target, float revealDistance, int side)
        {
            if (target == null || target.IsBoss) return;
            MonsterTelegraph telegraph = target.gameObject.AddComponent<MonsterTelegraph>();
            telegraph.target = target;
            telegraph.revealDistance = revealDistance;
            telegraph.side = side < 0 ? -1 : 1;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                telegraph.hiddenRenderers.Add(renderer);
                renderer.enabled = false;
            }
            target.SetTargetable(false);

            GameObject cue = new GameObject("Monsterförvarning");
            cue.transform.SetParent(target.transform, false);
            cue.transform.localPosition = Vector3.up * 1.65f;
            telegraph.warningLight = cue.AddComponent<Light>();
            telegraph.warningLight.type = LightType.Point;
            telegraph.warningLight.color = new Color(0.85f, 0.025f, 0.018f);
            telegraph.warningLight.range = 4.2f;
            telegraph.warningLight.intensity = 0f;
            telegraph.warningLight.shadows = LightShadows.None;
        }

        private void Update()
        {
            if (target == null || Camera.main == null) return;
            float gap = transform.position.z - Camera.main.transform.position.z;
            if (!warned && gap <= revealDistance + 5.5f && gap > -1f)
            {
                warned = true;
                HauntedRideGame.ReportThreatCue(side);
            }
            if (warned && !revealed && warningLight != null)
                warningLight.intensity = 1.1f + Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 2.3f;
            if (!revealed && gap <= revealDistance && gap > -1.5f) Reveal();
        }

        private void Reveal()
        {
            revealed = true;
            foreach (Renderer renderer in hiddenRenderers)
                if (renderer != null) renderer.enabled = true;
            if (warningLight != null) warningLight.intensity = 0.45f;
            if (target != null) target.SetTargetable(true);
            transform.localScale *= 0.12f;
            StartCoroutine(GrowIn());
        }

        private System.Collections.IEnumerator GrowIn()
        {
            Vector3 start = transform.localScale;
            Vector3 end = start / 0.12f;
            float elapsed = 0f;
            while (elapsed < 0.24f)
            {
                elapsed += Time.deltaTime;
                float amount = Mathf.SmoothStep(0f, 1f, elapsed / 0.24f);
                transform.localScale = Vector3.Lerp(start, end, amount);
                yield return null;
            }
            transform.localScale = end;
        }
    }

    /// <summary>Synliga sprickor, glöd och gnistor visar vagnens faktiska skick.</summary>
    public sealed class WagonDamageVisual : MonoBehaviour
    {
        public static WagonDamageVisual Instance { get; private set; }
        private readonly List<Renderer> damageLayers = new List<Renderer>();
        private Light damageGlow;

        public static WagonDamageVisual Attach(GameObject car)
        {
            WagonDamageVisual visual = car.AddComponent<WagonDamageVisual>();
            Instance = visual;
            Material crack = DarkRideWorld.GlowMaterial(new Color(1f, 0.07f, 0.015f), 1.7f);
            for (int i = 0; i < 4; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Synlig vagnskada " + (i + 1);
                shard.transform.SetParent(car.transform, false);
                shard.transform.localPosition = new Vector3(-1.05f + i * 0.70f,
                    -1.03f + (i % 2) * 0.24f, 1.30f);
                shard.transform.localScale = new Vector3(0.055f, 0.55f + i * 0.09f, 0.055f);
                shard.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -24f : 29f);
                Renderer renderer = shard.GetComponent<Renderer>();
                renderer.material = new Material(crack);
                renderer.enabled = false;
                visual.damageLayers.Add(renderer);
                Destroy(shard.GetComponent<Collider>());
            }
            GameObject glow = new GameObject("Skadeglöd från vagnen");
            glow.transform.SetParent(car.transform, false);
            glow.transform.localPosition = new Vector3(0f, -1.0f, 1.2f);
            visual.damageGlow = glow.AddComponent<Light>();
            visual.damageGlow.type = LightType.Point;
            visual.damageGlow.color = new Color(1f, 0.06f, 0.01f);
            visual.damageGlow.range = 3.2f;
            visual.damageGlow.intensity = 0f;
            visual.damageGlow.shadows = LightShadows.None;
            return visual;
        }

        public void SetHealth(int health, int maximum, bool impact)
        {
            int lost = Mathf.Clamp(maximum - health, 0, damageLayers.Count);
            for (int i = 0; i < damageLayers.Count; i++)
                if (damageLayers[i] != null) damageLayers[i].enabled = i < lost;
            if (damageGlow != null) damageGlow.intensity = lost <= 0 ? 0f : 0.35f + lost * 0.32f;
            if (impact)
                MagicBolt.CreateImpact(transform.position + transform.forward * 1.4f + Vector3.down * 0.85f, true);
        }
    }

    /// <summary>Ett skjutbart sigill som öppnar den dolda vänstervägen.</summary>
    public sealed class SecretRouteSeal : MonoBehaviour
    {
        public static GhostTarget Create(Vector3 position)
        {
            GameObject root = new GameObject("Sigill till den hemliga vägen");
            root.transform.position = position;
            root.AddComponent<SecretRouteSeal>();
            Material frame = DarkRideWorld.MaterialOf(new Color(0.09f, 0.08f, 0.07f), 0.65f);
            Material glow = DarkRideWorld.GlowMaterial(new Color(0.06f, 0.75f, 0.32f), 2.4f);
            Add(root.transform, PrimitiveType.Cylinder, "Gammalt stensigill", Vector3.zero,
                new Vector3(0.85f, 0.12f, 0.85f), frame, Quaternion.Euler(90f, 0f, 0f));
            Add(root.transform, PrimitiveType.Cube, "Sigillets lodräta runa", new Vector3(0f, 0f, -0.14f),
                new Vector3(0.13f, 1.15f, 0.08f), glow, Quaternion.identity);
            Add(root.transform, PrimitiveType.Cube, "Sigillets tvärruna", new Vector3(0f, 0f, -0.15f),
                new Vector3(1.15f, 0.13f, 0.08f), glow, Quaternion.Euler(0f, 0f, 45f));
            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.06f, 1f, 0.42f);
            light.range = 5f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
            return GhostTarget.AttachExisting(root, 2, Vector3.zero, 2f, 1.05f, TargetKind.Ghost);
        }

        private static void Add(Transform parent, PrimitiveType type, string name, Vector3 position,
            Vector3 scale, Material material, Quaternion rotation)
        {
            GameObject part = PrimitiveBuilder.Create(type, name, parent, position, scale, rotation);
            part.GetComponent<Renderer>().material = new Material(material);
        }
    }

    /// <summary>Skjutbar belöning som reparerar en skadad vagn.</summary>
    public sealed class RepairSigil : MonoBehaviour
    {
        public static GhostTarget Create(Vector3 position)
        {
            GameObject root = new GameObject("Magiskt reparationssigill");
            root.transform.position = position;
            root.AddComponent<RepairSigil>();
            Material glow = DarkRideWorld.GlowMaterial(new Color(0.08f, 0.75f, 1f), 2.8f);
            AddBar(root.transform, Vector3.zero, new Vector3(0.22f, 1.55f, 0.16f), glow);
            AddBar(root.transform, Vector3.zero, new Vector3(1.55f, 0.22f, 0.16f), glow);
            HauntedProp.Attach(root, HauntedMotion.Spin, 0.08f, 1.4f);
            return GhostTarget.AttachExisting(root, 1, Vector3.zero, 1.9f, 0.95f, TargetKind.Ghost);
        }

        private static void AddBar(Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = position;
            bar.transform.localScale = scale;
            bar.GetComponent<Renderer>().material = new Material(material);
            Destroy(bar.GetComponent<Collider>());
        }
    }
}
