namespace Attendance.Api.Modules.Reporting.Contracts;
public sealed record AttendanceReportRow(Guid EmployeeId,string EmployeeCode,string EmployeeName,DateOnly Date,string? Status,int? WorkedMinutes,int? LunchMinutes,IReadOnlyCollection<string> Anomalies,string? Failure);
public sealed record AttendanceReportSummary(int EmployeesIncluded,int DaysEvaluated,int Present,int UnexcusedAbsences,int Incomplete,int TotalWorkedMinutes);
public sealed record AttendanceReportResponse(DateOnly From,DateOnly To,Guid? EmployeeId,AttendanceReportSummary Summary,IReadOnlyCollection<AttendanceReportRow> Rows);
