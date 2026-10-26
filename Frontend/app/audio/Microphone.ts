import workletUrl from './capture.worklet.js?worker&url'

// mic -> capture worklet (RNNoise + voice activation) -> WebCodecs Opus 20 ms frames -> onFrame
export class Microphone {
  private stream: MediaStream | null = null
  private capture: AudioWorkletNode | null = null
  private encoder: AudioEncoder | null = null
  private settings = { denoise: true, sensitivity: 0.5 }
  deviceId = '' // '' is the system default; set before start()
  onLevel = (_level: number) => {}

  constructor(
    private readonly ctx: AudioContext,
    private readonly onFrame: (bytes: Uint8Array) => void,
    private readonly onSpeaking: (speaking: boolean) => void,
    private readonly onError: (message: string) => void,
  ) {}

  configure(denoise: boolean, sensitivity: number) {
    this.settings = { denoise, sensitivity }
    this.capture?.port.postMessage(this.settings)
    // RNNoise replaces the browser's own suppression instead of stacking on it
    this.stream?.getAudioTracks()[0]?.applyConstraints(this.constraints()).catch(() => {})
  }

  async start() {
    const { ctx } = this
    this.stream = await navigator.mediaDevices.getUserMedia({ audio: this.constraints() })
    await ctx.audioWorklet.addModule(workletUrl)
    const capture = this.capture = new AudioWorkletNode(ctx, 'capture')
    capture.port.postMessage(this.settings)
    // a silent sink keeps the worklet in the rendering graph
    ctx.createMediaStreamSource(this.stream).connect(capture).connect(new GainNode(ctx, { gain: 0 })).connect(ctx.destination)

    const encoder = this.encoder = new AudioEncoder({
      output: (chunk) => {
        const bytes = new Uint8Array(chunk.byteLength)
        chunk.copyTo(bytes)
        this.onFrame(bytes)
      },
      error: (e) => this.onError(`Кодек: ${e.message}`),
    })
    encoder.configure({
      codec: 'opus', sampleRate: ctx.sampleRate, numberOfChannels: 1, bitrate: 24000,
      opus: { frameDuration: 20000, usedtx: true },
    } as AudioEncoderConfig)

    const rate = ctx.sampleRate
    let timestamp = 0
    // the worklet posts a boolean when speaking starts or stops, a level number every 50 ms, and samples only while speaking
    capture.port.onmessage = (e: MessageEvent<boolean | number | Float32Array<ArrayBuffer>>) => {
      const samples = e.data
      if (typeof samples === 'boolean') return this.onSpeaking(samples)
      if (typeof samples === 'number') return this.onLevel(samples)
      const data = new AudioData({ format: 'f32', sampleRate: rate, numberOfFrames: samples.length, numberOfChannels: 1, timestamp, data: samples })
      encoder.encode(data)
      data.close()
      timestamp += samples.length / rate * 1e6
    }
  }

  stop() {
    this.stream?.getTracks().forEach((t) => t.stop())
    this.capture?.disconnect()
    this.capture?.port.close()
    if (this.encoder && this.encoder.state !== 'closed') this.encoder.close()
  }

  private constraints(): MediaTrackConstraints {
    return { deviceId: this.deviceId || undefined, echoCancellation: true, noiseSuppression: !this.settings.denoise, autoGainControl: true, channelCount: 1 }
  }
}
