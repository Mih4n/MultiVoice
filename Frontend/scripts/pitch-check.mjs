// self-check for the pitch worklet: node scripts/pitch-check.mjs
import assert from 'node:assert/strict'

globalThis.AudioWorkletProcessor = class {}
let Pitch
globalThis.registerProcessor = (_, c) => { Pitch = c }
await import('../app/audio/pitch.worklet.js')

const RATE = 48000, BLOCK = 128, N = 24000
// stereo in like the HRTF panner gives: left 440 Hz, right 660 Hz
function run(semitones) {
  const node = new Pitch(), out = [[], []]
  for (let b = 0; b < N / BLOCK; b++) {
    const t = (i) => (b * BLOCK + i) / RATE
    const input = [440, 660].map((f) => Float32Array.from({ length: BLOCK }, (_, i) => Math.sin(2 * Math.PI * f * t(i))))
    const output = [new Float32Array(BLOCK), new Float32Array(BLOCK)]
    node.process([input], [output], { semitones: [semitones] })
    output.forEach((ch, c) => out[c].push(...ch))
  }
  return out
}
// strongest frequency between 100 and 1500 Hz, skipping the first 1/6 s
function peak(samples) {
  let best = 0, at = 0
  for (let f = 100; f < 1500; f += 2) {
    let re = 0, im = 0
    for (let i = 8000; i < samples.length; i++) {
      re += samples[i] * Math.cos(2 * Math.PI * f * i / RATE)
      im += samples[i] * Math.sin(2 * Math.PI * f * i / RATE)
    }
    if (re * re + im * im > best) { best = re * re + im * im; at = f }
  }
  return at
}

const [left, right] = run(0)
assert.deepEqual([peak(left), peak(right)], [440, 660], 'both ears pass through untouched')
const [up, upRight] = run(5)
const near = (got, want) => Math.abs(got - want) / want < 0.05
assert.ok(near(peak(up), 440 * 2 ** (5 / 12)), `left shifted to ${peak(up)}`)
assert.ok(near(peak(upRight), 660 * 2 ** (5 / 12)), `right shifted to ${peak(upRight)}`)
console.log('pitch OK')
