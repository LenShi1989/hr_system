<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const email = ref('')
const password = ref('')
const errorMessage = ref('')
const loading = ref(false)

async function submit() {
  if (!email.value || !password.value) {
    errorMessage.value = '請輸入信箱與密碼'
    return
  }
  loading.value = true
  errorMessage.value = ''
  try {
    await auth.login(email.value, password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    router.replace(redirect)
  } catch (error: unknown) {
    const err = error as { response?: { data?: { error?: { message?: string } } } }
    errorMessage.value = err.response?.data?.error?.message ?? '登入失敗，請稍後再試'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <form class="login-card" @submit.prevent="submit">
      <h1>人事管理系統</h1>
      <label>
        信箱
        <input v-model="email" type="email" placeholder="admin@hr.local" autocomplete="username" />
      </label>
      <label>
        密碼
        <input v-model="password" type="password" placeholder="••••••••" autocomplete="current-password" />
      </label>
      <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
      <button type="submit" :disabled="loading">{{ loading ? '登入中…' : '登入' }}</button>
      <p class="hint">開發帳號：admin / hr / manager / employee @hr.local</p>
    </form>
  </div>
</template>