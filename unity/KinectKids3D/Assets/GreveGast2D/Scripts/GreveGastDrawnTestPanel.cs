using UnityEngine;

namespace GreveGast2D
{
    public sealed class GreveGastDrawnTestPanel : MonoBehaviour
    {
        [SerializeField] private GreveGastDrawnController greveGast;
        private float distance = 0.45f;
        private bool running;

        public void Configure(GreveGastDrawnController target)
        {
            greveGast = target;
        }

        private void Update()
        {
            if (greveGast == null) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) greveGast.PlayReach();
            if (Input.GetKeyDown(KeyCode.Alpha2)) greveGast.PlayStumble();
            if (Input.GetKeyDown(KeyCode.Alpha3)) greveGast.PlayLaugh();
            if (Input.GetKeyDown(KeyCode.Alpha4)) greveGast.PlayDance();
            if (Input.GetKeyDown(KeyCode.Alpha5)) greveGast.PlayCatch();
            if (Input.GetKeyDown(KeyCode.Alpha6)) greveGast.PlaySurprise();
            if (Input.GetKeyDown(KeyCode.R))
            {
                running = !running;
                greveGast.SetRunning(running);
            }
            ApplyDistance();
        }

        private void ApplyDistance()
        {
            if (greveGast == null) return;
            greveGast.SetChaseDistance(distance);
            Vector3 position = greveGast.transform.position;
            position.z = Mathf.Lerp(1.5f, -15f, distance);
            greveGast.transform.position = position;
        }

        private void OnGUI()
        {
            if (greveGast == null) return;
            GUI.Box(new Rect(20, 20, 310, 390), "GREVE GAST - TECKNAD FORM");
            GUI.Label(new Rect(40, 55, 250, 24), "Avstand till kameran");
            distance = GUI.HorizontalSlider(new Rect(40, 84, 250, 24), distance, 0f, 1f);
            if (GUI.Button(new Rect(40, 116, 120, 38), running ? "STOPPA RUN" : "STARTA RUN"))
            {
                running = !running;
                greveGast.SetRunning(running);
            }
            if (GUI.Button(new Rect(170, 116, 120, 38), "IDLE"))
            {
                running = false;
                greveGast.SetRunning(false);
            }
            if (GUI.Button(new Rect(40, 166, 120, 38), "REACH (1)")) greveGast.PlayReach();
            if (GUI.Button(new Rect(170, 166, 120, 38), "STUMBLE (2)")) greveGast.PlayStumble();
            if (GUI.Button(new Rect(40, 216, 120, 38), "LAUGH (3)")) greveGast.PlayLaugh();
            if (GUI.Button(new Rect(170, 216, 120, 38), "DANCE (4)")) greveGast.PlayDance();
            if (GUI.Button(new Rect(40, 266, 120, 38), "CATCH (5)")) greveGast.PlayCatch();
            if (GUI.Button(new Rect(170, 266, 120, 38), "SURPRISE (6)")) greveGast.PlaySurprise();
            BillboardToCamera billboard = greveGast.GetComponent<BillboardToCamera>();
            if (GUI.Button(new Rect(40, 316, 250, 38),
                    billboard != null && billboard.enabled ? "BILLBOARD: PA" : "BILLBOARD: AV"))
                if (billboard != null) billboard.enabled = !billboard.enabled;
            GUI.Label(new Rect(40, 365, 250, 24), "R = Run  |  1-6 = animationer");
        }
    }
}
