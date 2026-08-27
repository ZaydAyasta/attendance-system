export type CaptureAction='Entry'|'LunchStart'|'LunchEnd'|'Exit'|'CommissionExit'|'CommissionReturn'|'OtherExit'|'OtherReturn'
export interface CheckpointSummary { id:string; code:string; name:string; type:string }
export interface CaptureResolution { checkpoint:CheckpointSummary; availableActions:CaptureAction[]; message:string }
export interface CaptureMark { id:string; type:CaptureAction; occurredAt:string; checkpoint:CheckpointSummary; message:string }
export interface TodayMark { id:string; type:CaptureAction; occurredAt:string; checkpoint:CheckpointSummary|null }
