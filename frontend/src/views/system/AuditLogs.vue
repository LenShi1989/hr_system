<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { actionLabels, categoryLabels, getAuditLogs, type AuditLog } from '@/services/audit'

const loading = ref(false)
const logs = ref<AuditLog[]>([])
const total = ref(0)

const query = reactive({
  page: 1,
  pageSize: 20,
  category: '',
  action: '',
  keyword: '',
  fromDate: '',
  toDate: '',
})

const totalPages = computed(() => Math.max(1, Math.ceil(total.value / query.pageSize)))

async function load() {
  loading.value = true
  try {
    const page = await getAuditLogs({
      page: query.page,
      pageSize: query.pageSize,
      category: query.category || undefined,
      action: query.action || undefined,
      keyword: query.keyword || undefined,
      fromDate: query.fromDate || undefined,
      toDate: query.toDate || undefined,
    })
    logs.value = page.items
    total.value = page.total
  } finally {
    loading.value = false
  }
}

function applyFilters() {
  query.page = 1
  load()
}

function goPage(p: number) {
  if (p < 1 || p > totalPages.value) return
  query.page = p
  load()
}

function fmtTime(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleString('zh-TW', { hour12: false })
}

function fmtDetail(a: AuditLog): string {
  if (!a.detail) return '-'
  try {
    const parsed = JSON.parse(a.detail) as Record<string, unknown>
    return Object.entries(parsed)
      .map(([k, v]) => `${k}: ${JSON.stringify(v)}`)
      .join('，')
  } catch {
    return a.detail
  }
}

onMounted(load)
</script>

<template>
  <section class="card">
    <h2>操作紀錄</h2>
    <div class="toolbar">
      <select v-model="query.category" class="btn" @change="applyFilters">
        <option value="">全部分類</option>
        <option v-for="(label, code) in categoryLabels" :key="code" :value="code">{{ label }}</option>
      </select>
      <select v-model="query.action" class="btn" @change="applyFilters">
        <option value="">全部動作</option>
        <option v-for="(label, code) in actionLabels" :key="code" :value="code">{{ label }}</option>
      </select>
      <input v-model="query.keyword" class="btn" style="width: 180px" placeholder="搜尋信箱/角色" @keyup.enter="applyFilters" />
      <input v-model="query.fromDate" class="btn" type="date" title="開始日期" @change="applyFilters" />
      <input v-model="query.toDate" class="btn" type="date" title="結束日期" @change="applyFilters" />
      <button class="btn" @click="applyFilters">查詢</button>
      <button class="btn" @click="load">重新整理</button>
    </div>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>時間</th>
            <th>使用者</th>
            <th>角色</th>
            <th>分類</th>
            <th>動作</th>
            <th>對象</th>
            <th>詳情</th>
            <th>IP</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in logs" :key="a.id">
            <td>{{ fmtTime(a.createdAt) }}</td>
            <td>
              {{ a.userEmail || '（未知）' }}
              <span v-if="a.userId === 0" class="tag tag-red">失敗</span>
            </td>
            <td>{{ a.role || '-' }}</td>
            <td>{{ categoryLabels[a.category] ?? a.category }}</td>
            <td>{{ actionLabels[a.action] ?? a.action }}</td>
            <td>{{ a.entity }}{{ a.entityId ? ` #${a.entityId}` : '' }}</td>
            <td class="detail-cell">{{ fmtDetail(a) }}</td>
            <td>{{ a.ipAddress ?? '-' }}</td>
          </tr>
          <tr v-if="logs.length === 0">
            <td colspan="8" class="muted">沒有符合的紀錄</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div class="pager">
      <span class="muted">共 {{ total }} 筆</span>
      <button class="btn" :disabled="query.page <= 1" @click="goPage(query.page - 1)">上一頁</button>
      <span>{{ query.page }} / {{ totalPages }}</span>
      <button class="btn" :disabled="query.page >= totalPages" @click="goPage(query.page + 1)">下一頁</button>
    </div>
  </section>
</template>

<style scoped>
.detail-cell {
  max-width: 320px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>