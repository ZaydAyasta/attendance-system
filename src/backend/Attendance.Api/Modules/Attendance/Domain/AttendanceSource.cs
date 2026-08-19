namespace Attendance.Api.Modules.Attendance.Domain;

/// <summary>
/// Defines the mechanism through which an attendance mark was captured.
/// </summary>
public enum AttendanceSource
{
    LegacyFingerprint = 1,
    DynamicQr = 2,
    Nfc = 3,
    Mobile = 4,
    Manual = 5
}