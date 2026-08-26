import apiClient from '@/services/api-client'; import type {WorkAssignment,WorkAssignmentRequest} from './work-assignments.types'
const path='/work-assignments'
export const listWorkAssignments=async(filters:{from:string;to:string;status?:string})=>(await apiClient.get<WorkAssignment[]>(path,{params:filters})).data
export const createWorkAssignment=async(x:WorkAssignmentRequest)=>(await apiClient.post<WorkAssignment>(path,x)).data
export const updateWorkAssignment=async(id:string,x:Omit<WorkAssignmentRequest,'employeeId'>&{version:number})=>(await apiClient.put<WorkAssignment>(`${path}/${id}`,x)).data
export const cancelWorkAssignment=async(id:string,version:number)=>{await apiClient.post(`${path}/${id}/cancel`,{version})}
