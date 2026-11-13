using MedicalApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Статистика для главврача: GET /api/stats
[ApiController]
[Route("api/stats")]
public class StatisticsController : ControllerBase
{
    private readonly MedicalDbContext _db;

    public StatisticsController(MedicalDbContext db)
    {
        _db = db;
    }

    // CountAsync и SumAsync считает сама база (COUNT и SUM в SQL),
    // а не программа после загрузки всех строк
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        int patientsCount = await _db.Patients.CountAsync();
        int spravkiCount = await _db.Spravki.CountAsync();
        decimal spravkiTotalSum = await _db.Spravki.SumAsync(s => s.Price);
        int appointmentsCount = await _db.Appointments.CountAsync();
        int appointmentsDone = await _db.Appointments.CountAsync(a => a.IsDone);

        return Ok(new
        {
            patientsCount,
            spravkiCount,
            spravkiTotalSum,
            appointmentsCount,
            appointmentsDone
        });
    }
}
