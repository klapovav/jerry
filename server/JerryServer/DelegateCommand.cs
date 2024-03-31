using System;
using System.Windows.Input;

namespace JerryServer;

public class DelegateCommand : ICommand
{
    public Action CommandAction { get; set; }
    public Func<bool> CanExecuteFunc { get; set; }

    public void Execute(object parameter)
    {
        CommandAction();
    }

    public bool CanExecute(object parameter)
    {
        return CanExecuteFunc is null || CanExecuteFunc();
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}