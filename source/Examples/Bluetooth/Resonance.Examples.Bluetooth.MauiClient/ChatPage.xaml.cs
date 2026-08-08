using System.Collections.Specialized;

namespace Resonance.Examples.Bluetooth.MauiClient
{
    public partial class ChatPage : ContentPage
    {
        public ChatPage()
        {
            InitializeComponent();
            Appearing += ChatPage_Appearing;
        }

        private void ChatPage_Appearing(object sender, EventArgs e)
        {
            (BindingContext as ViewModel).Navigation = Navigation;

            if (list.ItemsSource is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged -= ChatPage_CollectionChanged;
                observable.CollectionChanged += ChatPage_CollectionChanged;
            }
        }

        private void ChatPage_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?.Count > 0)
            {
                list.ScrollTo(e.NewItems[0], position: ScrollToPosition.MakeVisible, animate: false);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            (BindingContext as ViewModel).Disconnect();
            return base.OnBackButtonPressed();
        }
    }
}
