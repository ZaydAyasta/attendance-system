# Attendance Evaluation Rules

## 1. Purpose

This document defines the confirmed business rules used to determine the real daily attendance status of an employee.

The evaluator must classify a day using:

- employee employment dates;
- work calendar;
- registered authorized absences;
- attendance marks;
- work schedule, when applicable;
- detected inconsistencies.

The system must not invent missing business information or silently assume that an unconfigured day is a working day.

---

## 2. General principles

### 2.1 Registered absences are already authorized

The system does **not** manage absence approval requests.

If Human Resources registers an absence, it is already authorized and must immediately affect attendance evaluation.

Therefore, the business flow does not require states such as:

- Pending
- Approved
- Rejected

An absence either exists as an authorized absence or does not exist.

Historical cancellation/anulment must preserve traceability rather than physically destroying the record.

### 2.2 Daily status and anomalies are separate concepts

The primary daily status must not be multiplied into many combinations.

Example:

- Status: `Vacation`
- Anomaly: `MarksDuringAbsence`

instead of creating states such as `VacationWorked`, `VacationWorkedIncomplete`, etc.

### 2.3 Historical information must be preserved

Administrative corrections must not silently destroy the original information.

Attendance marks, absences and administrative changes should preserve an audit trail.

---

## 3. Daily attendance statuses

The evaluator should support at least:

- `Present`
- `Incomplete`
- `UnexcusedAbsence`
- `Vacation`
- `MedicalLeave`
- `Permission`
- `Commission`
- `JustifiedAbsence`
- `Holiday`
- `NonWorkingDay`
- `NotApplicable`

A missing work-calendar configuration should not be represented as an attendance status. It is an evaluation/configuration problem.

---

## 4. Attendance anomalies

The evaluator should be able to report anomalies separately from the primary status.

Minimum anomalies:

- `IncompleteMarks`
- `MarksOnHoliday`
- `MarksOnNonWorkingDay`
- `MarksDuringAuthorizedAbsence`
- `DuplicatedMarks`
- `InvalidMarkSequence`

Additional anomalies may be introduced later when business rules require them.

---

## 5. Employee applicability

A day does not apply to an employee when:

- the date is before `HireDate`; or
- the date is after `TerminationDate`, when a termination date exists.

Result:

`NotApplicable`

`IsActive` alone must not be used to rewrite historical attendance.

Employment dates are the main historical reference.

---

## 6. Work calendar

Every evaluated date must have a `WorkCalendarDay`.

Supported day types:

- `WorkingDay`
- `NonWorkingDay`
- `Holiday`

### Missing calendar day

If the date has no calendar configuration:

- do not assume it is a working day;
- do not generate an absence;
- report the date as not evaluable / calendar not configured.

Human Resources must correct the calendar.

If the calendar is corrected retroactively, newly generated reports must recalculate the affected dates using the corrected information.

---

## 7. Authorized absences

Supported full-day absence types:

- Vacation
- Medical Leave
- Permission
- Commission
- Justified Absence

Absences are currently full-day only.

The MVP does **not** support:

- hourly permissions;
- half-day vacations;
- partial medical leave.

### Authorized absence without marks

For a working day:

- Vacation -> `Vacation`
- Medical Leave -> `MedicalLeave`
- Permission -> `Permission`
- Commission -> `Commission`
- Justified Absence -> `JustifiedAbsence`

### Authorized absence with attendance marks

The authorized absence remains the primary status.

Example:

Vacation + Entry + Exit

Result:

- Status: `Vacation`
- Anomaly: `MarksDuringAuthorizedAbsence`

The system must not silently change the day to `Present`.

This same rule applies to Medical Leave, Permission, Commission and Justified Absence.

---

## 8. Commission rules

Two different concepts exist.

### Full-day commission

An authorized `AbsenceType.Commission` means the employee works outside the office for the full day.

Result:

`Commission`

### Partial commission during a normal working day

`CommissionExit` and `CommissionReturn` are attendance marks.

Example:

- Entry
- CommissionExit
- CommissionReturn
- Exit

Result:

`Present`

The partial commission remains part of the attendance-mark history but does not replace the daily status with `Commission`.

### Commission ending outside the office

If the employee leaves the office on commission and finishes the workday outside the office, the exact closing rule must be supported operationally.

The system should not invent a final `Exit` mark automatically unless the business process explicitly provides a mechanism for closing that workday.

This scenario may require manual confirmation or a specific "finish workday outside office" action.

---

## 9. Holiday rules

### Holiday without marks

Result:

`Holiday`

Never treat lack of marks on a holiday as absence.

### Holiday with marks

Result:

- Status: `Holiday`
- Anomaly: `MarksOnHoliday`

The system must preserve the fact that the employee worked or produced marks, but the primary calendar classification remains Holiday.

No overtime calculation is required in the current MVP.

---

## 10. Non-working-day rules

### Non-working day without marks

Result:

`NonWorkingDay`

### Non-working day with marks

Result:

- Status: `NonWorkingDay`
- Anomaly: `MarksOnNonWorkingDay`

---

## 11. Minimum marks for normal presence

For the MVP, a normal working day counts as `Present` when at least:

- one `Entry`; and
- one `Exit`

exist for the day.

Lunch marks are **not required** to decide whether the employee attended.

Examples:

Entry + Exit -> `Present`

Entry + LunchStart + LunchEnd + Exit -> `Present`

Entry + CommissionExit + CommissionReturn + Exit -> `Present`

Entry + OtherExit + OtherReturn + Exit -> `Present`

---

## 12. Incomplete attendance

If a working day has attendance marks but does not have the minimum valid combination `Entry + Exit`, the day is:

`Incomplete`

with:

`IncompleteMarks`

Examples:

- Entry only
- Exit only
- Entry + LunchStart + LunchEnd, without Exit
- LunchEnd without LunchStart
- CommissionReturn without CommissionExit

The system should prefer exposing the inconsistency instead of guessing what happened.

---

## 13. No marks on a working day

If all conditions below are true:

- employee is applicable for the date;
- calendar says `WorkingDay`;
- no authorized absence exists;
- no attendance marks exist;

result:

`UnexcusedAbsence`

---

## 14. Lunch rules

Lunch must be registered.

This applies whether the employee:

- eats in the company cafeteria; or
- leaves the company to eat outside.

### Internal cafeteria

The employee must register:

- LunchStart
- LunchEnd

A dedicated cafeteria checkpoint/QR may be used.

### Lunch outside the office

The system must **ask the reason for leaving** rather than assuming lunch only because of the time.

Possible reasons may include:

- Lunch
- Commission
- Other / personal reason

If Lunch is selected:

- register LunchStart;
- on return, register LunchEnd.

### Missing lunch marks

Missing lunch marks do not by themselves turn a valid `Entry + Exit` day into an absence.

They may later generate an operational warning if Human Resources decides it is necessary.

---

## 15. Other exits during the workday

Short personal/other exits should be registered.

Use:

- `OtherExit`
- `OtherReturn`

If the system detects that the employee has been outside for a significant amount of time, it should ask for the reason when the employee returns.

The exact duration that counts as "significant" is still a configurable business value and should not be hardcoded without confirmation.

A normal sequence such as:

- Entry
- OtherExit
- OtherReturn
- Exit

still results in:

`Present`

---

## 16. Duplicate and invalid marks

### Duplicate technical marks

If the same action is sent multiple times due to technical reasons, the system should prevent accidental duplicate registration where possible.

The exact technical deduplication window may be defined during checkpoint/QR implementation.

### Duplicate logical marks

Examples:

- Entry followed immediately by another Entry
- Exit followed by another Exit

The system should:

- preserve the information;
- flag the sequence as anomalous when relevant;
- avoid silently guessing the employee's intention.

### Invalid order

Examples:

- Exit before Entry
- LunchEnd without LunchStart
- CommissionReturn without CommissionExit

These cases should be treated as inconsistent and may require review.

---

## 17. Work schedules

Not all employees necessarily use the same schedule.

The system must support employees with different expected schedules.

A future/next domain model must represent expected work schedules rather than assuming one global entry and exit time.

The schedule is required for:

- tardiness;
- early departure;
- expected worked time;
- hour recovery/compensation.

---

## 18. Tardiness

Tardiness must be supported.

It must be calculated against the employee's expected work schedule.

The system must eventually know:

- expected entry time;
- allowed tolerance, if any.

Tardiness is additional information about a `Present` day and should not necessarily replace the daily attendance status.

Example:

- Status: `Present`
- LateMinutes: 18

The exact tolerance policy still needs a configurable value.

---

## 19. Early departure

Early departure depends on the employee's expected work schedule.

The system should be capable of identifying that an employee left before the expected end of the workday.

However, early departure may interact with authorized hour recovery/compensation rules and therefore must not automatically be treated as an infraction without considering those rules.

---

## 20. Hour recovery / compensation between days

The company has real cases where an employee works less time on one day and compensates by working additional time on another day.

Example:

- Tuesday: works one hour less.
- Wednesday: works one additional hour.

This is **not considered overtime** for the current business requirement.

The system therefore needs a future concept for hour compensation/recovery rather than treating every minute beyond the expected end time as overtime.

The detailed policy is still pending, including:

- who authorizes the compensation;
- whether compensation must occur within a specific period;
- whether one day can compensate multiple previous days;
- how the relationship between owed and recovered minutes is recorded.

This does not block AttendanceEvaluator V1 because the evaluator can initially classify daily presence independently from final hour-balance calculations.

---

## 21. Overtime

Overtime calculation is out of scope for the current MVP.

Working additional time does not automatically mean overtime because it may represent recovery/compensation from another day.

---

## 22. Manual correction when automatic marking fails

If Wi-Fi, QR, system availability or another technical problem prevents marking:

1. attendance may initially be recorded on a physical sheet;
2. the information can later be registered manually in the system;
3. depending on permissions, either an authorized administrative user or the employee may register the missing mark.

Manual marks must be identifiable by their source.

Example:

`AttendanceSource.Manual`

Corrections must preserve traceability.

---

## 23. Corrections and history

The system must preserve historical information.

Do not physically destroy important attendance history when a correction is made.

For incorrect attendance marks:

- preserve the original mark;
- register correction/anulment information;
- record who performed the change;
- record when it happened.

For absences:

- preserve cancelled/anulled records for historical purposes.

The audit module will later formalize this behavior.

---

## 24. Retroactive changes

If Human Resources corrects:

- an absence;
- a work-calendar day;
- an attendance mark;

future report generation must use the corrected current business information.

Example:

Monday initially appears as UnexcusedAbsence.

On Wednesday an authorized Justified Absence is registered for Monday.

A newly generated report should show:

`JustifiedAbsence`

Historical audit information must still show what was changed and when.

---

## 25. Report behavior

Reports must display the evaluated daily status, not infer attendance directly from raw marks.

Possible displayed statuses:

- Present
- Incomplete
- Unexcused Absence
- Vacation
- Medical Leave
- Permission
- Commission
- Justified Absence
- Holiday
- Non-working Day
- Not Applicable

If the work calendar is missing:

- show that the date is not configured;
- do not report an unjustified absence.

If anomalies exist, reports/UI should be able to indicate them separately.

---

## 26. Attendance evaluation precedence

For AttendanceEvaluator V1, use this order:

### Step 1 — Employee applicability

If date is before HireDate or after TerminationDate:

`NotApplicable`

### Step 2 — Work calendar

If no WorkCalendarDay exists:

evaluation cannot be completed.

Do not assume WorkingDay.

### Step 3 — Authorized full-day absence

If an authorized absence exists for the date:

map the absence type to its corresponding daily status.

If marks also exist:

add `MarksDuringAuthorizedAbsence`.

### Step 4 — Holiday

If DayType is Holiday:

`Holiday`

If marks exist:

add `MarksOnHoliday`.

### Step 5 — Non-working day

If DayType is NonWorkingDay:

`NonWorkingDay`

If marks exist:

add `MarksOnNonWorkingDay`.

### Step 6 — Working day

If Entry and Exit exist:

`Present`

If marks exist but minimum Entry + Exit is incomplete:

`Incomplete`

If no marks exist:

`UnexcusedAbsence`

---

## 27. Confirmed out-of-scope items for AttendanceEvaluator V1

The first evaluator does not calculate:

- overtime;
- hourly permissions;
- half-day vacations;
- partial medical leave;
- hour-compensation balances;
- exact worked minutes for payroll;
- complete attendance-mark state machine;
- advanced duplicate resolution.

These may be implemented in later modules/features.

---

## 28. Pending business configuration values

The following do not block AttendanceEvaluator V1 but must be confirmed/configurable before advanced schedule evaluation:

- tardiness tolerance in minutes;
- threshold for considering an `OtherExit` long enough to ask for a reason;
- detailed hour-recovery/compensation policy;
- who may correct attendance marks;
- who may manually register missing marks;
- rules for ending a workday when a commission finishes outside the office;
- schedule assignment model (per employee, group, or another organizational unit).

---

## 29. Core rule summary

The primary daily classification answers:

> What was the employee's valid attendance condition for this date?

Anomalies answer:

> Is there information that conflicts with or makes that classification suspicious?

Schedules and time-balance rules answer a different question:

> Did the employee comply with expected working time, arrive late, leave early, or compensate hours?

These concerns must remain separate in the implementation.