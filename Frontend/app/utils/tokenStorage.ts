const KEY = 'multivoice-token'

// login token survives reloads; storage can be blocked (private mode), so every access is guarded
export const tokenStorage = {
  read(): string | null {
    try { return localStorage.getItem(KEY) } catch { return null }
  },
  write(token: string | null) {
    try { token ? localStorage.setItem(KEY, token) : localStorage.removeItem(KEY) } catch {}
  },
}
