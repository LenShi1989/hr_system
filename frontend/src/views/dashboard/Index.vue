<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import Chart from 'chart.js/auto'
import type { ChartOptions, TooltipItem } from 'chart.js'
import { getDashboardSummary, type DashboardSummary } from '@/services/dashboard'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()

const loading = ref(true)
const error = ref('')
const stats = ref<DashboardSummary | null>(null)

const deptCanvas = ref<HTMLCanvasElement | null>(null)
const posCanvas = ref<HTMLCanvasElement | null>(null)
const trendCanvas = ref<HTMLCanvasElement | null>(null)
const statusCanvas = ref<HTMLCanvasElement | null>(null)

const charts: Array<{ destroy(): void }> = []

function destroyCharts() {
  charts.forEach((c) => c.destroy())
  charts.length = 0
}

const PALETTE = ['#2563eb', '#10b981', '#f59e0b', '#8b5cf6', '#ef4444', '#06b6d4', '#ec4899', '#84cc16']

const STATUS_LABELS: Record<string, string> = {
  draft: '草稿',
  confirmed: '已確認',
  paid: '已發放',
  cancelled: '已取消',
}

const TODAY_LABELS: Record<string, string> = {
  not_clocked: '尚未打卡',
  clocked_in: '已打卡上班',
  clocked_out: '已打卡下班',
}

function money(n: number): string {
  return n.toLocaleString('zh-TW', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function todayClass(status: string): string {
  if (status === 'clocked_out') return 'tag-green'
  if (status === 'clocked_in') return 'tag-amber'
  return 'tag-off'
}

async function load() {
  loading.value = true
  try {
    stats.value = await getDashboardSummary()
    error.value = ''
    await nextTick()
    renderCharts()
  } catch (err: unknown) {
    const e = err as { response?: { data?: { error?: { message?: string } } } }
    error.value = e.response?.data?.error?.message ?? '載入儀表板失敗'
  } finally {
    loading.value = false
  }
}

function renderCharts() {
  destroyCharts()
  const s = stats.value
  if (!s) return

  if (deptCanvas.value && s.employees) {
    charts.push(
      new Chart(deptCanvas.value, {
        type: 'bar',
        data: {
          labels: s.employees.byDepartment.map((x) => x.name),
          datasets: [
            {
              label: '人數',
              data: s.employees.byDepartment.map((x) => x.count),
              backgroundColor: PALETTE,
            },
          ],
        },
        options: baseOptions('筆'),
      }),
    )
  }

  if (posCanvas.value && s.employees) {
    charts.push(
      new Chart(posCanvas.value, {
        type: 'doughnut',
        data: {
          labels: s.employees.byPosition.map((x) => x.name),
          datasets: [
            {
              data: s.employees.byPosition.map((x) => x.count),
              backgroundColor: PALETTE,
            },
          ],
        },
        options: baseOptions('人'),
      }),
    )
  }

  if (trendCanvas.value && s.payroll) {
    const labels = s.payroll.trend.map((x) => `${x.period.slice(0, 4)}/${x.period.slice(4)}`)
    charts.push(
      new Chart(trendCanvas.value, {
        type: 'line',
        data: {
          labels,
          datasets: [
            {
              label: '實發金額',
              data: s.payroll.trend.map((x) => x.netPay),
              borderColor: '#2563eb',
              backgroundColor: 'rgba(37, 99, 235, 0.15)',
              fill: true,
              tension: 0.3,
              pointRadius: 4,
            },
          ],
        },
        options: baseOptions('元'),
      }),
    )
  }

  if (statusCanvas.value && s.payroll) {
    charts.push(
      new Chart(statusCanvas.value, {
        type: 'doughnut',
        data: {
          labels: s.payroll.byStatus.map((x) => STATUS_LABELS[x.status] ?? x.status),
          datasets: [
            {
              data: s.payroll.byStatus.map((x) => x.count),
              backgroundColor: PALETTE,
            },
          ],
        },
        options: baseOptions('筆'),
      }),
    )
  }
}

function baseOptions(valueLabel: string): ChartOptions<'bar' | 'line' | 'doughnut'> {
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (ctx: TooltipItem<'bar' | 'line' | 'doughnut'>) => ` ${valueLabel} ${ctx.formattedValue}`,
        },
      },
    },
    scales: {
      x: { grid: { display: false } },
      y: { beginAtZero: true, ticks: { precision: 0 } },
    },
  }
}

onMounted(load)
onBeforeUnmount(destroyCharts)
</script>

<template>
  <section class="card dash-head">
    <div>
      <h2>儀表板</h2>
      <p class="muted">登入成功，歡迎 {{ auth.user?.email }}（角色：{{ auth.user?.roleCode }}）</p>
    </div>
    <button class="btn" :disabled="loading" @click="load">重新整理</button>
  </section>

  <p v-if="loading" class="muted">載入中…</p>
  <p v-else-if="error" class="error">{{ error }}</p>

  <template v-else-if="stats">
    <section class="stat-grid">
      <div v-if="stats.employees" class="stat-card">
        <span class="stat-value">{{ stats.employees.total }}</span>
        <span class="stat-label">員工總數</span>
      </div>
      <div v-if="stats.payroll" class="stat-card">
        <span class="stat-value">{{ money(stats.payroll.currentMonthNetPay) }}</span>
        <span class="stat-label">本月薪資實發</span>
      </div>
      <div v-if="stats.attendance" class="stat-card">
        <span class="stat-value">{{ stats.attendance.todayClockedIn }} / {{ stats.attendance.todayClockedOut }}</span>
        <span class="stat-label">今日已打卡 / 已下班</span>
      </div>
      <div v-if="stats.pending" class="stat-card">
        <span class="stat-value">{{ stats.pending.leave + stats.pending.overtime }}</span>
        <span class="stat-label">待審核申請</span>
      </div>
    </section>

    <section v-if="stats.employees" class="chart-grid">
      <div class="card chart-card">
        <h3>各部門人數</h3>
        <div class="chart-box"><canvas ref="deptCanvas"></canvas></div>
      </div>
      <div class="card chart-card">
        <h3>各職位人數</h3>
        <div class="chart-box"><canvas ref="posCanvas"></canvas></div>
      </div>
    </section>

    <section v-if="stats.payroll" class="chart-grid">
      <div class="card chart-card">
        <h3>近六個月實發薪資</h3>
        <div class="chart-box"><canvas ref="trendCanvas"></canvas></div>
      </div>
      <div class="card chart-card">
        <h3>本月薪資狀態</h3>
        <div class="chart-box"><canvas ref="statusCanvas"></canvas></div>
      </div>
    </section>

    <section v-if="stats.pending" class="card">
      <h3>待審核申請</h3>
      <div class="stat-grid">
        <div class="stat-card">
          <span class="stat-value">{{ stats.pending.leave }}</span>
          <span class="stat-label">待審請假</span>
        </div>
        <div class="stat-card">
          <span class="stat-value">{{ stats.pending.overtime }}</span>
          <span class="stat-label">待審加班</span>
        </div>
      </div>
    </section>

    <section v-if="stats.my" class="card">
      <h3>我的本月概況</h3>
      <div class="stat-grid">
        <div class="stat-card">
          <span class="stat-value tag" :class="todayClass(stats.my.todayStatus)">
            {{ TODAY_LABELS[stats.my.todayStatus] ?? stats.my.todayStatus }}
          </span>
          <span class="stat-label">今日狀態</span>
        </div>
        <div class="stat-card">
          <span class="stat-value">{{ stats.my.monthWorkDays }}</span>
          <span class="stat-label">本月出勤天數</span>
        </div>
        <div class="stat-card">
          <span class="stat-value">{{ money(stats.my.monthWorkHours) }}</span>
          <span class="stat-label">本月總工時(時)</span>
        </div>
        <div class="stat-card">
          <span class="stat-value">{{ money(stats.my.monthOvertimeHours) }}</span>
          <span class="stat-label">本月加班核准(時)</span>
        </div>
        <div class="stat-card">
          <span class="stat-value">{{ stats.my.pendingLeave }}</span>
          <span class="stat-label">我的待審請假</span>
        </div>
        <div class="stat-card">
          <span class="stat-value">{{ stats.my.pendingOvertime }}</span>
          <span class="stat-label">我的待審加班</span>
        </div>
      </div>
    </section>
  </template>
</template>