namespace KinectKids3D
{
    /// <summary>
    /// Centraliserade PlayerPrefs-nyckelnamn. Att samla dem här gör att ett
    /// stavfel blir ett kompileringsfel istället för tyst dataförlust, och att
    /// alla sparade inställningar/rekord kan hittas på ett ställe.
    ///
    /// OBS: Nyckelvärdena döptes om från "Spokjakten*"/"GreveGast*" till
    /// "HauntedRide*"/"Chase*". Tidigare sparade highscores och inställningar
    /// nollställs därför en gång (medvetet val vid namnbytet till engelska).
    /// </summary>
    public static class PrefsKeys
    {
        // Haunted Ride – inställningar
        public const string EasyMode = "HauntedRideEasyMode";
        public const string Players = "HauntedRidePlayers";
        public const string MusicVolume = "HauntedRideMusicVolume";
        public const string MusicMuted = "HauntedRideMusicMuted";
        public const string EffectsVolume = "HauntedRideEffectsVolume";
        public const string VoiceVolume = "HauntedRideVoiceVolume";
        public const string Brightness = "HauntedRideBrightness";
        public const string Fullscreen = "HauntedRideFullscreen";

        // Haunted Ride – rekord
        public const string HighScore = "HauntedRideHighScore";
        public const string BestCombo = "HauntedRideBestCombo";
        public const string BestRelics = "HauntedRideBestRelics";
        public const string BestMedals = "HauntedRideBestMedals";

        // Chase
        public const string ChasePlayerView = "ChasePlayerView";
    }
}
