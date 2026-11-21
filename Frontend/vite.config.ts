import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, '.', '')
  const apiUrl = env.VITE_API_URL?.trim()
  if (command === 'build' && env.VERCEL === '1' && apiUrl) {
    const api = URL.parse(apiUrl)
    if (!api || api.protocol !== 'https:' ||
        /^(localhost|.*\.localhost|127\..*|\[::1\])$/i.test(api.hostname)) {
      throw new Error('Usa una URL HTTPS pública en VITE_API_URL o deja la variable vacía para usar el proxy de Vercel.')
    }
  }

  return {
    plugins: [react(), tailwindcss()],
    server: { port: 5173, strictPort: true },
  }
})
