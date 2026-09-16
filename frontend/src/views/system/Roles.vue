<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getRoles, type Role } from '@/services/system'

const loading = ref(false)
const roles = ref<Role[]>([])

async function load() {
  loading.value = true
  try {
    roles.value = await getRoles()
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="card">
    <h2>角色與權限</h2>
    <div class="toolbar">
      <button class="btn" @click="load">重新整理</button>
    </div>
    <p v-if="loading" class="muted">載入中…</p>
    <div v-else class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>角色代碼</th>
            <th>名稱</th>
            <th>使用者數</th>
            <th>狀態</th>
            <th>權限</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in roles" :key="r.id">
            <td>{{ r.code }}</td>
            <td>{{ r.name }}</td>
            <td>{{ r.userCount }}</td>
            <td>
              <span class="tag" :class="r.isActive ? 'tag-green' : 'tag-red'">{{ r.isActive ? '啟用' : '停用' }}</span>
            </td>
            <td>
              <span v-for="p in r.permissionCodes" :key="p" class="tag permission-tag">{{ p }}</span>
            </td>
          </tr>
          <tr v-if="roles.length === 0">
            <td colspan="5" class="muted">沒有角色資料</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<style scoped>
.permission-tag {
  margin: 2px 4px 2px 0;
}
</style>