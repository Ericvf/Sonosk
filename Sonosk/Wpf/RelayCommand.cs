using System.Diagnostics;
using System.Windows.Input;

namespace Sonosk.Wpf
{
    public class AsyncRelayCommand : ICommand
    {
        readonly Func<object?, Task> _executeAsync;
        readonly Predicate<object?>? _canExecute;

        public AsyncRelayCommand(Func<object?, Task> executeAsync)
            : this(executeAsync, null)
        { }

        public AsyncRelayCommand(Func<object?, Task> executeAsync, Predicate<object?>? canExecute)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // ICommand requires void Execute — use async void but surface Task via ExecuteAsync for callers/tests.
        public async void Execute(object? parameter)
        {
            try
            {
                await _executeAsync(parameter).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }

        public Task ExecuteAsync(object? parameter) => _executeAsync(parameter);
    }


    public class RelayCommand : ICommand
    {
        #region Fields

        readonly Action<object?>? _execute;
        readonly Predicate<object?>? _canExecute;

        #endregion 

        #region Constructors

        public RelayCommand(Action<object?>? execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<object?>? execute, Predicate<object?>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        #endregion

        #region ICommand Members

        [DebuggerStepThrough]
        public bool CanExecute(object? parameters)
        {
            return _canExecute == null || _canExecute(parameters);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object? parameters)
        {
            _execute?.Invoke(parameters);
        }

        #endregion // ICommand Members
    }

    public class RelayCommand<TEntity> : ICommand
    {
        private readonly Action<TEntity> _execute;
        private readonly Func<bool>? _canExecute;

  
        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<TEntity> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<TEntity> execute, Func<bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }


        public void Execute(object? parameter)
        {
            if (parameter != null)
            {
                var tEntity = (TEntity)parameter;
                _execute(tEntity);
            }
        }

  
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
