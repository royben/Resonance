using Resonance.Examples.Common.Logging;

namespace Resonance.Examples.Bluetooth.MauiClient
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            LoggingConfiguration.ConfigureLogging();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            return new Window(new NavigationPage(new MainPage()));
        }
    }
}
