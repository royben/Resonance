using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Resonance.Examples.Common
{
    public abstract class ResonanceViewModel : INotifyPropertyChanged
    {
        private Dispatcher _dispatcher;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <returns></returns>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the logger for this view model.
        /// </summary>
        protected ILogger Logger { get; }

        private bool _isFree;
        /// <summary>
        /// Gets or sets a value indicating whether to disable the view.
        /// </summary>
        public bool IsFree
        {
            get { return _isFree; }
            set { _isFree = value; RaisePropertyChangedAuto(); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceViewModel"/> class.
        /// </summary>
        public ResonanceViewModel()
        {
            IsFree = true;
            Logger = ResonanceGlobalSettings.Default.LoggerFactory.CreateLogger(this.GetType().Name);
            Application.Current.MainWindow.ContentRendered += MainWindow_ContentRendered;
            Application.Current.MainWindow.Closing += MainWindow_Closing;

            var fakeToRef = MaterialDesignThemes.Wpf.PackIconKind.Abc; //Just to make VS copy the assembly... :/
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Task.Factory.StartNew(() =>
            {
                OnApplicationShutdown();
                Environment.Exit(0);
            });
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            _dispatcher = Application.Current.MainWindow.Dispatcher;
            OnApplicationReady();
        }

        /// <summary>
        /// Called when the application window has finished first rendering.
        /// </summary>
        protected virtual void OnApplicationReady()
        {

        }

        /// <summary>
        /// Called when the application window is closing.
        /// </summary>
        protected virtual void OnApplicationShutdown()
        {

        }

        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        protected virtual void RaisePropertyChanged(String propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        protected virtual void RaisePropertyChangedAuto([CallerMemberName] string caller = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

        /// <summary>
        /// Raises all relay commands CanExecute methods in the current instance.
        /// </summary>
        protected virtual void InvalidateRelayCommands()
        {
            InvokeUI(() =>
            {
                foreach (var prop in this.GetType().GetProperties().Where(x => typeof(RelayCommand).IsAssignableFrom(x.PropertyType)))
                {
                    var value = prop.GetValue(this) as RelayCommand;

                    if (value != null)
                    {
                        value.RaiseCanExecuteChanged();
                    }
                }
            });
        }

        /// <summary>
        /// Invokes the specified action on the UI Thread.
        /// </summary>
        /// <param name="action">The action.</param>
        protected virtual void InvokeUI(Action action)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Displays an error message to the user.
        /// </summary>
        /// <param name="message">The message.</param>
        protected virtual Task ShowErrorMessage(String message)
        {
            TaskCompletionSource<object> completion = new TaskCompletionSource<object>();

            _dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(Application.Current.MainWindow, message, "Resonance", MessageBoxButton.OK, MessageBoxImage.Error);
                completion.SetResult(true);
            }));

            return completion.Task;
        }

        /// <summary>
        /// Displays an information message to the user.
        /// </summary>
        /// <param name="message">The message.</param>
        protected virtual Task ShowInfoMessage(String message)
        {
            TaskCompletionSource<object> completion = new TaskCompletionSource<object>();

            _dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(Application.Current.MainWindow, message, "Resonance", MessageBoxButton.OK, MessageBoxImage.Information);
                completion.SetResult(true);
            }));

            return completion.Task;
        }

        /// <summary>
        /// Displays a question message to the user and return true when 'Yes' pressed.
        /// </summary>
        /// <param name="message">The message.</param>
        protected virtual Task<bool> ShowQuestionMessage(String message)
        {
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

            _dispatcher.BeginInvoke(new Action(() =>
            {
                var result = MessageBox.Show(Application.Current.MainWindow, message, "Resonance", MessageBoxButton.YesNo, MessageBoxImage.Question);
                completion.SetResult(result == MessageBoxResult.Yes);
            }));

            return completion.Task;
        }
    }
}
