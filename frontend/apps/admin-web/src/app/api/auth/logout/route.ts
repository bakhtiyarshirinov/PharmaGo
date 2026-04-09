import { logoutHandler } from '@pharmago/auth/server'

export async function POST() {
  return logoutHandler({ portal: 'admin' })
}
