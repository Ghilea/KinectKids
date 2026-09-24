using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class ChaseGame
    {
        private void OnGUI()
        {
            EnsureStyles();
            float now = song != null ? song.SongTime : 0f;
            if (drawnIntro != null && drawnIntro.enabled) return;
            if (now < 3.0f)
                GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, 100), "GREVE GASTS TECKNINGSJAKT", titleStyle);
            if (now < 3.2f)
                GUI.Label(new Rect(0, Screen.height * 0.72f, Screen.width, 60), "Ror hela kroppen i takt med musiken", panelStyle);
            if (!string.IsNullOrEmpty(warningSymbol) && now <= warningUntil)
                GUI.Label(new Rect(0, Screen.height * 0.15f, Screen.width, 190), warningSymbol, hugeStyle);
            if (now <= feedbackUntil)
            {
                Color old = GUI.color;
                GUI.color = feedbackColor;
                GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 180), feedback, hugeStyle);
                GUI.color = old;
            }

            GUI.Box(new Rect(22, 20, 540, 76), GUIContent.none);
            GUI.Label(new Rect(34, 25, 516, 27), "AVSTAND TILL GREVE GAST", hudStyle);
            Color previous = GUI.color;
            GUI.color = Color.Lerp(new Color(0.3f, 0.95f, 0.7f), new Color(0.95f, 0.18f, 0.3f), chase);
            GUI.DrawTexture(new Rect(36, 58, 512 * chase, 24), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(36, 56, 512, 28), Mathf.RoundToInt(chase * 100f) + "%", hudStyle);

            if (song != null && now <= activeRunUntil)
            {
                GUI.Box(new Rect(22, 104, 540, 67), GUIContent.none);
                GUI.Label(new Rect(34, 108, 516, 25), global::KinectKids3D.Platform.KinectKidsPlatformRoot.IsActive
                    ? "SPRING!  SHIFT = TANGENTBORD" : "SPRING!  W = TANGENTBORD", hudStyle);
                GUI.color = new Color(0.22f, 0.88f, 1f);
                GUI.DrawTexture(new Rect(36, 140, 512 * Mathf.Clamp01(runSpeedBoost), 18), Texture2D.whiteTexture);
                GUI.color = previous;
            }

            if (caught)
            {
                GUI.Box(new Rect(Screen.width * 0.20f, Screen.height * 0.34f,
                    Screen.width * 0.60f, Screen.height * 0.26f), GUIContent.none);
                GUI.Label(new Rect(Screen.width * 0.20f, Screen.height * 0.37f,
                    Screen.width * 0.60f, Screen.height * 0.15f), "GREVE GAST TOG DIG!", catchStyle);
                GUI.Label(new Rect(Screen.width * 0.20f, Screen.height * 0.52f,
                    Screen.width * 0.60f, 42f), "Jakten fortsatter strax...", hudStyle);
            }

            if (debug) DrawDebug(now);
            if (paused) DrawPause();
        }

        private void DrawDebug(float now)
        {
            GUI.Box(new Rect(20, 64, 430, 270), "TIDSLINJEDEBUG (F2)");
            GUI.Label(new Rect(35, 92, 350, 90),
                "Tid: " + now.ToString("0.00") + " / " + (song?.Source?.clip?.length ?? 0f).ToString("0.00") +
                "\nSektion: " + section + "\nKropp: " + body.Current +
                "  hojd " + body.HeightDelta.ToString("+0.00;-0.00") +
                "  sida " + body.HorizontalDelta.ToString("+0.00;-0.00") +
                "\n" + body.Status);
            if (GUI.Button(new Rect(35, 188, 82, 30), "Spela/paus")) song.TogglePause();
            if (GUI.Button(new Rect(123, 188, 72, 30), "-0.10")) song.Seek(now - 0.10f);
            if (GUI.Button(new Rect(201, 188, 72, 30), "+0.10")) song.Seek(now + 0.10f);
            if (GUI.Button(new Rect(279, 188, 102, 30), "Nasta cue"))
            {
                GreveGastCue next = song.NextCue();
                if (next != null) song.Seek(Mathf.Max(0, next.time - next.warning - 0.3f));
            }
            GUI.Label(new Rect(35, 226, 340, 35), "F3 = forsta refrangen  |  PgDn = nasta handelse");
            if (greveAnimation != null || drawnGreveAnimation != null)
            {
                if (GUI.Button(new Rect(35, 270, 58, 30), "Idle")) SetChaserRunning(false);
                if (GUI.Button(new Rect(97, 270, 58, 30), "Run")) SetChaserRunning(true);
                if (GUI.Button(new Rect(159, 270, 62, 30), "Reach")) PlayChaserReach();
                if (GUI.Button(new Rect(225, 270, 72, 30), "Stumble")) PlayChaserStumble();
                if (GUI.Button(new Rect(301, 270, 62, 30), "Laugh")) PlayChaserLaugh();
                if (GUI.Button(new Rect(367, 270, 62, 30), "Dance")) PlayChaserDance();
                if (GUI.Button(new Rect(301, 306, 62, 30), "Catch")) PlayChaserCatch();
                if (GUI.Button(new Rect(367, 306, 62, 30), "Surprise")) PlayChaserSurprise();
            }
        }

        private void DrawPause()
        {
            float w = 420f, h = 335f;
            Rect rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(rect, "PAUS");
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 60, 300, 48), "FORTSATT")) TogglePause();
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 120, 300, 48), "STARTA OM"))
            {
                song.Seek(0f);
                chase = 0.5f;
                currentLane = 0;
                laneGestureArmed = true;
                TogglePause();
            }
            string playerView = skeletonMode ? "SPELARE: SKELETT" : "SPELARE: FIGUR";
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 180, 300, 48), playerView))
                TogglePlayerView();
            if (GUI.Button(new Rect(rect.x + 60, rect.y + 240, 300, 48), "TILL GEMENSAM MENY"))
                LauncherReturnService.ReturnToLauncher();
        }
        private void EnsureStyles()
        {
            if (hugeStyle != null) return;
            hugeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 6, 72, 150),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.75f, 0.1f) }
            };
            titleStyle = new GUIStyle(hugeStyle) { fontSize = Mathf.Clamp(Screen.height / 13, 42, 78) };
            panelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 42, 16, 28),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            hudStyle = new GUIStyle(panelStyle)
            {
                fontSize = Mathf.Clamp(Screen.height / 48, 16, 23),
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            catchStyle = new GUIStyle(panelStyle)
            {
                fontSize = Mathf.Clamp(Screen.height / 15, 42, 76),
                wordWrap = false,
                clipping = TextClipping.Overflow
            };
        }

        private void OnDestroy()
        {
            if (body != null) body.Dispose();
        }
    }
}