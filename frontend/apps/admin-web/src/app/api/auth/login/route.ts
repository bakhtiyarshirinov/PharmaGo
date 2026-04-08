import { loginHandler } from '@pharmago/auth/server'

export async function POST(request: Request) {
  return loginHandler(request)
}
