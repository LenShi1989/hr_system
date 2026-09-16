<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import {
  cancelOvertimeRequest,
  createOvertimeRequest,
  getMyOvertimeRequests,
  type OvertimeRequest,
} from '@/services/attendance'

const loading = ref(false)
const toast = ref('')
const items = ref<OvertimeRequest[]>([])
const statusFilter = ref('')

const form = reactive({
  workDate: '',
  startAt: '',
  endAt: '',
  reason: '',
})

const today = new Date()
const todayStr = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`

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
    const page = await getMyOvertimeRequests({ page: 1, pageSize: 100, status: statusFilter.value || undefined })
    items.value = page.items
  } finally {
    loading.value = false
  }
}

async function submit() {
  if (!form.workDate || !form.startAt || !form.endAt || !form.reason.trim()) {
    showToast('請完整填寫日期、時間與事由')
    return
  }
  try {
    await createOvertimeRequest({
      workDate: form.workDate,
      startAt: new Date(`${form.workDate}T${form.startAt}`).toISOString(),
      endAt: new Date(`${form.workDate}T${form.endAt}`).toISOString(),
      reason: form.reason.trim(),
    })
    showToast('加班申請已送出')
    Object.assign(form, { startAt: '', endAt: '', reason: '' })
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

async function cancel(r: OvertimeRequest) {
  if (!window.confirm('確定取消這筆加班？')) return
  try {
    await cancelOvertimeRequest(r.id)
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

onMounted(() => {
  form.workDate = todayStr
  load()
})
</script>

<template>
  <section class="card">
    <h2>加班申請</h2>
    <div class="form-grid">
      <div class="field">
        <label>加班日期 *</label>
        <input v-model="form.workDate" type="date" />
      </div>
      <div class="field">
        <label>開始時間 *</label>
        <input v-model="form.startAt" type="time" />
      </div>
      <div class="field">
        <label>結束時間 *</label>
        <input v-model="form.endAt" type="time" />
      </div>
      <div class="field field-full">
        <label>事由 *</label>
        <textarea v-model="form.reason" rows="2" placeholder="請填寫加班事由" />
      </div>
    </div>
    <div class="toolbar" style="margin-top: 12px">
      <div class="spacer"></div>
      <button class="btn btn-primary" @click="submit">送出申請</button>
    </div>
  </section>

  <section class="card">
    <h3>我的加班記錄</h3>
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
            <th>日期</th>
            <th>開始</th>
            <th>結束</th>
            <th>時數</th>
            <th>事由</th>
            <th>狀態</th>
            <th>審核人</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in items" :key="r.id">
            <td>{{ r.workDate }}</td>
            <td>{{ fmt(r.startAt) }}</td>
            <td>{{ fmt(r.endAt) }}</td>
            <td>{{ r.hours }}</td>
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
            <td colspan="8" class="muted">尚無加班記錄</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>

  <Transition name="toast">
    <div v-if="toast" class="toast">{{ toast }}</div>
  </Transition>
</template>