using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class GreveGastGame
    {
        private void AddHauntedDecoration(Transform segment, int index)
        {
            int side = (index & 1) == 0 ? -1 : 1;
            float x = side * 11.6f;

            // Pelare, nischer och trasiga ramar fran Spokjaktens slottsmiljo.
            Cube("Fuktig stenpelare", segment, new Vector3(side * 13.75f, 4.6f, 0f),
                new Vector3(0.85f, 8.8f, 1.15f), stone);
            if (index % 3 == 0)
            {
                Material oldWood = DarkRideWorld.TexturedMaterial(new Color(0.28f, 0.13f, 0.055f),
                    HauntedTextureFactory.OldWood(90 + index), 0f);
                Cube("Gammal tavla", segment, new Vector3(side * 14.05f, 6.1f, 0.3f),
                    new Vector3(0.18f, 2.6f, 2.1f), oldWood);
                Cube("Morkt portratt", segment, new Vector3(side * 13.93f, 6.1f, 0.3f),
                    new Vector3(0.10f, 1.95f, 1.5f), purple);
            }

            if (index % 4 == 0)
            {
                GameObject armorRoot = new GameObject("Hemsokt riddarrustning");
                armorRoot.transform.SetParent(segment, false);
                ImportedModelFactory.Create("Models/QuaterniusKnight/KnightCharacter", armorRoot.transform,
                    "Animerad rustning", new Vector3(x, 1.65f, 0f), 3.3f,
                    Quaternion.Euler(0f, 180f, 0f), "idle", "stand");
                HauntedProp.Attach(armorRoot, HauntedMotion.Bob, 0.035f, 0.75f);
            }
            else if (index % 4 == 1)
            {
                string[] graves =
                {
                    "Models/KenneyGraveyard/gravestone-broken",
                    "Models/KenneyGraveyard/gravestone-cross-large",
                    "Models/KenneyGraveyard/gravestone-decorative"
                };
                ImportedModelFactory.Create(graves[index % graves.Length], segment, "Gammal gravsten",
                    new Vector3(x, 1.05f, 0f), 2.5f, Quaternion.Euler(0f, 180f, side * 4f));
            }
            else if (index % 4 == 2)
            {
                GameObject swarm = new GameObject("Fladdermoss i taket");
                swarm.transform.SetParent(segment, false);
                for (int batIndex = 0; batIndex < 3; batIndex++)
                {
                    GameObject batRoot = new GameObject("Fladdermus " + batIndex);
                    batRoot.transform.SetParent(swarm.transform, false);
                    ImportedModelFactory.Create("Models/Quaternius/Bat", batRoot.transform,
                        "Animerad fladdermus", new Vector3(x + batIndex * -side * 0.75f,
                            11.5f + batIndex * 0.45f, batIndex * 0.5f), 1.25f,
                        Quaternion.Euler(0f, 180f, 0f), "fly", "flying", "idle");
                    HauntedProp.Attach(batRoot, HauntedMotion.Flutter, 0.30f, 2.4f + batIndex * 0.35f);
                }
            }
            else
            {
                GameObject ghostRoot = new GameObject("Spoke i slottsgangen");
                ghostRoot.transform.SetParent(segment, false);
                ImportedModelFactory.Create("Models/Quaternius/Ghost", ghostRoot.transform,
                    "Smygande spoke", new Vector3(x, 3.0f, 0f), 3.1f,
                    Quaternion.Euler(0f, 180f, 0f), "fly", "idle");
                HauntedProp.Attach(ghostRoot, HauntedMotion.Bob, 0.32f, 1.35f);
            }

            if (index % 5 == 3)
            {
                GameObject spiderRoot = new GameObject("Takspindel");
                spiderRoot.transform.SetParent(segment, false);
                ImportedModelFactory.Create("Models/Quaternius/Spider", spiderRoot.transform,
                    "Animerad takspindel", new Vector3(side * 5.5f, 14.7f, -2f), 2.4f,
                    Quaternion.Euler(180f, 0f, 0f), "walk", "attack", "idle");
                HauntedProp.Attach(spiderRoot, HauntedMotion.Flutter, 0.12f, 1.8f);
            }
        }

        private void SpawnAtmosphereScare(GreveGastCue cue)
        {
            GameObject scare = new GameObject("Tidsstyrd skramsel - " + cue.id);
            scare.transform.SetParent(transform, false);
            float side = cue.id != null && cue.id.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0 ? -1f : 1f;
            GameObject ghost = ImportedModelFactory.Create("Models/Quaternius/Ghost", scare.transform,
                "Spoke som kastar sig fram", new Vector3(side * 8.5f, 3.2f, -1.5f), 4.2f,
                Quaternion.Euler(0f, 180f, 0f), "fly", "attack", "idle");
            if (ghost == null)
                ghost = Cube("Spokskugga", scare.transform, new Vector3(side * 8.5f, 3.2f, -1.5f),
                    new Vector3(2.2f, 4.4f, 0.4f), purple);
            HauntedProp.Attach(scare, HauntedMotion.Flutter, 0.85f, 3.8f);
            AudioClip moan = Resources.Load<AudioClip>("Audio/SFX/Creatures/ghost_moan_03");
            if (moan != null) effects.PlayOneShot(moan, 0.65f);
            Destroy(scare, Mathf.Max(2.2f, cue.duration + 1.1f));
        }

        private void AddTorch(Transform parent, float x, float z)
        {
            GameObject torch = Cube("Fackla", parent, new Vector3(x, 2.25f, z), new Vector3(0.12f, 0.8f, 0.12f), floor);
            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Laga";
            flame.transform.SetParent(torch.transform, false);
            flame.transform.localPosition = new Vector3(0, 0.65f, 0);
            flame.transform.localScale = new Vector3(2.2f, 0.6f, 2.2f);
            flame.GetComponent<Renderer>().material = MakeMaterial(new Color(1f, 0.38f, 0.04f), new Color(1f, 0.18f, 0.01f));
            Light light = flame.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 17f;
            light.intensity = 4.25f;
            light.color = new Color(1f, 0.38f, 0.12f);
            HauntedProp.Attach(flame, HauntedMotion.Flicker, 0f, UnityEngine.Random.Range(7f, 11f));
        }

        private void BuildPlayerCharacter()
        {
            Material clothes = MakeMaterial(new Color(0.08f, 0.48f, 0.88f));
            Material skin = MakeMaterial(new Color(1f, 0.68f, 0.48f));
            Material dark = MakeMaterial(new Color(0.035f, 0.025f, 0.055f));
            Material shoes = MakeMaterial(new Color(0.12f, 0.07f, 0.055f));

            player = new GameObject("Spelaren - springer mot kameran").transform;
            player.position = new Vector3(0f, 0f, PlayerZ);
            player.rotation = Quaternion.Euler(0f, 180f, 0f);
            playerAvatarVisual = new GameObject("Figur");
            playerAvatarVisual.transform.SetParent(player, false);
            GameObject bodyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyObject.name = "Kropp";
            bodyObject.transform.SetParent(playerAvatarVisual.transform, false);
            bodyObject.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            bodyObject.transform.localScale = new Vector3(0.62f, 0.68f, 0.44f);
            bodyObject.GetComponent<Renderer>().material = clothes;
            Destroy(bodyObject.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Huvud med ansikte mot kameran";
            head.transform.SetParent(playerAvatarVisual.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.08f, -0.02f);
            head.transform.localScale = new Vector3(0.84f, 0.9f, 0.78f);
            head.GetComponent<Renderer>().material = skin;
            Destroy(head.GetComponent<Collider>());
            FacePart("Vanster oga", head.transform, new Vector3(-0.19f, 0.10f, -0.47f), new Vector3(0.16f, 0.20f, 0.07f), dark);
            FacePart("Hoger oga", head.transform, new Vector3(0.19f, 0.10f, -0.47f), new Vector3(0.16f, 0.20f, 0.07f), dark);
            FacePart("Leende", head.transform, new Vector3(0f, -0.20f, -0.49f), new Vector3(0.31f, 0.07f, 0.06f), dark);

            playerLeftArm = Limb("Vanster arm", playerAvatarVisual.transform, new Vector3(-0.56f, 1.45f, 0f), 0.72f, skin, false);
            playerRightArm = Limb("Hoger arm", playerAvatarVisual.transform, new Vector3(0.56f, 1.45f, 0f), 0.72f, skin, false);
            playerLeftLeg = Limb("Vanster ben", playerAvatarVisual.transform, new Vector3(-0.24f, 0.58f, 0f), 0.82f, shoes, true);
            playerRightLeg = Limb("Hoger ben", playerAvatarVisual.transform, new Vector3(0.24f, 0.58f, 0f), 0.82f, shoes, true);

            BuildSkeletonCharacter();
            skeletonMode = PlayerPrefs.GetInt(PrefsKeys.GreveGastPlayerView, 0) == 1;
            ApplyPlayerView();
        }

        private void BuildSkeletonCharacter()
        {
            Material bones = MakeMaterial(new Color(0.32f, 0.9f, 1f), new Color(0.08f, 0.42f, 0.62f));
            playerSkeletonVisual = new GameObject("Skelett");
            playerSkeletonVisual.transform.SetParent(player, false);
            FacePart("Huvud", playerSkeletonVisual.transform, new Vector3(0f, 2.08f, 0f), new Vector3(0.58f, 0.62f, 0.40f), bones);
            Cube("Ryggrad", playerSkeletonVisual.transform, new Vector3(0f, 1.22f, 0f), new Vector3(0.13f, 1.15f, 0.13f), bones);
            Cube("Axlar", playerSkeletonVisual.transform, new Vector3(0f, 1.62f, 0f), new Vector3(1.15f, 0.12f, 0.12f), bones);
            Cube("Hoft", playerSkeletonVisual.transform, new Vector3(0f, 0.68f, 0f), new Vector3(0.58f, 0.12f, 0.12f), bones);
            skeletonLeftArm = ThinLimb("Vanster skelettarm", playerSkeletonVisual.transform, new Vector3(-0.56f, 1.58f, 0f), 0.82f, bones);
            skeletonRightArm = ThinLimb("Hoger skelettarm", playerSkeletonVisual.transform, new Vector3(0.56f, 1.58f, 0f), 0.82f, bones);
            skeletonLeftLeg = ThinLimb("Vanster skelettben", playerSkeletonVisual.transform, new Vector3(-0.20f, 0.68f, 0f), 0.88f, bones);
            skeletonRightLeg = ThinLimb("Hoger skelettben", playerSkeletonVisual.transform, new Vector3(0.20f, 0.68f, 0f), 0.88f, bones);
        }

        private static Transform ThinLimb(string limbName, Transform parent, Vector3 joint, float length, Material material)
        {
            Transform pivot = new GameObject(limbName + " led").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = joint;
            Cube(limbName, pivot, new Vector3(0f, -length * 0.5f, 0f), new Vector3(0.11f, length, 0.11f), material);
            return pivot;
        }

        private void TogglePlayerView()
        {
            skeletonMode = !skeletonMode;
            PlayerPrefs.SetInt(PrefsKeys.GreveGastPlayerView, skeletonMode ? 1 : 0);
            PlayerPrefs.Save();
            ApplyPlayerView();
        }

        private void ApplyPlayerView()
        {
            if (playerAvatarVisual != null) playerAvatarVisual.SetActive(!skeletonMode);
            if (playerSkeletonVisual != null) playerSkeletonVisual.SetActive(skeletonMode);
        }

        private static Transform Limb(string limbName, Transform parent, Vector3 joint, float length,
            Material material, bool leg)
        {
            Transform pivot = new GameObject(limbName + " led").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = joint;
            GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            limb.name = limbName;
            limb.transform.SetParent(pivot, false);
            limb.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            limb.transform.localScale = new Vector3(leg ? 0.24f : 0.18f, length, leg ? 0.30f : 0.20f);
            limb.GetComponent<Renderer>().material = material;
            Destroy(limb.GetComponent<Collider>());
            return pivot;
        }

        private static void FacePart(string partName, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material = material;
            Destroy(part.GetComponent<Collider>());
        }

        private static GameObject Cube(string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().material = material;
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return cube;
        }

        private static Material MakeMaterial(Color color, Color? emission = null)
        {
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = color };
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            return material;
        }
    }
}