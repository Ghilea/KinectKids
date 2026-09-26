using System;
using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    public sealed class TendrilSpawner : MonoBehaviour
    {
        private readonly ProceduralTendril[] tendrils = new ProceduralTendril[10];

        public void Initialize(Material material, float wallWidth, float wallHeight = 6f,
            bool corridorMode = false)
        {
            System.Random random = new System.Random(371);
            for (int i = 0; i < tendrils.Length; i++)
            {
                GameObject go = new GameObject("Living tendril " + (i + 1));
                go.transform.SetParent(transform, false);
                ProceduralTendril tendril = go.AddComponent<ProceduralTendril>();
                tendril.Initialize(material);
                int origin = i % 4;
                bool fromFloor = origin >= 2;
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 start = fromFloor
                    ? new Vector3(side * (1.0f + Next(random) * (wallWidth * 0.37f)),
                        0.08f, (corridorMode ? -4f : -2.6f) -
                            Next(random) * (corridorMode ? 2f : 1.3f))
                    : new Vector3(side * (wallWidth * 0.5f - 0.18f),
                        0.35f + Next(random) *
                            (corridorMode ? wallHeight * 0.72f : 4.3f),
                        corridorMode ? -4f + Next(random) * 6f : -0.23f);
                Vector3 travel = fromFloor
                    ? new Vector3(-side * (0.18f + Next(random) * 0.38f),
                        0.15f + Next(random) * 0.35f, -1f)
                    : new Vector3(-side * (0.76f + Next(random) * 0.26f),
                        (Next(random) - 0.5f) * 0.55f, -0.42f - Next(random) * 0.3f);
                float duration = 3.0f + Next(random) * 2.8f;
                tendril.Configure(start, travel,
                    (2.1f + Next(random) * 2.4f) * (corridorMode ? 1.55f : 1f),
                    fromFloor ? 0.12f + Next(random) * 0.10f
                        : 0.16f + Next(random) * 0.12f,
                    0.48f + Next(random) * 0.62f,
                    duration, duration * (0.12f + Next(random) * 0.58f),
                    5f + Next(random) * 71f, fromFloor);
                tendrils[i] = tendril;
            }
        }

        public void SetAggression(float aggression)
        {
            int activeCount = Mathf.RoundToInt(Mathf.Lerp(7f, tendrils.Length, aggression));
            for (int i = 0; i < tendrils.Length; i++)
            {
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
