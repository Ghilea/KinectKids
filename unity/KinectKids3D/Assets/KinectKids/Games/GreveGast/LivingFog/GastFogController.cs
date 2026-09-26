using UnityEngine;

namespace KinectKids.Games.GreveGast.LivingFog
{
    public sealed class GastFogController : MonoBehaviour
    {
        [Range(0f, 1f)] public float aggression = 0.45f;
        public bool showTestControls = true;
        private GastLivingFog livingFog;

        private void Awake()
        {
            livingFog = GetComponent<GastLivingFog>();
        }

        private void Update()
        {
            if (showTestControls)
            {
                if (Input.GetKey(KeyCode.UpArrow)) aggression += Time.deltaTime * 0.35f;
                if (Input.GetKey(KeyCode.DownArrow)) aggression -= Time.deltaTime * 0.35f;
            }
            aggression = Mathf.Clamp01(aggression);
            if (livingFog != null) livingFog.SetAggression(aggression);
        }

        private void OnGUI()
        {
            if (!showTestControls) return;
            Rect panel = new Rect(20f, 20f, 320f, 100f);
            GUI.Box(panel, "Greve Gast - levande svart massa");
            GUI.Label(new Rect(36f, 48f, 280f, 22f),
                "Aggression " + aggression.ToString("0.00") + "  (upp/ner)");
            aggression = GUI.HorizontalSlider(new Rect(36f, 78f, 280f, 20f),
                aggression, 0f, 1f);
        }
    }
}
