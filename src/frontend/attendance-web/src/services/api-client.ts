import axios from 'axios'
import { appEnv } from '@/config/env'

export const apiClient = axios.create({
  baseURL: appEnv.VITE_API_BASE_URL,
  timeout: 10000,
  headers: {
    Accept: 'application/json',
    'Content-Type': 'application/json',
  },
})

export default apiClient
