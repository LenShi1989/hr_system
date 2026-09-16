import { defineStore } from 'pinia'
import http, { TOKEN_KEY, USER_KEY } from '@/services/http'
import type { CurrentUser, LoginResponse } from '@/types'

function readUser(): CurrentUser | null {
  const raw = localStorage.getItem(USER_KEY)
  if (!raw) {
    return null
  }
  try {
    return JSON.parse(raw) as CurrentUser
  } catch {
    return null
  }
}

interface AuthState {
  token: string
  user: CurrentUser | null
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: localStorage.getItem(TOKEN_KEY) ?? '',
    user: readUser(),
  }),
  getters: {
    isAuthenticated: (state) => Boolean(state.token),
    permissions: (state) => state.user?.permissions ?? [],
  },
  actions: {
    async login(email: string, password: string) {
      const { data } = await http.post<LoginResponse>('/auth/login', { email, password })
      this.token = data.accessToken
      this.user = data.user
      localStorage.setItem(TOKEN_KEY, data.accessToken)
      localStorage.setItem(USER_KEY, JSON.stringify(data.user))
    },
    logout() {
      this.token = ''
      this.user = null
      localStorage.removeItem(TOKEN_KEY)
      localStorage.removeItem(USER_KEY)
      window.location.href = '/login'
    },
    hasPermission(code: string) {
      return this.permissions.includes(code)
    },
  },
})