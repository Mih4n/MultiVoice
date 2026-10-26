import { HubConnectionState } from '@microsoft/signalr'
import { Microphone } from '~/audio/Microphone'
import { ReverbImpulses } from '~/audio/ReverbImpulses'
import { setListenerPose } from '~/audio/setListenerPose'
import { SpeakerChain } from '~/audio/SpeakerChain'
import type { VoiceMix } from '~/audio/VoiceMix'

// mirror PlayerVoice and SpeakerSetting on the server
export type PlayerVoice = { Xuid: string, Name: string, State: 'listening' | 'mic' | 'speaking' }
export type SpeakerSetting = { Volume: number, Muted: boolean }

// /voice hub: nearby speakers in, each through its own SpeakerChain; our mic out when it's on
export function useVoice() {
  const live = ref(false) // connected to the voice hub
  const suspended = ref(true) // audio waits for the first click or key
  const error = ref('')
  const micOn = usePref('multivoice-mic', true)
  const denoise = usePref('multivoice-denoise', true)
  const sensitivity = usePref('multivoice-sensitivity', 0.5)
  const deviceId = usePref('multivoice-mic-device', '')
  const deafened = usePref('multivoice-deafened', false) // not listening, so not talking either
  const devices = ref<MediaDeviceInfo[]>([])
  const talking = computed(() => micOn.value && !deafened.value)
  const micLive = ref(false) // mic actually capturing
  const speaking = ref(false)
  const level = ref(0) // mic level 0..1 for the sensitivity meter
  const gameMuted = ref(false) // /mute in game
  const players = ref<Record<string, PlayerVoice>>({}) // everyone else in voice, server-wide
  const settings = ref<Record<string, SpeakerSetting>>({})

  const hub = createHub('/api/voice', { accessTokenFactory: () => tokenStorage.read() ?? '' })
  let ctx: AudioContext | null = null
  let impulses: ReverbImpulses | null = null
  let mic: Microphone | null = null
  const speakers = new Map<string, SpeakerChain>()

  hub.on('Self', (x: number, y: number, z: number, yaw: number, pitch: number, isMuted: boolean) => {
    gameMuted.value = isMuted
    if (ctx) setListenerPose(ctx, x, y, z, yaw, pitch)
  })

  hub.on('Voice', (speaker: string, frame: Uint8Array, x: number, y: number, z: number, mix: VoiceMix) => {
    if (!ctx || !impulses || deafened.value) return
    let chain = speakers.get(speaker)
    if (!chain) speakers.set(speaker, chain = new SpeakerChain(ctx, impulses, speaker))
    chain.apply(x, y, z, mix)
    chain.decode(frame)
  })

  hub.on('Settings', (saved: Record<string, SpeakerSetting>) => { settings.value = saved })
  hub.on('Players', (list: PlayerVoice[]) => { players.value = Object.fromEntries(list.map((p) => [p.Xuid, p])) })
  hub.on('PlayerState', (p: { Xuid: string, Name: string, State: PlayerVoice['State'] | 'off' }) => {
    if (p.State === 'off') delete players.value[p.Xuid]
    else players.value[p.Xuid] = p as PlayerVoice
  })

  const report = (e: unknown) => { error.value = e instanceof Error ? e.message.replace(/^.*HubException: /, '') : String(e) }
  function sendState() {
    if (hub.state === HubConnectionState.Connected) hub.send('State', micLive.value, speaking.value).catch(report)
  }
  hub.onreconnected(sendState)
  watch([micLive, speaking], sendState)
  watch([denoise, sensitivity], ([d, s]) => mic?.configure(d, s))
  watch(talking, (on) => on ? startMic() : stopMic())
  watch(deviceId, () => { if (mic) { stopMic(); startMic() } })

  // labels are only filled in once the mic permission is granted
  async function refreshDevices() {
    devices.value = (await navigator.mediaDevices.enumerateDevices()).filter((d) => d.kind === 'audioinput' && d.deviceId)
  }
  navigator.mediaDevices.addEventListener('devicechange', refreshDevices)

  function ensureCtx() {
    if (ctx) return ctx
    const c = ctx = new AudioContext({ sampleRate: 48000 }) // RNNoise and Opus both want 48 kHz
    impulses = new ReverbImpulses(c)
    c.onstatechange = () => { suspended.value = c.state !== 'running' }
    suspended.value = c.state !== 'running'
    return c
  }

  // browsers keep audio suspended until a click or key; any one counts, the login click included
  // pointerup, not pointerdown: on touch screens only the release unlocks audio
  const unlock = () => { ensureCtx().resume() }
  for (const type of ['pointerup', 'keydown']) document.addEventListener(type, unlock)

  // called once logged in; the mic permission is asked once, then the browser remembers it
  async function start() {
    if (hub.state !== HubConnectionState.Disconnected) return
    error.value = ''
    ensureCtx()
    try {
      await hub.start()
      live.value = true
    } catch (e) {
      error.value = `Нет связи с голосовым сервером: ${e}`
      return
    }
    if (talking.value) await startMic()
    sendState()
  }

  async function startMic() {
    if (!ctx || mic) return
    const m = mic = new Microphone(ctx,
      (bytes) => { if (hub.state === HubConnectionState.Connected) hub.send('Voice', bytes) },
      (value) => { if (mic === m) speaking.value = value },
      (message) => { error.value = message })
    m.deviceId = deviceId.value
    m.onLevel = (value) => { if (mic === m) level.value = value }
    m.configure(denoise.value, sensitivity.value)
    try {
      await m.start()
      refreshDevices()
      if (mic === m) micLive.value = true
      else m.stop() // switched off while the permission prompt was open
    } catch (e) {
      m.stop()
      if (mic === m) { mic = null; micOn.value = false }
      error.value = `Нет доступа к микрофону: ${e}`
    }
  }

  function stopMic() {
    mic?.stop()
    mic = null
    micLive.value = false
    speaking.value = false
    level.value = 0
  }

  const settingOf = (xuid: string) => settings.value[xuid] ?? { Volume: 1, Muted: false }

  function setVolume(xuid: string, volume: number, save: boolean) {
    settings.value[xuid] = { ...settingOf(xuid), Volume: volume }
    if (save) hub.invoke('SetVolume', xuid, volume).catch(report)
  }

  function setMuted(xuid: string, muted: boolean) {
    settings.value[xuid] = { ...settingOf(xuid), Muted: muted }
    hub.invoke('SetMuted', xuid, muted).catch(report)
  }

  onBeforeUnmount(() => {
    for (const type of ['pointerup', 'keydown']) document.removeEventListener(type, unlock)
    navigator.mediaDevices.removeEventListener('devicechange', refreshDevices)
    mic?.stop(); hub.stop(); ctx?.close()
  })

  return { live, suspended, error, micOn, deafened, deviceId, devices, denoise, sensitivity, speaking, level, gameMuted, players, settings, start, setVolume, setMuted }
}
