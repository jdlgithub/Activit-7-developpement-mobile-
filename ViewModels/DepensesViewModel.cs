using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using CarnetDepenses.Models;
using CarnetDepenses.Services;

namespace CarnetDepenses.ViewModels;

public class DepensesViewModel : BaseViewModel
{
    private readonly IDepenseRepository _repo;
    private readonly IApiService _api;
    private bool _isBusy;
    private string _totalConverti = "", _message = "";
    private string _libelle = "", _montantTexte = "", _recherche = "";
    private List<Depense> _toutes = new();

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

    // Filtre la liste affichee a chaque frappe (insensible a la casse).
    public string Recherche
    {
        get => _recherche;
        set { if (SetField(ref _recherche, value)) AppliquerFiltre(); }
    }

    public RelayCommand AjouterCommand { get; }
    public RelayCommandP SupprimerCommand { get; }
    public bool IsBusy
    {
        get => _isBusy;
        set { if (SetField(ref _isBusy, value))
                ConvertirCommand.Rafraichir(); }
    }
    public string TotalConverti
    {
        get => _totalConverti;
        set => SetField(ref _totalConverti, value);
    }
    public string Message
    {
        get => _message;
        set => SetField(ref _message, value);
    }
    public RelayCommand ConvertirCommand { get; }

    // Le repository est INJECTE par le conteneur (voir 2.6).
    public DepensesViewModel(IDepenseRepository repo, IApiService api)
    {
        _repo = repo;
        _api = api;
        AjouterCommand = new RelayCommand(
            async () => await AjouterAsync(),
            () => !string.IsNullOrWhiteSpace(Libelle)
               && !string.IsNullOrWhiteSpace(MontantTexte));
        SupprimerCommand = new RelayCommandP(
            async p => await SupprimerAsync((Depense)p));
        ConvertirCommand = new RelayCommand(
            async () => await ConvertirAsync(), () => !IsBusy);
    }

    // Relit le stockage puis applique le filtre courant.
    public async Task ChargerAsync()
    {
        _toutes = await _repo.ListerAsync();
        AppliquerFiltre();
    }

    // Reconstruit la liste affichee depuis _toutes selon la recherche.
    private void AppliquerFiltre()
    {
        string q = (_recherche ?? "").Trim();
        Depenses.Clear();
        foreach (var d in _toutes)
            if (q.Length == 0 ||
                d.Libelle.Contains(q, StringComparison.OrdinalIgnoreCase))
                Depenses.Add(d);
        OnPropertyChanged(nameof(Total));
    }

    private async Task AjouterAsync()
    {
        if (!decimal.TryParse(MontantTexte, out decimal m))
            return;
        var d = new Depense { Libelle = Libelle, Montant = m };
        await _repo.AjouterAsync(d);     // 1. on sauvegarde
        _toutes.Add(d);                  // 2. on memorise
        AppliquerFiltre();                // 3. on affiche (filtre compris)
        Libelle = ""; MontantTexte = "";
    }

    private async Task SupprimerAsync(Depense d)
    {
        await _repo.SupprimerAsync(d);
        _toutes.Remove(d);
        AppliquerFiltre();
    }

    private async Task ConvertirAsync()
    {
        IsBusy = true; Message = "";
        try
        {
            var t = await _api.GetTauxAsync();   // appel reseau
            Preferences.Set("taux_eur",
              (double)t.Taux["EUR"]);             // cache
            TotalConverti = $"{Total * t.Taux["EUR"]:F2} EUR";
        }
        catch (HttpRequestException)              // reseau coupe
        {
            double c = Preferences.Get("taux_eur", 0.0);
            if (c == 0) Message = "Hors ligne, pas de taux.";
            else
            {
                TotalConverti =
                  $"{Total * (decimal)c:F2} EUR (cache)";
                Message = "Hors ligne : dernier taux connu.";
            }
        }
        finally { IsBusy = false; }  // execute TOUJOURS
    }
}
