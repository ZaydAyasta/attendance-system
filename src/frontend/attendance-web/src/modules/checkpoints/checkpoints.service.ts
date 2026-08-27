import apiClient from '@/services/api-client'
import type {Checkpoint,CheckpointQr,CheckpointRequest} from './checkpoints.types'
const path='/checkpoints'
export const listCheckpoints=async()=> (await apiClient.get<Checkpoint[]>(path)).data
export const createCheckpoint=async(request:CheckpointRequest)=> (await apiClient.post<Checkpoint>(path,request)).data
export const updateCheckpoint=async(id:string,request:CheckpointRequest&{version:number})=> (await apiClient.put<Checkpoint>(`${path}/${id}`,request)).data
export const setCheckpointStatus=async(id:string,isActive:boolean,version:number)=> (await apiClient.put<Checkpoint>(`${path}/${id}/status`,{isActive,version})).data
export const getCheckpointQr=async(id:string)=> (await apiClient.get<CheckpointQr>(`${path}/${id}/qr`)).data
