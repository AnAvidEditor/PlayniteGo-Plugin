using System.Windows.Controls;

namespace PlayniteGo
{
    public partial class PlayniteGoSettingsView : UserControl
    {
        // The constructor now accepts the view model as an argument.
        public PlayniteGoSettingsView(PlayniteGoSettingsViewModel viewModel)
        {
            InitializeComponent();
            // Explicitly setting the DataContext ensures the bindings in the XAML will work.
            DataContext = viewModel;
        }
    }
}