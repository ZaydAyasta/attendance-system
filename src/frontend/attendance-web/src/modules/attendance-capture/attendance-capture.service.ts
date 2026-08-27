import apiClient from '@/services/api-client'
import type {CaptureAction,CaptureMark,CaptureResolution,TodayMark} from './attendance-capture.types'
export const resolveCapture=async(qrToken:string)=> (await apiClient.post<CaptureResolution>('/attendance/capture/resolve',{qrToken})).data
export const markCapture=async(qrToken:string,action:CaptureAction)=> (await apiClient.post<CaptureMark>('/attendance/capture/mark',{qrToken,action})).data
export const getTodayMarks=async()=> (await apiClient.get<TodayMark[]>('/me/attendance/marks/today')).data
export const captureActionLabel=(action:CaptureAction)=>({Entry:'Registrar entrada',LunchStart:'Inicio de almuerzo',LunchEnd:'Fin de almuerzo',Exit:'Registrar salida',CommissionExit:'Salida a comisión',CommissionReturn:'Regresar de comisión',OtherExit:'Otra salida',OtherReturn:'Registrar retorno'}[action])
