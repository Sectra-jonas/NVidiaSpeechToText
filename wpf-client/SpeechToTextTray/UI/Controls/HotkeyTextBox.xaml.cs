using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SpeechToTextTray.Core.Models;

namespace SpeechToTextTray.UI.Controls
{
    /// <summary>
    /// Custom control for capturing hotkey combinations
    /// </summary>
    public partial class HotkeyTextBox : UserControl
    {
        public static readonly DependencyProperty HotkeyConfigProperty =
            DependencyProperty.Register(
                nameof(HotkeyConfig),
                typeof(HotkeyConfig),
                typeof(HotkeyTextBox),
                new PropertyMetadata(null, OnHotkeyConfigChanged));

        public static readonly DependencyProperty HotkeyTextProperty =
            DependencyProperty.Register(
                nameof(HotkeyText),
                typeof(string),
                typeof(HotkeyTextBox),
                new PropertyMetadata("Press keys..."));

        public HotkeyConfig HotkeyConfig
        {
            get => (HotkeyConfig)GetValue(HotkeyConfigProperty);
            set => SetValue(HotkeyConfigProperty, value);
        }

        public string HotkeyText
        {
            get => (string)GetValue(HotkeyTextProperty);
            private set => SetValue(HotkeyTextProperty, value);
        }

        public HotkeyTextBox()
        {
            InitializeComponent();
            UpdateDisplay();
        }

        private static void OnHotkeyConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HotkeyTextBox control)
            {
                control.UpdateDisplay();
            }
        }

        private void TxtHotkey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            // Get the pressed key
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignore modifier keys alone
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // Get modifiers
            ModifierKeys modifiers = Keyboard.Modifiers;

            // Require at least one modifier
            if (modifiers == ModifierKeys.None)
            {
                HotkeyText = "Please use at least one modifier key (Ctrl, Alt, Shift)";
                return;
            }

            // Create new hotkey config
            HotkeyConfig = new HotkeyConfig
            {
                Modifiers = modifiers,
                Key = key
            };

            UpdateDisplay();

            // Clear focus to indicate capture is complete
            Keyboard.ClearFocus();
        }

        private void TxtHotkey_GotFocus(object sender, RoutedEventArgs e)
        {
            HotkeyText = "Press key combination...";
            txtHotkey.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightYellow);
        }

        private void TxtHotkey_LostFocus(object sender, RoutedEventArgs e)
        {
            txtHotkey.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (HotkeyConfig != null)
            {
                HotkeyText = HotkeyConfig.ToString();
            }
            else
            {
                HotkeyText = "Press to set hotkey...";
            }
        }
    }
}
