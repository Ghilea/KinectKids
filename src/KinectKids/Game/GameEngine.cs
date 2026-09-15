using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KinectKids.Game
{
    public sealed class GameEngine
    {
        private static readonly Brush[] Colors =
        {
            new SolidColorBrush(Color.FromRgb(255, 91, 119)),
            new SolidColorBrush(Color.FromRgb(255, 190, 71)),
            new SolidColorBrush(Color.FromRgb(87, 213, 167)),
            new SolidColorBrush(Color.FromRgb(94, 174, 255)),
            new SolidColorBrush(Color.FromRgb(184, 113, 255))
        };

        private readonly Canvas canvas;
        private readonly Random random = new Random();
        private readonly List<Balloon> balloons = new List<Balloon>();
        private double spawnAccumulator;

        public GameEngine(Canvas canvas)
        {
            this.canvas = canvas;
        }

        public IReadOnlyList<Balloon> Balloons => balloons;

        public void Reset()
        {
            foreach (var balloon in balloons) canvas.Children.Remove(balloon.Shape);
            balloons.Clear();
            spawnAccumulator = 0;
        }

        public void Update(double elapsedSeconds)
        {
            if (canvas.ActualWidth < 100 || canvas.ActualHeight < 100) return;
            spawnAccumulator += elapsedSeconds;
            if (spawnAccumulator >= 0.72 && balloons.Count < 14)
            {
                spawnAccumulator = 0;
                Spawn();
            }

            foreach (var balloon in balloons.ToArray())
            {
                balloon.Y -= balloon.Speed * elapsedSeconds;
                Position(balloon);
                if (balloon.Y + balloon.Radius < 0) Remove(balloon);
            }
        }

        public int PopAt(Point point, double hitRadius)
        {
            var hit = balloons
                .OrderByDescending(item => item.Points)
                .FirstOrDefault(item => Distance(point.X, point.Y, item.X, item.Y) <= item.Radius + hitRadius);
            if (hit == null) return 0;
            int points = hit.Points;
            Remove(hit);
            return points;
        }

        private void Spawn()
        {
            double radius = random.Next(34, 59);
            var color = Colors[random.Next(Colors.Length)];
            var shape = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2.25,
                Fill = color,
                Stroke = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                StrokeThickness = 5,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 18,
                    ShadowDepth = 5,
                    Opacity = 0.25,
                    Color = System.Windows.Media.Colors.Black
                },
                IsHitTestVisible = false
            };

            var balloon = new Balloon
            {
                Shape = shape,
                Radius = radius,
                X = radius + random.NextDouble() * Math.Max(1, canvas.ActualWidth - radius * 2),
                Y = canvas.ActualHeight + radius,
                Speed = random.Next(80, 151),
                Points = radius < 45 ? 20 : 10,
                Color = color
            };
            balloons.Add(balloon);
            canvas.Children.Insert(0, shape);
            Position(balloon);
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static void Position(Balloon balloon)
        {
            Canvas.SetLeft(balloon.Shape, balloon.X - balloon.Radius);
            Canvas.SetTop(balloon.Shape, balloon.Y - balloon.Radius);
        }

        private void Remove(Balloon balloon)
        {
            balloons.Remove(balloon);
            canvas.Children.Remove(balloon.Shape);
        }
    }
}
