using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HrSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "PermissionCode", "RoleId" },
                values: new object[,]
                {
                    { "attendance.read", 1L },
                    { "attendance.self", 1L },
                    { "audit.read", 1L },
                    { "dashboard.read", 1L },
                    { "employee.manage", 1L },
                    { "employee.read", 1L },
                    { "leave.approve", 1L },
                    { "leave.request", 1L },
                    { "overtime.approve", 1L },
                    { "overtime.request", 1L },
                    { "payroll.manage", 1L },
                    { "payroll.read", 1L },
                    { "user.manage", 1L },
                    { "attendance.read", 2L },
                    { "attendance.self", 2L },
                    { "dashboard.read", 2L },
                    { "employee.manage", 2L },
                    { "employee.read", 2L },
                    { "leave.approve", 2L },
                    { "leave.request", 2L },
                    { "overtime.approve", 2L },
                    { "overtime.request", 2L },
                    { "payroll.manage", 2L },
                    { "payroll.read", 2L },
                    { "attendance.read", 3L },
                    { "attendance.self", 3L },
                    { "dashboard.read", 3L },
                    { "employee.read", 3L },
                    { "leave.approve", 3L },
                    { "leave.request", 3L },
                    { "overtime.approve", 3L },
                    { "overtime.request", 3L },
                    { "payroll.read", 3L },
                    { "attendance.self", 4L },
                    { "dashboard.read", 4L },
                    { "leave.request", 4L },
                    { "overtime.request", 4L },
                    { "payroll.read", 4L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "attendance.read", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "attendance.self", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "audit.read", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "dashboard.read", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "employee.manage", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "employee.read", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "leave.approve", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "leave.request", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "overtime.approve", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "overtime.request", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "payroll.manage", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "payroll.read", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "user.manage", 1L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "attendance.read", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "attendance.self", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "dashboard.read", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "employee.manage", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "employee.read", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "leave.approve", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "leave.request", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "overtime.approve", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "overtime.request", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "payroll.manage", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "payroll.read", 2L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "attendance.read", 3L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "attendance.self", 3L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "dashboard.read", 3L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "employee.read", 3L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "leave.approve", 3L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "leave.request", 3L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "overtime.approve", 3L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "overtime.request", 3L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "payroll.read", 3L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "attendance.self", 4L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "dashboard.read", 4L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "leave.request", 4L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "overtime.request", 4L });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "PermissionCode", "RoleId" },
                keyValues: new object[] { "payroll.read", 4L });
        }
    }
}
