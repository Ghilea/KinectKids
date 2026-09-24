using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Delad hjälpare för att bygga enkla primitiv-baserade GameObjects.
    /// Kapslar in det återkommande mönstret: skapa primitiv, namnge, sätt
    /// parent/position/scale/rotation och ta bort standard-collidern.
    ///
    /// Materialtilldelning lämnas medvetet till anroparen eftersom vissa
    /// komponenter använder en materialkopia (new Material(...)) medan andra
    /// delar material (sharedMaterial) – två olika runtime-beteenden.
    /// </summary>
    public static class PrimitiveBuilder
    {
        /// <summary>
        /// Skapar en primitiv, kopplar den under <paramref name="parent"/> och
        /// tar bort collidern. Returnerar det skapade objektet så att anroparen
        /// kan sätta material eller läsa av dess Renderer.
        /// </summary>
        public static GameObject Create(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, Quaternion? localRotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation ?? Quaternion.identity;
            RemoveCollider(part);
            return part;
        }

        /// <summary>Tar bort en primitivs standard-collider om den finns.</summary>
        public static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
        }
    }
}
