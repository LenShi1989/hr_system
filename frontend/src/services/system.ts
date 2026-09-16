import http from '@/services/http'
import type { Paged } from '@/services/organization'

export interface UserItem {
  id: number
  email: string
  employeeId: number | null
  employeeName: string | null
  employeeNo: string | null
  roleId: number
  roleCode: string
  roleName: string
  isActive: boolean
  createdAt: string
}

export interface UserUpsert {
  email: string
  password?: string
  roleId: number
  employeeId: number | null
  isActive: boolean
}

export interface Role {
  id: number
  code: string
  name: string
  isActive: boolean
  userCount: number
  permissionCodes: string[]
}

export interface UserQuery {
  page?: number
  pageSize?: number
  keyword?: string
  roleId?: number
}

export async function getUsers(query: UserQuery = {}): Promise<Paged<UserItem>> {
  const { data } = await http.get<Paged<UserItem>>('/users', { params: query })
  return data
}

export async function createUser(body: UserUpsert): Promise<boolean> {
  const { data } = await http.post<boolean>('/users', body)
  return data
}

export async function updateUser(id: number, body: UserUpsert): Promise<boolean> {
  const { data } = await http.put<boolean>(`/users/${id}`, body)
  return data
}

export async function getRoles(): Promise<Role[]> {
  const { data } = await http.get<Role[]>('/roles')
  return data
}