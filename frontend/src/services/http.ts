import axios from 'axios'
import type { ApiResponse } from '@/types'

export const TOKEN_KEY = 'hr_token'
export const USER_KEY = 'hr_user'

const http = axios.create({
  baseURL: '/api/v1',
})

http.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

http.interceptors.response.use(
  (response) => {
    const payload = response.data as ApiResponse<unknown>
    if (payload && typeof payload === 'object' && 'data' in payload) {
      response.data = payload.data
    }
    return response
  },
  (error) => {
    const isLoginRequest = error.config?.url?.includes('/auth/login')
    if (error.response?.status === 401 && !isLoginRequest) {
      localStorage.removeItem(TOKEN_KEY)
      localStorage.removeItem(USER_KEY)
      window.location.href = '/login'
    }
    return Promise.reject(error)
  },
)

export default http