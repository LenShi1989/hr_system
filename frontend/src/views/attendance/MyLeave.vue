<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import {
  cancelLeaveRequest,
  createLeaveRequest,
  getLeaveTypes,
  getMyLeaveRequests,
  type LeaveRequest,
  type LeaveType,
} from '@/services/attendance'

const loading = ref(false)
const toast = ref('')
const leaveTypes = ref<LeaveType[]>([])
const items = ref<LeaveRequest[]>([])
const statusFilter = ref('')

const form = reactive({
  leaveTypeId: 0,
  startAt: '',
  endAt: '',
  reason: '',
})

const statusLabels: Record<string, string> = {
  pending: '待審核',
  approved: '核准',
  rejected: '駁回',
  cancelled: '已取消',
}
const statusTags: Record<string, string> = {
  pending: 'tag-amber',
  approved: 'tag-green',
  rejected: 'tag-red',
  cancelled: 'tag-off',
}

async function load() {
  loading.value = true
  try {
    const page = await getMyLeaveRequests({ page: 1, pageSize: 100, status: statusFilter.value || undefined })
    items.value = page.items
  } finally {
    loading.value = false
  }
}

async function submit() {
  if (!form.leaveTypeId || !form.startAt || !form.endAt || !form.reason.trim()) {
    showToast('請完整填寫假別、時間與事由')
    return
  }
  try {
    await createLeaveRequest({
      leaveTypeId: form.leaveTypeId,
      startAt: new Date(form.startAt).toISOString(),
      endAt: new Date(form.endAt).toISOString(),
      reason: form.reason.trim(),
    })
    showToast('請假申請已送出')
    Object.assign(form, { startAt: '', endAt: '', reason: '' })
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

async function cancel(r: LeaveRequest) {
  if (!window.confirm('確定取消這筆請假？')) return
  try {
    await cancelLeaveRequest(r.id)
    showToast('已取消')
    await load()
  } catch (error) {
    showToast(errorMessage(error))
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

function fmt(iso: string): string {
  return new Date(iso).toLocaleString('zh-TW', { hour12: false })
}

onMounted(async () => {
  leaveTypes.value = await getLeaveTypes()
  if (leaveTypes.value.length > 0) form.leaveTypeId = leaveTypes.value[0].id
  load()
})
</script>

<template>
  <section class="card">
    <h2>請假申請</h2>
    <div class="form-grid">
      <div class="field">
        <label>假別 *</label>
        <select v-model="form.leaveTypeId">
          <option v-for="t in leaveTypes" :key="t.id" :value="t.id">
            {{ t.name }}{{ t.isPaid ? '（有薪）' : '' }}
          </option>
        </select>
      </div>
      <div class="field">
        <label>開始時間 *</label>
        <input v-model="form.startAt" type="datetime-local" />
      </div>
      <div class="field">
        <label>結束時間 *</label>
        <input v-model="form.endAt" type="datetime-local" />
      </div>
      <div class="field field-full">
        <label>事由 *</label>
        <textarea v-model="form.reason" rows="2" placeholder="請填寫請假事由" />
      </div>
    </div>
    <div class="toolbar" style="margin-top: 12px">
      <div class="spacer"></div>
      <button class="btn btn-primary" @click="submit">送出申請</button>
    </div>
  </section>

  <section class="card">
    <h3>我的請假記錄</h3>
    <div class="toolbar">
      <select v-model="statusFilter" class="btn" @change="load">
        <option value="">全部狀態</option>
        <option value="pending">待審核</option>
        <option value="approved">核准</option>
        <option value="rejected">駁回</option>
        <option value="cancelled">已取消</option>
      </select>
      <button class="btn" @click="load">查詢</button>
    </div>
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>假別</th>
            <th>開始</th>
            <th>結束</th>
            <th>天數</th>
            <th>事由</th>
            <th>狀態</th>
            <th>審核人</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in items" :key="r.id">
            <td>{{ r.leaveTypeName }}</td>
            <td>{{ fmt(r.startAt) }}</td>
            <td>{{ fmt(r.endAt) }}</td>
            <td>{{ r.days }}</td>
            <td>{{ r.reason }}</td>
            <td>
              <span class="tag" :class="statusTags[r.status] ?? 'tag'">{{ statusLabels[r.status] }}</span>
            </td>
            <td>{{ r.approverName ?? '-' }}</td>
            <td>
              <button v-if="r.status === 'pending'" class="btn btn-danger" @click="cancel(r)">取消</button>
              <span v-else class="muted">-</span>
            </td>
          </tr>
          <tr v-if="!loading && items.length === 0">
            <td colspan="8" class="muted">尚無請假記錄</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>

  <Transition name="toast">
    <div v-if="toast" class="toast">{{ toast }}</div>
  </Transition>
</template>