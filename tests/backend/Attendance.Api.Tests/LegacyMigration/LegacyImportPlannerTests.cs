using Attendance.Api.Modules.Attendance.Domain;
using Attendance.LegacyImporter;
using Xunit;

namespace Attendance.Api.Tests.LegacyMigration;

public sealed class LegacyImportPlannerTests
{
    [Fact]
    public void CreatePlan_NormalizesNamesAndCollapsesDuplicateMarksAcrossFiles()
    {
        var employee = new LegacyEmployeeSourceRow("EMP-001", "Ana María Torres", new DateOnly(2020, 1, 2));
        var first = Mark("ANA MARIA   TORRES", "one.xlsx");
        var duplicate = Mark("ana maria torres", "two.xlsx");

        var plan = new LegacyImportPlanner().CreatePlan([employee], [first, duplicate], new LegacyValidationReport());

        Assert.True(plan.Validation.IsValid);
        Assert.Single(plan.Employees);
        Assert.Single(plan.Marks);
        Assert.Equal(1, plan.DuplicateMarkOccurrences);
        Assert.Equal(LegacyText.CreateMarkLegacyId("EMP-001", new DateOnly(2026, 8, 1), new TimeOnly(8, 0), "Entry"), plan.Marks[0].LegacyId);
    }

    [Fact]
    public void CreatePlan_RejectsNullHireDateSingleNameAndMissingExcelEmployee()
    {
        var invalid = new LegacyEmployeeSourceRow("EMP-001", "Ana", null);
        var plan = new LegacyImportPlanner().CreatePlan([invalid], [Mark("Missing Person", "one.xlsx")], new LegacyValidationReport());

        Assert.False(plan.Validation.IsValid);
        Assert.Contains(plan.Validation.Blockers, message => message.Contains("hire_date", StringComparison.Ordinal));
        Assert.Contains(plan.Validation.Blockers, message => message.Contains("no exact Supabase employee", StringComparison.Ordinal));
    }

    private static LegacyExcelMark Mark(string name, string file) => new(name, LegacyText.NormalizeName(name), new DateOnly(2026, 8, 1), new TimeOnly(8, 0), AttendanceMarkType.Entry, file, "Registro", 6);
}
