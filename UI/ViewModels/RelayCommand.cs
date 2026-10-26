using System;
using System.Windows.Input;

namespace UI.ViewModels;

/// <summary>Commande minimale qui appelle une action quand on clique sur un bouton.</summary>
public class RelayCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute();
}
