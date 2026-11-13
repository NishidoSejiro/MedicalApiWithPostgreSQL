using MedicalApi.Data;
using MedicalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Свой модуль: запись на приём к врачу
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly MedicalDbContext _db;

    public AppointmentsController(MedicalDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<List<Appointment>> GetAll()
    {
        return await _db.Appointments.OrderBy(a => a.Date).ToListAsync();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Appointment>> GetById(int id)
    {
        Appointment? appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
        {
            return NotFound();
        }
        return appointment;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Appointment>> Create(Appointment appointment)
    {
        appointment.Id = 0;
        appointment.Date = ToUtc(appointment.Date);

        string? error = await Validate(appointment);
        if (error != null)
        {
            return BadRequest(error);
        }

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Appointment>> Update(int id, Appointment appointment)
    {
        Appointment? existing = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        // в теле запроса Id обычно 0, берём настоящий из адреса,
        // иначе запись сочтёт саму себя "занятым временем"
        appointment.Id = id;
        appointment.Date = ToUtc(appointment.Date);

        string? error = await Validate(appointment);
        if (error != null)
        {
            return BadRequest(error);
        }

        existing.PatientId = appointment.PatientId;
        existing.DoctorName = appointment.DoctorName;
        existing.Specialty = appointment.Specialty;
        existing.Date = appointment.Date;
        existing.Complaint = appointment.Complaint;
        existing.IsDone = appointment.IsDone;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        Appointment? appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
        {
            return NotFound();
        }

        _db.Appointments.Remove(appointment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PostgreSQL принимает время с часовым поясом только в UTC.
    // Если клиент прислал время без "Z", считаем, что это местное время.
    private static DateTime ToUtc(DateTime date)
    {
        if (date.Kind == DateTimeKind.Unspecified)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Local);
        }
        return date.ToUniversalTime();
    }

    private async Task<string?> Validate(Appointment appointment)
    {
        bool patientExists = await _db.Patients.AnyAsync(p => p.Id == appointment.PatientId);
        if (!patientExists)
        {
            return "Пациент с таким номером не найден. Сначала создайте пациента в картотеке";
        }
        if (string.IsNullOrWhiteSpace(appointment.DoctorName))
        {
            return "Укажите ФИО врача";
        }
        if (string.IsNullOrWhiteSpace(appointment.Specialty))
        {
            return "Укажите специальность врача, например: Терапевт";
        }
        if (string.IsNullOrWhiteSpace(appointment.Complaint))
        {
            return "Укажите причину обращения";
        }
        if (appointment.Date < DateTime.UtcNow && !appointment.IsDone)
        {
            return "Нельзя записать на прошедшее время. Выберите дату в будущем";
        }

        // часы приёма смотрим по местному времени клиники, а не по UTC
        DateTime local = appointment.Date.ToLocalTime();
        if (local.Hour < 8 || local.Hour >= 18)
        {
            return "Поликлиника принимает с 8:00 до 18:00. Выберите другое время";
        }

        // один врач не может принять двоих в одно время
        bool busy = await _db.Appointments.AnyAsync(a => a.Id != appointment.Id
            && a.DoctorName == appointment.DoctorName
            && a.Date == appointment.Date);
        if (busy)
        {
            return "У этого врача на это время уже есть запись. Выберите другое время";
        }
        return null;
    }
}
