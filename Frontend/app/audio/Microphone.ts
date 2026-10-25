// mic -> AudioWorklet (raw samples) -> WebCodecs Opus 20 ms frames -> onFrame
export class Microphone {
  muted = false

  constructor(
    private readonly ctx: AudioContext,
    private readonly onFrame: (bytes: Uint8Array) => void,
    private readonly onError: (message: string) => void,
  ) {}

  async start() {
    const { ctx } = this
    const stream = await navigator.mediaDevices.getUserMedia({
      audio: { echoCancellation: true, noiseSuppression: true, autoGainControl: true, channelCount: 1 },
    })
    await ctx.audioWorklet.addModule('/capture-worklet.js')
    const capture = new AudioWorkletNode(ctx, 'capture')
    // a silent sink keeps the worklet in the rendering graph
    ctx.createMediaStreamSource(stream).connect(capture).connect(new GainNode(ctx, { gain: 0 })).connect(ctx.destination)

    const encoder = new AudioEncoder({
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
    capture.port.onmessage = (e: MessageEvent<Float32Array<ArrayBuffer>>) => {
      const samples = e.data
      if (!this.muted) {
        const data = new AudioData({ format: 'f32', sampleRate: rate, numberOfFrames: samples.length, numberOfChannels: 1, timestamp, data: samples })
        encoder.encode(data)
        data.close()
      }
      timestamp += samples.length / rate * 1e6
    }
  }
}
