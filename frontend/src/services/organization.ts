import http from '@/services/http'

export interface Department {
  id: number
  parentId: number | null
  code: string
  name: string
  managerId: number | null
  isActive: boolean
  children: Department[]
}

export interface DepartmentUpsert {
  code: string
  name: string
  parentId: number | null
  managerId: number | null
}

export interface Position {
  id: number
  code: string
  name: string
  departmentId: number | null
  departmentName: string | null
  level: number
  isActive: boolean
}

export interface PositionUpsert {
  code: string
  name: string
  departmentId: number | null
  level: number
}

export interface Employee {
  id: number
  employeeNo: string
  name: string
  gender: number
  birthDate: string | null
  phone: string | null
  email: string | null
  address: string | null
  hireDate: string | null
  leaveDate: string | null
  employmentStatus: string
  departmentId: number
  departmentName: string
  positionId: number
  positionName: string
  managerId: number | null
  managerName: string | null
  isActive: boolean
  createdAt: string
}

export interface EmployeeUpsert {
  employeeNo: string
  name: string
  gender: number
  birthDate: string | null
  phone: string | null
  email: string | null
  address: string | null
  hireDate: string | null
  leaveDate: string | null
  employmentStatus: string
  departmentId: number
  positionId: number
  managerId: number | null
}

export interface Paged<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export async function getDepartmentsTree(): Promise<Department[]> {
  const { data } = await http.get<Department[]>('/departments/tree')
  return data
}

export async function createDepartment(body: DepartmentUpsert): Promise<Department> {
  const { data } = await http.post<Department>('/departments', body)
  return data
}

export async function updateDepartment(id: number, body: DepartmentUpsert): Promise<Department> {
  const { data } = await http.put<Department>(`/departments/${id}`, body)
  return data
}

export async function deleteDepartment(id: number): Promise<boolean> {
  const { data } = await http.delete<boolean>(`/departments/${id}`)
  return data
}

export async function getPositions(): Promise<Position[]> {
  const { data } = await http.get<Position[]>('/positions')
  return data
}

export async function createPosition(body: PositionUpsert): Promise<Position> {
  const { data } = await http.post<Position>('/positions', body)
  return data
}

export async function updatePosition(id: number, body: PositionUpsert): Promise<Position> {
  const { data } = await http.put<Position>(`/positions/${id}`, body)
  return data
}

export async function deletePosition(id: number): Promise<boolean> {
  const { data } = await http.delete<boolean>(`/positions/${id}`)
  return data
}

export interface EmployeeQuery {
  page?: number
  pageSize?: number
  keyword?: string
  departmentId?: number | null
  employmentStatus?: string
}

export async function getEmployees(query: EmployeeQuery = {}): Promise<Paged<Employee>> {
  const { data } = await http.get<Paged<Employee>>('/employees', { params: query })
  return data
}

export async function createEmployee(body: EmployeeUpsert): Promise<Employee> {
  const { data } = await http.post<Employee>('/employees', body)
  return data
}

export async function updateEmployee(id: number, body: EmployeeUpsert): Promise<Employee> {
  const { data } = await http.put<Employee>(`/employees/${id}`, body)
  return data
}

export async function deleteEmployee(id: number): Promise<boolean> {
  const { data } = await http.delete<boolean>(`/employees/${id}`)
  return data
}

export function flattenDepartments(tree: Department[]): Array<Department & { depth: number; hasChildren: boolean }> {
  const result: Array<Department & { depth: number; hasChildren: boolean }> = []
  const walk = (nodes: Department[], depth: number) => {
    for (const node of nodes) {
      result.push({ ...node, depth, hasChildren: node.children.length > 0 })
      walk(node.children, depth + 1)
    }
  }
  walk(tree, 0)
  return result
}