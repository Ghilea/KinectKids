using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KinectKids3D
{
    public enum TargetKind
    {
        Ghost,
        Zombie,
        ConductorBoss
    }

    public sealed partial class GhostTarget : MonoBehaviour
    {
        private static readonly HashSet<GhostTarget> activeTargets = new HashSet<GhostTarget>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Color> baseColors = new List<Color>();
        private float baseY;
        private float phase;
        private bool defeated;
        private Transform leftArm;
        private Transform rightArm;
        private bool externalMotion;
        private Collider hitArea;
        private Vector3 baseLocalScale = Vector3.one;
        private Quaternion baseLocalRotation = Quaternion.identity;

        public TargetKind Kind { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public bool IsBoss => Kind == TargetKind.ConductorBoss;
        public bool IsDefeated => defeated;
        public bool IsTargetable => !defeated && hitArea != null && hitArea.enabled;
        public static IEnumerable<GhostTarget> ActiveTargets => activeTargets.Where(target => target != null);

        public static GhostTarget Create(TargetKind kind, Vector3 position)
        {
            GameObject root = new GameObject(kind == TargetKind.ConductorBoss ? "Zombie-konduktören" : kind.ToString());
            root.transform.position = position;
            GhostTarget target = root.AddComponent<GhostTarget>();
            target.Kind = kind;
            target.MaxHealth = kind == TargetKind.ConductorBoss ? 16 : kind == TargetKind.Zombie ? 2 : 1;
            target.Health = target.MaxHealth;
            target.baseY = position.y;
            target.phase = Random.value * Mathf.PI * 2f;
            target.baseLocalScale = root.transform.localScale;
            target.baseLocalRotation = root.transform.localRotation;
            target.BuildModel();
            CapsuleCollider hitArea = root.AddComponent<CapsuleCollider>();
            hitArea.center = new Vector3(0, kind == TargetKind.ConductorBoss ? 1.65f : 1.05f, 0);
            hitArea.height = kind == TargetKind.ConductorBoss ? 3.8f : 2.55f;
            hitArea.radius = kind == TargetKind.ConductorBoss ? 1.15f : 0.75f;
            target.hitArea = hitArea;
            return target;
        }

        public static GhostTarget AttachExisting(GameObject root, int health, Vector3 colliderCenter,
            float colliderHeight, float colliderRadius, TargetKind kind = TargetKind.Zombie)
        {
            GhostTarget target = root.GetComponent<GhostTarget>();
            if (target == null) target = root.AddComponent<GhostTarget>();
            target.Kind = kind;
            target.MaxHealth = Mathf.Max(1, health);
            target.Health = target.MaxHealth;
            target.externalMotion = true;
            target.baseLocalScale = root.transform.localScale;
            target.baseLocalRotation = root.transform.localRotation;
            target.RegisterRenderers(root);
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = colliderCenter;
            capsule.height = colliderHeight;
            capsule.radius = colliderRadius;
            target.hitArea = capsule;
            return target;
        }

        public void SetTargetable(bool targetable)
        {
            if (hitArea != null && !defeated) hitArea.enabled = targetable;
        }

        private void OnEnable()
        {
            activeTargets.Add(this);
        }

        private void OnDestroy()
        {
            activeTargets.Remove(this);
        }

        private void Update()
        {
            if (externalMotion) return;
            float bob = Mathf.Sin(Time.time * (IsBoss ? 1.6f : 2.4f) + phase) * (IsBoss ? 0.11f : 0.18f);
            transform.position = new Vector3(transform.position.x, baseY + bob, transform.position.z);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(
                8f + Mathf.Sin(Time.time * 2.5f + phase) * 12f, 0f, 8f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(
                8f - Mathf.Sin(Time.time * 2.5f + phase) * 12f, 0f, -8f);
        }

        public int Hit()
        {
            if (defeated) return 0;
            Health--;
            MagicBolt.CreateImpact(transform.position + Vector3.up * (IsBoss ? 1.7f : 1.05f), IsBoss);
            StopAllCoroutines();
            StartCoroutine(HitFlash());
            int points = IsBoss ? 25 : 15;
            if (Health <= 0)
            {
                defeated = true;
                if (hitArea != null) hitArea.enabled = false;
                foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
                    if (behaviour != this) behaviour.enabled = false;
                points += IsBoss ? 250 : 35;
                StartCoroutine(Defeat());
            }
            return points;
        }

        private IEnumerator HitFlash()
        {
            // Importerade modeller kan byta eller förstöra rendererdelar när en
            // animation startas. Unitys "fake null" måste därför kontrolleras
            // varje gång, annars fortsätter en träff-coroutine att skriva till en
            // MeshRenderer/SkinnedMeshRenderer som inte längre finns.
            for (int i = 0; i < renderers.Count; i++)
                if (renderers[i] != null) renderers[i].material.color = Color.white;
            transform.localScale = baseLocalScale * 1.12f;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(
                Random.Range(-7f, 7f), Random.Range(-11f, 11f), Random.Range(-8f, 8f));
            yield return new WaitForSeconds(0.11f);
            for (int i = 0; i < renderers.Count && i < baseColors.Count; i++)
                if (renderers[i] != null) renderers[i].material.color = baseColors[i];
            transform.localScale = baseLocalScale;
            transform.localRotation = baseLocalRotation;
        }

        private IEnumerator Defeat()
        {
            float time = 0;
            Vector3 start = transform.localScale;
            Vector3 startPosition = transform.position;
            MagicBolt.CreateImpact(transform.position + Vector3.up * 0.55f, IsBoss);
            MagicBolt.CreateImpact(transform.position + Vector3.up * 1.45f, IsBoss);
            while (time < 0.52f)
            {
                time += Time.deltaTime;
                float amount = Mathf.Clamp01(time / 0.52f);
                transform.localScale = Vector3.Lerp(start, Vector3.zero, amount * amount);
                transform.position = startPosition + Vector3.down * amount * 0.65f;
                transform.Rotate(0, 620f * Time.deltaTime, 55f * Time.deltaTime);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}