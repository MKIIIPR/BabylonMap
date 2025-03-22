using AshesMapBib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;
using API.A0Need; // Für eventuelle Dateizugriffe

[ApiController]
[Route("api/nodepositions_db")]
public class NodePositions_DbController : ControllerBase
{
    private readonly SCContext _context;

    public NodePositions_DbController(SCContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // GET: api/resourcepositions_db
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Lade alle ResourcePositions inklusive der zugehörigen Resource
        var positions = await _context.NodePositions
            .Include(rp => rp.Node)
            .ToListAsync();
        return Ok(positions);
    }

    // GET: api/resourcepositions_db/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var position = await _context.NodePositions
            .Include(rp => rp.Node)
            .FirstOrDefaultAsync(rp => rp.Id == id);
        if (position == null)
            return NotFound($"ResourcePosition mit ID {id} wurde nicht gefunden.");
        return Ok(position);
    }

    // POST: api/resourcepositions_db
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NodePosition position)
    {
        if (position == null)
            return BadRequest("Ungültige Daten.");

        // Neue ID generieren
        position.Id = Guid.NewGuid().ToString();

        _context.NodePositions.Add(position);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = position.Id }, position);
    }

    // PUT: api/resourcepositions_db/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] NodePosition updatedPosition)
    {
        if (updatedPosition == null || id != updatedPosition.Id)
            return BadRequest("Ungültige Daten.");

        var existingPosition = await _context.NodePositions.FindAsync(id);
        if (existingPosition == null)
            return NotFound($"ResourcePosition mit ID {id} wurde nicht gefunden.");

        // Aktualisiere die Felder
        existingPosition.ResourceId = updatedPosition.ResourceId;
        existingPosition.Description = updatedPosition.Description;
        existingPosition.Lat = updatedPosition.Lat;
        existingPosition.Lng = updatedPosition.Lng;
        existingPosition.Rarity = updatedPosition.Rarity;
        existingPosition.Image = updatedPosition.Image;
        existingPosition.LastHarvest = updatedPosition.LastHarvest;
        existingPosition.TimerMod = updatedPosition.TimerMod;
        // RespawnAt wird automatisch berechnet

        _context.Entry(existingPosition).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/resourcepositions_db/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existingPosition = await _context.NodePositions.FindAsync(id);
        if (existingPosition == null)
            return NotFound($"ResourcePosition mit ID {id} wurde nicht gefunden.");

        _context.NodePositions.Remove(existingPosition);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
