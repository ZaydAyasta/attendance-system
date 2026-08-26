export const workAssignmentTypes=['WeekendWork','Recovery','TemporaryWork'] as const
export type WorkAssignmentType=typeof workAssignmentTypes[number]
export type WorkAssignmentStatus='Active'|'Cancelled'
export interface WorkAssignment { id:string; employeeId:string; employee:{employeeCode:string;fullName:string}; date:string; type:WorkAssignmentType; comment:string|null; status:WorkAssignmentStatus; version:number }
export interface WorkAssignmentRequest { employeeId:string; date:string; type:WorkAssignmentType; comment:string|null }
