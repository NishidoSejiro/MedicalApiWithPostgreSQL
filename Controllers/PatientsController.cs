using MedicalApi.Data;
using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly MedicalDbContext _db;

    public PatientsController(MedicalDbContext db)
    {
        _db = db;
    }

    // GET /api/patients               все пациенты по алфавиту
    // GET /api/patients?name=ай       поиск по части имени, регистр не важен
    [HttpGet]
    public async Task<List<Patient>> GetAll(string? name)
    {
        IQueryable<Patient> query = _db.Patients;
        if (name != null)
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{name}%"));
        }
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Patient>> GetById(int id)
    {
        Patient? patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }
        return patient;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Patient>> Create(Patient patient)
    {
        // номер выдаёт база, а не клиент: Id из тела запроса сбрасываем
        patient.Id = 0;

        string? error = Validate(patient);
        if (error != null)
        {
            return BadRequest(error);
        }

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Patient>> Update(int id, Patient patient)
    {
        Patient? existing = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        string? error = Validate(patient);
        if (error != null)
        {
            return BadRequest(error);
        }

        existing.Name = patient.Name;
        existing.BirthDate = patient.BirthDate;
        existing.Phone = patient.Phone;
        existing.Address = patient.Address;
        existing.DistrictNumber = patient.DistrictNumber;
        existing.HasInsurance = patient.HasInsurance;

        existing.Email = patient.Email;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    // Пациента со справками или приёмами удалять нельзя: это медицинские записи.
    // Без этой проверки база всё равно откажет (Restrict), но клиент получил бы 500.
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        Patient? patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }

        bool hasSpravki = await _db.Spravki.AnyAsync(s => s.PatientId == id);
        if (hasSpravki)
        {
            return BadRequest("Нельзя удалить пациента: на него выписаны справки. Справки это медицинские документы, они не удаляются вместе с пациентом");
        }

        bool hasAppointments = await _db.Appointments.AnyAsync(a => a.PatientId == id);
        if (hasAppointments)
        {
            return BadRequest("Нельзя удалить пациента: у него есть записи на приём. Сначала удалите или отмените их");
        }

        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/spravki")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<Spravka>>> GetSpravki(int id)
    {
        bool exists = await _db.Patients.AnyAsync(p => p.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        return await _db.Spravki.Where(s => s.PatientId == id).ToListAsync();
    }

    [HttpGet("{id}/appointments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<Appointment>>> GetAppointments(int id)
    {
        bool exists = await _db.Patients.AnyAsync(p => p.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        return await _db.Appointments.Where(a => a.PatientId == id).ToListAsync();
    }

    // null = данные верные, иначе текст ошибки (его покажет фронтенд)
    private string? Validate(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.Name))
        {
            return "Укажите ФИО пациента";
        }
        if (patient.Name.Length > 100)
        {
            return "ФИО слишком длинное: максимум 100 символов";
        }
        if (patient.BirthDate > DateOnly.FromDateTime(DateTime.Now))
        {
            return "Дата рождения не может быть в будущем";
        }
        if (patient.BirthDate < new DateOnly(1900, 1, 1))
        {
            return "Дата рождения слишком старая";
        }
        if (string.IsNullOrWhiteSpace(patient.Phone) || patient.Phone.Length < 7)
        {
            return "Укажите телефон, минимум 7 символов";
        }
        if (patient.Phone.Length > 20)
        {
            return "Телефон слишком длинный: максимум 20 символов";
        }
        if (patient.DistrictNumber <= 0)
        {
            return "Номер участка должен быть больше 0";
        }
        return null;
    }
}
