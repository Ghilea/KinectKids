using UnityEngine;

namespace KinectKids3D
{
    public enum HauntedMotion
    {
        Bob,
        Spin,
        Flutter,
        Swing,
        Flicker
    }

    public sealed class HauntedProp : MonoBehaviour
    {
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Light animatedLight;
        private float baseIntensity;
        private float phase;
        private float amplitude;
        private float speed;
        private HauntedMotion motion;

        public static HauntedProp Attach(GameObject target, HauntedMotion motion, float amplitude, float speed)
        {
            HauntedProp prop = target.AddComponent<HauntedProp>();
            prop.motion = motion;
            prop.amplitude = amplitude;
            prop.speed = speed;
            prop.startPosition = target.transform.localPosition;
            prop.startRotation = target.transform.localRotation;
            prop.animatedLight = target.GetComponent<Light>();
            prop.baseIntensity = prop.animatedLight != null ? prop.animatedLight.intensity : 0f;
            prop.phase = Random.value * Mathf.PI * 2f;
            return prop;
        }

        private void Update()
        {
            float wave = Mathf.Sin(Time.time * speed + phase);
            switch (motion)
            {
                case HauntedMotion.Bob:
                    transform.localPosition = startPosition + Vector3.up * wave * amplitude;
                    break;
                case HauntedMotion.Spin:
                    transform.localRotation = startRotation * Quaternion.Euler(0f, Time.time * speed * 45f, wave * 8f);
                    break;
                case HauntedMotion.Flutter:
                    transform.localPosition = startPosition + new Vector3(wave * amplitude,
                        Mathf.Sin(Time.time * speed * 1.7f + phase) * amplitude * 0.55f, 0f);
                    transform.localRotation = startRotation * Quaternion.Euler(wave * 20f, Time.time * speed * 25f, wave * 35f);
                    break;
                case HauntedMotion.Swing:
                    transform.localRotation = startRotation * Quaternion.Euler(0f, 0f, wave * amplitude);
                    break;
                case HauntedMotion.Flicker:
                    if (animatedLight != null)
                        animatedLight.intensity = baseIntensity * (0.72f + Mathf.Abs(wave) * 0.38f);
                    break;
            }
        }
    }
}
