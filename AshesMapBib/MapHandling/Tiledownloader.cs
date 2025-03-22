using Microsoft.JSInterop;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using AshesMapBib.Models;

namespace AshesMapBib.MapHandling
{
    public class TileDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        public TileDownloader(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task DownloadTilesAsZip()
        {
            var baseUrl = "http://heroes.s6labs.com/images/tiles";
            MapTileSrc mapTileSrc = await GetAOCMap();

            var files = new List<object>();

            foreach (var tile in mapTileSrc.Tiles)
            {
                for (int x = 0; x <= tile.X; x++)
                {
                    for (int y = 0; y <= tile.Y; y++)
                    {
                        var tileUrl = $"{baseUrl}/{tile.Z}/{x}/{y}.png";
                        try
                        {
                            try
                            {
                                var response = await _httpClient.GetAsync(tileUrl);
                                if (response.IsSuccessStatusCode)
                                {
                                    var tileData = await response.Content.ReadAsByteArrayAsync();
                                    string folderPath = $"{tile.Z}/{x}";
                                    string fileName = $"{y}.png";

                                    files.Add(new
                                    {
                                        folder = folderPath,
                                        name = fileName,
                                        content = Convert.ToBase64String(tileData)
                                    });
                                }
                                else
                                {
                                    Console.WriteLine($"HTTP-Fehler: {response.StatusCode} - {response.ReasonPhrase}");
                                }
                            }
                            catch (HttpRequestException httpEx)
                            {
                                Console.WriteLine($"HTTP-Fehler bei {tileUrl}: {httpEx}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Allgemeiner Fehler: {ex}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Fehler beim Herunterladen {tileUrl}: {ex.ToString()}");
                        }
                    }
                }
            }

            string json = JsonSerializer.Serialize(files);
            await _jsRuntime.InvokeVoidAsync("downloadTilesAsZip", json);
        }

        private async Task<MapTileSrc> GetAOCMap()
        {
            return new MapTileSrc
            {
                Name = "AOC",
                TilePath = "/AOC/tile/",
                Tiles = new List<TileSet>
                {
                    new() { Z = 2, Y = 5, X = 5 },
                    new() { Z = 3, Y = 10, X = 10 },
                    new() { Z = 4, Y = 20, X = 20 },
                    new() { Z = 5, Y = 40, X = 40 },
                    new() { Z = 6, Y = 60, X = 60 },
                    new() { Z = 7, Y = 80, X = 80 },
                    new() { Z = 8, Y = 130, X = 130 }
                }
            };
        }
    }
}
