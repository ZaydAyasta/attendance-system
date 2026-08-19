namespace Attendance.Api.Modules.Employees.Domain;

/// <summary>
/// Represents an employee whose attendance is managed by the system.
/// </summary>
public sealed class Employee
{
    /// <summary>
    /// Gets the unique identifier of the employee.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the business identifier assigned to the employee.
    /// </summary>
    public string EmployeeCode { get; private set; } = null!;

    /// <summary>
    /// Gets the employee's first name.
    /// </summary>
    public string FirstName { get; private set; } = null!;

    /// <summary>
    /// Gets the employee's last name.
    /// </summary>
    public string LastName { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the employee is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the date on which the employee joined the company.
    /// </summary>
    public DateOnly HireDate { get; private set; }

    /// <summary>
    /// Gets the employee's termination date, or <see langword="null"/>
    /// if the employee has not been terminated.
    /// </summary>
    public DateOnly? TerminationDate { get; private set; }
}