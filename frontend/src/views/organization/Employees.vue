<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import Modal from '@/components/Modal.vue'
import { useAuthStore } from '@/stores/auth'
import {
  createEmployee,
  deleteEmployee,
  flattenDepartments,
  getDepartmentsTree,
  getEmployees,
  getPositions,
  type Employee,
  type EmployeeUpsert,
  type Position,
  updateEmployee,
} from '@/services/organization'

type FlatDept = ReturnType<typeof flattenDepartments>[number]

const auth = useAuthStore()
const canManage = auth.hasPermission('employee.manage')

const loading = ref(false)
const toast = ref('')

const employees = ref<Employee[]>([])
const departments = ref<FlatDept[]>([])
const positions = ref<Position[]>([])
const managersOptions = ref<Employee[]>([])
const total = ref(0)

const query = reactive({
  page: 1,
  pageSize: 20,
  keyword: '',
  departmentId: null as number | null,
  employmentStatus: '',
})

const genderLabels: Record<number, string> = { 0: '未設定', 1: '男', 2: '女' }
const statusLabels: Record<string, string> = { active: '在職', on_leave: '留停', resigned: '離職' }
const statusTags: Record<string, string> = { active: 'tag-green', on_leave: 'tag-amber', resigned: 'tag-red' }

const totalPages = computed(() => Math.max(1, Math.ceil(total.value / query.pageSize)))

async function load() {
  loading.value = true
  try {
    const [page, deptTree, pos] = await Promise.all([
      getEmployees(query),
      getDepartmentsTree(),
      getPositions(),
    ])
    employees.value = page.items
    total.value = page.total
    departments.value = flattenDepartments(deptTree)
    positions.value = pos
  } finally {
    loading.value = false
  }
}

async function loadManagers() {
  if (!canManage) return
  const page = await getEmployees({ page: 1, pageSize: 100 })
  managersOptions.value = page.items
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

function showToast(msg: string) {
  toast.value = msg
  setTimeout(() => (toast.value = ''), 2500)
}

const modal = reactive({ open: false, editingId: null as number | null })
const form = reactive<EmployeeUpsert>({
  employeeNo: '',
  name: '',
  gender: 0,
  birthDate: null,
  phone: null,
  email: null,
  address: null,
  hireDate: null,
  leaveDate: null,
  employmentStatus: 'active',
  departmentId: 0,
  positionId: 0,
  managerId: null,
})

function openCreate() {
  modal.editingId = null
  Object.assign(form, {
    employeeNo: '',
    name: '',
    gender: 0,
    birthDate: null,
    phone: null,
    email: null,
    address: null,
    hireDate: null,
    leaveDate: null,
    employmentStatus: 'active',
    departmentId: departments.value[0]?.id ?? 0,
    positionId: positions.value[0]?.id ?? 0,
    managerId: null,
  })
  modal.open = true
}

function openEdit(emp: Employee) {
  modal.editingId = emp.id
  Object.assign(form, {
    employeeNo: emp.employeeNo,
    name: emp.name,
    gender: emp.gender,
    birthDate: emp.birthDate,
    phone: emp.phone,
    email: emp.email,
    address: emp.address,
    hireDate: emp.hireDate,
    leaveDate: emp.leaveDate,
    employmentStatus: emp.employmentStatus,
    departmentId: emp.departmentId,
    positionId: emp.positionId,
    managerId: emp.managerId,
  })
  modal.open = true
}

async function submit() {
  if (!form.employeeNo.trim() || !form.name.trim() || !form.departmentId || !form.positionId) {
    showToast('請填寫工號、姓名、部門與職位')
    return
  }
  try {
    if (modal.editingId === null) {
      await createEmployee({ ...form })
      showToast('員工已建立')
    } else {
      await updateEmployee(modal.editingId, { ...form })
      showToast('員工已更新')
    }
    modal.open = false
    await Promise.all([load(), loadManagers()])
  } catch (error) {
    showToast(errorMessage(error))
  }
}

async function remove(emp: Employee) {
  if (!window.confirm(`確定刪除員工「${emp.name}」`)) return
  try {
    await deleteEmployee(emp.id)
    showToast('員工已刪除')
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

function errorMessage(error: unknown): string {
  const err = error as { response?: { data?: { error?: { message?: string } } } }
  return err.response?.data?.error?.message ?? '操作失敗'
}

onMounted(() => {
  load()
  loadManagers()
})
</script>

<template>
  <section class="card">
    <h2>員工管理</h2>
    <div class="toolbar">
      <input v-model="query.keyword" class="btn" style="width: 200px" placeholder="工號/姓名/信箱" @keyup.enter="applyFilters" />
      <select v-model="query.departmentId" class="btn" @change="applyFilters">
        <option :value="null">全部部門</option>
        <option v-for="d in departments" :key="d.id" :value="d.id">
          {{ '　'.repeat(d.depth) }}{{ d.name }}
        </option>
      </select>
      <select v-model="query.employmentStatus" class="btn" @change="applyFilters">
        <option value="">全部狀態</option>
        <option value="active">在職</option>
        <option value="on_leave">留停</option>
        <option value="resigned">離職</option>
      </select>
      <button class="btn" @click="applyFilters">查詢</button>
      <div class="spacer"></div>
      <button v-if="canManage" class="btn btn-primary" @click="openCreate">新增員工</button>
    </div>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>工號</th>
            <th>姓名</th>
            <th>性別</th>
            <th>部門</th>
            <th>職位</th>
            <th>主管</th>
            <th>狀態</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="e in employees" :key="e.id">
            <td>{{ e.employeeNo }}</td>
            <td>{{ e.name }}</td>
            <td>{{ genderLabels[e.gender] }}</td>
            <td>{{ e.departmentName }}</td>
            <td>{{ e.positionName }}</td>
            <td>{{ e.managerName ?? '-' }}</td>
            <td>
              <span class="tag" :class="statusTags[e.employmentStatus] ?? 'tag'">{{ statusLabels[e.employmentStatus] }}</span>
            </td>
            <td>
              <template v-if="canManage">
                <button class="btn" @click="openEdit(e)">編輯</button>
                <button v-if="e.isActive" class="btn btn-danger" @click="remove(e)">刪除</button>
              </template>
              <span v-else class="muted">-</span>
            </td>
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

  <Modal :open="modal.open" :title="modal.editingId === null ? '新增員工' : '編輯員工'" @close="modal.open = false">
    <div class="form-grid">
      <div class="field">
        <label>工號 *</label>
        <input v-model="form.employeeNo" placeholder="如 E001" />
      </div>
      <div class="field">
        <label>姓名 *</label>
        <input v-model="form.name" />
      </div>
      <div class="field">
        <label>性別</label>
        <select v-model="form.gender">
          <option :value="0">未設定</option>
          <option :value="1">男</option>
          <option :value="2">女</option>
        </select>
      </div>
      <div class="field">
        <label>生日</label>
        <input v-model="form.birthDate" type="date" />
      </div>
      <div class="field">
        <label>電話</label>
        <input v-model="form.phone" />
      </div>
      <div class="field">
        <label>信箱</label>
        <input v-model="form.email" />
      </div>
      <div class="field field-full">
        <label>地址</label>
        <input v-model="form.address" />
      </div>
      <div class="field">
        <label>部門 *</label>
        <select v-model="form.departmentId">
          <option v-for="d in departments" :key="d.id" :value="d.id">
            {{ '　'.repeat(d.depth) }}{{ d.name }}
          </option>
        </select>
      </div>
      <div class="field">
        <label>職位 *</label>
        <select v-model="form.positionId">
          <option v-for="p in positions" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
      </div>
      <div class="field">
        <label>直屬主管</label>
        <select v-model="form.managerId">
          <option :value="null">（無）</option>
          <option v-for="m in managersOptions" :key="m.id" :value="m.id">{{ m.name }}（{{ m.employeeNo }}）</option>
        </select>
      </div>
      <div class="field">
        <label>雇用狀態</label>
        <select v-model="form.employmentStatus">
          <option value="active">在職</option>
          <option value="on_leave">留停</option>
          <option value="resigned">離職</option>
        </select>
      </div>
      <div class="field">
        <label>到職日</label>
        <input v-model="form.hireDate" type="date" />
      </div>
      <div class="field">
        <label>離職日</label>
        <input v-model="form.leaveDate" type="date" />
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