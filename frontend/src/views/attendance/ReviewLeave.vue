<script setup lang="ts">
import { onMounted, ref } from 'vue'
import {
  getMyLeaveRequests,
  reviewLeaveRequest,
  type LeaveRequest,
} from '@/services/attendance'

const loading = ref(false)
const toast = ref('')
const items = ref<LeaveRequest[]>([])

const statusLabels: Record<string, string> = {
  pending: '待審核',
  approved: '核准',
  rejected: '駁回',
}
const statusTags: Record<string, string> = {
  pending: 'tag-amber',
  approved: 'tag-green',
  rejected: 'tag-red',
}

async function load() {
  loading.value = true
  try {
    const page = await getMyLeaveRequests({ all: true, page: 1, pageSize: 100, status: 'pending' })
    items.value = page.items
  } finally {
    loading.value = false
  }
}

async function review(r: LeaveRequest, action: 'approve' | 'reject') {
  const comment = action === 'reject' ? (window.prompt('請填寫駁回原因', '') ?? '') : ''
  if (action === 'reject' && !comment) {
    showToast('駁回必須填寫原因')
    return
  }
  if (!window.confirm(`確定${action === 'approve' ? '核准' : '駁回'}「${r.employeeName}」的請假？`)) return
  try {
    await reviewLeaveRequest(r.id, action, comment)
    showToast(`${action === 'approve' ? '核准' : '駁回'}完成`)
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

onMounted(load)
</script>

<template>
  <section class="card">
    <h2>請假審核</h2>
    <p class="muted">待審核的請假申請（HR/主管可查看與審核職權範圍內申請）</p>
    <div class="toolbar">
      <button class="btn" @click="load">重新整理</button>
    </div>
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>工號</th>
            <th>姓名</th>
            <th>假別</th>
            <th>開始</th>
            <th>結束</th>
            <th>天數</th>
            <th>事由</th>
            <th>狀態</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in items" :key="r.id">
            <td>{{ r.employeeNo }}</td>
            <td>{{ r.employeeName }}</td>
            <td>{{ r.leaveTypeName }}</td>
            <td>{{ fmt(r.startAt) }}</td>
            <td>{{ fmt(r.endAt) }}</td>
            <td>{{ r.days }}</td>
            <td>{{ r.reason }}</td>
            <td>
              <span class="tag" :class="statusTags[r.status] ?? 'tag'">{{ statusLabels[r.status] }}</span>
            </td>
            <td>
              <button class="btn btn-primary" @click="review(r, 'approve')">核准</button>
              <button class="btn btn-danger" @click="review(r, 'reject')">駁回</button>
            </td>
          </tr>
          <tr v-if="!loading && items.length === 0">
            <td colspan="9" class="muted">目前沒有待審核的請假申請</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>

  <Transition name="toast">
    <div v-if="toast" class="toast">{{ toast }}</div>
  </Transition>
</template>