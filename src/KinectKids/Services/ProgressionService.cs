using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace KinectKids.Services
{
    public class ProgressionService
    {
        private const string DataFile = "Data/player_progress.json";
        
        private static readonly Dictionary<string, string> DefaultLockedGames = new Dictionary<string, string>
        {
            { "MathGame", "L\u00E5st" },
            { "SpookyAdventure", "L\u00E5st" }
        };

        public string PlayerId => GetAnonymousPlayerId();

        private static string GetAnonymousPlayerId()
        {
            int randomId = new Random().Next(1, 1000);
            return $"AnonPlayer_{randomId}";
        }

        private Dictionary<string, object> GetData()
        {
            if (!File.Exists(DataFile))
            {
                return new Dictionary<string, object>
                {
                    ["playerId"] = GetAnonymousPlayerId(),
                    ["score"] = 0,
                    ["unlockedGames"] = new List<string>(),
                    ["lastPlayed"] = DateTime.UtcNow.ToString("o")
                };
            }

            try
            {
                string json = File.ReadAllText(DataFile);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return data;
            }
            catch (Exception)
            {
                return new Dictionary<string, object>
                {
                    ["playerId"] = GetAnonymousPlayerId(),
                    ["score"] = 0,
                    ["unlockedGames"] = new List<string>(),
                    ["lastPlayed"] = DateTime.UtcNow.ToString("o")
                };
            }
        }

        private void SaveData(Dictionary<string, object> data)
        {
            EnsureDataDirectory();
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                DefaultDateTimeHandler = (value, options) => value.ToString("o")
            });
            File.WriteAllText(DataFile, json);
        }

        private void EnsureDataDirectory()
        {
            string directory = Path.GetDirectoryName(DataFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public int GetTotalScore()
        {
            var data = GetData();
            return Convert.ToInt32(data["score"] ?? 0);
        }

        public void AddScore(int points)
        {
            var data = GetData();
            data["score"] = (Convert.ToInt32(data["score"] ?? 0)) + points;
            SaveData(data);
        }

        public bool IsGameUnlocked(string gameName)
        {
            if (!DefaultLockedGames.ContainsKey(gameName))
            {
                return true; // Default: all games unlocked if not in locked list
            }

            var data = GetData();
            List<string> unlockedGames = GetUnlockedGamesList(data);

            return unlockedGames.Contains(gameName);
        }

        public void UnlockGame(string gameName)
        {
            if (!IsGameUnlocked(gameName))
            {
                var data = GetData();
                List<string> unlockedGames = GetUnlockedGamesList(data);
                if (!unlockedGames.Contains(gameName))
                {
                    unlockedGames.Add(gameName);
                    data["unlockedGames"] = unlockedGames;
                    SaveData(data);
                }
            }
        }

        private List<string> GetUnlockedGamesList(Dictionary<string, object> data)
        {
            return JsonSerializer.Deserialize<List<string>>(data["unlockedGames"] ?? new List<string>()) ?? 
                   new List<string>();
        }

        public string GetLastPlayed()
        {
            var data = GetData();
            string lastPlayed = data["lastPlayed"]?.ToString();
            if (string.IsNullOrEmpty(lastPlayed))
            {
                return DateTime.UtcNow.ToString("o");
            }
            return lastPlayed;
        }

        public void SetLastPlayed()
        {
            var data = GetData();
            data["lastPlayed"] = DateTime.UtcNow.ToString("o");
            SaveData(data);
        }

        public Dictionary<string, string> GetLockedGames() => DefaultLockedGames;
    }
}

