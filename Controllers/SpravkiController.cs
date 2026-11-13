using MedicalApi.Data;
using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class SpravkiController : ControllerBase
{
    private readonly MedicalDbContext _db;

    public SpravkiController(MedicalDbContext db)
    {
        _db = db;
    }

    // GET /api/spravki                                   все справки
    // GET /api/spravki?patientId=1                       справки одного пациента
    // GET /api/spravki?minPrice=200                      не дешевле 200
    // GET /api/spravki?sort=price  или  ?sort=date       сортировка (date: новые сверху)
    // Фильтры можно комбинировать: ?patientId=1&minPrice=200&sort=price
    [HttpGet]
    public async Task<List<Spravka>> GetAll(int? patientId, decimal? minPrice, string? sort)
    {
        // пока не вызван ToListAsync, это только описание запроса, в базу он не уходит
        IQueryable<Spravka> query = _db.Spravki;

        if (patientId != null)
        {
            query = query.Where(s => s.PatientId == patientId);
        }
        if (minPrice != null)
        {
            query = query.Where(s => s.Price >= minPrice);
        }

        if (sort == "price")
        {
            query = query.OrderBy(s => s.Price);
        }
        else if (sort == "date")
        {
            query = query.OrderByDescending(s => s.IssuedAt);
        }

        return await query.ToListAsync();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Spravka>> GetById(int id)
    {
        Spravka? spravka = await _db.Spravki.FirstOrDefaultAsync(s => s.Id == id);
        if (spravka == null)
        {
            return NotFound();
        }
        return spravka;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Spravka>> Create(Spravka spravka)
    {
        // номер выдаёт база, Id из тела запроса не используем
        spravka.Id = 0;

        string? error = await CheckAndFill(spravka);
        if (error != null)
        {
            return BadRequest(error);
        }

        spravka.IssuedAt = DateOnly.FromDateTime(DateTime.Now);
        _db.Spravki.Add(spravka);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = spravka.Id }, spravka);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Spravka>> Update(int id, Spravka spravka)
    {
        Spravka? existing = await _db.Spravki.FirstOrDefaultAsync(s => s.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        string? error = await CheckAndFill(spravka);
        if (error != null)
        {
            return BadRequest(error);
        }

        existing.PatientId = spravka.PatientId;
        existing.SpravkaTypeId = spravka.SpravkaTypeId;
        existing.Name = spravka.Name;
        existing.Price = spravka.Price;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        Spravka? spravka = await _db.Spravki.FirstOrDefaultAsync(s => s.Id == id);
        if (spravka == null)
        {
            return NotFound();
        }

        _db.Spravki.Remove(spravka);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Проверяет пациента и вид справки, и берёт название и цену из каталога.
    // Возвращает текст ошибки или null, если всё хорошо.
    private async Task<string?> CheckAndFill(Spravka spravka)
    {
        Patient? patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == spravka.PatientId);
        if (patient == null)
        {
            return "Пациент с таким номером не найден. Сначала создайте пациента в картотеке";
        }

        SpravkaType? type = await _db.SpravkaTypes.FirstOrDefaultAsync(t => t.Id == spravka.SpravkaTypeId);
        if (type == null)
        {
            return "Такого вида справки нет в каталоге. Посмотрите список в /api/SpravkaTypes";
        }

        // справка для детей только детям (до 18 лет)
        int age = DateTime.Now.Year - patient.BirthDate.Year;
        if (type.ForChildren && age >= 18)
        {
            return "Эта справка только для детей, а пациенту уже есть 18 лет. Выберите другой вид справки";
        }

        spravka.Name = type.Name;
        spravka.Price = type.Price;
        return null;
    }
}
