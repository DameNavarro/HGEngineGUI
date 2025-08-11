using Microsoft.UI.Xaml.Controls;

namespace HGEngineGUI.Pages
{
    public sealed partial class PlaceholderPage : Page
    {
        public PlaceholderPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string title = e.Parameter as string ?? "Coming soon";
            TitleText.Text = title;
            if (title == "Trainers")
            {
                List.ItemsSource = Data.HGParsers.TrainerClasses;
            }
        }
    }
}


