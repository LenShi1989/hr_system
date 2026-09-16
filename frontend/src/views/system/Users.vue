<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import Modal from '@/components/Modal.vue'
import { getEmployees, type Employee } from '@/services/organization'
import { createUser, getRoles, getUsers, updateUser, type Role, type UserItem, type UserUpsert } from '@/services/system'

const loading = ref(false)
const toast = ref('')
const users = ref<UserItem[]>([])
const roles = ref<Role[]>([])
const employees = ref<Employee[]>([])
const total = ref(0)

const query = reactive({ page: 1, pageSize: 20, keyword: '', roleId: null as number | null })
const totalPages = computed(() => Math.max(1, Math.ceil(total.value / query.pageSize)))

const modal = reactive({ open: false, editingId: null as number | null })
const form = reactive<UserUpsert>({
  email: '',
  password: '',
  roleId: 0,
  employeeId: null,
  isActive: true,
})

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
    const page = await getUsers({
      page: query.page,
      pageSize: query.pageSize,
      keyword: query.keyword || undefined,
      roleId: query.roleId ?? undefined,
    })
    users.value = page.items
    total.value = page.total
  } finally {
    loading.value = false
  }
}

async function loadOptions() {
  const [roleList, empPage] = await Promise.all([getRoles(), getEmployees({ page: 1, pageSize: 200 })])
  roles.value = roleList
  employees.value = empPage.items
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

function openCreate() {
  modal.editingId = null
  Object.assign(form, {
    email: '',
    password: '',
    roleId: roles.value.find((r) => r.code === 'employee')?.id ?? roles.value[0]?.id ?? 0,
    employeeId: null,
    isActive: true,
  })
  modal.open = true
}

function openEdit(u: UserItem) {
  modal.editingId = u.id
  Object.assign(form, {
    email: u.email,
    password: '',
    roleId: u.roleId,
    employeeId: u.employeeId,
    isActive: u.isActive,
  })
  modal.open = true
}

async function submit() {
  const body: UserUpsert = { ...form }
  if (modal.editingId !== null && !body.password) {
    delete body.password
  }
  try {
    if (modal.editingId === null) {
      if (!body.password) {
        showToast('請設定初始密碼')
        return
      }
      await createUser(body)
      showToast('帳號已建立')
    } else {
      await updateUser(modal.editingId!, body)
      showToast('帳號已更新')
    }
    modal.open = false
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

async function toggleActive(u: UserItem) {
  try {
    await updateUser(u.id, {
      email: u.email,
      roleId: u.roleId,
      employeeId: u.employeeId,
      isActive: !u.isActive,
    })
    showToast(u.isActive ? '帳號已停用' : '帳號已啟用')
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

onMounted(async () => {
  await loadOptions()
  await load()
})
</script>

<template>
  <section class="card">
    <h2>使用者管理</h2>
    <div class="toolbar">
      <input v-model="query.keyword" class="btn" style="width: 200px" placeholder="信箱/姓名/工號" @keyup.enter="applyFilters" />
      <select v-model="query.roleId" class="btn" @change="applyFilters">
        <option :value="null">全部角色</option>
        <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.name }}</option>
      </select>
      <button class="btn" @click="applyFilters">查詢</button>
      <div class="spacer"></div>
      <button class="btn btn-primary" @click="openCreate">新增帳號</button>
    </div>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>信箱</th>
            <th>員工</th>
            <th>角色</th>
            <th>狀態</th>
            <th>建立時間</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in users" :key="u.id">
            <td>{{ u.email }}</td>
            <td>{{ u.employeeName ? `${u.employeeName}（${u.employeeNo}）` : '-' }}</td>
            <td>{{ u.roleName }}</td>
            <td>
              <span class="tag" :class="u.isActive ? 'tag-green' : 'tag-red'">{{ u.isActive ? '啟用' : '停用' }}</span>
            </td>
            <td>{{ new Date(u.createdAt).toLocaleString('zh-TW', { hour12: false }) }}</td>
            <td>
              <button class="btn" @click="openEdit(u)">編輯</button>
              <button class="btn" :class="u.isActive ? 'btn-danger' : 'btn-primary'" @click="toggleActive(u)">
                {{ u.isActive ? '停用' : '啟用' }}
              </button>
            </td>
          </tr>
          <tr v-if="users.length === 0">
            <td colspan="6" class="muted">沒有符合的帳號</td>
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

  <Modal :open="modal.open" :title="modal.editingId === null ? '新增帳號' : '編輯帳號'" @close="modal.open = false">
    <div class="form-grid">
      <div class="field field-full">
        <label>信箱（登入帳號）*</label>
        <input v-model="form.email" placeholder="name@example.com" />
      </div>
      <div class="field field-full">
        <label>{{ modal.editingId === null ? '初始密碼 *' : '重設密碼（留空不變）' }}</label>
        <input v-model="form.password" type="password" placeholder="至少 8 個字元" />
      </div>
      <div class="field">
        <label>角色 *</label>
        <select v-model="form.roleId">
          <option v-for="r in roles" :key="r.id" :value="r.id">{{ r.name }}（{{ r.code }}）</option>
        </select>
      </div>
      <div class="field">
        <label>綁定員工</label>
        <select v-model="form.employeeId">
          <option :value="null">（不綁定）</option>
          <option v-for="e in employees" :key="e.id" :value="e.id">{{ e.name }}（{{ e.employeeNo }}）</option>
        </select>
      </div>
      <div class="field">
        <label>狀態</label>
        <select v-model="form.isActive">
          <option :value="true">啟用</option>
          <option :value="false">停用</option>
        </select>
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