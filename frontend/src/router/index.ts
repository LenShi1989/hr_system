import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    title?: string
    permission?: string
    public?: boolean
  }
}

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/auth/Login.vue'),
    meta: { title: '登入', public: true },
  },
  {
    path: '/',
    component: () => import('@/layouts/AdminLayout.vue'),
    children: [
      {
        path: '',
        name: 'dashboard',
        component: () => import('@/views/dashboard/Index.vue'),
        meta: { title: '儀表板', permission: 'dashboard.read' },
      },
      {
        path: 'organization/departments',
        name: 'departments',
        component: () => import('@/views/organization/Departments.vue'),
        meta: { title: '部門管理', permission: 'employee.read' },
      },
      {
        path: 'organization/positions',
        name: 'positions',
        component: () => import('@/views/organization/Positions.vue'),
        meta: { title: '職位管理', permission: 'employee.read' },
      },
      {
        path: 'organization/employees',
        name: 'employees',
        component: () => import('@/views/organization/Employees.vue'),
        meta: { title: '員工管理', permission: 'employee.read' },
      },
      {
        path: 'attendance/my',
        name: 'my-attendance',
        component: () => import('@/views/attendance/MyAttendance.vue'),
        meta: { title: '我的出勤', permission: 'attendance.self' },
      },
      {
        path: 'attendance/records',
        name: 'attendance-records',
        component: () => import('@/views/attendance/AttendanceRecords.vue'),
        meta: { title: '出勤記錄', permission: 'attendance.read' },
      },
      {
        path: 'leave/my',
        name: 'my-leave',
        component: () => import('@/views/attendance/MyLeave.vue'),
        meta: { title: '我的請假', permission: 'leave.request' },
      },
      {
        path: 'leave/review',
        name: 'leave-review',
        component: () => import('@/views/attendance/ReviewLeave.vue'),
        meta: { title: '請假審核', permission: 'leave.approve' },
      },
      {
        path: 'overtime/my',
        name: 'my-overtime',
        component: () => import('@/views/attendance/MyOvertime.vue'),
        meta: { title: '我的加班', permission: 'overtime.request' },
      },
      {
        path: 'overtime/review',
        name: 'overtime-review',
        component: () => import('@/views/attendance/ReviewOvertime.vue'),
        meta: { title: '加班審核', permission: 'overtime.approve' },
      },
      {
        path: 'payroll/payrolls',
        name: 'payrolls',
        component: () => import('@/views/payroll/Payrolls.vue'),
        meta: { title: '薪資單', permission: 'payroll.read' },
      },
      {
        path: 'payroll/salaries',
        name: 'payroll-salaries',
        component: () => import('@/views/payroll/EmployeeSalaries.vue'),
        meta: { title: '薪資結構', permission: 'payroll.manage' },
      },
      {
        path: 'system/users',
        name: 'users',
        component: () => import('@/views/system/Users.vue'),
        meta: { title: '使用者管理', permission: 'user.manage' },
      },
      {
        path: 'system/roles',
        name: 'roles',
        component: () => import('@/views/system/Roles.vue'),
        meta: { title: '角色權限', permission: 'user.manage' },
      },
      {
        path: 'system/audit-logs',
        name: 'audit-logs',
        component: () => import('@/views/system/AuditLogs.vue'),
        meta: { title: '操作紀錄', permission: 'audit.read' },
      },
    ],
  },
  {
    path: '/403',
    name: 'forbidden',
    component: () => import('@/views/system/Forbidden.vue'),
    meta: { title: '權限不足' },
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/',
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.meta.public) {
    return true
  }

  if (!auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  const permission = to.meta.permission
  if (permission && !auth.hasPermission(permission)) {
    return { name: 'forbidden' }
  }

  return true
})

export default router