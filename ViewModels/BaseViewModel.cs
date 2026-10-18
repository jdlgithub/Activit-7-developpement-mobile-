using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CarnetDepenses.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string nom = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));

    protected bool SetField<T>(ref T champ, T valeur,
                               [CallerMemberName] string nom = null)
    {
        if (EqualityComparer<T>.Default.Equals(champ, valeur)) return false;
        champ = valeur;
        OnPropertyChanged(nom);
        return true;
    }
}
