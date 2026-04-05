import { apiRequest } from './client'

export function login(payload) {
  return apiRequest('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function register(payload) {
  return apiRequest('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function logout(refreshToken) {
  return apiRequest('/api/auth/logout', {
    method: 'POST',
    body: JSON.stringify({ refreshToken }),
  })
}

export function getMe() {
  return apiRequest('/api/auth/me')
}
