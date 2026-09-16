import http from '@/services/http'
import type { Paged } from '@/services/organization'

export interface LeaveType {
  id: number
  code: string
  name: string
  annualQuota: number | null
  isPaid: boolean
  isActive: boolean
}

export interface LeaveRequest {
  id: number
  employeeId: number
  employeeName: string | null
  employeeNo: string
  leaveTypeId: number
  leaveTypeName: string | null
  startAt: string
  endAt: string
  days: number
  reason: string
  status: string
  approverId: number | null
  approverName: string | null
  createdAt: string
}

export interface CreateLeaveRequest {
  leaveTypeId: number
  startAt: string
  endAt: string
  reason: string
}

export interface OvertimeRequest {
  id: number
  employeeId: number
  employeeName: string | null
  employeeNo: string
  workDate: string
  startAt: string
  endAt: string
  hours: number
  reason: string
  status: string
  approverId: number | null
  approverName: string | null
  createdAt: string
}

export interface CreateOvertimeRequest {
  workDate: string
  startAt: string
  endAt: string
  reason: string
}

export interface AttendanceRecord {
  id: number
  employeeId: number
  employeeName: string | null
  employeeNo: string
  workDate: string
  clockInAt: string | null
  clockOutAt: string | null
  workHours: number
  lateMinutes: number
  earlyLeaveMinutes: number
  status: string
}

export interface RequestQuery {
  page?: number
  pageSize?: number
  status?: string
  employeeId?: number
  all?: boolean
}

export async function getLeaveTypes(): Promise<LeaveType[]> {
  const { data } = await http.get<LeaveType[]>('/leave-types')
  return data
}

export async function getMyLeaveRequests(query: RequestQuery = {}): Promise<Paged<LeaveRequest>> {
  const { data } = await http.get<Paged<LeaveRequest>>('/leave-requests', { params: query })
  return data
}

export async function createLeaveRequest(body: CreateLeaveRequest): Promise<LeaveRequest> {
  const { data } = await http.post<LeaveRequest>('/leave-requests', body)
  return data
}

export async function cancelLeaveRequest(id: number): Promise<boolean> {
  const { data } = await http.put<boolean>(`/leave-requests/${id}/cancel`)
  return data
}

export async function reviewLeaveRequest(id: number, action: 'approve' | 'reject', comment = ''): Promise<boolean> {
  const { data } = await http.put<boolean>(`/leave-requests/${id}/approve`, { action, comment })
  return data
}

export async function getMyOvertimeRequests(query: RequestQuery = {}): Promise<Paged<OvertimeRequest>> {
  const { data } = await http.get<Paged<OvertimeRequest>>('/overtime-requests', { params: query })
  return data
}

export async function createOvertimeRequest(body: CreateOvertimeRequest): Promise<OvertimeRequest> {
  const { data } = await http.post<OvertimeRequest>('/overtime-requests', body)
  return data
}

export async function cancelOvertimeRequest(id: number): Promise<boolean> {
  const { data } = await http.put<boolean>(`/overtime-requests/${id}/cancel`)
  return data
}

export async function reviewOvertimeRequest(id: number, action: 'approve' | 'reject', comment = ''): Promise<boolean> {
  const { data } = await http.put<boolean>(`/overtime-requests/${id}/approve`, { action, comment })
  return data
}

export interface ClockResult {
  record: AttendanceRecord
  message: string
}

export async function clockIn(): Promise<ClockResult> {
  const { data } = await http.post<ClockResult>('/attendance/me/clock-in')
  return data
}

export async function clockOut(): Promise<ClockResult> {
  const { data } = await http.post<ClockResult>('/attendance/me/clock-out')
  return data
}

export async function getMyAttendance(fromDate?: string, toDate?: string): Promise<AttendanceRecord[]> {
  const { data } = await http.get<AttendanceRecord[]>('/attendance/me', { params: { fromDate, toDate } })
  return data
}

export async function getAttendanceRecords(fromDate?: string, toDate?: string, employeeId?: number): Promise<AttendanceRecord[]> {
  const { data } = await http.get<AttendanceRecord[]>('/attendance/records', { params: { fromDate, toDate, employeeId } })
  return data
}