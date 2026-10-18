using System.Collections.ObjectModel;
using CarnetDepenses.Models;
using CarnetDepenses.Services;

namespace CarnetDepenses.ViewModels;

public class DepensesViewModel : BaseViewModel
{
    private readonly IDepenseRepository _repo;
    private string _libelle = "", _montantTexte = "";

    public ObservableCollection<Depense> Depenses { get; } = new();
    public decimal Total => Depenses.Sum(d => d.Montant);

    public string Libelle
    {
        get => _libelle;
        set { if (SetField(ref _libelle, value)) AjouterCommand.Rafraichir(); }
    }
    public string MontantTexte
    {
        get => _montantTexte;
        set { if (SetField(ref _montantTexte, value)) AjouterCommand.Rafraichir(); }
    }

    public RelayCommand AjouterCommand { get; }
    public RelayCommandP SupprimerCommand { get; }

    // Le repository est INJECTE par le conteneur (voir 2.6).
    public DepensesViewModel(IDepenseRepository repo)
    {
        _repo = repo;
        AjouterCommand = new RelayCommand(
            async () => await AjouterAsync(),
            () => !string.IsNullOrWhiteSpace(Libelle)
               && !string.IsNullOrWhiteSpace(MontantTexte));
        SupprimerCommand = new RelayCommandP(
            async p => await SupprimerAsync((Depense)p));
    }

    // Relit le stockage et remplit la liste affichee.
    public async Task ChargerAsync()
    {
        Depenses.Clear();
        foreach (var d in await _repo.ListerAsync())
            Depenses.Add(d);
        OnPropertyChanged(nameof(Total));
    }

    private async Task AjouterAsync()
    {
        if (!decimal.TryParse(MontantTexte, out decimal m))
            return;
        var d = new Depense { Libelle = Libelle, Montant = m };
        await _repo.AjouterAsync(d);     // 1. on sauvegarde
        Depenses.Add(d);                 // 2. on affiche
        OnPropertyChanged(nameof(Total));
        Libelle = ""; MontantTexte = "";
    }

    private async Task SupprimerAsync(Depense d)
    {
        await _repo.SupprimerAsync(d);
        Depenses.Remove(d);
        OnPropertyChanged(nameof(Total));
    }
}
