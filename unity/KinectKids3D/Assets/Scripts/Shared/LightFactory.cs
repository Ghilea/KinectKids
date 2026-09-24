using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Delad hjälpare för de vanliga runtime-ljusen. Nästan alla props, mål och
    /// effekter skapar en skugglös punktljuskälla med färg/intensitet/räckvidd –
    /// den uppsättningen samlas här.
    /// </summary>
    public static class LightFactory
    {
        /// <summary>
        /// Lägger till en skugglös punktljuskälla på <paramref name="target"/>
        /// och returnerar den för ev. vidare finjustering.
        /// </summary>
        public static Light AddPoint(GameObject target, Color color, float intensity, float range)
        {
            Light light = target.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }
    }
}
