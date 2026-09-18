using System;
using System.Windows;
using System.Windows.Controls;

namespace KinectKids.Controls
{
    public partial class GameSessionMenu : UserControl
    {
        public GameSessionMenu()
        {
            InitializeComponent();
        }

        public event EventHandler ContinueRequested;
        public event EventHandler RestartRequested;
        public event EventHandler HomeRequested;
        public event EventHandler Opened;

        public bool IsOpen => Overlay.Visibility == Visibility.Visible;

        public void Open()
        {
            Overlay.Visibility = Visibility.Visible;
            OpenButton.Visibility = Visibility.Collapsed;
        }

        public void Close()
        {
            Overlay.Visibility = Visibility.Collapsed;
            OpenButton.Visibility = Visibility.Visible;
        }

        public void HideAll()
        {
            Overlay.Visibility = Visibility.Collapsed;
            OpenButton.Visibility = Visibility.Collapsed;
        }

        public void ShowButton()
        {
            Overlay.Visibility = Visibility.Collapsed;
            OpenButton.Visibility = Visibility.Visible;
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            Open();
            Opened?.Invoke(this, EventArgs.Empty);
        }
        private void ContinueButton_Click(object sender, RoutedEventArgs e) => ContinueRequested?.Invoke(this, EventArgs.Empty);
        private void RestartButton_Click(object sender, RoutedEventArgs e) => RestartRequested?.Invoke(this, EventArgs.Empty);
        private void HomeButton_Click(object sender, RoutedEventArgs e) => HomeRequested?.Invoke(this, EventArgs.Empty);
    }
}
