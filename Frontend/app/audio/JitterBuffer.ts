const MIN_DELAY = 0.06 // seconds buffered on a good connection
const MAX_DELAY = 0.5 // never wait longer than this, latency would hurt more than gaps
const SHRINK = 0.0002 // taken off per frame that played on time: 10 ms/s
const WATCH = 25 // frames (0.5 s) after running dry in which a burst marks a stall
const BURST = 0.06 // this much more than the target delay queued right after running dry is a burst
const CATCH_UP = 1.04 // playback rate while draining a backlog, too small a pitch shift to notice
const BACKLOG = 0.1 // seconds above the target delay before catching up
const OVERFLOW = 1 // seconds above the target delay that are dropped outright

export type Slot = { at: number, rate: number, resumed: boolean }

// adaptive playout for one speaker. Frames come over TCP: never lost, but late and then in a burst.
// Running dry mid-speech is what crackles, so a stall grows the buffer to its length and a calm network shrinks it back.
// Running dry also happens on every pause between phrases; those are told apart by what follows:
// a stall is followed by a burst of queued frames, a pause by frames at the normal 20 ms pace.
export class JitterBuffer {
  delay = MIN_DELAY
  private next = 0 // when the queued audio runs out, on the AudioContext clock
  private watch = 0 // frames left to look for a burst after running dry

  // when to start a frame of `duration` seconds, or null to drop it
  schedule(now: number, duration: number): Slot | null {
    const ahead = this.next - now
    if (ahead <= 0) {
      this.next = now + this.delay
      this.watch = WATCH
      return this.take(duration, 1, true)
    }
    if (ahead > this.delay + OVERFLOW) return null
    if (this.watch > 0) {
      this.watch--
      // the burst is as long as the stall was: wait that long from now on, so the same stall is absorbed
      if (ahead > this.delay + BURST) this.delay = Math.min(MAX_DELAY, ahead)
    } else {
      this.delay = Math.max(MIN_DELAY, this.delay - SHRINK)
    }
    return this.take(duration, ahead > this.delay + BACKLOG ? CATCH_UP : 1, false)
  }

  private take(duration: number, rate: number, resumed: boolean): Slot {
    const at = this.next
    this.next += duration / rate
    return { at, rate, resumed }
  }
}
