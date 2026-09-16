<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  clockIn,
  clockOut,
  getMyAttendance,
  type AttendanceRecord,
} from '@/services/attendance'

const today = new Date()
const todayStr = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`

const loading = ref(false)
const toast = ref('')
const records = ref<AttendanceRecord[]>([])
const busy = ref(false)

const todayRecord = computed(() => records.value.find((r) => r.workDate === todayStr) ?? null)
const canClockIn = computed(() => !todayRecord.value || !todayRecord.value.clockInAt)
const canClockOut = computed(() => todayRecord.value?.clockInAt && !todayRecord.value.clockOutAt)

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

const display = (r: AttendanceRecord) => {
  if (r.status !== 'normal') {
    const parts = [statusLabels[r.status]]
    if (r.lateMinutes > 0) parts.push(`遲到 ${r.lateMinutes} 分`)
    if (r.earlyLeaveMinutes > 0) parts.push(`早退 ${r.earlyLeaveMinutes} 分`)
    return parts.join(' ')
  }
  return statusLabels.normal
}

async function load() {
  loading.value = true
  try {
    records.value = await getMyAttendance()
  } finally {
    loading.value = false
  }
}

async function doClockIn() {
  busy.value = true
  try {
    const res = await clockIn()
    showToast(res.message || '上班打卡成功')
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  } finally {
    busy.value = false
  }
}

async function doClockOut() {
  busy.value = true
  try {
    const res = await clockOut()
    showToast(res.message || '下班打卡成功')
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  } finally {
    busy.value = false
  }
}

function showToast(msg: string) {
  toast.value = msg
  setTimeout(() => (toast.value = ''), 2500)
}

function errorMessage(error: unknown): string {
  const err = error as { response?: { data?: { error?: { message?: string } } } }
  return err.response?.data?.error?.message ?? '操作失敗'
}

function fmt(iso: string | null): string {
  if (!iso) return '-'
  return new Date(iso).toLocaleString('zh-TW', { hour12: false })
}

function fmtDate(d: string): string {
  return d
}

onMounted(load)
</script>

<template>
  <section class="card">
    <h2>我的出勤</h2>
    <div class="toolbar">
      <p class="muted">上班時間以系統排班為準（預設 09:00–18:00）</p>
      <div class="spacer"></div>
      <button class="btn btn-primary" :disabled="!canClockIn || busy" @click="doClockIn">上班打卡</button>
      <button class="btn" :disabled="!canClockOut || busy" @click="doClockOut">下班打卡</button>
    </div>

    <div v-if="todayRecord" class="summary">
      <span class="tag" :class="statusTags[todayRecord.status] ?? 'tag'">
        {{ todayRecord.clockInAt ? `上班 ${fmt(todayRecord.clockInAt)}` : '未打卡' }}
      </span>
      <span class="tag" :class="statusTags[todayRecord.status] ?? 'tag'">
        {{ todayRecord.clockOutAt ? `下班 ${fmt(todayRecord.clockOutAt)}` : '未打卡' }}
      </span>
      <span v-if="todayRecord.clockOutAt" class="tag">工時 {{ todayRecord.workHours }} 小時</span>
      <span v-if="todayRecord.clockOutAt && todayRecord.status !== 'normal'" class="tag tag-amber">{{ display(todayRecord) }}</span>
    </div>
    <p v-else class="muted">今天尚未打卡。</p>
  </section>

  <section class="card">
    <h3>出勤記錄</h3>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>日期</th>
            <th>上班</th>
            <th>下班</th>
            <th>工時</th>
            <th>狀態</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in records" :key="r.id">
            <td>{{ fmtDate(r.workDate) }}</td>
            <td>{{ fmt(r.clockInAt) }}</td>
            <td>{{ fmt(r.clockOutAt) }}</td>
            <td>{{ r.clockOutAt ? `${r.workHours} 小時` : '-' }}</td>
            <td>
              <span class="tag" :class="statusTags[r.status] ?? 'tag'">{{ display(r) }}</span>
            </td>
          </tr>
          <tr v-if="records.length === 0">
            <td colspan="5" class="muted">尚無出勤記錄</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>

  <Transition name="toast">
    <div v-if="toast" class="toast">{{ toast }}</div>
  </Transition>
</template>

<style scoped>
.summary {
  display: flex;
  gap: 8px;
  margin-bottom: 8px;
  flex-wrap: wrap;
}
</style>