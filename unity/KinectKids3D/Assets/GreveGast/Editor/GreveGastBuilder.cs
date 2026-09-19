using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GreveGast.Editor
{
    public static class GreveGastBuilder
    {
        private const string Root = "Assets/GreveGast";
        private const string Generated = Root + "/Generated";
        private const string Materials = Generated + "/Materials";
        private const string Animations = Generated + "/Animations";
        private const string Prefabs = Generated + "/Prefabs";
        private const string Controllers = Generated + "/Controllers";

        [MenuItem("Tools/Greve Gast/Build Character")]
        public static void BuildCharacter()
        {
            EnsureFolders();

            Material coat = MakeMaterial("M_GreveGast_Coat", Root + "/Textures/Coat_Purple.png", new Color(0.26f, 0.17f, 0.36f), false);
            Material vest = MakeMaterial("M_GreveGast_Vest", Root + "/Textures/Vest_Teal.png", new Color(0.12f, 0.34f, 0.28f), false);
            Material skin = MakeMaterial("M_GreveGast_Skin", Root + "/Textures/Skin_Ghost.png", new Color(0.64f, 0.92f, 0.90f), true);
            Material hair = MakeMaterial("M_GreveGast_Hair", Root + "/Textures/Hair_Silver.png", new Color(0.80f, 0.84f, 0.88f), false);
            Material white = MakeMaterial("M_GreveGast_Cravat", Root + "/Textures/Cravat_OffWhite.png", new Color(0.92f, 0.90f, 0.84f), false);
            Material gold = MakeMaterial("M_GreveGast_Gold", Root + "/Textures/Gold.png", new Color(0.75f, 0.50f, 0.15f), false, 0.65f);
            Material boot = MakeMaterial("M_GreveGast_Boot", Root + "/Textures/Boot_Leather.png", new Color(0.08f, 0.09f, 0.11f), false);
            Material eye = MakeMaterial("M_GreveGast_Eye", Root + "/Textures/Eye_White.png", Color.white, false);
            Material pupil = MakeSolidMaterial("M_GreveGast_Pupil", new Color(0.055f, 0.045f, 0.065f), false);
            Material mouth = MakeSolidMaterial("M_GreveGast_Mouth", new Color(0.12f, 0.025f, 0.04f), false);

            GameObject root = new GameObject("GreveGast");
            root.transform.position = Vector3.zero;

            Animator animator = root.AddComponent<Animator>();
            root.AddComponent<GreveGastAnimationDriver>();

            Transform model = NewTransform(root.transform, "Model", Vector3.zero);
            BuildModel(model, coat, vest, skin, hair, white, gold, boot, eye, pupil, mouth);

            AnimationClip idle = CreateIdle();
            AnimationClip run = CreateRun();
            AnimationClip reach = CreateReach();
            AnimationClip stumble = CreateStumble();
            AnimationClip dance = CreateDance();
            AnimationClip catchClip = CreateCatch();

            AnimatorController controller = CreateController(idle, run, reach, stumble, dance, catchClip);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            string prefabPath = Prefabs + "/GreveGast.prefab";
            AssetDatabase.DeleteAsset(prefabPath);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log("Greve Gast generated: " + prefabPath);
        }

        private static void BuildModel(
            Transform model,
            Material coat, Material vest, Material skin, Material hair, Material white,
            Material gold, Material boot, Material eye, Material pupil, Material mouth)
        {
            // Broad readable silhouette, approximately 2.4 m tall.
            Part(model, "Torso", PrimitiveType.Cube, new Vector3(0f, 1.48f, 0f), new Vector3(0.92f, 0.78f, 0.52f), coat);
            Part(model, "Vest", PrimitiveType.Cube, new Vector3(0f, 1.43f, 0.285f), new Vector3(0.62f, 0.58f, 0.055f), vest);

            // High collar.
            Part(model, "Collar_L", PrimitiveType.Cube, new Vector3(-0.32f, 1.84f, 0.02f), new Vector3(0.20f, 0.34f, 0.10f), coat, new Vector3(0,0,-18));
            Part(model, "Collar_R", PrimitiveType.Cube, new Vector3(0.32f, 1.84f, 0.02f), new Vector3(0.20f, 0.34f, 0.10f), coat, new Vector3(0,0,18));

            // Cravat and brooch.
            Part(model, "Cravat_A", PrimitiveType.Sphere, new Vector3(0f, 1.72f, 0.34f), new Vector3(0.28f, 0.22f, 0.08f), white);
            Part(model, "Cravat_B", PrimitiveType.Cube, new Vector3(0f, 1.56f, 0.34f), new Vector3(0.23f, 0.31f, 0.045f), white);
            Part(model, "Brooch", PrimitiveType.Sphere, new Vector3(0f, 1.77f, 0.395f), new Vector3(0.10f, 0.10f, 0.045f), gold);

            // Head group.
            Transform head = NewTransform(model, "Head", new Vector3(0f, 1.94f, 0f));
            Part(head, "HeadMesh", PrimitiveType.Sphere, new Vector3(0f, 0.15f, 0.03f), new Vector3(0.48f, 0.58f, 0.43f), skin);
            Part(head, "Nose", PrimitiveType.Capsule, new Vector3(0f, 0.13f, 0.44f), new Vector3(0.10f, 0.18f, 0.10f), skin, new Vector3(90,0,0));
            Part(head, "Ear_L", PrimitiveType.Sphere, new Vector3(-0.43f, 0.15f, 0.02f), new Vector3(0.11f,0.17f,0.08f), skin);
            Part(head, "Ear_R", PrimitiveType.Sphere, new Vector3(0.43f, 0.15f, 0.02f), new Vector3(0.11f,0.17f,0.08f), skin);

            // Eyes and brows.
            Part(head, "Eye_L", PrimitiveType.Sphere, new Vector3(-0.16f, 0.26f, 0.39f), new Vector3(0.115f,0.15f,0.06f), eye);
            Part(head, "Eye_R", PrimitiveType.Sphere, new Vector3(0.16f, 0.26f, 0.39f), new Vector3(0.115f,0.15f,0.06f), eye);
            Part(head, "Pupil_L", PrimitiveType.Sphere, new Vector3(-0.16f, 0.25f, 0.448f), new Vector3(0.044f,0.060f,0.025f), pupil);
            Part(head, "Pupil_R", PrimitiveType.Sphere, new Vector3(0.16f, 0.25f, 0.448f), new Vector3(0.044f,0.060f,0.025f), pupil);
            Part(head, "Brow_L", PrimitiveType.Cube, new Vector3(-0.17f, 0.43f, 0.405f), new Vector3(0.18f,0.045f,0.035f), hair, new Vector3(0,0,-10));
            Part(head, "Brow_R", PrimitiveType.Cube, new Vector3(0.17f, 0.43f, 0.405f), new Vector3(0.18f,0.045f,0.035f), hair, new Vector3(0,0,10));
            Part(head, "Mouth", PrimitiveType.Sphere, new Vector3(0f, -0.03f, 0.415f), new Vector3(0.22f,0.105f,0.035f), mouth);

            // Hair tufts.
            Part(head, "Hair_C", PrimitiveType.Sphere, new Vector3(0f, 0.63f, -0.02f), new Vector3(0.32f,0.22f,0.28f), hair);
            Part(head, "Hair_L", PrimitiveType.Sphere, new Vector3(-0.20f, 0.57f, 0.00f), new Vector3(0.23f,0.18f,0.20f), hair, new Vector3(0,0,18));
            Part(head, "Hair_R", PrimitiveType.Sphere, new Vector3(0.18f, 0.67f, -0.03f), new Vector3(0.25f,0.16f,0.18f), hair, new Vector3(0,0,-22));
            Part(head, "Goatee", PrimitiveType.Capsule, new Vector3(0f,-0.25f,0.32f), new Vector3(0.11f,0.26f,0.10f), hair);

            // Arms use empty pivot transforms so the generated clips have usable "bones".
            BuildArm(model, "Arm_L", -1f, coat, skin, white);
            BuildArm(model, "Arm_R",  1f, coat, skin, white);

            // Hips / legs.
            BuildLeg(model, "Leg_L", -1f, coat, boot);
            BuildLeg(model, "Leg_R",  1f, coat, boot);

            // Tattered coat tails.
            Transform tailL = NewTransform(model, "CoatTail_L", new Vector3(-0.29f, 1.14f, -0.13f));
            Part(tailL, "Mesh", PrimitiveType.Cube, new Vector3(0,-0.42f,0), new Vector3(0.30f,0.82f,0.10f), coat, new Vector3(5,0,-8));
            Transform tailR = NewTransform(model, "CoatTail_R", new Vector3(0.29f, 1.14f, -0.13f));
            Part(tailR, "Mesh", PrimitiveType.Cube, new Vector3(0,-0.42f,0), new Vector3(0.30f,0.82f,0.10f), coat, new Vector3(5,0,8));

            // Small glowing spectral wisps around the lower coat.
            Part(model, "GhostWisp_L", PrimitiveType.Sphere, new Vector3(-0.28f,0.78f,-0.08f), new Vector3(0.18f,0.34f,0.12f), skin);
            Part(model, "GhostWisp_R", PrimitiveType.Sphere, new Vector3(0.28f,0.72f,-0.08f), new Vector3(0.16f,0.30f,0.11f), skin);
        }

        private static void BuildArm(Transform model, string name, float side, Material coat, Material skin, Material white)
        {
            Transform upper = NewTransform(model, name, new Vector3(0.59f * side, 1.72f, 0f));
            upper.localEulerAngles = new Vector3(0,0,-4f * side);

            Part(upper, "UpperArmMesh", PrimitiveType.Capsule, new Vector3(0,-0.29f,0), new Vector3(0.25f,0.34f,0.25f), coat);
            Part(upper, "Ruffle", PrimitiveType.Sphere, new Vector3(0,-0.61f,0), new Vector3(0.24f,0.11f,0.24f), white);

            Transform fore = NewTransform(upper, name.Replace("Arm", "Forearm"), new Vector3(0,-0.61f,0));
            Part(fore, "ForearmMesh", PrimitiveType.Capsule, new Vector3(0,-0.28f,0), new Vector3(0.19f,0.31f,0.19f), coat);
            Transform hand = NewTransform(fore, name.Replace("Arm", "Hand"), new Vector3(0,-0.60f,0.02f));
            Part(hand, "HandMesh", PrimitiveType.Sphere, new Vector3(0,-0.08f,0.04f), new Vector3(0.20f,0.23f,0.13f), skin);
            // Three exaggerated fingers.
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.07f;
                Part(hand, "Finger_" + i, PrimitiveType.Capsule, new Vector3(x,-0.25f,0.07f), new Vector3(0.038f,0.13f,0.038f), skin, new Vector3(4f*(i-1),0,0));
            }
        }

        private static void BuildLeg(Transform model, string name, float side, Material trouser, Material boot)
        {
            Transform leg = NewTransform(model, name, new Vector3(0.25f * side, 1.10f, 0f));
            Part(leg, "LegMesh", PrimitiveType.Capsule, new Vector3(0,-0.40f,0), new Vector3(0.17f,0.44f,0.17f), trouser);

            Transform foot = NewTransform(leg, name.Replace("Leg", "Foot"), new Vector3(0,-0.86f,0.08f));
            Part(foot, "Boot", PrimitiveType.Cube, new Vector3(0,-0.03f,0.12f), new Vector3(0.27f,0.23f,0.47f), boot);
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Material material, Vector3? euler = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return go;
        }

        private static Transform NewTransform(Transform parent, string name, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        // ---------------- Animations ----------------

        private static AnimationClip CreateIdle()
        {
            AnimationClip c = NewClip("GreveGast_Idle", true, 2.0f);
            Pos(c, "Model", "y", 0f, 0.00f, 0.03f, 0.50f, 0f, 1.00f, 0.03f, 1.50f, 0f, 2.00f);
            Rot(c, "Model/Head", "z", -3f, 0f, 3f, 1f, -3f, 2f);
            Rot(c, "Model/Arm_L", "z", -4f, 0f, -9f, 1f, -4f, 2f);
            Rot(c, "Model/Arm_R", "z", 4f, 0f, 9f, 1f, 4f, 2f);
            SaveClip(c);
            return c;
        }

        private static AnimationClip CreateRun()
        {
            AnimationClip c = NewClip("GreveGast_Run", true, 0.70f);
            Pos(c, "Model", "y", 0f,0f, 0.08f,0.175f, 0f,0.35f, 0.08f,0.525f, 0f,0.70f);
            Rot(c, "Model/Arm_L", "x", -48f,0f, 48f,0.35f, -48f,0.70f);
            Rot(c, "Model/Arm_R", "x", 48f,0f, -48f,0.35f, 48f,0.70f);
            Rot(c, "Model/Leg_L", "x", 36f,0f, -36f,0.35f, 36f,0.70f);
            Rot(c, "Model/Leg_R", "x", -36f,0f, 36f,0.35f, -36f,0.70f);
            Rot(c, "Model/CoatTail_L", "x", 4f,0f, -17f,0.35f, 4f,0.70f);
            Rot(c, "Model/CoatTail_R", "x", -6f,0f, -21f,0.35f, -6f,0.70f);
            Rot(c, "Model", "z", -2f,0f, 2f,0.35f, -2f,0.70f);
            SaveClip(c);
            return c;
        }

        private static AnimationClip CreateReach()
        {
            AnimationClip c = NewClip("GreveGast_Reach", false, 1.0f);
            Rot(c, "Model/Arm_L", "x", 0f,0f, -78f,0.32f, -88f,0.55f, -15f,1f);
            Rot(c, "Model/Arm_R", "x", 0f,0f, -78f,0.32f, -88f,0.55f, -15f,1f);
            Rot(c, "Model/Forearm_L", "x", 0f,0f, -25f,0.45f, 0f,1f);
            Rot(c, "Model/Forearm_R", "x", 0f,0f, -25f,0.45f, 0f,1f);
            Rot(c, "Model", "x", 0f,0f, 10f,0.55f, 0f,1f);
            Pos(c, "Model", "z", 0f,0f, 0.13f,0.55f, 0f,1f);
            SaveClip(c);
            return c;
        }

        private static AnimationClip CreateStumble()
        {
            AnimationClip c = NewClip("GreveGast_Stumble", false, 1.35f);
            Rot(c, "Model", "z", 0f,0f, 17f,0.30f, -12f,0.65f, 7f,0.95f, 0f,1.35f);
            Rot(c, "Model/Arm_L", "z", -4f,0f, -95f,0.35f, 35f,0.80f, -4f,1.35f);
            Rot(c, "Model/Arm_R", "z", 4f,0f, 95f,0.35f, -35f,0.80f, 4f,1.35f);
            Pos(c, "Model", "y", 0f,0f, -0.07f,0.55f, 0.02f,0.95f, 0f,1.35f);
            SaveClip(c);
            return c;
        }

        private static AnimationClip CreateDance()
        {
            AnimationClip c = NewClip("GreveGast_Dance", true, 1.6f);
            Rot(c, "Model", "y", -12f,0f, 12f,0.4f, -12f,0.8f, 12f,1.2f, -12f,1.6f);
            Rot(c, "Model/Arm_L", "z", -22f,0f, -115f,0.4f, -22f,0.8f, -115f,1.2f, -22f,1.6f);
            Rot(c, "Model/Arm_R", "z", 22f,0f, 115f,0.4f, 22f,0.8f, 115f,1.2f, 22f,1.6f);
            Rot(c, "Model/Leg_L", "x", 0f,0f, 26f,0.4f, 0f,0.8f, -20f,1.2f, 0f,1.6f);
            Rot(c, "Model/Leg_R", "x", 0f,0f, -26f,0.4f, 0f,0.8f, 20f,1.2f, 0f,1.6f);
            Pos(c, "Model", "y", 0f,0f, 0.09f,0.4f, 0f,0.8f, 0.09f,1.2f, 0f,1.6f);
            SaveClip(c);
            return c;
        }

        private static AnimationClip CreateCatch()
        {
            AnimationClip c = NewClip("GreveGast_Catch", false, 1.25f);
            Rot(c, "Model/Arm_L", "x", 0f,0f, -105f,0.45f, -105f,0.80f, -25f,1.25f);
            Rot(c, "Model/Arm_R", "x", 0f,0f, -105f,0.45f, -105f,0.80f, -25f,1.25f);
            Rot(c, "Model/Forearm_L", "x", 0f,0f, 18f,0.65f, 0f,1.25f);
            Rot(c, "Model/Forearm_R", "x", 0f,0f, 18f,0.65f, 0f,1.25f);
            Pos(c, "Model", "z", 0f,0f, 0.28f,0.60f, 0.22f,0.86f, 0f,1.25f);
            Rot(c, "Model/Head", "x", 0f,0f, -10f,0.60f, 0f,1.25f);
            SaveClip(c);
            return c;
        }

        private static AnimationClip NewClip(string name, bool loop, float length)
        {
            AnimationClip c = new AnimationClip { name = name, frameRate = 30f };
            AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(c);
            s.loopTime = loop;
            s.stopTime = length;
            AnimationUtility.SetAnimationClipSettings(c, s);
            return c;
        }

        private static void SaveClip(AnimationClip c)
        {
            string path = Animations + "/" + c.name + ".anim";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(c, path);
        }

        private static void Pos(AnimationClip c, string path, string axis, params float[] timeValuePairs)
        {
            SetCurve(c, path, "localPosition." + axis, timeValuePairs);
        }

        private static void Rot(AnimationClip c, string path, string axis, params float[] valueTimePairs)
        {
            // Parameters are value,time,value,time...
            AnimationCurve curve = new AnimationCurve();
            for (int i = 0; i + 1 < valueTimePairs.Length; i += 2)
                curve.AddKey(valueTimePairs[i + 1], valueTimePairs[i]);
            c.SetCurve(path, typeof(Transform), "localEulerAnglesRaw." + axis, curve);
        }

        private static void SetCurve(AnimationClip c, string path, string property, params float[] valueTimePairs)
        {
            AnimationCurve curve = new AnimationCurve();
            for (int i = 0; i + 1 < valueTimePairs.Length; i += 2)
                curve.AddKey(valueTimePairs[i + 1], valueTimePairs[i]);
            c.SetCurve(path, typeof(Transform), property, curve);
        }

        private static AnimatorController CreateController(
            AnimationClip idle, AnimationClip run, AnimationClip reach,
            AnimationClip stumble, AnimationClip dance, AnimationClip catchClip)
        {
            string path = Controllers + "/GreveGast.controller";
            AssetDatabase.DeleteAsset(path);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Running", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Reach", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Stumble", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dance", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Catch", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idleState = sm.AddState("Idle");
            AnimatorState runState = sm.AddState("Run");
            AnimatorState reachState = sm.AddState("Reach");
            AnimatorState stumbleState = sm.AddState("Stumble");
            AnimatorState danceState = sm.AddState("Dance");
            AnimatorState catchState = sm.AddState("Catch");

            idleState.motion = idle;
            runState.motion = run;
            reachState.motion = reach;
            stumbleState.motion = stumble;
            danceState.motion = dance;
            catchState.motion = catchClip;
            sm.defaultState = idleState;

            AnimatorStateTransition toRun = idleState.AddTransition(runState);
            toRun.hasExitTime = false;
            toRun.duration = 0.12f;
            toRun.AddCondition(AnimatorConditionMode.If, 0, "Running");

            AnimatorStateTransition toIdle = runState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.12f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Running");

            AddTriggered(sm, reachState, "Reach", idleState, 0.85f);
            AddTriggered(sm, stumbleState, "Stumble", idleState, 0.90f);
            AddTriggered(sm, danceState, "Dance", idleState, 0.95f);
            AddTriggered(sm, catchState, "Catch", idleState, 0.90f);

            return controller;
        }

        private static void AddTriggered(AnimatorStateMachine sm, AnimatorState state, string parameter, AnimatorState idle, float exitTime)
        {
            AnimatorStateTransition enter = sm.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.duration = 0.08f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0, parameter);

            AnimatorStateTransition leave = state.AddTransition(idle);
            leave.hasExitTime = true;
            leave.exitTime = exitTime;
            leave.duration = 0.12f;
        }

        // ---------------- Assets ----------------

        private static Material MakeMaterial(string name, string texturePath, Color tint, bool emission, float metallic = 0f)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Material m = MakeSolidMaterial(name, tint, emission);
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", metallic > 0f ? 0.55f : 0.25f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material MakeSolidMaterial(string name, Color tint, bool emission)
        {
            string path = Materials + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                ApplyMaterialSettings(existing, tint, emission);
                return existing;
            }

            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("HDRP/Lit") ??
                Shader.Find("Standard");

            if (shader == null)
                throw new InvalidOperationException("No supported Lit/Standard shader was found.");

            Material m = new Material(shader) { name = name };
            ApplyMaterialSettings(m, tint, emission);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static void ApplyMaterialSettings(Material m, Color tint, bool emission)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            if (m.HasProperty("_Color")) m.SetColor("_Color", tint);

            if (emission)
            {
                Color e = new Color(tint.r, tint.g, tint.b) * 0.35f;
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", e);
                m.EnableKeyword("_EMISSION");
            }
        }

        private static void EnsureFolders()
        {
            Ensure("Assets", "GreveGast");
            Ensure(Root, "Generated");
            Ensure(Generated, "Materials");
            Ensure(Generated, "Animations");
            Ensure(Generated, "Prefabs");
            Ensure(Generated, "Controllers");
        }

        private static void Ensure(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
