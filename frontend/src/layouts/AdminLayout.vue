<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()

const menuGroups = computed(() => {
  const groups: Array<{
    title: string
    items: Array<{ label: string; to: string; icon: string }>
  }> = []

  if (auth.hasPermission('dashboard.read')) {
    groups.push({
      title: '',
      items: [{ label: '儀表板', to: '/', icon: '📊' }],
    })
  }

  if (auth.hasPermission('employee.read')) {
    groups.push({
      title: '組織員工',
      items: [
        { label: '部門', to: '/organization/departments', icon: '🏢' },
        { label: '職位', to: '/organization/positions', icon: '🛠️' },
        { label: '員工', to: '/organization/employees', icon: '👥' },
      ],
    })
  }

  const attendanceItems: Array<{ label: string; to: string; icon: string }> = []
  if (auth.hasPermission('attendance.self')) {
    attendanceItems.push({ label: '我的出勤', to: '/attendance/my', icon: '🕘' })
  }
  if (auth.hasPermission('attendance.read')) {
    attendanceItems.push({ label: '出勤記錄', to: '/attendance/records', icon: '📅' })
  }
  if (auth.hasPermission('leave.request')) {
    attendanceItems.push({ label: '我的請假', to: '/leave/my', icon: '🏖️' })
  }
  if (auth.hasPermission('leave.approve')) {
    attendanceItems.push({ label: '請假審核', to: '/leave/review', icon: '✅' })
  }
  if (auth.hasPermission('overtime.request')) {
    attendanceItems.push({ label: '我的加班', to: '/overtime/my', icon: '🌙' })
  }
  if (auth.hasPermission('overtime.approve')) {
    attendanceItems.push({ label: '加班審核', to: '/overtime/review', icon: '🚦' })
  }
  if (attendanceItems.length > 0) {
    groups.push({ title: '出勤請假', items: attendanceItems })
  }

  const payrollItems: Array<{ label: string; to: string; icon: string }> = []
  if (auth.hasPermission('payroll.read')) {
    payrollItems.push({ label: '薪資單', to: '/payroll/payrolls', icon: '💵' })
  }
  if (auth.hasPermission('payroll.manage')) {
    payrollItems.push({ label: '薪資結構', to: '/payroll/salaries', icon: '⚙️' })
  }
  if (payrollItems.length > 0) {
    groups.push({ title: '薪資', items: payrollItems })
  }

  const systemItems: Array<{ label: string; to: string; icon: string }> = []
  if (auth.hasPermission('user.manage')) {
    systemItems.push({ label: '使用者', to: '/system/users', icon: '👤' })
    systemItems.push({ label: '角色權限', to: '/system/roles', icon: '🎫' })
  }
  if (auth.hasPermission('audit.read')) {
    systemItems.push({ label: '操作紀錄', to: '/system/audit-logs', icon: '📜' })
  }
  if (systemItems.length > 0) {
    groups.push({ title: '系統', items: systemItems })
  }

  return groups
})

function onLogout() {
  auth.logout()
}
</script>

<template>
  <div class="layout">
    <aside class="sidebar">
      <div class="brand">HR 系統</div>
      <nav>
        <div v-for="group in menuGroups" :key="group.title" class="menu-group">
          <p v-if="group.title" class="menu-title">{{ group.title }}</p>
          <router-link
            v-for="item in group.items"
            :key="item.to"
            :to="item.to"
            class="nav-item"
          >
            <span class="nav-icon">{{ item.icon }}</span>
            <span>{{ item.label }}</span>
          </router-link>
        </div>
      </nav>
    </aside>
    <div class="main">
      <header class="header">
        <div class="header-info">
          <span>{{ auth.user?.email }}</span>
          <span class="badge">{{ auth.user?.roleCode }}</span>
        </div>
        <button @click="onLogout">登出</button>
      </header>
      <main class="content">
        <router-view />
      </main>
    </div>
  </div>
</template>