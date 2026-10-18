namespace CarnetDepenses.Models;

public class Depense
{
    public int Id { get; set; }
    public string Libelle { get; set; } = "";
    public decimal Montant { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
}
