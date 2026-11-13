namespace MedicalApi.Models;

public class Spravka
{
    public int Id { get; set; }
    public string Name { get; set; } = "";      // название на момент выписки (берётся из каталога)
    public int PatientId { get; set; }
    public int SpravkaTypeId { get; set; }
    public decimal Price { get; set; }          // цена на момент выписки (берётся из каталога)
    public DateOnly IssuedAt { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
