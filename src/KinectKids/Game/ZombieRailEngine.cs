using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace KinectKids.Game
{
    public sealed class ZombieRailEngine
    {
        private const double AimMilliseconds = 440;
        private readonly Canvas canvas;
        private readonly Random random = new Random();
        private readonly List<ZombieTarget> targets = new List<ZombieTarget>();
        private readonly Dictionary<int, AimState> aimStates = new Dictionary<int, AimState>();
        private readonly ImageSource scoutSource;
        private readonly ImageSource bossSource;
        private double spawnAccumulator;
        private double elapsed;
        private bool bossSpawned;

        public ZombieRailEngine(Canvas canvas)
        {
            this.canvas = canvas;
            scoutSource = LoadImage("Assets/zombie-scout.png");
            bossSource = LoadImage("Assets/zombie-conductor-boss.png");
        }

        public bool BossIsActive => targets.Any(item => item.IsBoss);

        public void Reset()
        {
            foreach (var target in targets) canvas.Children.Remove(target.Visual);
            targets.Clear();
            aimStates.Clear();
            spawnAccumulator = 0;
            elapsed = 0;
            bossSpawned = false;
        }

        public void Update(double elapsedSeconds)
        {
            if (canvas.ActualWidth < 100 || canvas.ActualHeight < 100) return;
            elapsed += elapsedSeconds;
            spawnAccumulator += elapsedSeconds;

            double spawnDelay = elapsed < 20 ? 1.55 : 1.25;
            if (spawnAccumulator >= spawnDelay && targets.Count(item => !item.IsBoss) < 5)
            {
                spawnAccumulator = 0;
                Spawn(false);
            }

            if (!bossSpawned && elapsed >= 43)
            {
                bossSpawned = true;
                Spawn(true);
            }

            foreach (var target in targets.ToArray())
            {
                target.Age += elapsedSeconds;
                Position(target);
                if (target.Age >= target.Lifetime) Remove(target);
            }
        }

        public ZombieAimResult AimAt(Point point, int handId, DateTime now)
        {
            var result = new ZombieAimResult { HitPoint = point };
            AimState state;
            if (!aimStates.TryGetValue(handId, out state))
            {
                state = new AimState();
                aimStates[handId] = state;
            }

            if (now < state.CooldownUntil) return result;

            var target = targets
                .Where(item => Contains(item, point))
                .OrderByDescending(item => item.IsBoss)
                .ThenByDescending(item => item.Width)
                .FirstOrDefault();

            if (target == null)
            {
                state.Target = null;
                state.StartedAt = now;
                return result;
            }

            if (state.Target != target)
            {
                state.Target = target;
                state.StartedAt = now;
            }

            result.IsAiming = true;
            result.Progress = Math.Min(1, (now - state.StartedAt).TotalMilliseconds / AimMilliseconds);
            if (result.Progress < 1) return result;

            result.WasBossHit = target.IsBoss;
            result.Points = target.IsBoss ? 25 : 15;
            target.Health--;
            UpdateHealth(target);
            Flash(target);
            if (target.Health <= 0)
            {
                result.Points += target.IsBoss ? 150 : 10;
                Remove(target);
            }

            state.Target = null;
            state.StartedAt = now;
            state.CooldownUntil = now.AddMilliseconds(230);
            result.Progress = 0;
            return result;
        }

        public void ClearAim(int handId)
        {
            AimState state;
            if (aimStates.TryGetValue(handId, out state)) state.Target = null;
        }

        private void Spawn(bool boss)
        {
            double lane = boss ? 0.5 : new[] { 0.22, 0.38, 0.62, 0.78 }[random.Next(4)];
            int health = boss ? 8 : (elapsed > 28 && random.NextDouble() < 0.34 ? 2 : 1);
            var image = new Image
            {
                Source = boss ? bossSource : scoutSource,
                Stretch = Stretch.Uniform,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect
                {
                    BlurRadius = boss ? 28 : 18,
                    ShadowDepth = 6,
                    Opacity = 0.72,
                    Color = boss ? Color.FromRgb(255, 190, 71) : Colors.Black
                }
            };

            var healthFill = new Border
            {
                Background = boss ? BrushFrom("#FFFFBE47") : BrushFrom("#FF57D5A7"),
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(4)
            };
            var healthTrack = new Border
            {
                Height = boss ? 14 : 9,
                Background = BrushFrom("#AA14213D"),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(12, 0, 12, 0),
                Child = healthFill
            };
            var visual = new Grid { IsHitTestVisible = false };
            visual.Children.Add(image);
            visual.Children.Add(healthTrack);

            var target = new ZombieTarget
            {
                Visual = visual,
                HealthFill = healthFill,
                LaneX = lane,
                Lifetime = boss ? 18 : random.Next(8, 12),
                Phase = random.NextDouble() * Math.PI * 2,
                Health = health,
                MaxHealth = health,
                IsBoss = boss
            };
            targets.Add(target);
            canvas.Children.Add(visual);
            Position(target);
            UpdateHealth(target);
        }

        private void Position(ZombieTarget target)
        {
            double progress = Math.Min(1, target.Age / target.Lifetime);
            double eased = progress * progress * (3 - 2 * progress);
            double baseWidth = target.IsBoss ? 205 : 105;
            double finalWidth = target.IsBoss ? 390 : 255;
            target.Width = baseWidth + (finalWidth - baseWidth) * eased;
            target.Height = target.Width * 1.5;
            double sway = Math.Sin(target.Age * (target.IsBoss ? 1.4 : 2.1) + target.Phase) * canvas.ActualWidth * 0.025;
            target.X = target.LaneX * canvas.ActualWidth + sway - target.Width / 2;
            target.Y = canvas.ActualHeight * (0.20 + 0.38 * eased) - target.Height * 0.25;

            target.Visual.Width = target.Width;
            target.Visual.Height = target.Height;
            Canvas.SetLeft(target.Visual, target.X);
            Canvas.SetTop(target.Visual, target.Y);
            Panel.SetZIndex(target.Visual, (int)(100 + target.Width));
        }

        private static bool Contains(ZombieTarget target, Point point)
        {
            double paddingX = target.Width * 0.08;
            double paddingY = target.Height * 0.08;
            return point.X >= target.X + paddingX
                   && point.X <= target.X + target.Width - paddingX
                   && point.Y >= target.Y + paddingY
                   && point.Y <= target.Y + target.Height - paddingY;
        }

        private static void UpdateHealth(ZombieTarget target)
        {
            double available = Math.Max(1, target.Width - 28);
            target.HealthFill.Width = available * Math.Max(0, target.Health) / target.MaxHealth;
        }

        private static void Flash(ZombieTarget target)
        {
            var flash = new DoubleAnimation(0.25, 1, TimeSpan.FromMilliseconds(180));
            target.Visual.BeginAnimation(UIElement.OpacityProperty, flash);
        }

        private void Remove(ZombieTarget target)
        {
            targets.Remove(target);
            canvas.Children.Remove(target.Visual);
            foreach (var state in aimStates.Values.Where(item => item.Target == target)) state.Target = null;
        }

        private static ImageSource LoadImage(string relativePath)
        {
            return new BitmapImage(new Uri("pack://application:,,,/KinectKids;component/" + relativePath, UriKind.Absolute));
        }

        private static Brush BrushFrom(string color)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private sealed class AimState
        {
            public ZombieTarget Target { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime CooldownUntil { get; set; }
        }
    }
}
