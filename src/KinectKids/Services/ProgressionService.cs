using System;
using System.IO;
using System.Xml.Serialization;

namespace KinectKids.Services
{
    /// <summary>
    /// Lokal, anonym progression. Filen innehåller bara två spelarplatser,
    /// poäng och upplåsningar – aldrig namn, bild, ljud eller skelettdata.
    /// </summary>
    public sealed class ProgressionService
    {
        public const int SpookyAdventureUnlockScore = 120;
        private const string SpookyAdventureId = "SpookyAdventure3D";
        private readonly string dataFile;
        private ProgressionState state;

        public ProgressionService()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KinectKids");
            dataFile = Path.Combine(folder, "progress.xml");
            state = Load();
        }

        public int GetMathScore(int playerIndex)
        {
            return playerIndex == 1 ? state.PlayerTwoMathScore : state.PlayerOneMathScore;
        }

        public int GetCombinedMathScore()
        {
            return state.PlayerOneMathScore + state.PlayerTwoMathScore;
        }

        public int GetRemainingForSpookyAdventure()
        {
            return Math.Max(0, SpookyAdventureUnlockScore - GetCombinedMathScore());
        }

        public bool IsGameUnlocked(string gameId)
        {
            if (!string.Equals(gameId, SpookyAdventureId, StringComparison.OrdinalIgnoreCase))
                return true;
            return state.SpookyAdventureUnlocked || GetCombinedMathScore() >= SpookyAdventureUnlockScore;
        }

        public bool AddMathScore(int playerIndex, int points)
        {
            if (points <= 0) return false;

            bool wasUnlocked = IsGameUnlocked(SpookyAdventureId);
            if (playerIndex == 1)
                state.PlayerTwoMathScore += points;
            else
                state.PlayerOneMathScore += points;

            state.SpookyAdventureUnlocked = GetCombinedMathScore() >= SpookyAdventureUnlockScore;
            state.LastPlayedUtc = DateTime.UtcNow;
            Save();
            return !wasUnlocked && state.SpookyAdventureUnlocked;
        }

        private ProgressionState Load()
        {
            if (!File.Exists(dataFile)) return new ProgressionState();
            try
            {
                var serializer = new XmlSerializer(typeof(ProgressionState));
                using (FileStream stream = File.OpenRead(dataFile))
                    return (ProgressionState)serializer.Deserialize(stream);
            }
            catch (InvalidOperationException)
            {
                return new ProgressionState();
            }
            catch (IOException)
            {
                return new ProgressionState();
            }
        }

        private void Save()
        {
            string folder = Path.GetDirectoryName(dataFile);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string temporary = dataFile + ".tmp";
            var serializer = new XmlSerializer(typeof(ProgressionState));
            using (FileStream stream = File.Create(temporary))
                serializer.Serialize(stream, state);

            if (File.Exists(dataFile)) File.Delete(dataFile);
            File.Move(temporary, dataFile);
        }
    }

    public sealed class ProgressionState
    {
        public int PlayerOneMathScore { get; set; }
        public int PlayerTwoMathScore { get; set; }
        public bool SpookyAdventureUnlocked { get; set; }
        public DateTime LastPlayedUtc { get; set; }
    }
}
