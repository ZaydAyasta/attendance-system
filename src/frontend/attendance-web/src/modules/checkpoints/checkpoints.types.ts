export type CheckpointType = 'EntryExit' | 'Cafeteria' | 'General'
export type CheckpointQrMode = 'Dynamic' | 'Static'
export interface Checkpoint { id:string; code:string; name:string; type:CheckpointType; isActive:boolean; version:number; qrMode:CheckpointQrMode }
export interface CheckpointRequest { code:string; name:string; type:CheckpointType; qrMode:CheckpointQrMode }
export interface CheckpointQr { token:string; expiresAt:string|null }
