import { HubConnectionState } from '@microsoft/signalr'
import { Microphone } from '~/audio/Microphone'
import { ReverbImpulses } from '~/audio/ReverbImpulses'
import { setListenerPose } from '~/audio/setListenerPose'
import { SpeakerChain } from '~/audio/SpeakerChain'
import type { VoiceMix } from '~/audio/VoiceMix'

// /voice hub: our mic out, nearby speakers in, each through its own SpeakerChain
export function useVoice() {
  const live = ref(false)
  const error = ref('')
  const muted = ref(false) // site button
  const gameMuted = ref(false) // /mute in game

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
    if (!ctx || !impulses) return
    let chain = speakers.get(speaker)
    if (!chain) speakers.set(speaker, chain = new SpeakerChain(ctx, impulses, speaker))
    chain.apply(x, y, z, mix)
    chain.decode(frame)
  })

  watch(muted, (value) => { if (mic) mic.muted = value })

  // needs a click: browsers only allow audio and mic after a user gesture
  async function start() {
    error.value = ''
    try {
      ctx = new AudioContext()
      impulses = new ReverbImpulses(ctx)
      await hub.start()
      mic = new Microphone(ctx,
        (bytes) => { if (hub.state === HubConnectionState.Connected) hub.send('Voice', bytes) },
        (message) => { error.value = message })
      mic.muted = muted.value
      await mic.start()
      live.value = true
    } catch (e) {
      error.value = `Не удалось включить звук: ${e}`
    }
  }

  onBeforeUnmount(() => { hub.stop(); ctx?.close() })

  return { live, error, muted, gameMuted, start }
}
