using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using System.Text;
using Microsoft.Extensions.Logging;

public class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return name.ToLowerInvariant();
    }
}
public class ResourceApiClient<T> where T : class
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _JO;
    private readonly ILogger<ResourceApiClient<T>> _log;
    // Konstanten für den API-Key
    private const string ApiKeyHeaderName = "apikey";
    private const string ApiKeyValue = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImtlZXJpb29kanZ3am9vb3d2dXh2Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDIwNzM4MjIsImV4cCI6MjA1NzY0OTgyMn0.3iDHdvbkpxW9Oof8t6MHJngK_MCF4JxPOlppZbbJi_Y";
   
    public ResourceApiClient(HttpClient httpClient, ILogger<ResourceApiClient<T>> log)
    {
        _log = log;
        _httpClient = httpClient;
        _baseUrl = "https://keerioodjvwjooowvuxv.supabase.co/rest/v1";
        
        // Füge den API-Key-Header hinzu, falls er noch nicht vorhanden ist.
        if (!_httpClient.DefaultRequestHeaders.Contains(ApiKeyHeaderName))
        {
            _httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, ApiKeyValue);
        }
        _JO =new JsonSerializerOptions
        {
            PropertyNamingPolicy = new LowerCaseNamingPolicy(),
            WriteIndented = false
        };
    }

    // Alle Objekte abrufen (mit dynamischem Endpoint)
    public async Task<List<T>> GetAllAsync(string endpoint)
    {
        try
        {
            Console.WriteLine($"📡 Sende Anfrage an API: {_baseUrl}{endpoint}");

            // Sende die Anfrage
            var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");

            // Prüfe, ob der HTTP-Statuscode OK ist
            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Fehler: HTTP {response.StatusCode} - {errorDetails}");
                return new List<T>(); // Rückgabe einer leeren Liste im Fehlerfall
            }

            // Versuche, die Antwort in eine Liste von T zu deserialisieren
            var result = await response.Content.ReadFromJsonAsync<List<T>>();

            if (result == null)
            {
                Console.WriteLine("⚠️ Warnung: Leere oder ungültige Antwort empfangen.");
                return new List<T>(); // Rückgabe einer leeren Liste, wenn keine Daten empfangen wurden
            }

            Console.WriteLine($"✅ Erfolgreich {result.Count} Objekte erhalten.");
            return result; // Rückgabe der deserialisierten Liste
        }
        catch (HttpRequestException httpEx)
        {
            // Spezielle Fehlerbehandlung für HTTP-Fehler (z.B. Netzwerkprobleme, Timeouts)
            Console.WriteLine($"❌ HTTP-Fehler: {httpEx.Message}");
            return new List<T>(); // Rückgabe einer leeren Liste bei einem HTTP-Fehler
        }
        catch (TaskCanceledException timeoutEx)
        {
            // Fehlerbehandlung für Timeout-Fehler
            Console.WriteLine("⏳ Timeout-Fehler: Die Anfrage hat das Zeitlimit überschritten.");
            return new List<T>(); // Rückgabe einer leeren Liste im Timeout-Fall
        }
        catch (JsonException jsonEx)
        {
            // Fehlerbehandlung für Deserialisierungsfehler
            Console.WriteLine($"💥 JSON-Fehler: {jsonEx.Message}");
            return new List<T>(); // Rückgabe einer leeren Liste bei Deserialisierungsfehler
        }
        catch (Exception ex)
        {
            // Allgemeine Fehlerbehandlung für alle anderen Fehler
            Console.WriteLine($"💥 Unerwarteter Fehler: {ex.Message}");
            return new List<T>(); // Rückgabe einer leeren Liste bei anderen Fehlern
        }
    }
    // Ein Objekt abrufen
    public async Task<T?> GetByIdAsync(string id, string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/{endpoint}/{id}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Fehler: HTTP {response.StatusCode}");
                return null;
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Fehler: {ex.Message}");
            return null;
        }
    }

    // Neues Objekt erstellen
    public async Task<HttpResponseMessage> CreateAsync(T item, string endpoint)
    {
        try
        {
            Console.WriteLine($"📡 Sende POST-Anfrage an API: {_baseUrl}{endpoint}");

            // Konvertiere das Item in einen JSON-String
            var json = JsonSerializer.Serialize(item, _JO);
            Console.WriteLine($"JSON Payload: {json}");

            // Erstelle den Content mit dem JSON, Encoding und dem passenden Content-Type
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Sende die POST-Anfrage
            var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);

            // Überprüfe, ob die Antwort erfolgreich war
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Fehler: HTTP {response.StatusCode} - {errorContent}");
            }

            return response;
        }

        catch (HttpRequestException httpEx)
        {
            // Spezielle Fehlerbehandlung für HTTP-Fehler (z.B. Netzwerkprobleme, Zeitüberschreitungen)
            Console.WriteLine($"❌ HTTP-Fehler: {httpEx.Message}");
            return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError); // Rückgabe eines allgemeinen Fehlercodes
        }
        catch (TaskCanceledException timeoutEx)
        {
            // Fehlerbehandlung bei Timeout
            Console.WriteLine("⏳ Fehler: Anfrage überschreitet das Zeitlimit.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout); // Timeout-Fehler zurückgeben
        }
        catch (JsonException jsonEx)
        {
            // Fehlerbehandlung bei JSON-Serialisierungsproblemen
            Console.WriteLine($"💥 JSON-Fehler: {jsonEx.Message}");
            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest); // BadRequest für ungültige JSON-Daten
        }
        catch (Exception ex)
        {
            // Allgemeine Fehlerbehandlung
            Console.WriteLine($"💥 Unerwarteter Fehler: {ex.Message}");
            return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        }
    }

    // Objekt aktualisieren
    public async Task<HttpResponseMessage> UpdateAsync(string id, T item, string endpoint)
    {
        try
        {
            Console.WriteLine($"📡 Sende Put-Anfrage an API: {_baseUrl}{endpoint}");

            // Konvertiere das Item in einen JSON-String
            var json = JsonSerializer.Serialize(item, _JO);
            Console.WriteLine($"JSON Payload: {json}");

            // Erstelle den Content mit dem JSON, Encoding und dem passenden Content-Type
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Sende die POST-Anfrage
            var response = await _httpClient.PutAsync($"{_baseUrl}{endpoint}{id}", content);

            // Überprüfe, ob die Antwort erfolgreich war
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Fehler: HTTP {response.StatusCode} - {errorContent}");
            }

            return response;
        }

        catch (HttpRequestException httpEx)
        {
            // Spezielle Fehlerbehandlung für HTTP-Fehler (z.B. Netzwerkprobleme, Zeitüberschreitungen)
            Console.WriteLine($"❌ HTTP-Fehler: {httpEx.Message}");
            return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError); // Rückgabe eines allgemeinen Fehlercodes
        }
        catch (TaskCanceledException timeoutEx)
        {
            // Fehlerbehandlung bei Timeout
            Console.WriteLine("⏳ Fehler: Anfrage überschreitet das Zeitlimit.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout); // Timeout-Fehler zurückgeben
        }
        catch (JsonException jsonEx)
        {
            // Fehlerbehandlung bei JSON-Serialisierungsproblemen
            Console.WriteLine($"💥 JSON-Fehler: {jsonEx.Message}");
            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest); // BadRequest für ungültige JSON-Daten
        }
        catch (Exception ex)
        {
            // Allgemeine Fehlerbehandlung
            Console.WriteLine($"💥 Unerwarteter Fehler: {ex.Message}");
            return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        }
    }


    // Objekt löschen
    public async Task<HttpResponseMessage> DeleteAsync(string id, string endpoint)
    {
        try
        {
            Console.WriteLine($"📡 Sende DELETE-Anfrage an API: {_baseUrl}/{endpoint}{id}");

            var response = await _httpClient.DeleteAsync($"{_baseUrl}/{endpoint}{id}");

            // Überprüfen, ob die Antwort erfolgreich war
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Fehler: HTTP {response.StatusCode} - {errorContent}");
            }

            return response;
        }
        catch (HttpRequestException httpEx)
        {
            // Spezielle Fehlerbehandlung für HTTP-Fehler (z.B. Netzwerkprobleme, Zeitüberschreitungen)
            Console.WriteLine($"❌ HTTP-Fehler: {httpEx.Message}");
            return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        }
        catch (TaskCanceledException timeoutEx)
        {
            // Fehlerbehandlung bei Timeout
            Console.WriteLine("⏳ Fehler: Anfrage überschreitet das Zeitlimit.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            // Allgemeine Fehlerbehandlung
            Console.WriteLine($"💥 Unerwarteter Fehler: {ex.Message}");
            return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
        }
    }

}
