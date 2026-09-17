namespace HrSystem.Api.Models;

public class EmployeeSalary
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal PositionAllowance { get; set; }
    public decimal MealAllowance { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Employee? Employee { get; set; }
}

public class Payroll
{
    public long Id { get; set; }
    public int Period { get; set; }
    public long EmployeeId { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Bonus { get; set; }
    public decimal LeaveDeduction { get; set; }
    public decimal InsuranceDeduction { get; set; }
    public decimal TaxWithheld { get; set; }
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }
    public string Status { get; set; } = "draft";
    public long GeneratedBy { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public Employee? Employee { get; set; }
    public User? GeneratedByUser { get; set; }
}

public class PayrollSetting
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public bool IsActive { get; set; } = true;
}