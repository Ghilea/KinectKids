using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace KinectKids.Services
{
    /// <summary>Spelar förinspelade svenska ord och meningar utan nätanslutning.</summary>
    public sealed class SpokenInstructionService : IDisposable
    {
        private readonly MediaPlayer player = new MediaPlayer();
        private readonly Queue<string> queue = new Queue<string>();
        private readonly string voiceDirectory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Assets", "Voice");

        public SpokenInstructionService()
        {
            player.Volume = 0.92;
            player.MediaOpened += OnMediaOpened;
            player.MediaEnded += OnMediaEnded;
            player.MediaFailed += OnMediaFailed;
        }

        public void Speak(string voiceKey)
        {
            Stop();
            foreach (string part in (voiceKey ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (string file in Resolve(part))
                {
                    string path = Path.Combine(voiceDirectory, file + ".wav");
                    if (File.Exists(path)) queue.Enqueue(path);
                }
            }
            PlayNext();
        }

        public void Stop()
        {
            queue.Clear();
            player.Stop();
            player.Close();
        }

        private void PlayNext()
        {
            if (queue.Count == 0) return;
            player.Open(new Uri(queue.Dequeue(), UriKind.Absolute));
        }

        private void OnMediaOpened(object sender, EventArgs e) => player.Play();
        private void OnMediaEnded(object sender, EventArgs e) => PlayNext();
        private void OnMediaFailed(object sender, ExceptionEventArgs e) => PlayNext();

        private static IEnumerable<string> Resolve(string key)
        {
            string[] parts = (key ?? string.Empty).Split('|');
            if (parts.Length == 4 && parts[0] == "math")
                return new[] { "prompt_math", "number_" + parts[1], parts[2], "number_" + parts[3] };
            if (parts.Length == 2 && parts[0] == "starts")
                return new[] { "starts_" + parts[1] };
            if (parts.Length == 2 && parts[0] == "missing")
                return new[] { "missing_" + parts[1] };
            if (parts.Length == 2 && (parts[0] == "shape" || parts[0] == "pattern" || parts[0] == "simon"))
                return new[] { parts[0] + "_" + parts[1] };
            return new[] { key };
        }

        public void Dispose()
        {
            Stop();
            player.MediaOpened -= OnMediaOpened;
            player.MediaEnded -= OnMediaEnded;
            player.MediaFailed -= OnMediaFailed;
        }
    }
}
