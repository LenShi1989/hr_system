using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Services;

public class PayrollCalculator(HrDbContext db)
{
    public const string InsuranceRate = "insurance_rate";
    public const string TaxRate = "tax_rate";
    public const string OvertimeRate = "overtime_rate";

    private const decimal MonthlyWorkHours = 240m;
    private const decimal MonthlyWorkDays = 30m;

    public async Task<GeneratePayrollResult> GenerateAsync(int period, long generatedBy, CancellationToken ct)
    {
        var from = PeriodStart(period);
        var to = from.AddMonths(1);

        var settings = await db.PayrollSettings
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToDictionaryAsync(s => s.Code, s => s.Value, ct);

        var insuranceRate = settings.GetValueOrDefault(InsuranceRate, 0.10m);
        var taxRate = settings.GetValueOrDefault(TaxRate, 0.05m);
        var overtimeRate = settings.GetValueOrDefault(OvertimeRate, 1.5m);

        var salaries = await db.EmployeeSalaries
            .AsNoTracking()
            .Where(s => s.Employee != null && s.Employee.IsActive)
            .Select(s => new { s.EmployeeId, s.BaseSalary, s.PositionAllowance, s.MealAllowance })
            .ToListAsync(ct);

        var employeeIds = salaries.Select(s => s.EmployeeId).ToList();

        var overtimeHours = await db.OvertimeRequests
            .AsNoTracking()
            .Where(o => o.Status == "approved" && employeeIds.Contains(o.EmployeeId) && o.WorkDate >= from && o.WorkDate < to)
            .GroupBy(o => o.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Hours = g.Sum(o => o.Hours) })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.Hours, ct);

        var unpaidLeaves = await db.LeaveRequests
            .AsNoTracking()
            .Where(l => l.Status == "approved" && employeeIds.Contains(l.EmployeeId)
                        && l.LeaveType != null && l.LeaveType.IsPaid == false)
            .ToListAsync(ct);

        var generated = 0;
        var skipped = 0;

        foreach (var salary in salaries)
        {
            var exists = await db.Payrolls.AnyAsync(p => p.Period == period && p.EmployeeId == salary.EmployeeId, ct);
            if (exists)
            {
                skipped++;
                continue;
            }

            var baseAmount = salary.BaseSalary + salary.PositionAllowance + salary.MealAllowance;
            var hours = overtimeHours.GetValueOrDefault(salary.EmployeeId, 0m);
            var hourly = salary.BaseSalary / MonthlyWorkHours;
            var overtimePay = Math.Round(hours * hourly * overtimeRate, 2);

            var leaveDays = unpaidLeaves
                .Where(l => l.EmployeeId == salary.EmployeeId
                            && DateOnly.FromDateTime(l.EndAt.UtcDateTime) >= from
                            && DateOnly.FromDateTime(l.StartAt.UtcDateTime) < to)
                .Sum(l => l.Days);
            var leaveDeduction = Math.Round(leaveDays * (salary.BaseSalary / MonthlyWorkDays), 2);

            var gross = baseAmount + overtimePay;
            var insurance = Math.Round(gross * insuranceRate, 2);
            var tax = Math.Round(gross * taxRate, 2);
            var net = gross - insurance - tax - leaveDeduction;

            db.Payrolls.Add(new Payroll
            {
                Period = period,
                EmployeeId = salary.EmployeeId,
                BaseAmount = baseAmount,
                OvertimePay = overtimePay,
                Bonus = 0m,
                LeaveDeduction = leaveDeduction,
                InsuranceDeduction = insurance,
                TaxWithheld = tax,
                GrossPay = gross,
                NetPay = net,
                Status = "draft",
                GeneratedBy = generatedBy,
                GeneratedAt = DateTimeOffset.UtcNow
            });
            generated++;
        }

        await db.SaveChangesAsync(ct);
        return new GeneratePayrollResult(period, generated, skipped);
    }

    public static DateOnly PeriodStart(int period)
    {
        var year = period / 100;
        var month = period % 100;
        if (month is < 1 or > 12)
        {
            throw new ArgumentException("period 格式必須為 YYYYMM", nameof(period));
        }

        return new DateOnly(year, month, 1);
    }

    public static string PeriodLabel(int period) =>
        $"{period / 100} 年 {period % 100} 月";
}