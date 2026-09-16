<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import Modal from '@/components/Modal.vue'
import { useAuthStore } from '@/stores/auth'
import {
  createPosition,
  deletePosition,
  getDepartmentsTree,
  getPositions,
  type Position,
  type PositionUpsert,
  flattenDepartments,
  updatePosition,
} from '@/services/organization'

const auth = useAuthStore()
const canManage = auth.hasPermission('employee.manage')

const positions = ref<Position[]>([])
const departments = ref(flattenDepartments([]))
const keyword = ref('')
const loading = ref(false)
const toast = ref('')

const filtered = computed(() => {
  const kw = keyword.value.trim().toLowerCase()
  if (!kw) return positions.value
  return positions.value.filter(
    (p) => p.code.toLowerCase().includes(kw) || p.name.toLowerCase().includes(kw),
  )
})

const modal = reactive({ open: false, editingId: null as number | null })
const form = reactive<PositionUpsert>({ code: '', name: '', departmentId: null, level: 1 })

async function load() {
  loading.value = true
  try {
    const [pos, deptTree] = await Promise.all([getPositions(), getDepartmentsTree()])
    positions.value = pos
    departments.value = flattenDepartments(deptTree)
  } finally {
    loading.value = false
  }
}

function showToast(msg: string) {
  toast.value = msg
  setTimeout(() => (toast.value = ''), 2500)
}

function openCreate() {
  modal.editingId = null
  form.code = ''
  form.name = ''
  form.departmentId = null
  form.level = 1
  modal.open = true
}

function openEdit(pos: Position) {
  modal.editingId = pos.id
  form.code = pos.code
  form.name = pos.name
  form.departmentId = pos.departmentId
  form.level = pos.level
  modal.open = true
}

function deptName(id: number | null): string {
  const d = departments.value.find((x) => x.id === id)
  return d?.name ?? '-'
}

async function submit() {
  if (!form.code.trim() || !form.name.trim()) return
  try {
    if (modal.editingId === null) {
      await createPosition({ ...form })
      showToast('職位已建立')
    } else {
      await updatePosition(modal.editingId, { ...form })
      showToast('職位已更新')
    }
    modal.open = false
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

async function remove(pos: Position) {
  if (!window.confirm(`確定停用職位「${pos.name}」？`)) return
  try {
    await deletePosition(pos.id)
    showToast('職位已停用')
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
    <h2>職位管理</h2>
    <div class="toolbar">
      <input v-model="keyword" class="btn" style="width: 220px" placeholder="搜尋代碼/名稱" />
      <div class="spacer"></div>
      <button v-if="canManage" class="btn btn-primary" @click="openCreate">新增職位</button>
    </div>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>代碼</th>
            <th>職位</th>
            <th>所屬部門</th>
            <th>職級</th>
            <th>狀態</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in filtered" :key="p.id">
            <td>{{ p.code }}</td>
            <td>{{ p.name }}</td>
            <td>{{ deptName(p.departmentId) }}</td>
            <td>{{ p.level }}</td>
            <td>
              <span class="tag" :class="p.isActive ? 'tag-green' : 'tag-off'">{{ p.isActive ? '啟用' : '停用' }}</span>
            </td>
            <td>
              <template v-if="canManage">
                <button class="btn" @click="openEdit(p)">編輯</button>
                <button v-if="p.isActive" class="btn btn-danger" @click="remove(p)">停用</button>
              </template>
              <span v-else class="muted">-</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>

  <Modal :open="modal.open" :title="modal.editingId === null ? '新增職位' : '編輯職位'" @close="modal.open = false">
    <div class="form-grid">
      <div class="field">
        <label>代碼</label>
        <input v-model="form.code" placeholder="如 ENG" />
      </div>
      <div class="field">
        <label>職位名稱</label>
        <input v-model="form.name" placeholder="如 工程師" />
      </div>
      <div class="field">
        <label>所屬部門</label>
        <select v-model="form.departmentId">
          <option :value="null">（不限）</option>
          <option v-for="d in departments" :key="d.id" :value="d.id">
            {{ '　'.repeat(d.depth) }}{{ d.name }}
          </option>
        </select>
      </div>
      <div class="field">
        <label>職級（1 = 最高）</label>
        <input v-model.number="form.level" type="number" min="1" />
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