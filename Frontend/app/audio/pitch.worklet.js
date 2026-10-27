// delay-line pitch shifter: two taps sweep through a short window at the shifted speed, crossfaded so the jumps don't click
const WINDOW = 2048 // samples swept by each tap (~43 ms at 48 kHz)
const SIZE = 4096 // ring buffer, a power of two above WINDOW

class Pitch extends AudioWorkletProcessor {
  static get parameterDescriptors() {
    return [{ name: 'semitones', defaultValue: 0, minValue: -24, maxValue: 24, automationRate: 'k-rate' }]
  }

  constructor() {
    super()
    this.rings = [] // one per channel: the panner in front of this node is stereo
    this.write = 0
    this.phase = 0
  }

  process(inputs, outputs, parameters) {
    const input = inputs[0]
    const output = outputs[0]
    const ratio = 2 ** (parameters.semitones[0] / 12)
    while (this.rings.length < output.length) this.rings.push(new Float32Array(SIZE))
    for (let i = 0; i < (output[0]?.length ?? 0); i++) {
      if (ratio !== 1) this.phase = (this.phase + (1 - ratio) / WINDOW + 1) % 1
      const other = (this.phase + 0.5) % 1
      // sin² + cos² = 1 keeps the loudness flat across the crossfade
      const a = Math.sin(Math.PI * this.phase), b = Math.sin(Math.PI * other)
      for (let c = 0; c < output.length; c++) {
        const ring = this.rings[c]
        ring[this.write] = input[c] ? input[c][i] : 0
        output[c][i] = ratio === 1 ? ring[this.write] : this.tap(ring, this.phase) * a + this.tap(ring, other) * b
      }
      this.write = (this.write + 1) & (SIZE - 1)
    }
    return true
  }

  tap(ring, phase) {
    const pos = this.write - phase * WINDOW + SIZE
    const i = Math.floor(pos)
    const frac = pos - i
    return ring[i & (SIZE - 1)] * (1 - frac) + ring[(i + 1) & (SIZE - 1)] * frac
  }
}

registerProcessor('pitch', Pitch)
