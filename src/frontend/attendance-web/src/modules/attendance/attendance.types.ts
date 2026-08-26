export interface DailyAttendance { employeeId:string;date:string;status:string|null;anomalies:string[];failure:string|null;grossMinutes:number|null;lunchMinutes:number|null;workedMinutes:number|null;timeCalculationComplete:boolean;timeIssues:string[] }
export interface AttendanceRange { employeeId:string;from:string;to:string;days:DailyAttendance[] }
