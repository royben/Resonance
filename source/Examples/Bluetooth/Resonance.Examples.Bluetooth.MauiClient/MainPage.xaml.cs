namespace Resonance.Examples.Bluetooth.MauiClient
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            Appearing += MainPage_Appearing;
        }

        private void MainPage_Appearing(object sender, EventArgs e)
        {
            (BindingContext as ViewModel).Navigation = Navigation;
        }
    }
}
