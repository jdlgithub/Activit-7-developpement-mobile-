using System.Windows.Input;

namespace CarnetDepenses.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action _executer;
    private readonly Func<bool> _peutExecuter;

    public RelayCommand(Action executer, Func<bool> peutExecuter = null)
    {
        _executer = executer;
        _peutExecuter = peutExecuter;
    }

    public bool CanExecute(object p) => _peutExecuter?.Invoke() ?? true;
    public void Execute(object p) => _executer();

    public event EventHandler CanExecuteChanged;
    public void Rafraichir()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
