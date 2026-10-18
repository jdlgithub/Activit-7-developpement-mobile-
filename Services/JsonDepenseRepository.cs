using System.Text.Json;
using Microsoft.Maui.Storage;
using CarnetDepenses.Models;

namespace CarnetDepenses.Services;

public class JsonDepenseRepository : IDepenseRepository
{
    // Dossier prive de l'application : survit a la fermeture.
    private readonly string _chemin =
        Path.Combine(FileSystem.AppDataDirectory, "depenses.json");

    public async Task<List<Depense>> ListerAsync()
    {
        if (!File.Exists(_chemin)) return new();
        string json = await File.ReadAllTextAsync(_chemin);
        // Texte JSON -> objets C# (deserialisation).
        return JsonSerializer.Deserialize<List<Depense>>(json) ?? new();
    }

    private Task EcrireAsync(List<Depense> l) =>
        File.WriteAllTextAsync(_chemin, JsonSerializer.Serialize(l));

    public async Task AjouterAsync(Depense d)
    {
        var l = await ListerAsync();
        // Id manuel : plus grand id existant + 1
        d.Id = l.Count == 0 ? 1 : l.Max(x => x.Id) + 1;
        l.Add(d);
        await EcrireAsync(l);
    }

    public async Task SupprimerAsync(Depense d)
    {
        var l = await ListerAsync();
        l.RemoveAll(x => x.Id == d.Id);
        await EcrireAsync(l);
    }
}
