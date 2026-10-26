// a ref saved per browser; storage can be blocked (private mode), then it just starts from the default
export function usePref<T>(key: string, fallback: T): Ref<T> {
  let initial = fallback
  try {
    const raw = localStorage.getItem(key)
    if (raw !== null) initial = JSON.parse(raw)
  } catch {}
  const pref = ref(initial) as Ref<T>
  watch(pref, (value) => { try { localStorage.setItem(key, JSON.stringify(value)) } catch {} })
  return pref
}
