<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import Modal from '@/components/Modal.vue'
import { getSalaries, upsertSalary, type EmployeeSalary, type UpsertEmployeeSalary } from '@/services/payroll'
import { getEmployees, type Employee } from '@/services/organization'

const loading = ref(false)
const toast = ref('')
const employees = ref<Employee[]>([])
const salaries = ref<Map<number, EmployeeSalary>>(new Map())
const keyword = ref('')

const modal = reactive({ open: false, employee: null as Employee | null, idle: false })
const form = reactive<UpsertEmployeeSalary>({
  baseSalary: 0,
  positionAllowance: 0,
  mealAllowance: 0,
  effectiveDate: '',
})

function errorMessage(error: unknown): string {
  const err = error as { response?: { data?: { error?: { message?: string } } } }
  return err.response?.data?.error?.message ?? '操作失敗'
}

function showToast(msg: string) {
  toast.value = msg
  setTimeout(() => (toast.value = ''), 2500)
}

const filtered = () =>
  employees.value.filter(
    (e) => !keyword.value || e.name.includes(keyword.value) || e.employeeNo.toLowerCase().includes(keyword.value.toLowerCase()),
  )

async function load() {
  loading.value = true
  try {
    const [empPage, salaryPage] = await Promise.all([
      getEmployees({ page: 1, pageSize: 200 }),
      getSalaries(),
    ])
    employees.value = empPage.items
    salaries.value = new Map(salaryPage.items.map((s) => [s.employeeId, s]))
  } catch (error) {
    showToast(errorMessage(error))
  } finally {
    loading.value = false
  }
}

function openEdit(emp: Employee) {
  const s = salaries.value.get(emp.id)
  modal.employee = emp
  modal.idle = true
  Object.assign(form, {
    baseSalary: s?.baseSalary ?? 0,
    positionAllowance: s?.positionAllowance ?? 0,
    mealAllowance: s?.mealAllowance ?? 0,
    effectiveDate: s?.effectiveDate ?? new Date().toISOString().slice(0, 10),
  })
  modal.open = true
}

async function submit() {
  const emp = modal.employee
  if (!emp) return
  if (form.baseSalary < 0 || form.positionAllowance < 0 || form.mealAllowance < 0 || !form.effectiveDate) {
    showToast('請填寫有效的薪資數值與生效日')
    return
  }
  try {
    await upsertSalary(emp.id, { ...form })
    showToast(`已儲存「${emp.name}」的薪資結構`)
    modal.open = false
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

const money = (n: number) => n.toLocaleString('zh-TW', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

onMounted(load)
</script>

<template>
  <section class="card">
    <h2>薪資結構</h2>
    <div class="toolbar">
      <input v-model="keyword" class="btn" style="width: 220px" placeholder="搜尋姓名/工號" />
      <button class="btn" @click="load">重新整理</button>
    </div>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>工號</th>
            <th>姓名</th>
            <th>底薪</th>
            <th>職務加給</th>
            <th>伙食津貼</th>
            <th>生效日</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="e in filtered()" :key="e.id">
            <td>{{ e.employeeNo }}</td>
            <td>{{ e.name }}</td>
            <template v-if="salaries.get(e.id)">
              <td>{{ money(salaries.get(e.id)!.baseSalary) }}</td>
              <td>{{ money(salaries.get(e.id)!.positionAllowance) }}</td>
              <td>{{ money(salaries.get(e.id)!.mealAllowance) }}</td>
              <td>{{ (salaries.get(e.id)!.effectiveDate ?? '').slice(0, 10) }}</td>
            </template>
            <template v-else>
              <td colspan="4" class="muted">未設定</td>
            </template>
            <td><button class="btn" @click="openEdit(e)">設定/編輯</button></td>
          </tr>
          <tr v-if="filtered().length === 0">
            <td colspan="7" class="muted">沒有符合的員工</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>

  <Modal v-if="modal.employee" :open="modal.open" :title="`薪資結構：${modal.employee.name}`" @close="modal.open = false">
    <div class="form-grid">
      <div class="field">
        <label>底薪 *</label>
        <input v-model.number="form.baseSalary" type="number" min="0" step="0.01" />
      </div>
      <div class="field">
        <label>職務加給 *</label>
        <input v-model.number="form.positionAllowance" type="number" min="0" step="0.01" />
      </div>
      <div class="field">
        <label>伙食津貼 *</label>
        <input v-model.number="form.mealAllowance" type="number" min="0" step="0.01" />
      </div>
      <div class="field">
        <label>生效日 *</label>
        <input v-model="form.effectiveDate" type="date" />
      </div>
    </div>
    <template #footer>
      <button class="btn" @click="modal.open = false">取消</button>
      <button class="btn btn-primary" @click="submit">儲存</button>
    </template>
  </Modal>

  <Transition name="toast">
    <div v-if="toast" class="toast">{{ toast }}</div>
  </Transition>
</template>