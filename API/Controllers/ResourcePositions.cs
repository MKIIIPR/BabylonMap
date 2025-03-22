using AshesMapBib.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using API.A0Need; // Für Dateizugriffe

[ApiController]
[Route("api/resource_positionss")]
public class ResourcePositionsController : ControllerBase
{
    private readonly string _jsonFilePath;
    private readonly IWebHostEnvironment _env;
    private readonly SCContext _context; // Dein DbContext

    public ResourcePositionsController(IWebHostEnvironment env, SCContext context)
    {
        if (env == null)
            throw new ArgumentNullException(nameof(env), "IWebHostEnvironment wurde nicht korrekt injiziert!");
        if (context == null)
            throw new ArgumentNullException(nameof(context), "SCContext wurde nicht korrekt injiziert!");

        _env = env;
        _context = context;
        _jsonFilePath = Path.Combine(_env.WebRootPath, "textdb", "resources.json");
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var resources = await LoadAsync(_jsonFilePath);
        return Ok(resources);
    }

    [HttpPost]
    public async Task<IActionResult> Create(NodePosition resource)
    {
        if (resource == null) return BadRequest("Resource darf nicht null sein.");

        var resources = await LoadAsync(_jsonFilePath);
        resource.Id = Guid.NewGuid().ToString();
        resources.Add(resource);

        await SaveAsync(resources, _jsonFilePath);
        return CreatedAtAction(nameof(GetAll), new { id = resource.Id }, resource);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, NodePosition updatedResource)
    {
        if (updatedResource == null) return BadRequest("Ungültige Resource-Daten.");

        var resources = await LoadAsync(_jsonFilePath);
        var resourceIndex = resources.FindIndex(r => r.Id == id);

        if (resourceIndex == -1) return NotFound($"Resource mit ID {id} nicht gefunden.");

        updatedResource.Id = id; // Behalte die ID bei
        resources[resourceIndex] = updatedResource;

        await SaveAsync(resources, _jsonFilePath);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var resources = await LoadAsync(_jsonFilePath);
        var resource = resources.FirstOrDefault(r => r.Id == id);
        if (resource == null) return NotFound($"Resource mit ID {id} nicht gefunden.");

        resources.Remove(resource);
        await SaveAsync(resources, _jsonFilePath);
        return NoContent();
    }



    private async Task<List<NodePosition>> LoadAsync(string filePath)
    {
        // Überprüfen, ob das Verzeichnis existiert und es bei Bedarf erstellen
        var directoryPath = System.IO.Path.GetDirectoryName(filePath);
        if (!System.IO.Directory.Exists(directoryPath))
        {
            System.IO.Directory.CreateDirectory(directoryPath);
        }

        if (!System.IO.File.Exists(filePath))
            return new List<NodePosition>();

        var json = await System.IO.File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<NodePosition>>(json) ?? new List<NodePosition>();
    }

    private async Task SaveAsync(List<NodePosition> resources, string filePath)
    {
        // Überprüfen, ob das Verzeichnis existiert und es bei Bedarf erstellen
        var directoryPath = System.IO.Path.GetDirectoryName(filePath);
        if (!System.IO.Directory.Exists(directoryPath))
        {
            System.IO.Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(resources, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(filePath, json);
    }
}
