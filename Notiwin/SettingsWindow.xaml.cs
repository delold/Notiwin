using System.Windows;

namespace Notiwin {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        public SettingsWindow() {
            InitializeComponent();

            DismissalCheck.IsChecked = Properties.Settings.Default.NoDismiss;
            NoSoundCheck.IsChecked = Properties.Settings.Default.Silent;
            SquareIconsCheck.IsChecked = Properties.Settings.Default.SquareIcons;
            ContextMenuCheck.IsChecked = Properties.Settings.Default.ContextMenuActions;
        }

        private void SaveClick(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.NoDismiss = DismissalCheck.IsChecked == true;
            Properties.Settings.Default.Silent = NoSoundCheck.IsChecked == true;
            Properties.Settings.Default.SquareIcons = SquareIconsCheck.IsChecked == true;
            Properties.Settings.Default.ContextMenuActions = ContextMenuCheck.IsChecked == true;

            Properties.Settings.Default.Save();

            Close();
        }

        private void CloseClick(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.ToString()));
        }
    }
}
