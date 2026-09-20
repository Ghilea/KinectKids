using System;
using System.Collections.Generic;
using GreveGast2D;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace KinectKids3D.Editor
{
    public static class GreveGast2DBuilder
    {
        private const string Root = "Assets/GreveGast2D";
        private const string Sprites = Root + "/Sprites";
        private const string Animations = Root + "/Animations";
        private const string Prefabs = Root + "/Prefabs";
        private const string Materials = Root + "/Materials";
        private const string ControllerPath = Animations + "/GreveGastDrawnAnimator.controller";
        private const string PrefabPath = Prefabs + "/GreveGastDrawn.prefab";
        private const string TestScenePath = Root + "/GreveGastDrawnTest.unity";

        private static readonly string[] SpriteNames =
        {
            "Idle", "Run_A", "Run_B", "RunFront_A", "RunFront_B", "Reach", "Stumble", "Laugh", "Dance", "Catch", "Surprise"
        };

        [MenuItem("KinectKids/Greve Gast/Bygg tecknad prefab och testscen")]
        public static void BuildAll()
        {
            DeleteLegacyGeneratedAssets();
            // De ursprungliga, osorterade PNG-filerna lag i denna mapp. Efter
            // den explicita flytten finns bara en tom rest kvar.
            if (AssetDatabase.IsValidFolder("Assets/klotter-kasper"))
                AssetDatabase.DeleteAsset("Assets/klotter-kasper");
            EnsureFolders();
            ImportSprites();
            Dictionary<string, Sprite> sprites = LoadSprites();
            Material spriteMaterial = CreateSpriteMaterial();
            AnimatorController controller = CreateAnimatorController(sprites);
            GameObject prefab = CreatePrefab(sprites["Idle"], spriteMaterial, controller);
            CreateTestScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Greve Gasts tecknade skepnad skapad: " + PrefabPath + " | testscen: " + TestScenePath);
        }

        private static void DeleteLegacyGeneratedAssets()
        {
            foreach (string path in new[]
            {
                Root + "/KlotterKasperTest.unity",
                Prefabs + "/KlotterKasper.prefab",
                Materials + "/M_KlotterKasperSprite.mat",
                Animations + "/KlotterKasper.controller",
                Animations + "/KlotterKasper_Idle.anim",
                Animations + "/KlotterKasper_Run.anim",
                Animations + "/KlotterKasper_Reach.anim",
                Animations + "/KlotterKasper_Stumble.anim",
                Animations + "/KlotterKasper_Laugh.anim",
                Animations + "/KlotterKasper_Dance.anim",
                Animations + "/KlotterKasper_Catch.anim",
                Animations + "/KlotterKasper_Surprise.anim",
                "Assets/Resources/KlotterKasper"
            }) AssetDatabase.DeleteAsset(path);
        }

        private static void EnsureFolders()
        {
            Ensure("Assets", "GreveGast2D");
            Ensure(Root, "Sprites");
            Ensure(Root, "Animations");
            Ensure(Root, "Prefabs");
            Ensure(Root, "Scripts");
            Ensure(Root, "Materials");
            Ensure(Root, "Reference");
        }

        private static void ImportSprites()
        {
            foreach (string spriteName in SpriteNames)
            {
                string path = Sprites + "/" + spriteName + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Klotter-sprite saknas: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 280f;
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                textureSettings.spritePivot = new Vector2(0.5f, 0f);
                importer.SetTextureSettings(textureSettings);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        private static Dictionary<string, Sprite> LoadSprites()
        {
            var result = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (string spriteName in SpriteNames)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + "/" + spriteName + ".png");
                if (sprite == null) throw new InvalidOperationException("Kunde inte importera sprite: " + spriteName);
                result.Add(spriteName, sprite);
            }
            return result;
        }

        private static Material CreateSpriteMaterial()
        {
            string path = Materials + "/M_GreveGastDrawnSprite.mat";
            AssetDatabase.DeleteAsset(path);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Sprites/Default shader saknas.");
            Material material = new Material(shader) { name = "M_GreveGastDrawnSprite" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static AnimatorController CreateAnimatorController(Dictionary<string, Sprite> sprites)
        {
            AnimationClip idle = CreateClip("GreveGastDrawn_Idle", 2f, true,
                Frames((0f, sprites["Idle"]), (2f, sprites["Idle"])),
                new[] { 0f, 0f, 0.035f, 0.5f, 0f, 1f, 0.035f, 1.5f, 0f, 2f },
                new[] { 1f, 0f, 1.025f, 0.5f, 1f, 1f, 0.985f, 1.5f, 1f, 2f },
                new[] { -1.2f, 0f, 1.2f, 1f, -1.2f, 2f });

            AnimationClip run = CreateClip("GreveGastDrawn_Run", 0.25f, true,
                // Jakten visas framifran. De nya bildrutorna ar ritade for att
                // springa rakt mot spelaren och ersatter den gamla sidoloopen.
                Frames((0f, sprites["RunFront_A"]), (0.125f, sprites["RunFront_B"]), (0.25f, sprites["RunFront_A"])),
                new[] { 0f, 0f, 0.10f, 0.0625f, 0f, 0.125f, 0.10f, 0.1875f, 0f, 0.25f },
                new[] { 1.03f, 0f, 0.98f, 0.0625f, 1.03f, 0.125f, 0.98f, 0.1875f, 1.03f, 0.25f },
                new[] { -3f, 0f, 3f, 0.125f, -3f, 0.25f });

            AnimationClip reach = CreateClip("GreveGastDrawn_Reach", 0.85f, false,
                Frames((0f, sprites["Reach"]), (0.85f, sprites["Reach"])),
                new[] { 0f, 0f, 0.10f, 0.30f, 0f, 0.85f },
                new[] { 1f, 0f, 1.14f, 0.34f, 1f, 0.85f },
                new[] { 0f, 0f, -4f, 0.30f, 0f, 0.85f },
                new[] { 0f, 0f, 0.32f, 0.34f, 0f, 0.85f });

            AnimationClip stumble = CreateClip("GreveGastDrawn_Stumble", 1.05f, false,
                Frames((0f, sprites["Stumble"]), (1.05f, sprites["Stumble"])),
                new[] { 0f, 0f, 0.18f, 0.28f, -0.04f, 0.58f, 0f, 1.05f },
                new[] { 1f, 0f, 0.94f, 0.28f, 1.04f, 0.58f, 1f, 1.05f },
                new[] { 0f, 0f, 17f, 0.25f, -11f, 0.57f, 5f, 0.80f, 0f, 1.05f });

            AnimationClip laugh = CreateClip("GreveGastDrawn_Laugh", 1.15f, false,
                Frames((0f, sprites["Laugh"]), (1.15f, sprites["Laugh"])),
                new[] { 0f, 0f, 0.06f, 0.22f, 0f, 0.44f, 0.06f, 0.66f, 0f, 1.15f },
                new[] { 1f, 0f, 1.07f, 0.22f, 0.96f, 0.44f, 1.07f, 0.66f, 1f, 1.15f },
                new[] { -2f, 0f, 2f, 0.28f, -2f, 0.56f, 2f, 0.84f, 0f, 1.15f });

            AnimationClip dance = CreateClip("GreveGastDrawn_Dance", 1.45f, false,
                Frames((0f, sprites["Dance"]), (1.45f, sprites["Dance"])),
                new[] { 0f, 0f, 0.14f, 0.36f, 0f, 0.72f, 0.14f, 1.08f, 0f, 1.45f },
                new[] { 1f, 0f, 1.05f, 0.36f, 0.96f, 0.72f, 1.05f, 1.08f, 1f, 1.45f },
                new[] { -8f, 0f, 8f, 0.36f, -8f, 0.72f, 8f, 1.08f, 0f, 1.45f });

            AnimationClip catchClip = CreateClip("GreveGastDrawn_Catch", 1.15f, false,
                Frames((0f, sprites["Catch"]), (1.15f, sprites["Catch"])),
                new[] { 0f, 0f, 0.22f, 0.48f, 0.06f, 1.15f },
                new[] { 1f, 0f, 1.42f, 0.52f, 1.18f, 1.15f },
                new[] { 0f, 0f, -4f, 0.28f, 3f, 0.70f, 0f, 1.15f },
                new[] { 0f, 0f, 0.62f, 0.52f, 0.18f, 1.15f });

            AnimationClip surprise = CreateClip("GreveGastDrawn_Surprise", 0.95f, false,
                Frames((0f, sprites["Surprise"]), (0.95f, sprites["Surprise"])),
                new[] { 0f, 0f, 0.22f, 0.22f, 0f, 0.48f, 0.04f, 0.95f },
                new[] { 0.92f, 0f, 1.12f, 0.22f, 0.98f, 0.48f, 1f, 0.95f },
                new[] { 0f, 0f, 5f, 0.22f, -2f, 0.48f, 0f, 0.95f });

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Running", AnimatorControllerParameterType.Bool);
            foreach (string trigger in new[] { "Reach", "Stumble", "Laugh", "Dance", "Catch", "Surprise" })
                controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idleState = State(sm, "Idle", idle);
            AnimatorState runState = State(sm, "Run", run);
            sm.defaultState = idleState;
            Conditional(idleState, runState, "Running", true);
            Conditional(runState, idleState, "Running", false);
            Triggered(sm, "Reach", reach, idleState, 0.88f);
            Triggered(sm, "Stumble", stumble, idleState, 0.90f);
            Triggered(sm, "Laugh", laugh, idleState, 0.91f);
            Triggered(sm, "Dance", dance, idleState, 0.93f);
            Triggered(sm, "Catch", catchClip, idleState, 0.94f);
            Triggered(sm, "Surprise", surprise, idleState, 0.90f);
            return controller;
        }

        private static AnimationClip CreateClip(string name, float length, bool loop,
            ObjectReferenceKeyframe[] frames, float[] y, float[] scale, float[] rotation, float[] z = null)
        {
            string path = Animations + "/" + name + ".anim";
            AssetDatabase.DeleteAsset(path);
            AnimationClip clip = new AnimationClip { name = name, frameRate = 30f };
            EditorCurveBinding spriteBinding = new EditorCurveBinding
            {
                path = "VisualRoot/SpriteA",
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, frames);
            Curve(clip, "VisualRoot", "localPosition.y", y);
            if (z != null) Curve(clip, "VisualRoot", "localPosition.z", z);
            Curve(clip, "VisualRoot", "localScale.x", scale);
            Curve(clip, "VisualRoot", "localScale.y", scale);
            Curve(clip, "VisualRoot", "localScale.z", ConstantScale(length));
            Curve(clip, "VisualRoot", "localEulerAnglesRaw.z", rotation);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = length;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static ObjectReferenceKeyframe[] Frames(params (float time, Sprite sprite)[] values)
        {
            var frames = new ObjectReferenceKeyframe[values.Length];
            for (int i = 0; i < values.Length; i++)
                frames[i] = new ObjectReferenceKeyframe { time = values[i].time, value = values[i].sprite };
            return frames;
        }

        private static float[] ConstantScale(float length) => new[] { 1f, 0f, 1f, length };

        private static void Curve(AnimationClip clip, string path, string property, float[] valueTimePairs)
        {
            AnimationCurve curve = new AnimationCurve();
            for (int i = 0; i + 1 < valueTimePairs.Length; i += 2)
                curve.AddKey(valueTimePairs[i + 1], valueTimePairs[i]);
            clip.SetCurve(path, typeof(Transform), property, curve);
        }

        private static AnimatorState State(AnimatorStateMachine sm, string name, Motion motion)
        {
            AnimatorState state = sm.AddState(name);
            state.motion = motion;
            return state;
        }

        private static void Conditional(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void Triggered(AnimatorStateMachine sm, string trigger, Motion motion,
            AnimatorState idle, float exitTime)
        {
            AnimatorState state = State(sm, trigger, motion);
            AnimatorStateTransition enter = sm.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.duration = 0.06f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            AnimatorStateTransition leave = state.AddTransition(idle);
            leave.hasExitTime = true;
            leave.exitTime = exitTime;
            leave.duration = 0.09f;
        }

        private static GameObject CreatePrefab(Sprite idle, Material material, RuntimeAnimatorController controller)
        {
            GameObject root = new GameObject("GreveGastDrawn");
            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            root.AddComponent<GreveGastDrawnController>();
            root.AddComponent<BillboardToCamera>();

            Transform visualRoot = Child(root.transform, "VisualRoot");
            SpriteRenderer spriteA = Renderer(visualRoot, "SpriteA", idle, material, 10);
            SpriteRenderer spriteB = Renderer(visualRoot, "SpriteB", idle, material, 11);
            spriteB.color = new Color(1f, 1f, 1f, 0f);
            spriteB.enabled = false;

            Transform shadow = Child(root.transform, "Shadow");
            SpriteRenderer shadowRenderer = Renderer(shadow, "GroundShadow", idle, material, 2);
            shadowRenderer.color = new Color(0.05f, 0.03f, 0.08f, 0.24f);
            shadow.localPosition = new Vector3(0f, 0.025f, 0.10f);
            shadow.localScale = new Vector3(0.72f, 0.105f, 1f);
            Child(root.transform, "Effects");

            AssetDatabase.DeleteAsset(PrefabPath);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, Material material, int order)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void CreateTestScene(GameObject prefab)
        {
            Scene current = SceneManager.GetActiveScene();
            string previousPath = current.path;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GreveGastDrawnTest";

            GameObject cameraObject = new GameObject("TestCamera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 3.1f, 11f);
            camera.transform.LookAt(new Vector3(0f, 2.1f, -2f));
            camera.fieldOfView = 48f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.91f, 0.86f, 0.72f);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Greve Gast - tecknad testinstans";
            GreveGastDrawnController greveGast = instance.GetComponent<GreveGastDrawnController>();

            GameObject panelObject = new GameObject("Animationstest och avstand");
            GreveGastDrawnTestPanel panel = panelObject.AddComponent<GreveGastDrawnTestPanel>();
            panel.Configure(greveGast);

            EditorSceneManager.SaveScene(scene, TestScenePath);
            if (!string.IsNullOrEmpty(previousPath)) EditorSceneManager.OpenScene(previousPath);
        }

        private static void Ensure(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
