using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApiServer.Controllers.ComunityController
{
    [Route("api/[controller]")]
    [ApiController]
    public class AocMapController : ControllerBase
    {

        private readonly ILogger<AocMapController> _logger;
        private readonly HttpClient _httpClient;

        public AocMapController( ILogger<AocMapController> logger,HttpClient httpClient)
        {
           _httpClient = httpClient;
            _logger = logger;
        }

        // GET: api/Comunities
        [HttpGet]

        public async Task<ActionResult> DownloadTiles(int zoomLevel)
        {
            var baseUrl = "https://cdn.ashescodex.com/map/20250227";
                         
            MapTileSrc mapTileSrc = new MapTileSrc();
            mapTileSrc =await  GetAOCMap();
            var targetDirectory = Path.Combine("E:\\TileDownloads"+mapTileSrc.TilePath);
            // Zielordner für den jeweiligen Zoom-Level erstellen, falls er nicht existiert
            

            bool foundAnyTile = false;
            foreach (var tile in mapTileSrc.Tiles)
            {
                var zoomLevelDirectory = Path.Combine(targetDirectory, tile.Z.ToString());
            if (!Directory.Exists(zoomLevelDirectory))
            {
                Directory.CreateDirectory(zoomLevelDirectory);
            }
                for (int x = 0; x <= tile.X; x++) // Maximale X-Koordinate (oder nach Bedarf anpassen)
                {
                    for (int y = 1; y <= tile.Y; y++) // Maximale Y-Koordinate (oder nach Bedarf anpassen)
                    {
                        var tileUrl = $"{baseUrl}/{tile.Z}/{x}/{y}.webp";
                        
                        https://cdn.ashescodex.com/map/20250227/8/93/145.webp

                        // Erstelle das Verzeichnis für die Kachel, falls es nicht existiert
                        var tileDirectory = Path.Combine(zoomLevelDirectory, x.ToString());
                        if (!Directory.Exists(tileDirectory))
                        {
                            Directory.CreateDirectory(tileDirectory);
                        }

                        // Speicherpfad der Kachel
                        var filePath = Path.Combine(tileDirectory, $"{y}.webp");

                        try
                        {
                            var response = await _httpClient.GetAsync(tileUrl);

                            // Wenn die Kachel existiert (Statuscode 200), dann herunterladen
                            if (response.IsSuccessStatusCode)
                            {
                                // Kachel herunterladen und speichern
                                var tileData = await response.Content.ReadAsByteArrayAsync();
                                await System.IO.File.WriteAllBytesAsync(filePath, tileData);
                                foundAnyTile = true;
                                Console.WriteLine($"Kachel {zoomLevel}/{x}/{y} erfolgreich heruntergeladen.");
                            }
                            else
                            {
                                // Fehlerbehandlung für Kacheln, die nicht existieren (z. B. 404)
                                Console.WriteLine($"Kachel {zoomLevel}/{x}/{y} nicht gefunden (Statuscode {response.StatusCode}).");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Fehlerbehandlung bei anderen Problemen (z. B. Netzwerkfehler)
                            Console.WriteLine($"Fehler beim Herunterladen der Kachel {zoomLevel}/{x}/{y}: {ex.Message}");
                        }
                    }

                    // Falls keine Kachel in dieser Reihe (X) für den angegebenen Zoom-Level heruntergeladen wurde
                    if (!foundAnyTile)
                    {
                        Console.WriteLine($"Keine Kacheln gefunden für Zoom-Level {zoomLevel}, X {x}. Wechsele zu höherem Zoom-Level oder breche ab.");
                        break; // Wir brechen die Schleife ab, wenn keine Kacheln für diesen X-Wert vorhanden sind.
                    }
                }
            }
            // Schleife durch X und Y-Koordinaten für das angegebene Zoom-Level
            

            return Ok($"Tiles für Zoom-Level {zoomLevel} erfolgreich heruntergeladen.");
        }


        [HttpGet]
        [Route("/ZoomLvl")]

        public async Task<ActionResult> DLAocTilesPerZoomLvl(int zoomLevel)
        {
            var baseUrl = "https://cdn.ashescodex.com/map/20250227";

            MapTileSrc mapTileSrc = new MapTileSrc();
            mapTileSrc = await GetAOCMap();
            var targetDirectory = Path.Combine("E:\\TileDownloads" + mapTileSrc.TilePath);
            // Zielordner für den jeweiligen Zoom-Level erstellen, falls er nicht existiert


            bool foundAnyTile = false;

            foreach (var tile in mapTileSrc.Tiles.Where(e => e.Z == zoomLevel))
            {
                var zoomLevelDirectory = Path.Combine(targetDirectory, zoomLevel.ToString());
                if (!Directory.Exists(zoomLevelDirectory))
                {
                    Directory.CreateDirectory(zoomLevelDirectory);
                }
                for (int x = 0; x <= tile.X; x++) // Maximale X-Koordinate (oder nach Bedarf anpassen)
                {
                    for (int y = 1; y <= tile.Y; y++) // Maximale Y-Koordinate (oder nach Bedarf anpassen)
                    {
                        var tileUrl = $"{baseUrl}/{tile.Z}/{x}/{y}.webp";

                    https://cdn.ashescodex.com/map/20250227/8/93/145.webp

                        // Erstelle das Verzeichnis für die Kachel, falls es nicht existiert
                        var tileDirectory = Path.Combine(zoomLevelDirectory, x.ToString());
                        if (!Directory.Exists(tileDirectory))
                        {
                            Directory.CreateDirectory(tileDirectory);
                        }

                        // Speicherpfad der Kachel
                        var filePath = Path.Combine(tileDirectory, $"{y}.webp");

                        try
                        {
                            var response = await _httpClient.GetAsync(tileUrl);

                            // Wenn die Kachel existiert (Statuscode 200), dann herunterladen
                            if (response.IsSuccessStatusCode)
                            {
                                // Kachel herunterladen und speichern
                                var tileData = await response.Content.ReadAsByteArrayAsync();
                                await System.IO.File.WriteAllBytesAsync(filePath, tileData);
                                foundAnyTile = true;
                                Console.WriteLine($"Kachel {zoomLevel}/{x}/{y} erfolgreich heruntergeladen.");
                            }
                            else
                            {
                                // Fehlerbehandlung für Kacheln, die nicht existieren (z. B. 404)
                                Console.WriteLine($"Kachel {zoomLevel}/{x}/{y} nicht gefunden (Statuscode {response.StatusCode}).");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Fehlerbehandlung bei anderen Problemen (z. B. Netzwerkfehler)
                            Console.WriteLine($"Fehler beim Herunterladen der Kachel {zoomLevel}/{x}/{y}: {ex.Message}");
                        }
                    }

                    // Falls keine Kachel in dieser Reihe (X) für den angegebenen Zoom-Level heruntergeladen wurde
                    if (!foundAnyTile)
                    {
                        Console.WriteLine($"Keine Kacheln gefunden für Zoom-Level {zoomLevel}, X {x}. Wechsele zu höherem Zoom-Level oder breche ab.");
                        break; // Wir brechen die Schleife ab, wenn keine Kacheln für diesen X-Wert vorhanden sind.
                    }
                }
            }
            // Schleife durch X und Y-Koordinaten für das angegebene Zoom-Level


            return Ok($"Tiles für Zoom-Level {zoomLevel} erfolgreich heruntergeladen.");
        }
        public class MapTileSrc
        {
            public string Name { get; set; }
            public string TilePath { get; set; }
            public List<TileSet> Tiles { get; set; }

        }
        public class TileSet
        {
            public int Z { get; set; }
            public int X { get; set; }
            public int Y { get; set; }

        }
        private async Task<MapTileSrc> GetAOCMap()
        {
            MapTileSrc _result = new();
            _result.Tiles = new List<TileSet>();
            _result.Tiles.Add(new TileSet { Z = 2,  X = 5 ,Y = 5});
            _result.Tiles.Add(new TileSet { Z = 3,  X = 10 ,Y = 10});
            _result.Tiles.Add(new TileSet { Z = 4,  X = 20 ,Y = 20});
            _result.Tiles.Add(new TileSet { Z = 5,  X = 40 ,Y = 40});
            _result.Tiles.Add(new TileSet { Z = 6,  X = 60 ,Y = 60});
            _result.Tiles.Add(new TileSet { Z = 7,  X = 80 ,Y = 80});
            _result.Tiles.Add(new TileSet { Z = 8,  X = 130 ,Y = 130});

            _result.Name = "AOC";
            _result.TilePath = "/AOC/tile/";

            return _result;
        }
    }
    
    }
