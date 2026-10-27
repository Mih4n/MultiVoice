// self-check for the adaptive jitter buffer: node scripts/jitter-check.mjs
import assert from 'node:assert/strict'
import { JitterBuffer } from '../app/audio/JitterBuffer.ts'

const FRAME = 0.02
// feeds frames arriving at the given times, returns how often playback ran dry after the first frame
function run(buffer, arrivals) {
  let dry = 0
  arrivals.forEach((t, i) => { if (buffer.schedule(t, FRAME)?.resumed && i > 0) dry++ })
  return dry
}
const steady = (from, n) => Array.from({ length: n }, (_, i) => from + i * FRAME)

// phrases with pauses between them: dry at each pause, but no bursts, so the delay stays minimal
const phrases = new JitterBuffer()
assert.equal(run(phrases, [...steady(0, 50), ...steady(1.4, 50), ...steady(2.6, 50)]), 2)
assert.ok(Math.abs(phrases.delay - 0.06) < 1e-9, `pauses grew the delay to ${phrases.delay}`)

// a 300 ms stall mid-speech: frames 16..30 arrive together when it ends
const stall = (from) => steady(from, 40).map((t, i) => i >= 15 && i < 30 ? from + 30 * FRAME : t)
const net = new JitterBuffer()
assert.equal(run(net, stall(0)), 1, 'the first stall still runs dry')
assert.ok(net.delay > 0.25, `delay grew to ${net.delay}`)
assert.equal(run(net, stall(0.81).slice(0)), 0, 'the same stall again is absorbed')

// a long calm stretch brings the delay back down
run(net, steady(2, 1500))
assert.ok(net.delay < 0.07, `delay shrank to ${net.delay}`)
console.log('jitter OK')
