<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import Modal from '@/components/Modal.vue'
import { useAuthStore } from '@/stores/auth'
import {
  confirmPayroll,
  generatePayroll,
  getPayroll,
  getPayrolls,
  payPayroll,
  setPayrollBonus,
  type Payroll,
  type PayrollQuery,
} from '@/services/payroll'
import { getEmployees, type Employee } from '@/services/organization'

const auth = useAuthStore()
const canManage = auth.hasPermission('payroll.manage')

const loading = ref(false)
const toast = ref('')
const items = ref<Payroll[]>([])
const total = ref(0)
const employeesOpts = ref<Employee[]>([])
const showEmployeeFilter = ref(false)

const now = new Date()
const defaultMonth = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`

const query = reactive({
  page: 1,
  pageSize: 20,
  periodMonth: defaultMonth,
  employeeId: null as number | null,
})

const statusLabels: Record<string, string> = {
  draft: '草稿',
  confirmed: '已確認',
  paid: '已發放',
}
const statusTags: Record<string, string> = {
  draft: 'tag-amber',
  confirmed: 'tag-blue',
  paid: 'tag-green',
}

const periodNumber = computed(() => {
  const v = query.periodMonth.replace('-', '')
  return v.length === 6 ? Number(v) : null
})

const totalPages = computed(() => Math.max(1, Math.ceil(total.value / query.pageSize)))

function errorMessage(error: unknown): string {
  const err = error as { response?: { data?: { error?: { message?: string } } } }
  return err.response?.data?.error?.message ?? '操作失敗'
}

function showToast(msg: string) {
  toast.value = msg
  setTimeout(() => (toast.value = ''), 2500)
}

async function load() {
  loading.value = true
  try {
    const p: PayrollQuery = {
      page: query.page,
      pageSize: query.pageSize,
      period: periodNumber.value ?? undefined,
      employeeId: query.employeeId ?? undefined,
    }
    const page = await getPayrolls(p)
    items.value = page.items
    total.value = page.total
  } catch (error) {
    showToast(errorMessage(error))
  } finally {
    loading.value = false
  }
}

function goPage(p: number) {
  if (p < 1 || p > totalPages.value) return
  query.page = p
  load()
}

async function generate() {
  const period = periodNumber.value
  if (!period) {
    showToast('請先選擇月份')
    return
  }
  try {
    const res = await generatePayroll(period)
    showToast(`已生成 ${res.generated} 筆（略過 ${res.skipped}）`)
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

async function confirm(r: Payroll) {
  if (!window.confirm(`確定確認「${r.employeeName} (${r.employeeNo})」${r.period} 薪資單？`)) return
  try {
    await confirmPayroll(r.id)
    showToast('已確認')
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

async function pay(r: Payroll) {
  if (!window.confirm(`確定發放「${r.employeeName} (${r.employeeNo})」${r.period} 薪資${r.netPay} 元？`)) return
  try {
    await payPayroll(r.id)
    showToast('已發放')
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

async function bonus(r: Payroll) {
  const input = window.prompt(`調整「${r.employeeName}」${r.period} 獎金（目前 ${r.bonus}）`, String(r.bonus))
  if (input === null) return
  const amount = Number(input)
  if (!Number.isFinite(amount) || amount < 0) {
    showToast('獎金請輸入非負數字')
    return
  }
  try {
    await setPayrollBonus(r.id, amount)
    showToast('獎金已更新')
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

const detail = reactive({ open: false, loading: false, p: null as Payroll | null })

async function openDetail(r: Payroll) {
  detail.open = true
  detail.p = r
  detail.loading = true
  try {
    detail.p = await getPayroll(r.id)
  } catch (error) {
    showToast(errorMessage(error))
  } finally {
    detail.loading = false
  }
}

const fmt = (iso: string | null) => (iso ? new Date(iso).toLocaleString('zh-TW', { hour12: false }) : '-')
const money = (n: number) => n.toLocaleString('zh-TW', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

onMounted(async () => {
  load()
  try {
    const page = await getEmployees({ page: 1, pageSize: 100 })
    employeesOpts.value = page.items
    showEmployeeFilter.value = true
  } catch {
    showEmployeeFilter.value = false
  }
})
</script>

<template>
  <section class="card">
    <h2>薪資單</h2>
    <div class="toolbar">
      <input v-model="query.periodMonth" type="month" class="btn" @change="load" />
      <select v-if="showEmployeeFilter" v-model="query.employeeId" class="btn" @change="load">
        <option :value="null">全部員工</option>
        <option v-for="e in employeesOpts" :key="e.id" :value="e.id">{{ e.name }}（{{ e.employeeNo }}）</option>
      </select>
      <button class="btn" @click="load">查詢</button>
      <div class="spacer"></div>
      <button v-if="canManage" class="btn btn-primary" :disabled="!periodNumber" @click="generate">
        生成 {{ query.periodMonth }} 月結
      </button>
    </div>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>月份</th>
            <th>工號</th>
            <th>姓名</th>
            <th>應發</th>
            <th>加班費</th>
            <th>獎金</th>
            <th>請假扣款</th>
            <th>勞健保</th>
            <th>稅</th>
            <th>實發</th>
            <th>狀態</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in items" :key="r.id">
            <td>{{ r.period }}</td>
            <td>{{ r.employeeNo }}</td>
            <td>{{ r.employeeName }}</td>
            <td>{{ money(r.baseAmount) }}</td>
            <td>{{ money(r.overtimePay) }}</td>
            <td>{{ money(r.bonus) }}</td>
            <td>{{ money(r.leaveDeduction) }}</td>
            <td>{{ money(r.insuranceDeduction) }}</td>
            <td>{{ money(r.taxWithheld) }}</td>
            <td><strong>{{ money(r.netPay) }}</strong></td>
            <td>
              <span class="tag" :class="statusTags[r.status] ?? 'tag'">{{ statusLabels[r.status] }}</span>
            </td>
            <td>
              <button class="btn" @click="openDetail(r)">明細</button>
              <template v-if="canManage">
                <button v-if="r.status === 'draft'" class="btn" @click="bonus(r)">獎金</button>
                <button v-if="r.status === 'draft'" class="btn btn-primary" @click="confirm(r)">確認</button>
                <button v-if="r.status === 'confirmed'" class="btn btn-primary" @click="pay(r)">發放</button>
              </template>
            </td>
          </tr>
          <tr v-if="items.length === 0">
            <td colspan="12" class="muted">此月份暫無薪資單（HR 可點「生成」）</td>
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

  <Modal :open="detail.open" title="薪資單明細" @close="detail.open = false">
    <div v-if="detail.p" class="detail-grid">
      <div class="field"><label>月份</label><span>{{ detail.p.period }}</span></div>
      <div class="field"><label>員工</label><span>{{ detail.p.employeeName }}（{{ detail.p.employeeNo }}）</span></div>
      <div class="field"><label>應發（底薪＋津貼）</label><span>{{ money(detail.p.baseAmount) }}</span></div>
      <div class="field"><label>加班費</label><span>{{ money(detail.p.overtimePay) }}</span></div>
      <div class="field"><label>獎金</label><span>{{ money(detail.p.bonus) }}</span></div>
      <div class="field"><label>請假扣款</label><span>-{{ money(detail.p.leaveDeduction) }}</span></div>
      <div class="field"><label>勞健保自付</label><span>-{{ money(detail.p.insuranceDeduction) }}</span></div>
      <div class="field"><label>所得稅預扣</label><span>-{{ money(detail.p.taxWithheld) }}</span></div>
      <div class="field field-full"><label>應發合計</label><span>{{ money(detail.p.grossPay) }}</span></div>
      <div class="field field-full"><label>實發金額</label><strong>{{ money(detail.p.netPay) }}</strong></div>
      <div class="field"><label>狀態</label><span>{{ statusLabels[detail.p.status] }}</span></div>
      <div class="field"><label>產生者</label><span>{{ detail.p.generatedByName ?? detail.p.generatedBy }}</span></div>
      <div class="field"><label>產生時間</label><span>{{ fmt(detail.p.generatedAt) }}</span></div>
      <div class="field"><label>確認時間</label><span>{{ fmt(detail.p.confirmedAt) }}</span></div>
      <div class="field"><label>發放時間</label><span>{{ fmt(detail.p.paidAt) }}</span></div>
    </div>
    <p v-else-if="detail.loading" class="muted">載入中…</p>
  </Modal>

  <Transition name="toast">
    <div v-if="toast" class="toast">{{ toast }}</div>
  </Transition>
</template>

<style scoped>
:global(.tag-blue) {
  background: #dbeafe;
  color: #1e40af;
}
.detail-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}
.detail-grid .field-full {
  grid-column: 1 / -1;
}
</style>