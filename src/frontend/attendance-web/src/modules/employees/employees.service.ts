import apiClient from '@/services/api-client'

export interface EmployeeOption { id: string; employeeCode: string; fullName: string }
export interface Employee { id:string; employeeCode:string; firstName:string; lastName:string; isActive:boolean; hireDate:string; terminationDate:string|null; version:number }
export interface EmployeeRequest { employeeCode:string; firstName:string; lastName:string; hireDate:string }

export async function listActiveEmployees(): Promise<EmployeeOption[]> {
  const response = await apiClient.get<EmployeeOption[]>('/employees', { params: { isActive: true } })
  return response.data
}
export const listEmployees=async():Promise<Employee[]> => (await apiClient.get<Employee[]>('/employees/manage')).data
export const createEmployee=async(x:EmployeeRequest):Promise<Employee> => (await apiClient.post<Employee>('/employees',x)).data
export const updateEmployee=async(id:string,x:EmployeeRequest&{version:number}):Promise<Employee> => (await apiClient.put<Employee>(`/employees/${id}`,x)).data
export const setEmployeeStatus=async(id:string,isActive:boolean,terminationDate:string|null,version:number):Promise<Employee> => (await apiClient.put<Employee>(`/employees/${id}/status`,{isActive,terminationDate,version})).data
