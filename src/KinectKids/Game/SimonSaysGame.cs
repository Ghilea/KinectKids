using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using KinectKids.Models;

namespace KinectKids.Game
{
    /// <summary>
    /// Rörelsemodul som känner igen fyra tydliga helkroppsposer från de
    /// normaliserade leder som IPlayerTracker redan levererar.
    /// </summary>
    public sealed class SimonSaysGame : IGame
    {
        private static readonly GestureType[] Commands =
        {
            GestureType.HandsUp,
            GestureType.ArmsOut,
            GestureType.HandsTogether,
            GestureType.Duck
        };

        private readonly Random random = new Random();
        private IReadOnlyList<TrackedPlayer> players = Array.Empty<TrackedPlayer>();
        private GestureType currentCommand;
        private double commandRemaining;
        private double poseHold;
        private int lastPlayerIndex;

        public event EventHandler<GameEventArgs> GameEvent;

        public string Id => "SimonSays";
        public string DisplayName => "Simon säger";
        public string Instruction => "Simon säger: " + CommandText(currentCommand);
        public int CurrentScore { get; private set; }
        public bool IsActive { get; private set; }

        public void Start()
        {
            IsActive = true;
            NextCommand();
        }

        public void Stop()
        {
            IsActive = false;
        }

        public void Reset()
        {
            IsActive = false;
            CurrentScore = 0;
            currentCommand = GestureType.None;
            commandRemaining = 0;
            poseHold = 0;
            players = Array.Empty<TrackedPlayer>();
        }

        public void SetPlayers(IReadOnlyList<TrackedPlayer> currentPlayers)
        {
            players = currentPlayers ?? Array.Empty<TrackedPlayer>();
        }

        public void Update(double elapsedSeconds)
        {
            if (!IsActive) return;

            commandRemaining -= elapsedSeconds;
            int matchingPlayer = FindMatchingPlayer();
            if (matchingPlayer >= 0)
            {
                if (matchingPlayer != lastPlayerIndex) poseHold = 0;
                lastPlayerIndex = matchingPlayer;
                poseHold += elapsedSeconds;
                if (poseHold >= 0.65)
                {
                    const int points = 15;
                    CurrentScore += points;
                    Raise("Snyggt! Nästa rörelse…", points, matchingPlayer);
                    NextCommand("great_next");
                    return;
                }
            }
            else
            {
                poseHold = 0;
            }

            if (commandRemaining <= 0)
            {
                Raise("Nästan! Vi tar en ny rörelse.", 0, 0);
                NextCommand("new_movement");
            }
        }

        public void Render(DrawingContext context)
        {
            // Huvudfönstrets skelettlager visualiserar rörelsen.
        }

        public void OnKinectGesture(GestureType gesture)
        {
            // Poserna bedöms kontinuerligt från hela skelettet i Update.
        }

        private int FindMatchingPlayer()
        {
            TrackedPlayer[] ready = players.Where(item => item.IsReady).Take(2).ToArray();
            for (int index = 0; index < ready.Length; index++)
            {
                if (Matches(ready[index], currentCommand)) return index;
            }
            return -1;
        }

        private static bool Matches(TrackedPlayer player, GestureType gesture)
        {
            Point head = player.Head;
            Point hip = player.Hip;
            Point leftHand = player.LeftHand;
            Point rightHand = player.RightHand;
            Point leftShoulder = player.Joint(BodyJoint.ShoulderLeft);
            Point rightShoulder = player.Joint(BodyJoint.ShoulderRight);

            if (!Tracked(head) || !Tracked(hip) || !Tracked(leftHand) || !Tracked(rightHand))
                return false;

            switch (gesture)
            {
                case GestureType.HandsUp:
                    return leftHand.Y < head.Y + 0.02 && rightHand.Y < head.Y + 0.02;
                case GestureType.ArmsOut:
                    return Tracked(leftShoulder)
                           && Tracked(rightShoulder)
                           && leftHand.X < leftShoulder.X - 0.12
                           && rightHand.X > rightShoulder.X + 0.12
                           && Math.Abs(leftHand.Y - leftShoulder.Y) < 0.16
                           && Math.Abs(rightHand.Y - rightShoulder.Y) < 0.16;
                case GestureType.HandsTogether:
                    return Distance(leftHand, rightHand) < 0.115;
                case GestureType.Duck:
                    return head.Y > 0.31 || hip.Y - head.Y < 0.25;
                default:
                    return false;
            }
        }

        private void NextCommand(string introduction = null)
        {
            GestureType previous = currentCommand;
            do
            {
                currentCommand = Commands[random.Next(Commands.Length)];
            }
            while (Commands.Length > 1 && currentCommand == previous);

            commandRemaining = 5.5;
            poseHold = 0;
            lastPlayerIndex = -1;
            string voice = "simon|" + CommandKey(currentCommand);
            if (!string.IsNullOrWhiteSpace(introduction)) voice = introduction + ";" + voice;
            Raise(Instruction, 0, 0, voice);
        }

        private void Raise(string message, int scoreDelta, int playerIndex, string voiceKey = null)
        {
            GameEvent?.Invoke(this, new GameEventArgs
            {
                Message = message,
                ScoreDelta = scoreDelta,
                PlayerIndex = playerIndex,
                VoiceKey = voiceKey
            });
        }

        private static string CommandKey(GestureType command)
        {
            switch (command)
            {
                case GestureType.HandsUp: return "hands_up";
                case GestureType.ArmsOut: return "arms_out";
                case GestureType.HandsTogether: return "hands_together";
                case GestureType.Duck: return "duck";
                default: return "ready";
            }
        }

        private static string CommandText(GestureType command)
        {
            switch (command)
            {
                case GestureType.HandsUp: return "händerna över huvudet!";
                case GestureType.ArmsOut: return "armarna rakt ut!";
                case GestureType.HandsTogether: return "händerna tillsammans!";
                case GestureType.Duck: return "ducka!";
                default: return "gör dig redo!";
            }
        }

        private static bool Tracked(Point point)
        {
            return point.X >= 0 && point.X <= 1 && point.Y >= 0 && point.Y <= 1;
        }

        private static double Distance(Point first, Point second)
        {
            double dx = first.X - second.X;
            double dy = first.Y - second.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
