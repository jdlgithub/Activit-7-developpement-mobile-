using System.Net.Http.Json;
using CarnetDepenses.Models;

namespace CarnetDepenses.Services;

public interface IApiService
{
    Task<TauxReponse> GetTauxAsync();
}

public class ApiService : IApiService
{
    // HttpClient fourni par l'injection de dependances.
    private readonly HttpClient _http;
    public ApiService(HttpClient http) => _http = http;

    // GET + controle du code + deserialisation.
    public async Task<TauxReponse> GetTauxAsync() =>
        await _http.GetFromJsonAsync<TauxReponse>(
            "https://api.exemple.com/taux")
        ?? throw new InvalidOperationException("Reponse vide");
}
