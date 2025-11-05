using System.Windows;

namespace SpeechToTextTray
{
    /// <summary>
    /// Hidden main window (required for WPF application infrastructure)
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Hide immediately
            Visibility = Visibility.Hidden;
            ShowInTaskbar = false;
        }
    }
}
