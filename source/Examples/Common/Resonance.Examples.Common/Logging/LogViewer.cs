using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Resonance.Examples.Common.Logging
{
    public class LogViewer : Control
    {
        private DataGrid _dataGrid;
        private ComboBox _comboLogLevel;

        public ObservableCollection<LogEventVM> Logs
        {
            get { return (ObservableCollection<LogEventVM>)GetValue(LogsProperty); }
            set { SetValue(LogsProperty, value); }
        }
        public static readonly DependencyProperty LogsProperty =
            DependencyProperty.Register("Logs", typeof(ObservableCollection<LogEventVM>), typeof(LogViewer), new PropertyMetadata(null));

        public int MaxLogs
        {
            get { return (int)GetValue(MaxLogsProperty); }
            set { SetValue(MaxLogsProperty, value); }
        }
        public static readonly DependencyProperty MaxLogsProperty =
            DependencyProperty.Register("MaxLogs", typeof(int), typeof(LogViewer), new PropertyMetadata(1000));

        public bool ScrollToLast
        {
            get { return (bool)GetValue(ScrollToLastProperty); }
            set { SetValue(ScrollToLastProperty, value); }
        }
        public static readonly DependencyProperty ScrollToLastProperty =
            DependencyProperty.Register("ScrollToLast", typeof(bool), typeof(LogViewer), new PropertyMetadata(true));

        public LogViewer()
        {
            Logs = new ObservableCollection<LogEventVM>();
            LoggingConfiguration.LogReceived += LoggingConfiguration_LogReceived;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _dataGrid = GetTemplateChild("PART_Grid") as DataGrid;
            _comboLogLevel = GetTemplateChild("PART_ComboLevel") as ComboBox;
            _comboLogLevel.Items.Clear();
            _comboLogLevel.SelectionChanged -= _comboLogLevel_SelectionChanged;
            _comboLogLevel.SelectionChanged += _comboLogLevel_SelectionChanged;

            foreach (var value in Enum.GetValues(typeof(LogEventLevel)).Cast<LogEventLevel>())
            {
                _comboLogLevel.Items.Add(value);
            }

            _comboLogLevel.SelectedItem = LoggingConfiguration.LoggingLevelSwitch.MinimumLevel;
        }

        private void _comboLogLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoggingConfiguration.LoggingLevelSwitch.MinimumLevel = (LogEventLevel)_comboLogLevel.SelectedItem;
        }

        private void LoggingConfiguration_LogReceived(object sender, LogReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Logs.Count > MaxLogs)
                {
                    Logs.RemoveAt(0);
                }

                LogEventVM vm = new LogEventVM(e.LogEvent, e.FormatProvider);
                Logs.Add(vm);

                if (ScrollToLast) _dataGrid?.ScrollIntoView(vm);
            }));
        }

        static LogViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LogViewer), new FrameworkPropertyMetadata(typeof(LogViewer)));
        }
    }
}
