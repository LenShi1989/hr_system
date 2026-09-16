<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import Modal from '@/components/Modal.vue'
import { useAuthStore } from '@/stores/auth'
import {
  createDepartment,
  deleteDepartment,
  flattenDepartments,
  getDepartmentsTree,
  type Department,
  type DepartmentUpsert,
  updateDepartment,
} from '@/services/organization'

const auth = useAuthStore()
const canManage = auth.hasPermission('employee.manage')

const tree = ref<Department[]>([])
const flat = computed(() => flattenDepartments(tree.value))
const keyword = ref('')
const loading = ref(false)
const toast = ref('')

const filteredFlat = computed(() => {
  const kw = keyword.value.trim().toLowerCase()
  if (!kw) return flat.value
  return flat.value.filter((d) => d.code.toLowerCase().includes(kw) || d.name.toLowerCase().includes(kw))
})

const modal = reactive({ open: false, editingId: null as number | null })
const form = reactive<DepartmentUpsert>({ code: '', name: '', parentId: null, managerId: null })

function parentOptions(excludeId: number | null) {
  return flat.value.filter((d) => d.id !== excludeId)
}

async function load() {
  loading.value = true
  try {
    tree.value = await getDepartmentsTree()
  } finally {
    loading.value = false
  }
}

function showToast(msg: string) {
  toast.value = msg
  setTimeout(() => (toast.value = ''), 2500)
}

function openCreate(parentId: number | null = null) {
  modal.editingId = null
  form.code = ''
  form.name = ''
  form.parentId = parentId
  form.managerId = null
  modal.open = true
}

function openEdit(dept: Department) {
  modal.editingId = dept.id
  form.code = dept.code
  form.name = dept.name
  form.parentId = dept.parentId
  form.managerId = dept.managerId
  modal.open = true
}

async function submit() {
  if (!form.code.trim() || !form.name.trim()) return
  try {
    if (modal.editingId === null) {
      await createDepartment({ ...form })
      showToast('部門已建立')
    } else {
      await updateDepartment(modal.editingId, { ...form })
      showToast('部門已更新')
    }
    modal.open = false
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

async function remove(dept: Department) {
  if (!window.confirm(`確定停用部門「${dept.name}」？`)) return
  try {
    await deleteDepartment(dept.id)
    showToast('部門已停用')
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

function errorMessage(error: unknown): string {
  const err = error as { response?: { data?: { error?: { message?: string } } } }
  return err.response?.data?.error?.message ?? '操作失敗'
}

onMounted(load)
</script>

<template>
  <section class="card">
    <h2>部門管理</h2>
    <div class="toolbar">
      <input v-model="keyword" class="btn" style="width: 220px" placeholder="搜尋代碼/名稱" />
      <div class="spacer"></div>
      <button v-if="canManage" class="btn btn-primary" @click="openCreate(null)">新增根部門</button>
    </div>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>代碼</th>
            <th>部門名稱</th>
            <th>狀態</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="d in filteredFlat" :key="d.id">
            <td>{{ d.code }}</td>
            <td :class="`depth${Math.min(d.depth, 3)}`">{{ d.name }}</td>
            <td>
              <span class="tag" :class="d.isActive ? 'tag-green' : 'tag-off'">{{ d.isActive ? '啟用' : '停用' }}</span>
            </td>
            <td>
              <template v-if="canManage">
                <button class="btn" @click="openCreate(d.id)">+ 子部門</button>
                <button class="btn" @click="openEdit(d)">編輯</button>
                <button v-if="d.isActive" class="btn btn-danger" @click="remove(d)">停用</button>
              </template>
              <span v-else class="muted">-</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>

  <Modal :open="modal.open" :title="modal.editingId === null ? '新增部門' : '編輯部門'" @close="modal.open = false">
    <div class="form-grid">
      <div class="field">
        <label>代碼</label>
        <input v-model="form.code" placeholder="如 HQ" />
      </div>
      <div class="field">
        <label>名稱</label>
        <input v-model="form.name" placeholder="如 總公司" />
      </div>
      <div class="field">
        <label>上層部門</label>
        <select v-model="form.parentId">
          <option :value="null">（無，作為根部門）</option>
          <option v-for="d in parentOptions(modal.editingId)" :key="d.id" :value="d.id">
            {{ '　'.repeat(d.depth) }}{{ d.name }}
          </option>
        </select>
      </div>
      <div class="field">
        <label>主管員工 ID（選填）</label>
        <input v-model.number="form.managerId" type="number" placeholder="員工期 id" />
      </div>
    </div>
    <template #footer>
      <button class="btn" @click="modal.open = false">取消</button>
      <button class="btn btn-primary" :disabled="!form.code.trim() || !form.name.trim()" @click="submit">儲存</button>
    </template>
  </Modal>

  <Transition name="toast">
    <div v-if="toast" class="toast">{{ toast }}</div>
  </Transition>
</template>