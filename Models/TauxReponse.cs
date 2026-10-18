using System.Text.Json.Serialization;

namespace CarnetDepenses.Models;

public class TauxReponse
{
    [JsonPropertyName("base")]
    public string Base { get; set; } = "";
    // Nom exact de la cle dans le JSON recu.
    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Taux { get; set; } = new();
}
