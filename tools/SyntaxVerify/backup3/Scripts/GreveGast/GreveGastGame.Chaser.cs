using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class GreveGastGame
    {
        private static AudioClip CreateTone()
        {
            const int rate = 22050;
            float[] samples = new float[rate / 7];
            for (int i = 0; i < samples.Length; i++)
            {
                float envelope = 1f - i / (float)samples.Length;
                samples[i] = Mathf.Sin(i * Mathf.PI * 2f * 720f / rate) * envelope * 0.34f;
            }
            AudioClip clip = AudioClip.Create("Varningssignal", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static GreveGastAction ParseAction(string value)
        {
            switch (value)
            {
                case "run": return GreveGastAction.Run;
                case "jump": return GreveGastAction.Jump;
                case "duck": return GreveGastAction.Duck;
                case "left": return GreveGastAction.Left;
                case "right": return GreveGastAction.Right;
                default: return GreveGastAction.None;
            }
        }

        private static string Symbol(GreveGastAction action)
        {
            switch (action)
            {
                case GreveGastAction.Jump: return "↑";
                case GreveGastAction.Duck: return "↓";
                case GreveGastAction.Left: return "←";
                case GreveGastAction.Right: return "→";
                case GreveGastAction.Run: return "⚡";
                default: return string.Empty;
            }
        }

        private void SetChaserRunning(bool running)
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.SetRunning(running);
            if (greveAnimation != null) greveAnimation.SetRun(running);
        }

        private void PlayChaserReach()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlayReach();
            if (greveAnimation != null) greveAnimation.PlayReach();
        }

        private void PlayChaserStumble()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlayStumble();
            if (greveAnimation != null) greveAnimation.PlayStumble();
        }

        private void PlayChaserLaugh()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlayLaugh();
            else if (greveAnimation != null) greveAnimation.PlayDance();
        }

        private void PlayChaserDance()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlayDance();
            if (greveAnimation != null) greveAnimation.PlayDance();
        }

        private void PlayChaserCatch()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlayCatch();
            if (greveAnimation != null) greveAnimation.PlayCatch();
        }

        private void PlayChaserSurprise()
        {
            if (drawnGreveAnimation != null) drawnGreveAnimation.PlaySurprise();
            else if (greveAnimation != null) greveAnimation.PlayDance();
        }
    }
}