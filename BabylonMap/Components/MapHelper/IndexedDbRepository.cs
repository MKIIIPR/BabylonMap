using Microsoft.JSInterop;
using System.Text.Json;

public class IndexedDBRepository
{
    private readonly IJSRuntime jsRuntime;

    public IndexedDBRepository(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    // Speichern von Daten in einem spezifischen Store
    public async Task SaveDataAsync<T>(string storeName, T data, string key)
    {
        var jsonData = JsonSerializer.Serialize(data);
        await jsRuntime.InvokeVoidAsync("indexedDBRepository.saveData", storeName, jsonData, key);
    }

    // Laden von Daten aus einem spezifischen Store
    public async Task<T> LoadDataAsync<T>(string storeName, string key)
    {
        var jsonData = await jsRuntime.InvokeAsync<string>("indexedDBRepository.loadData", storeName, key);
        return jsonData != null ? JsonSerializer.Deserialize<T>(jsonData) : default;
    }

    // Laden aller Daten eines bestimmten Stores
    public async Task<List<T>> LoadAllDataAsync<T>(string storeName)
    {
        var jsonData = await jsRuntime.InvokeAsync<string>("indexedDBRepository.loadAllData", storeName);
        return jsonData != null ? JsonSerializer.Deserialize<List<T>>(jsonData) : new List<T>();
    }

    // Löschen eines Datensatzes aus einem Store
    public async Task DeleteDataAsync(string storeName, string key)
    {
        await jsRuntime.InvokeVoidAsync("indexedDBRepository.deleteData", storeName, key);
    }

    // Clear a specific store (for archiving or removing unused data)
    public async Task ClearStoreAsync(string storeName)
    {
        await jsRuntime.InvokeVoidAsync("indexedDBRepository.clearStore", storeName);
    }
}
