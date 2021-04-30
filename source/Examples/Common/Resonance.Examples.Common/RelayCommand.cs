using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Resonance.Examples.Common
{
    public class RelayCommand : ICommand
    {
        protected Action<object> _action;
        protected Func<object, bool> _canExecute;

        public event EventHandler<object> Executed;

        public RelayCommand(Action<object> action, Func<object, bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object> action, Func<bool> canExecute) : this(action, new Func<object, bool>((x) => canExecute()))
        {

        }

        public RelayCommand(Action<object> action) : this(action, new Func<object, bool>((x) => true))
        {

        }

        public RelayCommand(Action action, Func<object, bool> canExecute) : this((x) => action(), canExecute)
        {

        }

        public RelayCommand(Action action) : this((x) => action(), new Func<object, bool>((x) => true))
        {

        }

        public RelayCommand(Action action, Func<bool> canExecute) : this((x) => action(), canExecute)
        {

        }

        public virtual bool CanExecute(object parameter)
        {
            try
            {
                return _canExecute != null ? _canExecute(parameter) : true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error on CanExecute RelayCommand\n" + ex);
                return false;
            }
        }

        public virtual void Execute(object parameter)
        {
            _action?.Invoke(parameter);
            Executed?.Invoke(this, parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }

        public event EventHandler CanExecuteChanged;
    }

    public class RelayCommand<T> : RelayCommand
    {
        public new event EventHandler<T> Executed;

        public RelayCommand(Action<T> action, Func<T, bool> canExecute) : base((x) => action((T)x), (x) => canExecute((T)x))
        {

        }

        public RelayCommand(Action<T> action, Func<bool> canExecute) : this(action, new Func<T, bool>((x) => canExecute()))
        {

        }

        public RelayCommand(Action<T> action) : base((x) => action((T)x))
        {

        }

        public override void Execute(object parameter)
        {
            _action?.Invoke(parameter);
            Executed?.Invoke(this, parameter != null ? (T)parameter : default(T));
        }
    }

    public class DelayedRelayCommand : RelayCommand
    {
        public TimeSpan Delay { get; set; }


        public DelayedRelayCommand(Action<object> action) : base(action)
        {
        }

        public DelayedRelayCommand(Action action) : base(action)
        {
        }

        public DelayedRelayCommand(Action<object> action, Func<object, bool> canExecute) : base(action, canExecute)
        {
        }

        public DelayedRelayCommand(Action<object> action, Func<bool> canExecute) : base(action, canExecute)
        {
        }

        public DelayedRelayCommand(Action action, Func<object, bool> canExecute) : base(action, canExecute)
        {
        }

        public DelayedRelayCommand(Action action, Func<bool> canExecuteChange) : base(action, canExecuteChange)
        {
        }

        public override async void Execute(object parameter)
        {
            await Task.Delay(Delay);
            base.Execute(parameter);
        }
    }
}
