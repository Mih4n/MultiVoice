// /auth hub: nickname -> code shown here -> confirmed in game -> token
export function useAuth() {
  const status = ref<'server' | 'connecting' | 'login' | 'waiting' | 'ready'>(serverUrl.read() ? 'connecting' : 'server')
  const error = ref('')
  const code = ref<number | null>(null)
  const name = ref('')

  const hub = createHub('/api/auth')

  async function resume(token: string) {
    const who = await hub.invoke<string | null>('Resume', token)
    if (!who) { tokenStorage.write(null); status.value = 'login'; return }
    tokenStorage.write(token)
    name.value = who
    status.value = 'ready'
  }

  async function requestCode(nick: string) {
    error.value = ''
    try {
      code.value = await hub.invoke<number>('RequestCode', nick)
      status.value = 'waiting'
    } catch (e) {
      error.value = e instanceof Error ? e.message.replace(/^.*HubException: /, '') : String(e)
    }
  }

  function logout() {
    tokenStorage.write(null)
    location.reload()
  }

  hub.on('Confirmed', (token: string) => { code.value = null; resume(token) })
  hub.on('Rejected', () => { code.value = null; status.value = 'login'; error.value = 'Подтверждение отклонено в игре' })

  onMounted(async () => {
    if (status.value === 'server') return
    try {
      await hub.start()
      const token = tokenStorage.read()
      if (token) await resume(token)
      else status.value = 'login'
    } catch (e) {
      error.value = `Нет связи с сервером: ${e}`
    }
  })
  onBeforeUnmount(() => hub.stop())

  return { status, error, code, name, requestCode, logout }
}
