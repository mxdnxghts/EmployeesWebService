namespace EmployeesWebService.EmployeeAggregate;

public sealed class Passport
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
}