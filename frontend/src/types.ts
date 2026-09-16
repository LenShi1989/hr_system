export interface CurrentUser {
  id: number
  email: string
  roleCode: string
  permissions: string[]
}

export interface LoginResponse {
  accessToken: string
  tokenType: string
  expiresIn: number
  user: CurrentUser
}

export interface ApiResponse<T> {
  data: T | null
  error: { code: string; message: string } | null
}