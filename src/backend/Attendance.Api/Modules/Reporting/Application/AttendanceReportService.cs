using Attendance.Api.Modules.Attendance.Application;
using Attendance.Api.Modules.Attendance.Contracts;
using Attendance.Api.Modules.Attendance.Domain;
using Attendance.Api.Modules.Absences.Domain;
using Attendance.Api.Modules.Employees.Domain;
using Attendance.Api.Modules.WorkAssignments.Domain;
using Attendance.Api.Modules.WorkCalendar.Domain;
using Attendance.Api.Modules.Reporting.Contracts;
using Attendance.Api.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace Attendance.Api.Modules.Reporting.Application;
public sealed class AttendanceReportService(AttendanceDbContext db, AttendanceEvaluator evaluator, AttendanceTimeCalculator timeCalculator, AttendanceTimeZone attendanceTimeZone)
{
 private const string NakamaLogoResourceName = "Attendance.Api.Assets.NakamaLogo.png";

 public async Task<AttendanceReportResponse> GetAsync(DateOnly from,DateOnly to,Guid? employeeId,CancellationToken ct){
  var employees=await db.Employees.AsNoTracking().Where(x=>employeeId.HasValue?x.Id==employeeId.Value:x.IsActive).OrderBy(x=>x.LastName).ThenBy(x=>x.FirstName).ToListAsync(ct);
  var employeeIds=employees.Select(x=>x.Id).ToArray();
  var calendars=await db.WorkCalendarDays.AsNoTracking().Where(x=>x.Date>=from&&x.Date<=to).ToDictionaryAsync(x=>x.Date,ct);
  var absences=(await db.Absences.AsNoTracking().Where(x=>employeeIds.Contains(x.EmployeeId)&&x.Status==AbsenceStatus.Active).ToListAsync(ct)).Where(x=>x.Period.Start<=to&&x.Period.End>=from).ToArray();
  var assignments=await db.EmployeeWorkAssignments.AsNoTracking().Where(x=>employeeIds.Contains(x.EmployeeId)&&x.Status==WorkAssignmentStatus.Active&&x.Date>=from&&x.Date<=to).ToListAsync(ct);
  var rangeStart=attendanceTimeZone.GetStartOfDay(from);var rangeEnd=attendanceTimeZone.GetStartOfNextDay(to);
  var marks=(await db.AttendanceMarks.AsNoTracking().Where(x=>employeeIds.Contains(x.EmployeeId)).ToListAsync(ct)).Where(x=>x.OccurredAt>=rangeStart&&x.OccurredAt<rangeEnd).ToArray();
  var absenceByEmployee=absences.GroupBy(x=>x.EmployeeId).ToDictionary(x=>x.Key,x=>x.ToArray());
  var assignmentByEmployeeDate=assignments.ToDictionary(x=>(x.EmployeeId,x.Date));
  var marksByEmployeeDate=marks.GroupBy(x=>(x.EmployeeId,attendanceTimeZone.GetLocalDate(x.OccurredAt))).ToDictionary(x=>x.Key,x=>(IReadOnlyCollection<AttendanceMark>)x.OrderBy(m=>m.OccurredAt).ToArray());
  var rows=new List<AttendanceReportRow>(); foreach(var e in employees)for(var date=from;date<=to;date=date.AddDays(1)){calendars.TryGetValue(date,out var calendar);assignmentByEmployeeDate.TryGetValue((e.Id,date),out var assignment);marksByEmployeeDate.TryGetValue((e.Id,date),out var dayMarks);var dayAbsences=(absenceByEmployee.TryGetValue(e.Id,out var employeeAbsences)?employeeAbsences.Where(a=>a.Period.Contains(date)).ToArray():[]);var effective=AttendanceDayTypeResolver.Resolve(calendar,assignment);var result=evaluator.Evaluate(new AttendanceEvaluationContext(e,date,calendar,effective,null,dayMarks??[]));if(result.Status!=AttendanceStatus.NotApplicable&&result.Failure!=AttendanceEvaluationFailure.MissingWorkCalendarDay&&dayAbsences.Length>0)result=dayAbsences.Length>1?DailyAttendanceResult.FailureResult(e.Id,date,AttendanceEvaluationFailure.MultipleActiveAbsences):evaluator.Evaluate(new AttendanceEvaluationContext(e,date,calendar,effective,dayAbsences[0],dayMarks??[]));var time=timeCalculator.Calculate(dayMarks??[]);var noMarks=time.Issues.Count==1&&time.Issues.Contains(AttendanceTimeIssue.NoAttendanceMarks);rows.Add(new(e.Id,e.EmployeeCode,$"{e.FirstName} {e.LastName}",date,result.Status?.ToString(),noMarks?null:time.WorkedMinutes,noMarks?null:time.LunchMinutes,result.Anomalies.Where(a=>a!=AttendanceAnomaly.None).Select(a=>a.ToString()).ToArray(),result.Failure?.ToString()));}
  var summary=new AttendanceReportSummary(employees.Count,rows.Count,rows.Count(x=>x.Status=="Present"),rows.Count(x=>x.Status=="UnexcusedAbsence"),rows.Count(x=>x.Status=="Incomplete"),rows.Sum(x=>x.WorkedMinutes??0)); return new(from,to,employeeId,summary,rows);
 }
 public byte[] Excel(AttendanceReportResponse r)
 {
  using var wb = new XLWorkbook();
  var ws = wb.Worksheets.Add("Asistencia");
  StyleExcelHeader(ws, r);

  var headers = new[] { "Código", "Empleado", "Fecha", "Estado", "Tiempo trabajado", "Almuerzo", "Anomalías" };
  for (var index = 0; index < headers.Length; index++) ws.Cell(5, index + 1).Value = headers[index];

  var row = 6;
  foreach (var item in r.Rows)
  {
   ws.Cell(row, 1).Value = item.EmployeeCode;
   ws.Cell(row, 2).Value = item.EmployeeName;
   ws.Cell(row, 3).Value = item.Date.ToDateTime(TimeOnly.MinValue);
   ws.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy";
   ws.Cell(row, 4).Value = Human(item.Status, item.Failure);
   ws.Cell(row, 5).Value = Minutes(item.WorkedMinutes);
   ws.Cell(row, 6).Value = Minutes(item.LunchMinutes);
   ws.Cell(row, 7).Value = string.Join(", ", item.Anomalies.Select(anomaly => Human(anomaly, null)));
   row++;
  }

  ws.Range(5, 1, Math.Max(5, row - 1), headers.Length).CreateTable();
  ws.SheetView.FreezeRows(5);
  ws.Columns().AdjustToContents();

  var summary = wb.Worksheets.Add("Resumen");
  summary.Cell(1, 1).Value = "Resumen";
  var values = new[]
  {
   ("Empleados incluidos", r.Summary.EmployeesIncluded.ToString()),
   ("Días evaluados", r.Summary.DaysEvaluated.ToString()),
   ("Presentes", r.Summary.Present.ToString()),
   ("Ausencias injustificadas", r.Summary.UnexcusedAbsences.ToString()),
   ("Incompletos", r.Summary.Incomplete.ToString()),
   ("Horas trabajadas", Minutes(r.Summary.TotalWorkedMinutes))
  };
  for (var index = 0; index < values.Length; index++)
  {
   summary.Cell(index + 3, 1).Value = values[index].Item1;
   summary.Cell(index + 3, 2).Value = values[index].Item2;
  }
  summary.Columns().AdjustToContents();

  using var ms = new MemoryStream();
  wb.SaveAs(ms);
  return ms.ToArray();
 }

 private static void StyleExcelHeader(IXLWorksheet worksheet, AttendanceReportResponse report)
 {
  worksheet.Range(1, 1, 3, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("08075A");
  worksheet.Row(1).Height = 24;
  worksheet.Row(2).Height = 16;
  worksheet.Row(3).Height = 16;

  worksheet.Range(1, 3, 1, 7).Merge().Value = "REPORTE DE ASISTENCIA";
  worksheet.Range(2, 3, 2, 7).Merge().Value = $"Período: {report.From:dd/MM/yyyy} - {report.To:dd/MM/yyyy}";
  worksheet.Range(3, 3, 3, 7).Merge().Value = $"Empleados incluidos: {report.Summary.EmployeesIncluded}";
  worksheet.Range(1, 3, 1, 7).Style.Font.SetBold();
  worksheet.Range(1, 3, 1, 7).Style.Font.FontSize = 16;
  worksheet.Range(1, 3, 3, 7).Style.Font.FontColor = XLColor.White;
  worksheet.Range(1, 3, 3, 7).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
  worksheet.Range(1, 3, 1, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

  using var logo = typeof(AttendanceReportService).Assembly.GetManifestResourceStream(NakamaLogoResourceName)
   ?? throw new InvalidOperationException("The Nakama Excel logo resource is missing.");
  worksheet.Pictures.Add(logo, XLPictureFormat.Png, "NakamaLogo")
   .MoveTo(worksheet.Cell(1, 1))
   .WithSize(210, 53);
 }
 public byte[] Pdf(AttendanceReportResponse report)
 {
  using var document=new PdfDocument();
  var titleFont=new XFont("Arial",16,XFontStyleEx.Bold);
  var headerFont=new XFont("Arial",8,XFontStyleEx.Bold);
  var bodyFont=new XFont("Arial",8);
  PdfPage? page=null;
  XGraphics? graphics=null;
  var y=0d;
  var columns=new[]{(Name:"Código",Width:75d),(Name:"Empleado",Width:145d),(Name:"Fecha",Width:70d),(Name:"Estado",Width:125d),(Name:"Trabajado",Width:75d),(Name:"Almuerzo",Width:75d),(Name:"Anomalía principal",Width:180d)};
  void DrawText(string text,XFont font,double x,double top,double width,double height,XBrush? brush=null)
   =>graphics!.DrawString(Fit(graphics!,text,font,width),font,brush??XBrushes.Black,new XRect(x,top,width,height),XStringFormats.TopLeft);
  void NewPage()
  {
   graphics?.Dispose();
   page=document.AddPage();
   page.Size=PdfSharp.PageSize.A4;
   page.Orientation=PdfSharp.PageOrientation.Landscape;
   graphics=XGraphics.FromPdfPage(page);
   var width=page.Width.Point;
   DrawText("Sistema de Asistencia - Reporte de Asistencia",titleFont,36,28,width-72,22);
   DrawText($"Período: {report.From:dd/MM/yyyy} - {report.To:dd/MM/yyyy}    Empleados: {report.Summary.EmployeesIncluded}    Horas trabajadas: {Minutes(report.Summary.TotalWorkedMinutes)}",bodyFont,36,57,width-72,14);
   var x=36d;
   foreach(var column in columns){DrawText(column.Name,headerFont,x,82,column.Width,14);x+=column.Width;}
   graphics.DrawLine(XPens.LightGray,36,99,width-36,99);
   y=105;
  }
  NewPage();
  if(report.Rows.Count==0)
  {
   DrawText("No hay información de asistencia para este período.",bodyFont,36,y,page!.Width.Point-72,14);
  }
  else foreach(var row in report.Rows)
  {
   if(y+22>page!.Height.Point-38)NewPage();
   var values=new[]{row.EmployeeCode,row.EmployeeName,row.Date.ToString("dd/MM/yyyy"),Human(row.Status,row.Failure),Minutes(row.WorkedMinutes),Minutes(row.LunchMinutes),row.Anomalies.Count==0?"—":Human(row.Anomalies.First(),null)};
   var x=36d;
   for(var i=0;i<columns.Length;i++){DrawText(values[i],bodyFont,x,y,columns[i].Width,14);x+=columns[i].Width;}
   graphics!.DrawLine(XPens.LightGray,36,y+18,page!.Width.Point-36,y+18);
   y+=22;
  }
  graphics?.Dispose();
  for(var i=0;i<document.PageCount;i++)
  {
   using var footer=XGraphics.FromPdfPage(document.Pages[i]);
   footer.DrawString($"Página {i+1} de {document.PageCount}",bodyFont,XBrushes.Gray,new XRect(36,document.Pages[i].Height.Point-24,document.Pages[i].Width.Point-72,12),XStringFormats.CenterRight);
  }
  using var stream=new MemoryStream();
  document.Save(stream,false);
  return stream.ToArray();
 }
 static string Fit(XGraphics graphics,string text,XFont font,double width)
 {
  if(graphics.MeasureString(text,font).Width<=width)return text;
  const string suffix="...";
  var length=text.Length;
  while(length>0&&graphics.MeasureString(text[..length]+suffix,font).Width>width)length--;
  return length==0?suffix:text[..length]+suffix;
 }
 static string Minutes(int? m)=>m is null?"—":$"{m/60} h {m%60:D2} min"; static string Human(string? x,string? f)=>f=="MissingWorkCalendarDay"?"Calendario sin configurar":x switch{"Present"=>"Presente","Incomplete"=>"Incompleto","UnexcusedAbsence"=>"Ausencia injustificada","Vacation"=>"Vacaciones","MedicalLeave"=>"Licencia médica","Permission"=>"Permiso","Commission"=>"Comisión","JustifiedAbsence"=>"Ausencia justificada","Holiday"=>"Feriado","NonWorkingDay"=>"No laborable","NotApplicable"=>"No aplica","MarksOnHoliday"=>"Hay marcaciones en un feriado","MarksOnNonWorkingDay"=>"Hay marcaciones en un día no laborable","MarksDuringAuthorizedAbsence"=>"Hay marcaciones durante una ausencia autorizada","IncompleteMarks"=>"Marcaciones incompletas","MissingEntry"=>"Falta marcación de entrada","MissingExit"=>"Falta marcación de salida","MissingWorkCalendarDay"=>"Calendario sin configurar",_=>x??"Sin evaluación"};
}
