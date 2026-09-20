using System.Collections;
using UnityEngine;

namespace GreveGast2D
{
    public enum GreveGastDrawnPower
    {
        DrawObstacle,
        DrawWall,
        DrawHole,
        EraseRoad,
        DrawShortcut,
        ThrowChalk,
        SummonDoodle,
        EnterWallDrawing
    }

    /// <summary>
    /// Shared visual language for Greve Gast's powers. World objects are drawn
    /// into existence from a moving chalk line instead of simply popping in.
    /// </summary>
    public sealed class GreveGastDrawingPowers : MonoBehaviour
    {
        private Material lineMaterial;

        public void DrawIntoWorld(GameObject target, Transform origin, GreveGastDrawnPower power)
        {
            if (target == null) return;
            StartCoroutine(DrawRoutine(target.transform, origin, power));
        }

        private IEnumerator DrawRoutine(Transform target, Transform origin, GreveGastDrawnPower power)
        {
            Vector3 finalScale = target.localScale;
            target.localScale = new Vector3(finalScale.x * 0.025f, finalScale.y * 0.025f, finalScale.z);
            LineRenderer chalk = CreateChalkLine(power);
            float duration = power == GreveGastDrawnPower.DrawHole ? 0.62f : 0.48f;
            float elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                target.localScale = Vector3.LerpUnclamped(
                    new Vector3(finalScale.x * 0.025f, finalScale.y * 0.025f, finalScale.z), finalScale, progress);
                if (chalk != null)
                {
                    Vector3 start = origin != null ? origin.position + Vector3.up * 3.2f : target.position + Vector3.up * 5f;
                    Vector3 end = Vector3.Lerp(start, target.position + Vector3.up * 0.4f, progress);
                    chalk.SetPosition(0, start);
                    chalk.SetPosition(1, end + new Vector3(Mathf.Sin(progress * 24f) * 0.32f, 0f, 0f));
                }
                yield return null;
            }
            if (target != null) target.localScale = finalScale;
            if (chalk != null) Destroy(chalk.gameObject, 0.16f);
        }

        private LineRenderer CreateChalkLine(GreveGastDrawnPower power)
        {
            GameObject lineObject = new GameObject("Greve Gasts levande kritstreck - " + power);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.widthMultiplier = power == GreveGastDrawnPower.DrawHole ? 0.30f : 0.20f;
            line.numCapVertices = 6;
            line.numCornerVertices = 5;
            line.sharedMaterial = GetLineMaterial();
            Color colour = power == GreveGastDrawnPower.DrawHole
                ? new Color(0.16f, 0.04f, 0.25f, 1f)
                : new Color(1f, 0.24f, 0.08f, 1f);
            line.startColor = colour;
            line.endColor = new Color(1f, 0.82f, 0.08f, 0.9f);
            line.sortingOrder = 80;
            return line;
        }

        private Material GetLineMaterial()
        {
            if (lineMaterial != null) return lineMaterial;
            Shader shader = Shader.Find("Sprites/Default");
            lineMaterial = new Material(shader) { name = "Greve Gast - levande krita" };
            return lineMaterial;
        }

        private void OnDestroy()
        {
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
