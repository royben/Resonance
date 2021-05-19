using Resonance.Examples.Common.Logging;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinClient
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new MainPage());
        }

        protected override void OnStart()
        {
            LoggingConfiguration.ConfigureLogging();
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
