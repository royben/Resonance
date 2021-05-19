using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XamarinClient
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
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
            (list.ItemsSource as INotifyCollectionChanged).CollectionChanged += ChatPage_CollectionChanged;
        }

        private void ChatPage_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
            {
                list.ScrollTo(e.NewItems[0], ScrollToPosition.MakeVisible, false);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            (BindingContext as ViewModel).Disconnect();
            return base.OnBackButtonPressed();
        }
    }
}