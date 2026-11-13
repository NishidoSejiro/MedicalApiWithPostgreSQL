namespace MedicalApi.Models;

public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateOnly BirthDate { get; set; }     // дата без времени, в базе это столбец date
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public int DistrictNumber { get; set; }     // номер участка поликлиники
    public bool HasInsurance { get; set; }
    public string? Email { get; set; }
}
