// synthetic room responses (decaying noise), one per decay length, shared by all speakers
export class ReverbImpulses {
  private readonly cache = new Map<number, AudioBuffer>()

  constructor(private readonly ctx: AudioContext) {}

  get(decay: number) {
    const seconds = Math.max(0.5, Math.round(decay * 2) / 2) // 0.5 s steps keep the cache small
    let buf = this.cache.get(seconds)
    if (!buf) this.cache.set(seconds, buf = this.create(seconds))
    return buf
  }

  private create(seconds: number) {
    const { ctx } = this
    const buf = ctx.createBuffer(2, ctx.sampleRate * seconds, ctx.sampleRate)
    for (let c = 0; c < 2; c++) {
      const data = buf.getChannelData(c)
      for (let i = 0; i < data.length; i++) data[i] = (Math.random() * 2 - 1) * (1 - i / data.length) ** 3
    }
    return buf
  }
}
