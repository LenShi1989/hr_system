import http from '@/services/http'
import type { Paged } from '@/services/organization'

export interface EmployeeSalary {
  employeeId: number
  employeeName: string | null
  employeeNo: string
  baseSalary: number
  positionAllowance: number
  mealAllowance: number
  effectiveDate: string
  updatedAt: string
}

export interface UpsertEmployeeSalary {
  baseSalary: number
  positionAllowance: number
  mealAllowance: number
  effectiveDate: string
}

export interface Payroll {
  id: number
  period: number
  employeeId: number
  employeeName: string | null
  employeeNo: string
  baseAmount: number
  overtimePay: number
  bonus: number
  leaveDeduction: number
  insuranceDeduction: number
  taxWithheld: number
  grossPay: number
  netPay: number
  status: string
  generatedBy: number
  generatedByName: string | null
  generatedAt: string
  confirmedAt: string | null
  paidAt: string | null
}

export interface GenerateResult {
  period: number
  generated: number
  skipped: number
}

export interface PayrollQuery {
  page?: number
  pageSize?: number
  period?: number
  employeeId?: number
}

export async function getPayrolls(query: PayrollQuery = {}): Promise<Paged<Payroll>> {
  const { data } = await http.get<Paged<Payroll>>('/payrolls', { params: query })
  return data
}

export async function getPayroll(id: number): Promise<Payroll> {
  const { data } = await http.get<Payroll>(`/payrolls/${id}`)
  return data
}

export async function generatePayroll(period: number): Promise<GenerateResult> {
  const { data } = await http.post<GenerateResult>('/payrolls/generate', { period })
  return data
}

export async function confirmPayroll(id: number): Promise<boolean> {
  const { data } = await http.put<boolean>(`/payrolls/${id}/confirm`)
  return data
}

export async function payPayroll(id: number): Promise<boolean> {
  const { data } = await http.put<boolean>(`/payrolls/${id}/pay`)
  return data
}

export async function setPayrollBonus(id: number, amount: number): Promise<boolean> {
  const { data } = await http.put<boolean>(`/payrolls/${id}/bonus`, { amount })
  return data
}

export async function getSalaries(): Promise<Paged<EmployeeSalary>> {
  const { data } = await http.get<Paged<EmployeeSalary>>('/employee-salaries', { params: { page: 1, pageSize: 200 } })
  return data
}

export async function getSalary(employeeId: number): Promise<EmployeeSalary> {
  const { data } = await http.get<EmployeeSalary>(`/employee-salaries/${employeeId}`)
  return data
}

export async function upsertSalary(employeeId: number, body: UpsertEmployeeSalary): Promise<EmployeeSalary> {
  const { data } = await http.put<EmployeeSalary>(`/employee-salaries/${employeeId}`, body)
  return data
}