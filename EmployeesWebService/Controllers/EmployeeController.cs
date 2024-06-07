using System.Text;
using Dapper;
using EmployeesWebService.EmployeeAggregate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;

namespace EmployeesWebService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeeController : ControllerBase
{
    private readonly AppConfigOptions _appConfigOptions;

    public EmployeeController(IOptions<AppConfigOptions> options)
    {
        _appConfigOptions = options.Value;
    }

    [HttpGet("get-employees-by-company-id/{companyId}")]
    public async Task<List<Employee>> GetEmployeesByCompanyId(int companyId)
    {
        var sql = """
            SELECT employees.id, employees.name, employees.surname, employees.phone, employees.company_id, employees.department_id, employees.passport_id,
            	departments.id, departments.name, departments.phone,
            	passports.id, passports.type, passports.number
            	FROM employees
            INNER JOIN departments
            	ON employees.department_id = departments.id
            INNER JOIN passports
            	ON employees.passport_id = passports.id
            WHERE company_id = @companyId;
            """;
        using var connection = new NpgsqlConnection(_appConfigOptions.ConnectionString);
        var employees = (await connection.QueryAsync<Employee, Department, Passport, Employee>(sql,
            (employee, department, passport) =>
            {
                employee.Department = department;
                employee.Passport = passport;
                return employee;
            }, new { companyId })).ToList();
        return employees;
    }

    [HttpGet("get-employees-by-department-id/{departmentId}")]
    public async Task<List<Employee>> GetEmployeesByDepartmentId(int departmentId)
    {
        var sql = """
            SELECT employees.id, employees.name, employees.surname, employees.phone, employees.company_id, employees.department_id, employees.passport_id,
            	departments.id, departments.name, departments.phone,
            	passports.id, passports.type, passports.number
            	FROM employees
            INNER JOIN departments
            	ON employees.department_id = departments.id
            INNER JOIN passports
            	ON employees.passport_id = passports.id
            WHERE department_id = @departmentId;
            """;
        using var connection = new NpgsqlConnection(_appConfigOptions.ConnectionString);
        var employees = (await connection.QueryAsync<Employee, Department, Passport, Employee>(sql,
            (employee, department, passport) =>
            {
                employee.Department = department;
                employee.Passport = passport;
                return employee;
            }, new { departmentId })).ToList();
        return employees;
    }

    [HttpPost("create-employee/")]
    public async Task<int> CreateEmployeeAsync(Employee employee)
    {
        using var connection = new NpgsqlConnection(_appConfigOptions.ConnectionString);
        var sql = """
            INSERT INTO employees (name, surname, phone, company_id, department_id, passport_id)
            VALUES (@EmployeeName, @EmployeeSurname, @EmployeePhone, @CompanyId, @DepartmentId, @PassportId)
            RETURNING id;
            """;

        var result = (await connection.QueryAsync<int>(sql, new
        {
            EmployeeName = employee.Name,
            EmployeePhone = employee.Phone,
            EmployeeSurname = employee.Surname,
            employee.CompanyId,
            DepartmentId = employee.Department.Id,
            PassportId = employee.Passport.Id,
        })).FirstOrDefault();
        return result;
    }

    [HttpDelete("delete-employee/{employeeId}")]
    public async Task DeleteEmployeeAsync(int employeeId)
    {
        var sql = """
            DELETE FROM employees
            WHERE id = @employeeId;
            """;
        using var connection = new NpgsqlConnection(_appConfigOptions.ConnectionString);
        await connection.ExecuteAsync(sql, new { employeeId });
    }

    /// <summary>
    /// </summary>
    /// <param name="json">запрос на изменение</param>
    /// <remarks>
    /// запрос имеет вид {"Id": 2,"Name": "Новое имя", "Surname": "Новая фамилия" , "CompanyId": "999"}
    /// названия полей соответствующие им в базе данных
    /// </remarks>
    /// <returns></returns>
    [HttpPut("update-employee/")]
    public async Task UpdateEmployeeAsync(string json)
    {
        var sqlStringBuilder = new StringBuilder();
        sqlStringBuilder.AppendLine("UPDATE employees SET");

        var converted = json
            .Replace("{", "")
            .Replace("}", "")
            .Replace("\\", "")
            .Replace("\"", "")
            .Split(',');

        var convertedParametersDictionary = new Dictionary<string, object>();

        foreach (var item in converted)
        {
            var split = item.Split(":");
            var key = split[0].ToLower().Replace(" ", "");
            // значение может быть типом int, поэтому пытаемся спарсить
            if (int.TryParse(split[1], out var result))
            {
                convertedParametersDictionary.Add(key, result);
            }
            else
            {
                convertedParametersDictionary.Add(key, split[1]);
            }
            if (key.Equals("id", StringComparison.CurrentCultureIgnoreCase))
                continue;

            sqlStringBuilder.AppendLine($"{key.ToLower()} = @{key.ToLower()},");
        }
        // удаляем последнюю запятую
        sqlStringBuilder.Replace(",", "", sqlStringBuilder.Length - 5, 5);
        sqlStringBuilder.AppendLine("WHERE id = @id;");
        using var connection = new NpgsqlConnection(_appConfigOptions.ConnectionString);
        var parameters = new DynamicParameters(convertedParametersDictionary);
        var sql = sqlStringBuilder.ToString();
        await connection.ExecuteAsync(sql, parameters);
    }
}