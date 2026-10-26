using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UI.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private readonly Dictionary<string, object?> _values = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Change la valeur et prévient l'interface seulement si elle a vraiment changé.</summary>
    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }

    /// <summary>Lit une propriété stockée par Set (évite d'écrire un champ pour chaque propriété du HUD).</summary>
    protected T Get<T>([CallerMemberName] string propertyName = "") =>
        _values.TryGetValue(propertyName, out var value) && value is T typed ? typed : default!;

    /// <summary>Stocke une propriété et prévient l'interface si elle a changé.</summary>
    protected void Set<T>(T value, [CallerMemberName] string propertyName = "")
    {
        if (_values.TryGetValue(propertyName, out var old) && EqualityComparer<T>.Default.Equals((T)old!, value))
            return;

        _values[propertyName] = value;
        OnPropertyChanged(propertyName);
    }

    protected void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
