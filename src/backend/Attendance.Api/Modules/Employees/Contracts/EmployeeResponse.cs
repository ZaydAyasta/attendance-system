namespace Attendance.Api.Modules.Employees.Contracts;
public sealed record EmployeeResponse(Guid Id,string EmployeeCode,string FirstName,string LastName,bool IsActive,DateOnly HireDate,DateOnly? TerminationDate,uint Version);
public sealed record CreateEmployeeRequest(string EmployeeCode,string FirstName,string LastName,DateOnly HireDate);
public sealed record UpdateEmployeeRequest(string EmployeeCode,string FirstName,string LastName,DateOnly HireDate,uint Version);
public sealed record SetEmployeeStatusRequest(bool IsActive,DateOnly? TerminationDate,uint Version);
