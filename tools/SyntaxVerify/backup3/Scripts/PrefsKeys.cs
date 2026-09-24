namespace KinectKids3D
{
    /// <summary>
    /// Centraliserade PlayerPrefs-nyckelnamn. Att samla dem här gör att ett
    /// stavfel blir ett kompileringsfel istället för tyst dataförlust, och att
    /// alla sparade inställningar/rekord kan hittas på ett ställe.
    ///
    /// Värdena är <c>const string</c>, så kompilatorn infogar exakt samma
    /// strängliteraler som tidigare – runtime-beteendet är oförändrat.
    /// </summary>
    public static class PrefsKeys
    {
        // Spökjakten – inställningar
        public const string EasyMode = "SpokjaktenEasyMode";
        public const string Players = "SpokjaktenPlayers";
        public const string MusicVolume = "SpokjaktenMusicVolume";
        public const string MusicMuted = "SpokjaktenMusicMuted";
        public const string EffectsVolume = "SpokjaktenEffectsVolume";
        public const string VoiceVolume = "SpokjaktenVoiceVolume";
        public const string Brightness = "SpokjaktenBrightness";
        public const string Fullscreen = "SpokjaktenFullscreen";

        // Spökjakten – rekord
        public const string HighScore = "SpokjaktenHighScore";
        public const string BestCombo = "SpokjaktenBestCombo";
        public const string BestRelics = "SpokjaktenBestRelics";
        public const string BestMedals = "SpokjaktenBestMedals";

        // Greve Gast
        public const string GreveGastPlayerView = "GreveGastPlayerView";
    }
}
