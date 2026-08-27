import apiClient from '@/services/api-client'; import type {AttendanceRange} from './attendance.types'
export const getAttendanceRange=async(employeeId:string,from:string,to:string)=>(await apiClient.get<AttendanceRange>(`/employees/${employeeId}/attendance`,{params:{from,to}})).data
export const getMyAttendanceRange=async(from:string,to:string)=>(await apiClient.get<AttendanceRange>('/me/attendance',{params:{from,to}})).data
