export type CheckpointType = 'EntryExit' | 'Cafeteria'
export interface Checkpoint { id:string; code:string; name:string; type:CheckpointType; isActive:boolean; version:number }
export interface CheckpointRequest { code:string; name:string; type:CheckpointType }
export interface CheckpointQr { token:string; expiresAt:string }
