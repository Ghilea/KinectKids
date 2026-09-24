using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KinectKids3D
{
    public static class ImportedModelFactory
    {
        public static GameObject Create(string resourcePath, Transform parent, string objectName,
            Vector3 localCenter, float largestDimension, Quaternion localRotation, params string[] preferredClips)
        {
            GameObject source = Resources.Load<GameObject>(resourcePath);
            if (source == null) return null;

            GameObject model = UnityEngine.Object.Instantiate(source, parent, false);
            model.name = objectName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = localRotation;
            ApplyKnownTexture(model, resourcePath);
            RepairUnsupportedMaterials(model);

            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.Destroy(collider);

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                float currentSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                if (currentSize > 0.0001f)
                    model.transform.localScale *= largestDimension / currentSize;

                renderers = model.GetComponentsInChildren<Renderer>(true);
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                Vector3 currentLocalCenter = parent.InverseTransformPoint(bounds.center);
                model.transform.localPosition += localCenter - currentLocalCenter;
            }

            ImportedModelAnimator player = model.AddComponent<ImportedModelAnimator>();
            player.Configure(resourcePath, preferredClips);
            return model;
        }

        private static void RepairUnsupportedMaterials(GameObject model)
        {
            Shader fallback = Shader.Find("Standard");
            if (fallback == null || !fallback.isSupported)
                fallback = Shader.Find("Universal Render Pipeline/Lit");
            if (fallback == null) return;

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.materials;
                bool changed = false;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material source = materials[index];
                    if (source != null && source.shader != null && source.shader.isSupported
                        && source.shader.name != "Hidden/InternalErrorShader") continue;

                    Texture texture = source != null && source.HasProperty("_MainTex")
                        ? source.mainTexture : null;
                    Color color = source != null && source.HasProperty("_Color")
                        ? source.color : Color.white;
                    Material replacement = new Material(fallback)
                    {
                        name = (source != null ? source.name : "Material") + " (reparerad)",
                        color = color,
                        mainTexture = texture
                    };
                    materials[index] = replacement;
                    changed = true;
                }
                if (changed) renderer.materials = materials;
            }
        }

        private static void ApplyKnownTexture(GameObject model, string resourcePath)
        {
            Texture2D texture = null;
            if (resourcePath.StartsWith("Models/CuteMonsters/", StringComparison.Ordinal))
            {
                string modelName = resourcePath.Substring(resourcePath.LastIndexOf('/') + 1);
                texture = Resources.Load<Texture2D>(
                    "Models/CuteMonsters/Textures/" + modelName + "_Texture");
            }
            else if (resourcePath.StartsWith("Models/KenneyGraveyard/", StringComparison.Ordinal))
            {
                texture = Resources.Load<Texture2D>("Models/KenneyGraveyard/Textures/colormap");
            }

            if (texture == null) return;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            foreach (Material material in renderer.materials)
                material.mainTexture = texture;
        }
    }

    public sealed class ImportedModelAnimator : MonoBehaviour
    {
        private string resourcePath;
        private string[] preferredClips;
        private PlayableGraph graph;
        private AnimationClipPlayable playable;
        private AnimationClip clip;

        public void Configure(string path, string[] preferences)
        {
            resourcePath = path;
            preferredClips = preferences ?? Array.Empty<string>();
        }

        private void Start()
        {
            AnimationClip[] clips = Resources.LoadAll<AnimationClip>(resourcePath);
            clip = SelectClip(clips);
            if (clip == null) return;

            Animator animator = GetComponentInChildren<Animator>();
            if (animator == null) animator = gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;

            graph = PlayableGraph.Create(name + " animation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Model animation", animator);
            output.SetSourcePlayable(playable);
            graph.Play();
        }

        private void Update()
        {
            if (!graph.IsValid() || clip == null || clip.length <= 0f) return;
            if (playable.GetTime() >= clip.length)
                playable.SetTime(playable.GetTime() % clip.length);
        }

        private AnimationClip SelectClip(AnimationClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            foreach (string preference in preferredClips)
            foreach (AnimationClip candidate in clips)
                if (!candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                    candidate.name.IndexOf(preference, StringComparison.OrdinalIgnoreCase) >= 0)
                    return candidate;
            foreach (AnimationClip candidate in clips)
                if (!candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)) return candidate;
            return null;
        }

        private void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
