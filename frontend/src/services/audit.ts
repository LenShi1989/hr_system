import http from '@/services/http'
import type { Paged } from '@/services/organization'

export interface AuditLog {
  id: number
  userId: number
  userEmail: string
  role: string
  action: string
  category: string
  entity: string
  entityId: number | null
  detail: string | null
  ipAddress: string | null
  createdAt: string
}

export interface AuditQuery {
  page?: number
  pageSize?: number
  category?: string
  action?: string
  userId?: number
  keyword?: string
  fromDate?: string
  toDate?: string
}

export async function getAuditLogs(query: AuditQuery = {}): Promise<Paged<AuditLog>> {
  const { data } = await http.get<Paged<AuditLog>>('/audit-logs', { params: query })
  return data
}

export const categoryLabels: Record<string, string> = {
  auth: '登入',
  employee: '員工',
  organization: '組織',
  leave: '請假',
  overtime: '加班',
  attendance: '出勤',
  salary: '薪資結構',
  payroll: '薪資',
  user: '帳號',
}

export const actionLabels: Record<string, string> = {
  login: '登入成功',
  login_failed: '登入失敗',
  create: '新增',
  update: '修改',
  delete: '刪除',
  approve: '核准',
  reject: '駁回',
  cancel: '取消',
  clock_in: '上班打卡',
  clock_out: '下班打卡',
  generate: '產生月結',
  confirm: '確認',
  pay: '發放',
  set_bonus: '調整獎金',
}