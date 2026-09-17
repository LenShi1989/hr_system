<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import Modal from '@/components/Modal.vue'
import {
  createRole,
  deleteRole,
  getPermissions,
  getRoles,
  updateRole,
  type Permission,
  type Role,
} from '@/services/system'

const loading = ref(false)
const toast = ref('')
const roles = ref<Role[]>([])
const permissions = ref<Permission[]>([])

const permissionGroups = computed(() => {
  const map = new Map<string, Permission[]>()
  for (const p of permissions.value) {
    if (!map.has(p.group)) map.set(p.group, [])
    map.get(p.group)!.push(p)
  }
  return Array.from(map.entries()).map(([group, items]) => ({ group, items }))
})

const codeMap = computed(() => new Map(permissions.value.map((p) => [p.code, p.label])))

const modal = reactive({
  open: false,
  editingId: null as number | null,
  code: '',
  name: '',
  selected: [] as string[],
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
    const [roleList, permissionList] = await Promise.all([getRoles(), getPermissions()])
    roles.value = roleList
    permissions.value = permissionList
  } catch (error) {
    showToast(errorMessage(error))
  } finally {
    loading.value = false
  }
}

function openCreate() {
  modal.editingId = null
  modal.code = ''
  modal.name = ''
  modal.selected = []
  modal.open = true
}

function openEdit(role: Role) {
  modal.editingId = role.id
  modal.code = role.code
  modal.name = role.name
  modal.selected = [...role.permissionCodes]
  modal.open = true
}

function isSelected(code: string) {
  return modal.selected.includes(code)
}

function togglePermission(code: string) {
  if (isSelected(code)) {
    modal.selected = modal.selected.filter((c) => c !== code)
  } else {
    modal.selected = [...modal.selected, code]
  }
}

function toggleGroup(items: Permission[]) {
  const allSelected = items.every((p) => isSelected(p.code))
  if (allSelected) {
    const codes = new Set(items.map((p) => p.code))
    modal.selected = modal.selected.filter((c) => !codes.has(c))
  } else {
    const merged = new Set(modal.selected)
    for (const p of items) merged.add(p.code)
    modal.selected = [...merged]
  }
}

function groupAllSelected(items: Permission[]) {
  return items.every((p) => isSelected(p.code))
}

async function submit() {
  if (!modal.name.trim()) {
    showToast('請填寫角色名稱')
    return
  }
  if (modal.editingId === null && !modal.code.trim()) {
    showToast('請填寫角色代碼')
    return
  }
  try {
    if (modal.editingId === null) {
      await createRole({ code: modal.code.trim(), name: modal.name.trim(), permissionCodes: modal.selected })
      showToast('角色已建立')
    } else {
      await updateRole(modal.editingId, { name: modal.name.trim(), permissionCodes: modal.selected })
      showToast('角色已更新')
    }
    modal.open = false
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

async function remove(role: Role) {
  if (!window.confirm(`確定刪除角色「${role.name}」？勾選此角色的使用者將需重新指派。`)) return
  try {
    await deleteRole(role.id)
    showToast('角色已刪除')
    await load()
  } catch (error) {
    showToast(errorMessage(error))
  }
}

onMounted(load)
</script>

<template>
  <section class="card">
    <h2>角色與權限</h2>
    <div class="toolbar">
      <button class="btn" @click="load">重新整理</button>
      <div class="spacer"></div>
      <button class="btn btn-primary" @click="openCreate">新增角色</button>
    </div>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>角色代碼</th>
            <th>名稱</th>
            <th>使用者數</th>
            <th>權限</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in roles" :key="r.id">
            <td>{{ r.code }}</td>
            <td>{{ r.name }}</td>
            <td>{{ r.userCount }}</td>
            <td>
              <span v-for="p in r.permissionCodes" :key="p" class="tag permission-tag">
                {{ codeMap.get(p) ?? p }}
              </span>
              <span v-if="r.permissionCodes.length === 0" class="muted">無</span>
            </td>
            <td>
              <button class="btn" @click="openEdit(r)">編輯</button>
              <button class="btn btn-danger" @click="remove(r)">刪除</button>
            </td>
          </tr>
          <tr v-if="roles.length === 0">
            <td colspan="5" class="muted">沒有角色資料</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>

  <Modal :open="modal.open" :title="modal.editingId === null ? '新增角色' : '編輯角色'" @close="modal.open = false">
    <div class="form-grid">
      <div class="field">
        <label>角色代碼 *</label>
        <input v-model="modal.code" :disabled="modal.editingId !== null" placeholder="如 assistant" />
      </div>
      <div class="field">
        <label>角色名稱 *</label>
        <input v-model="modal.name" placeholder="如 助理" />
      </div>
    </div>

    <p class="muted perm-hint">勾選的權限會決定該角色登入後可見的功能與側邊選單（sidebar）。</p>

    <div v-for="group in permissionGroups" :key="group.group" class="perm-group">
      <label class="perm-group-head">
        <input
          type="checkbox"
          :checked="groupAllSelected(group.items)"
          @change="toggleGroup(group.items)"
        />
        <strong>{{ group.group }}</strong>
      </label>
      <div class="perm-items">
        <label v-for="p in group.items" :key="p.code" class="perm-item">
          <input type="checkbox" :checked="isSelected(p.code)" @change="togglePermission(p.code)" />
          <span>{{ p.label }}</span>
          <code class="perm-code">{{ p.code }}</code>
        </label>
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

<style scoped>
.permission-tag {
  margin: 2px 4px 2px 0;
}

.perm-hint {
  margin: 16px 0 8px;
}

.perm-group {
  border-top: 1px solid #f3f4f6;
  padding: 10px 0;
}

.perm-group-head {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 14px;
}

.perm-items {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4px 16px;
  margin: 8px 0 0 24px;
}

.perm-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  cursor: pointer;
}

.perm-code {
  color: #9ca3af;
  font-size: 11px;
}
</style>