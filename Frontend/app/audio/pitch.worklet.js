// delay-line pitch shifter: two taps sweep through a short window at the shifted speed, crossfaded so the jumps don't click
const WINDOW = 2048 // samples swept by each tap (~43 ms at 48 kHz)
const SIZE = 4096 // ring buffer, a power of two above WINDOW

class Pitch extends AudioWorkletProcessor {
  static get parameterDescriptors() {
    return [{ name: 'semitones', defaultValue: 0, minValue: -24, maxValue: 24, automationRate: 'k-rate' }]
  }

  constructor() {
    super()
    this.ring = new Float32Array(SIZE)
    this.write = 0
    this.phase = 0
  }

  process(inputs, outputs, parameters) {
    const input = inputs[0][0]
    const output = outputs[0][0]
    if (!output) return true
    const ratio = 2 ** (parameters.semitones[0] / 12)
    for (let i = 0; i < output.length; i++) {
      this.ring[this.write] = input ? input[i] : 0
      if (ratio === 1) {
        output[i] = this.ring[this.write]
      } else {
        this.phase = (this.phase + (1 - ratio) / WINDOW + 1) % 1
        const other = (this.phase + 0.5) % 1
        // sin² + cos² = 1 keeps the loudness flat across the crossfade
        output[i] = this.tap(this.phase) * Math.sin(Math.PI * this.phase) + this.tap(other) * Math.sin(Math.PI * other)
      }
      this.write = (this.write + 1) & (SIZE - 1)
    }
    return true
  }

  tap(phase) {
    const pos = this.write - phase * WINDOW + SIZE
    const i = Math.floor(pos)
    const frac = pos - i
    return this.ring[i & (SIZE - 1)] * (1 - frac) + this.ring[(i + 1) & (SIZE - 1)] * frac
  }
}

registerProcessor('pitch', Pitch)
