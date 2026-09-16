using HrSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Data;

public class HrDbContext(DbContextOptions<HrDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<EmployeeSalary> EmployeeSalaries => Set<EmployeeSalary>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayrollSetting> PayrollSettings => Set<PayrollSetting>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetUtcConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.Property(r => r.Code).HasMaxLength(50);
            e.HasIndex(r => r.Code).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.Property(u => u.Email).HasMaxLength(320);
            e.Property(u => u.PasswordHash).HasMaxLength(512);
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.ToTable("role_permissions");
            e.HasKey(rp => new { rp.RoleId, rp.PermissionCode });
            e.Property(rp => rp.PermissionCode).HasMaxLength(64);
            e.HasOne(rp => rp.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(rp => rp.RoleId);
        });

        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.Property(d => d.Code).HasMaxLength(50);
            e.Property(d => d.Name).HasMaxLength(100);
            e.HasIndex(d => d.Code).IsUnique();
            e.HasOne(d => d.Parent)
                .WithMany(d => d.Children)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Position>(e =>
        {
            e.ToTable("positions");
            e.Property(p => p.Code).HasMaxLength(50);
            e.Property(p => p.Name).HasMaxLength(100);
            e.HasIndex(p => p.Code).IsUnique();
            e.HasOne(p => p.Department)
                .WithMany()
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("employees");
            e.Property(em => em.EmployeeNo).HasMaxLength(20);
            e.Property(em => em.Name).HasMaxLength(100);
            e.Property(em => em.Phone).HasMaxLength(30);
            e.Property(em => em.Email).HasMaxLength(320);
            e.Property(em => em.Address).HasMaxLength(500);
            e.Property(em => em.EmploymentStatus).HasMaxLength(20);
            e.HasIndex(em => em.EmployeeNo).IsUnique();
            e.HasIndex(em => em.DepartmentId);
            e.HasIndex(em => em.PositionId);
            e.HasOne(em => em.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(em => em.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(em => em.Position)
                .WithMany(p => p.Employees)
                .HasForeignKey(em => em.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(em => em.Manager)
                .WithMany()
                .HasForeignKey(em => em.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LeaveType>(e =>
        {
            e.ToTable("leave_types");
            e.Property(l => l.Code).HasMaxLength(50);
            e.Property(l => l.Name).HasMaxLength(100);
            e.HasIndex(l => l.Code).IsUnique();
        });

        modelBuilder.Entity<LeaveRequest>(e =>
        {
            e.ToTable("leave_requests");
            e.Property(l => l.Reason).HasMaxLength(1000);
            e.Property(l => l.Status).HasMaxLength(20);
            e.Property(l => l.Days).HasPrecision(5, 2);
            e.HasIndex(l => l.EmployeeId);
            e.HasIndex(l => l.Status);
            e.HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.LeaveType)
                .WithMany(t => t.Requests)
                .HasForeignKey(l => l.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Approver)
                .WithMany()
                .HasForeignKey(l => l.ApproverId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OvertimeRequest>(e =>
        {
            e.ToTable("overtime_requests");
            e.Property(o => o.Reason).HasMaxLength(1000);
            e.Property(o => o.Status).HasMaxLength(20);
            e.Property(o => o.Hours).HasPrecision(5, 2);
            e.HasIndex(o => o.EmployeeId);
            e.HasIndex(o => o.Status);
            e.HasOne(o => o.Employee)
                .WithMany()
                .HasForeignKey(o => o.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Approver)
                .WithMany()
                .HasForeignKey(o => o.ApproverId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AttendanceRecord>(e =>
        {
            e.ToTable("attendance_records");
            e.Property(a => a.Status).HasMaxLength(20);
            e.Property(a => a.WorkHours).HasPrecision(5, 2);
            e.HasIndex(a => a.EmployeeId);
            e.HasIndex(a => new { a.EmployeeId, a.WorkDate }).IsUnique();
            e.HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeSalary>(e =>
        {
            e.ToTable("employee_salaries");
            e.Property(s => s.BaseSalary).HasPrecision(18, 2);
            e.Property(s => s.PositionAllowance).HasPrecision(18, 2);
            e.Property(s => s.MealAllowance).HasPrecision(18, 2);
            e.HasIndex(s => s.EmployeeId).IsUnique();
            e.HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payroll>(e =>
        {
            e.ToTable("payrolls");
            e.Property(p => p.BaseAmount).HasPrecision(18, 2);
            e.Property(p => p.OvertimePay).HasPrecision(18, 2);
            e.Property(p => p.Bonus).HasPrecision(18, 2);
            e.Property(p => p.LeaveDeduction).HasPrecision(18, 2);
            e.Property(p => p.InsuranceDeduction).HasPrecision(18, 2);
            e.Property(p => p.TaxWithheld).HasPrecision(18, 2);
            e.Property(p => p.GrossPay).HasPrecision(18, 2);
            e.Property(p => p.NetPay).HasPrecision(18, 2);
            e.Property(p => p.Status).HasMaxLength(20);
            e.HasIndex(p => p.Period);
            e.HasIndex(p => new { p.Period, p.EmployeeId }).IsUnique();
            e.HasIndex(p => p.EmployeeId);
            e.HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.GeneratedByUser)
                .WithMany()
                .HasForeignKey(p => p.GeneratedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollSetting>(e =>
        {
            e.ToTable("payroll_settings");
            e.Property(s => s.Code).HasMaxLength(50);
            e.Property(s => s.Label).HasMaxLength(100);
            e.Property(s => s.Value).HasPrecision(18, 4);
            e.HasIndex(s => s.Code).IsUnique();
        });

        modelBuilder.Entity<Role>()
            .HasData(
                new Role { Id = 1, Code = "admin", Name = "系統管理員" },
                new Role { Id = 2, Code = "hr", Name = "人事" },
                new Role { Id = 3, Code = "manager", Name = "部門主管" },
                new Role { Id = 4, Code = "employee", Name = "一般員工" });
    }
}