using System;
using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    public sealed class TendrilSpawner : MonoBehaviour
    {
        private ProceduralTendril[] tendrils;
        private bool corridor;

        public void Initialize(Material material, float wallWidth, float wallHeight = 6f,
            bool corridorMode = false, Material upperMaterial = null)
        {
            corridor = corridorMode;
            tendrils = new ProceduralTendril[corridorMode ? 16 : 10];
            System.Random random = new System.Random(371);
            for (int i = 0; i < tendrils.Length; i++)
            {
                GameObject go = new GameObject("Living tendril " + (i + 1));
                go.transform.SetParent(transform, false);
                ProceduralTendril tendril = go.AddComponent<ProceduralTendril>();
                int origin = i % 4;
                bool fromFloor = corridorMode ? origin == 1 : origin >= 2;
                bool fromUpperMass = corridorMode && origin >= 2;
                tendril.Initialize(fromUpperMass && upperMaterial != null
                    ? upperMaterial : material);
                float side = corridorMode
                    ? ((i / 4) % 2 == 0 ? -1f : 1f)
                    : (i % 2 == 0 ? -1f : 1f);
                Vector3 start;
                if (corridorMode && fromFloor)
                {
                    // FloorMass is opaque beside the walls at this depth. Keep
                    // the root under its surface so the tube grows out of black.
                    start = new Vector3(side * wallWidth *
                        (0.36f + Next(random) * 0.08f),
                        0.06f, -0.8f - Next(random) * 1.4f);
                }
                else if (fromUpperMass)
                {
                    // RearMass stays opaque at these heights and widths. The
                    // new upper roots therefore emerge from black above Gast.
                    float spread = origin == 2 ? 0.24f : 0.29f;
                    float rise = origin == 2 ? 0.59f : 0.48f;
                    start = new Vector3(side * wallWidth *
                        (spread + Next(random) * 0.045f),
                        wallHeight * (rise + Next(random) * 0.06f), 2.08f);
                }
                else if (corridorMode)
                {
                    // The animated side body is reliably solid below ~35% of
                    // wall height for z=1..5. Earlier roots extended up onto
                    // bare stone, above the black body's moving crest.
                    start = new Vector3(side * (wallWidth * 0.5f - 0.18f),
                        wallHeight * (0.10f + Next(random) * 0.23f),
                        1.8f + Next(random) * 2.7f);
                }
                else
                {
                    start = fromFloor
                        ? new Vector3(side * (1.0f + Next(random) * (wallWidth * 0.37f)),
                            0.08f, -2.6f - Next(random) * 1.3f)
                        : new Vector3(side * (wallWidth * 0.5f - 0.18f),
                            0.35f + Next(random) * 4.3f, -0.23f);
                }
                Vector3 travel;
                if (corridorMode && fromFloor)
                    travel = new Vector3(-side * (0.55f + Next(random) * 0.30f),
                        0.14f + Next(random) * 0.25f, -0.30f - Next(random) * 0.15f);
                else if (fromUpperMass)
                    travel = origin == 2
                        ? new Vector3(side * (0.38f + Next(random) * 0.22f),
                            0.72f + Next(random) * 0.24f,
                            -0.08f - Next(random) * 0.10f)
                        : new Vector3(side * (0.78f + Next(random) * 0.18f),
                            0.22f + Next(random) * 0.24f,
                            -0.10f - Next(random) * 0.12f);
                else if (corridorMode)
                    travel = new Vector3(-side * (0.84f + Next(random) * 0.20f),
                        (Next(random) - 0.5f) * 0.42f,
                        -0.16f - Next(random) * 0.13f);
                else
                    travel = fromFloor
                        ? new Vector3(-side * (0.18f + Next(random) * 0.38f),
                            0.15f + Next(random) * 0.35f, -1f)
                        : new Vector3(-side * (0.76f + Next(random) * 0.26f),
                            (Next(random) - 0.5f) * 0.55f,
                            -0.42f - Next(random) * 0.3f);
                float duration = 3.0f + Next(random) * 2.8f;
                float corridorLengthScale = fromUpperMass ? 0.78f :
                    fromFloor ? 0.78f : 0.85f;
                tendril.Configure(start, travel,
                    (2.1f + Next(random) * 2.4f) *
                        (corridorMode ? corridorLengthScale : 1f),
                    fromFloor ? 0.12f + Next(random) * 0.10f
                        : fromUpperMass ? 0.11f + Next(random) * 0.08f
                        : 0.16f + Next(random) * 0.12f,
                    0.48f + Next(random) * 0.62f,
                    duration, duration * (0.12f + Next(random) * 0.58f),
                    5f + Next(random) * 71f, fromFloor);
                tendrils[i] = tendril;
            }
        }

        public void SetAggression(float aggression)
        {
            if (tendrils == null)
                tendrils = new ProceduralTendril[transform.childCount];
            int activeCount = Mathf.RoundToInt(Mathf.Lerp(
                corridor ? 12f : 7f, tendrils.Length, aggression));
            for (int i = 0; i < tendrils.Length; i++)
            {
                if (tendrils[i] == null && i < transform.childCount)
                    tendrils[i] = transform.GetChild(i).GetComponent<ProceduralTendril>();
                if (tendrils[i] == null) continue;
                tendrils[i].Aggression = aggression;
                tendrils[i].gameObject.SetActive(i < activeCount);
            }
        }

        private static float Next(System.Random random)
        {
            return (float)random.NextDouble();
        }
    }
}
