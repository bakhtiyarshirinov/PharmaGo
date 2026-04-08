/** @type {import('next').NextConfig} */
const nextConfig = {
  transpilePackages: ['@pharmago/ui', '@pharmago/api-client', '@pharmago/auth', '@pharmago/config', '@pharmago/types'],
}

export default nextConfig

