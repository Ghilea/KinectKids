using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KinectKids.Game
{
    /// <summary>
    /// Gränssnitt som alla spel måste implementera för att fungera med GameEngine.
    /// </summary>
    public interface IGame
    {
        /// <summary>
        /// Återställer speltillståndet till start.
        /// </summary>
        void Reset();

        /// <summary>
        /// Uppdaterar spellogiken baserat på tidsskillnad sedan senaste uppdatering.
        /// </summary>
        /// <param name="elapsedSeconds">Tid som har förflutit sedan senaste Update-anrop.</param>
        void Update(double elapsedSeconds);

        /// <summary>
        /// Hanterar spelarens interaktion (t.ex. att "skjuta" på en måltavla).
        /// </summary>
        /// <param name="point">Punkten där interaktionen inträffade.</param>
        /// <param name="handId">Hand-ID som utförde åtgärden.</param>
        /// <param name="now">Nuvarande tidpunkt.</param>
        /// <returns>Svar på interaktionen (poäng, misslyckande etc).</returns>
        object HandleInput(Point point, int handId, DateTime now);

        /// <summary>
        /// Hämtar spelarens nuvarande poäng.
        /// </summary>
        int CurrentScore { get; }

        /// <summary>
        /// Är spelet aktivt och redo att ta emot input?
        /// </summary>
        bool IsActive { get; }
    }
}
