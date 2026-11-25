import apiClient from '@/services/api-client'; import type {AttendanceRange} from './attendance.types'
export const getAttendanceRange=async(employeeId:string,from:string,to:string)=>(await apiClient.get<AttendanceRange>(`/employees/${employeeId}/attendance`,{params:{from,to}})).data
export const getMyAttendanceRange=async(from:string,to:string)=>(await apiClient.get<AttendanceRange>('/me/attendance',{params:{from,to}})).data

export interface AttendanceHistoryMark {
  id: string
  occurredAt: string
  localTime: string
  type: string
  source: string
  manualReason: string | null
  voidedAt: string | null
  voidReason: string | null
  replacesMarkId: string | null
}

export interface AttendanceMarkChange {
  date?: string
  time?: string
  type?: string
  reason: string
}

export async function getAttendanceMarks(employeeId: string, date: string) {
  return (await apiClient.get<AttendanceHistoryMark[]>(`/employees/${employeeId}/attendance/marks/${date}`)).data
}

export async function saveAttendanceMark(employeeId: string, change: AttendanceMarkChange, markId?: string) {
  const path = `/employees/${employeeId}/attendance/marks`
  await apiClient.post(markId ? `${path}/${markId}/correction` : path, change)
}
