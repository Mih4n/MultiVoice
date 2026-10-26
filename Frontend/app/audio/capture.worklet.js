// mic samples -> RNNoise (optional) -> loudness gate -> main thread, only while speaking; plus a level for the meter
import createRNNWasmModuleSync from '@jitsi/rnnoise-wasm/dist/rnnoise-sync.js'

const FRAME = 480 // RNNoise works on 10 ms frames at 48 kHz
const HANGOVER = 30 // frames (300 ms) kept open after the last voiced one, so speech isn't chopped
const SCALE = 32768 // RNNoise expects 16-bit sample values
const METER_EVERY = 5 // frames (50 ms) between level reports for the sensitivity meter

class Capture extends AudioWorkletProcessor {
  constructor() {
    super()
    // the sync build has the wasm inlined: worklets can't fetch
    this.rnn = createRNNWasmModuleSync()
    this.state = this.rnn._rnnoise_create(0)
    this.input = this.rnn._malloc(FRAME * 4)
    this.output = this.rnn._malloc(FRAME * 4)
    this.frame = new Float32Array(FRAME)
    this.filled = 0
    this.hold = 0
    this.speaking = false
    this.peak = 0
    this.frames = 0
    this.denoise = true
    this.sensitivity = 0.5
    this.port.onmessage = (e) => { ({ denoise: this.denoise, sensitivity: this.sensitivity } = e.data) }
  }

  process(inputs) {
    const channel = inputs[0][0]
    for (let i = 0; channel && i < channel.length;) {
      const n = Math.min(channel.length - i, FRAME - this.filled)
      this.frame.set(channel.subarray(i, i + n), this.filled)
      this.filled += n
      i += n
      if (this.filled === FRAME) {
        this.filled = 0
        this.gate()
      }
    }
    return true
  }

  gate() {
    let frame
    if (this.denoise) {
      const heap = this.rnn.HEAPF32 // re-read: the view changes if wasm memory grows
      for (let i = 0; i < FRAME; i++) heap[this.input / 4 + i] = this.frame[i] * SCALE
      this.rnn._rnnoise_process_frame(this.state, this.output, this.input)
      frame = heap.slice(this.output / 4, this.output / 4 + FRAME).map((x) => x / SCALE)
    } else {
      frame = this.frame.slice()
    }
    // loudness of what is actually sent, -100..0 dBFS -> 0..1, like Discord's meter; the threshold is 1 - sensitivity
    const rms = Math.sqrt(frame.reduce((sum, x) => sum + x * x, 0) / FRAME)
    const level = Math.min(1, Math.max(0, (20 * Math.log10(rms) + 100) / 100))
    const voiced = level >= 1 - this.sensitivity
    this.peak = Math.max(this.peak, level)
    if (++this.frames === METER_EVERY) {
      this.port.postMessage(this.peak)
      this.peak = this.frames = 0
    }
    this.hold = voiced ? HANGOVER : Math.max(0, this.hold - 1)
    const speaking = this.hold > 0
    if (speaking !== this.speaking) this.port.postMessage(this.speaking = speaking)
    if (speaking) this.port.postMessage(frame, [frame.buffer])
  }
}
registerProcessor('capture', Capture)
