namespace HrSystem.Api.Dtos;

public record UpsertEmployeeSalary(
    decimal BaseSalary,
    decimal PositionAllowance,
    decimal MealAllowance,
    DateOnly EffectiveDate);

public record EmployeeSalaryDto(
    long EmployeeId,
    string? EmployeeName,
    string EmployeeNo,
    decimal BaseSalary,
    decimal PositionAllowance,
    decimal MealAllowance,
    DateOnly EffectiveDate,
    DateTimeOffset UpdatedAt);

public record PayrollDto(
    long Id,
    int Period,
    long EmployeeId,
    string? EmployeeName,
    string EmployeeNo,
    decimal BaseAmount,
    decimal OvertimePay,
    decimal Bonus,
    decimal LeaveDeduction,
    decimal InsuranceDeduction,
    decimal TaxWithheld,
    decimal GrossPay,
    decimal NetPay,
    string Status,
    long GeneratedBy,
    string? GeneratedByName,
    DateTimeOffset GeneratedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? PaidAt);

public record GeneratePayrollRequest(int Period);

public record GeneratePayrollResult(int Period, int Generated, int Skipped);

public record SetBonusRequest(decimal Amount);