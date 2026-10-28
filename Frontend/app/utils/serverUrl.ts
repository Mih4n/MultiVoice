const KEY = 'multivoice-server'

// the backend: set by the deployment (config or NUXT_PUBLIC_SERVER_URL), else typed in by the user, as in the Android app
export const serverUrl = {
  configured: () => !!useRuntimeConfig().public.serverUrl,
  read(): string {
    try { return useRuntimeConfig().public.serverUrl || localStorage.getItem(KEY) || '' } catch { return '' }
  },
  // the hubs are built with the address, so a change reloads; the token belongs to the old server
  write(url: string | null) {
    try { url ? localStorage.setItem(KEY, url) : localStorage.removeItem(KEY) } catch {}
    tokenStorage.write(null)
    location.reload()
  },
}
