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
    private Depense _enEdition;
    private DateTime _date = DateTime.Today;

    public ObservableCollection<Depense> Depenses { get; } = new();
    public decimal Total => Depenses.Sum(d => d.Montant);

    public string Libelle
    {
        get => _libelle;
        set { if (SetField(ref _libelle, value)) RafraichirFormulaires(); }
    }
    public string MontantTexte
    {
        get => _montantTexte;
        set { if (SetField(ref _montantTexte, value)) RafraichirFormulaires(); }
    }

    // Date selectionnee dans le formulaire (et pendant une edition).
    public DateTime Date
    {
        get => _date;
        set => SetField(ref _date, value);
    }

    // Vrai si une depense existante est en cours de modification.
    public bool EnEdition => _enEdition != null;
    public bool PasEnEdition => _enEdition == null;

    // Filtre la liste affichee a chaque frappe (insensible a la casse).
    public string Recherche
    {
        get => _recherche;
        set { if (SetField(ref _recherche, value)) AppliquerFiltre(); }
    }

    public RelayCommand AjouterCommand { get; }
    public RelayCommandP SupprimerCommand { get; }
    public RelayCommandP ModifierCommand { get; }
    public RelayCommand EnregistrerCommand { get; }
    public RelayCommand AnnulerCommand { get; }
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
            () => !EnEdition
               && !string.IsNullOrWhiteSpace(Libelle)
               && !string.IsNullOrWhiteSpace(MontantTexte));
        SupprimerCommand = new RelayCommandP(
            async p => await SupprimerAsync((Depense)p));
        ModifierCommand = new RelayCommandP(p => ChargerEdition((Depense)p));
        EnregistrerCommand = new RelayCommand(
            async () => await EnregistrerAsync(),
            () => EnEdition
               && !string.IsNullOrWhiteSpace(Libelle)
               && !string.IsNullOrWhiteSpace(MontantTexte));
        AnnulerCommand = new RelayCommand(AnnulerEdition);
        ConvertirCommand = new RelayCommand(
            async () => await ConvertirAsync(), () => !IsBusy);
    }

    private void RafraichirFormulaires()
    {
        AjouterCommand?.Rafraichir();
        EnregistrerCommand?.Rafraichir();
    }

    // Relit le stockage puis applique le filtre courant.
    public async Task ChargerAsync()
    {
        _enEdition = null;
        Libelle = ""; MontantTexte = ""; Date = DateTime.Today;
        OnPropertyChanged(nameof(EnEdition));
        OnPropertyChanged(nameof(PasEnEdition));
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
        var d = new Depense { Libelle = Libelle, Montant = m, Date = Date };
        await _repo.AjouterAsync(d);     // 1. on sauvegarde
        _toutes.Add(d);                  // 2. on memorise
        AppliquerFiltre();                // 3. on affiche (filtre compris)
        Libelle = ""; MontantTexte = ""; Date = DateTime.Today;
    }

    // Bascule le formulaire en mode edition et le pre-remplit.
    private void ChargerEdition(Depense d)
    {
        _enEdition = d;
        Libelle = d.Libelle;
        MontantTexte = d.Montant.ToString();
        Date = d.Date;
        OnPropertyChanged(nameof(EnEdition));
        OnPropertyChanged(nameof(PasEnEdition));
        RafraichirFormulaires();
    }

    private async Task EnregistrerAsync()
    {
        if (_enEdition == null) return;
        if (!decimal.TryParse(MontantTexte, out decimal m))
            return;
        _enEdition.Libelle = Libelle;
        _enEdition.Montant = m;
        _enEdition.Date = Date;
        await _repo.MettreAJourAsync(_enEdition);
        AnnulerEdition();
        AppliquerFiltre();
    }

    private void AnnulerEdition()
    {
        _enEdition = null;
        Libelle = ""; MontantTexte = ""; Date = DateTime.Today;
        OnPropertyChanged(nameof(EnEdition));
        OnPropertyChanged(nameof(PasEnEdition));
        RafraichirFormulaires();
    }

    private async Task SupprimerAsync(Depense d)
    {
        await _repo.SupprimerAsync(d);
        _toutes.Remove(d);
        if (_enEdition == d) AnnulerEdition();
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
