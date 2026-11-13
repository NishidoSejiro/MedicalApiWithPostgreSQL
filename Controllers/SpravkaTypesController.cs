using MedicalApi.Data;
using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class SpravkaTypesController : ControllerBase
{
    private readonly MedicalDbContext _db;

    public SpravkaTypesController(MedicalDbContext db)
    {
        _db = db;
    }

    // каталог лежит в базе, заполняется через SQL (sql/seed-medical.sql) или через POST
    [HttpGet]
    public async Task<List<SpravkaType>> GetAll()
    {
        return await _db.SpravkaTypes.OrderBy(t => t.Id).ToListAsync();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpravkaType>> GetById(int id)
    {
        SpravkaType? type = await _db.SpravkaTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (type == null)
        {
            return NotFound();
        }
        return type;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SpravkaType>> Create(SpravkaType type)
    {
        type.Id = 0;

        string? error = Validate(type);
        if (error != null)
        {
            return BadRequest(error);
        }

        _db.SpravkaTypes.Add(type);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpravkaType>> Update(int id, SpravkaType type)
    {
        SpravkaType? existing = await _db.SpravkaTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        string? error = Validate(type);
        if (error != null)
        {
            return BadRequest(error);
        }

        // меняется только каталог: уже выданные справки хранят свою цену
        existing.Name = type.Name;
        existing.Price = type.Price;
        existing.ValidDays = type.ValidDays;
        existing.NeedsDoctorExam = type.NeedsDoctorExam;
        existing.ForChildren = type.ForChildren;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        SpravkaType? type = await _db.SpravkaTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (type == null)
        {
            return NotFound();
        }

        bool used = await _db.Spravki.AnyAsync(s => s.SpravkaTypeId == id);
        if (used)
        {
            return BadRequest("Нельзя удалить этот вид справки: по нему уже выписаны справки");
        }

        _db.SpravkaTypes.Remove(type);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private string? Validate(SpravkaType type)
    {
        if (string.IsNullOrWhiteSpace(type.Name))
        {
            return "Укажите название вида справки";
        }
        if (type.Name.Length > 100)
        {
            return "Название слишком длинное: максимум 100 символов";
        }
        if (type.Price <= 0)
        {
            return "Цена должна быть больше 0";
        }
        if (type.Price >= 100000000)
        {
            return "Цена слишком большая";
        }
        if (type.ValidDays <= 0)
        {
            return "Срок действия должен быть хотя бы 1 день";
        }
        return null;
    }
}
