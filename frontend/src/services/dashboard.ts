import http from './http'

export interface CountItem {
  name: string
  count: number
}

export interface EmployeeStats {
  total: number
  byDepartment: CountItem[]
  byPosition: CountItem[]
}

export interface PayrollTrendItem {
  period: string
  netPay: number
}

export interface PayrollStatusItem {
  status: string
  count: number
}

export interface PayrollSummary {
  currentMonthNetPay: number
  trend: PayrollTrendItem[]
  byStatus: PayrollStatusItem[]
}

export interface AttendanceSummary {
  todayClockedIn: number
  todayClockedOut: number
  thisMonthWorkDays: number
}

export interface PendingApprovals {
  leave: number
  overtime: number
}

export interface MySummary {
  todayStatus: string
  monthWorkDays: number
  monthWorkHours: number
  monthOvertimeHours: number
  pendingLeave: number
  pendingOvertime: number
}

export interface DashboardSummary {
  employees: EmployeeStats | null
  payroll: PayrollSummary | null
  attendance: AttendanceSummary | null
  pending: PendingApprovals | null
  my: MySummary | null
}

export async function getDashboardSummary(): Promise<DashboardSummary> {
  const { data } = await http.get<DashboardSummary>('/dashboard/summary')
  return data
}