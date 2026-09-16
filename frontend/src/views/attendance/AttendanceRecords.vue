<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { getAttendanceRecords, type AttendanceRecord } from '@/services/attendance'
import { getEmployees, type Employee } from '@/services/organization'

const loading = ref(false)
const records = ref<AttendanceRecord[]>([])
const employeesOpts = ref<Employee[]>([])
const canFilterEmployee = ref(false)

const query = reactive({
  fromDate: '',
  toDate: '',
  employeeId: null as number | null,
})

const statusLabels: Record<string, string> = {
  normal: '正常',
  late: '遲到',
  early_leave: '早退',
}
const statusTags: Record<string, string> = {
  normal: 'tag-green',
  late: 'tag-amber',
  early_leave: 'tag-amber',
}

async function load() {
  loading.value = true
  try {
    records.value = await getAttendanceRecords(
      query.fromDate || undefined,
      query.toDate || undefined,
      query.employeeId ?? undefined,
    )
  } finally {
    loading.value = false
  }
}

function fmt(iso: string | null): string {
  if (!iso) return '-'
  return new Date(iso).toLocaleString('zh-TW', { hour12: false })
}

function display(r: AttendanceRecord) {
  if (r.status !== 'normal') {
    const parts = [statusLabels[r.status]]
    if (r.lateMinutes > 0) parts.push(`遲到 ${r.lateMinutes} 分`)
    if (r.earlyLeaveMinutes > 0) parts.push(`早退 ${r.earlyLeaveMinutes} 分`)
    return parts.join(' ')
  }
  return statusLabels.normal
}

onMounted(async () => {
  load()
  try {
    const page = await getEmployees({ page: 1, pageSize: 100 })
    employeesOpts.value = page.items
    canFilterEmployee.value = true
  } catch {
    canFilterEmployee.value = false
  }
})
</script>

<template>
  <section class="card">
    <h2>出勤記錄</h2>
    <div class="toolbar">
      <label class="muted">從 <input v-model="query.fromDate" type="date" @change="load" /></label>
      <label class="muted">到 <input v-model="query.toDate" type="date" @change="load" /></label>
      <select v-if="canFilterEmployee" v-model="query.employeeId" class="btn" @change="load">
        <option :value="null">全部員工</option>
        <option v-for="e in employeesOpts" :key="e.id" :value="e.id">{{ e.name }}（{{ e.employeeNo }}）</option>
      </select>
      <button class="btn" @click="load">查詢</button>
      <div class="spacer"></div>
    </div>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>工號</th>
            <th>姓名</th>
            <th>日期</th>
            <th>上班</th>
            <th>下班</th>
            <th>工時</th>
            <th>狀態</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in records" :key="r.id">
            <td>{{ r.employeeNo }}</td>
            <td>{{ r.employeeName }}</td>
            <td>{{ r.workDate }}</td>
            <td>{{ fmt(r.clockInAt) }}</td>
            <td>{{ fmt(r.clockOutAt) }}</td>
            <td>{{ r.clockOutAt ? `${r.workHours} 小時` : '-' }}</td>
            <td>
              <span class="tag" :class="statusTags[r.status] ?? 'tag'">{{ display(r) }}</span>
            </td>
          </tr>
          <tr v-if="records.length === 0">
            <td colspan="7" class="muted">尚無出勤記錄</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>