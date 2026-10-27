import pitchUrl from './pitch.worklet.js?worker&url'
import { JitterBuffer } from './JitterBuffer'
import type { ReverbImpulses } from './ReverbImpulses'
import type { VoiceMix } from './VoiceMix'

const SMOOTH = 0.05 // setTargetAtTime constant, avoids clicks when params change
const FRAME_US = 20000 // one Opus frame
const FADE_IN = 120 // samples (2.5 ms) ramped in after running dry, so restarting doesn't click

// one remote speaker: Opus decoder -> HRTF panner -> effects -> speakers
//   dry:    panner -> pitch -> highpass -> lowpass -> distortion -> compressor -> volume -> out
//           room voice skips the panner and goes straight into pitch
//   reverb: volume -> reverbWet -> convolver -> out
//   echo:   volume -> echoWet -> delay -> out, delay -> echoFeedback -> delay
export class SpeakerChain {
  private readonly panner: PannerNode
  private readonly pitch: AudioWorkletNode
  private readonly distortion: WaveShaperNode
  private readonly compressor: DynamicsCompressorNode
  private readonly highpass: BiquadFilterNode
  private readonly lowpass: BiquadFilterNode
  private readonly volume: GainNode
  private readonly reverb: ConvolverNode
  private readonly reverbWet: GainNode
  private readonly echo: DelayNode
  private readonly echoFeedback: GainNode
  private readonly echoWet: GainNode
  private readonly decoder: AudioDecoder
  private reverbDecay = 2
  private drive = 0
  private room = false
  private timestamp = 0
  private readonly jitter = new JitterBuffer()

  constructor(private readonly ctx: AudioContext, private readonly impulses: ReverbImpulses, name: string) {
    // the server sends the distance volume, so the panner only does direction
    this.panner = new PannerNode(ctx, { panningModel: 'HRTF', rolloffFactor: 0 })
    this.pitch = new AudioWorkletNode(ctx, 'pitch') // module loaded by loadPitch() before any chain exists
    this.distortion = new WaveShaperNode(ctx, { oversample: '4x' })
    this.compressor = new DynamicsCompressorNode(ctx, { threshold: 0, ratio: 1, knee: 10 })
    this.highpass = new BiquadFilterNode(ctx, { type: 'highpass', frequency: 0 })
    this.lowpass = new BiquadFilterNode(ctx, { type: 'lowpass', frequency: 20000 })
    this.volume = new GainNode(ctx, { gain: 0 })
    this.reverb = new ConvolverNode(ctx, { buffer: impulses.get(this.reverbDecay) })
    this.reverbWet = new GainNode(ctx, { gain: 0 })
    this.echo = new DelayNode(ctx, { maxDelayTime: 2 })
    this.echoFeedback = new GainNode(ctx, { gain: 0 })
    this.echoWet = new GainNode(ctx, { gain: 0 })

    this.panner.connect(this.pitch).connect(this.highpass).connect(this.lowpass)
      .connect(this.distortion).connect(this.compressor).connect(this.volume).connect(ctx.destination)
    this.volume.connect(this.reverbWet).connect(this.reverb).connect(ctx.destination)
    this.volume.connect(this.echoWet).connect(this.echo).connect(ctx.destination)
    this.echo.connect(this.echoFeedback).connect(this.echo)

    this.decoder = new AudioDecoder({
      output: (data) => this.play(data),
      error: (e) => console.error('decoder', name, e),
    })
    this.decoder.configure({ codec: 'opus', sampleRate: 48000, numberOfChannels: 1 })
  }

  apply(x: number, y: number, z: number, mix: VoiceMix) {
    const t = this.ctx.currentTime
    const to = (param: AudioParam, value: number) => param.setTargetAtTime(value, t, SMOOTH)
    to(this.panner.positionX, x)
    to(this.panner.positionY, y)
    to(this.panner.positionZ, z)
    to(this.volume.gain, mix.Volume)
    to(this.highpass.frequency, mix.Highpass)
    to(this.lowpass.frequency, mix.Lowpass)
    to(this.reverbWet.gain, mix.ReverbWet)
    if (mix.ReverbWet > 0 && Math.abs(mix.ReverbDecay - this.reverbDecay) >= 0.5) {
      this.reverbDecay = mix.ReverbDecay
      this.reverb.buffer = this.impulses.get(mix.ReverbDecay)
    }
    to(this.echo.delayTime, Math.min(mix.EchoDelay, 2))
    to(this.echoFeedback.gain, Math.min(mix.EchoFeedback, 0.95)) // >= 1 would ring forever
    to(this.echoWet.gain, mix.EchoWet)
    to(this.pitch.parameters.get('semitones')!, mix.Pitch)
    to(this.compressor.threshold, -50 * mix.Compressor)
    to(this.compressor.ratio, 1 + 19 * mix.Compressor)
    if (mix.Distortion !== this.drive) {
      this.drive = mix.Distortion
      this.distortion.curve = this.drive > 0 ? overdrive(this.drive * 100) : null
    }
    this.room = mix.Room
  }

  decode(frame: Uint8Array) {
    this.decoder.decode(new EncodedAudioChunk({ type: 'key', timestamp: this.timestamp += FRAME_US, data: frame }))
  }

  private play(data: AudioData) {
    const { ctx } = this
    const buf = ctx.createBuffer(1, data.numberOfFrames, data.sampleRate)
    data.copyTo(buf.getChannelData(0), { planeIndex: 0, format: 'f32-planar' })
    data.close()
    const slot = this.jitter.schedule(ctx.currentTime, buf.duration)
    if (!slot) return
    if (slot.resumed) {
      const samples = buf.getChannelData(0)
      for (let i = 0; i < Math.min(FADE_IN, samples.length); i++) samples[i]! *= i / FADE_IN
    }
    const src = new AudioBufferSourceNode(ctx, { buffer: buf, playbackRate: slot.rate })
    src.connect(this.room ? this.pitch : this.panner)
    src.start(slot.at)
  }
}

// soft clipper: k = 0 is clean, higher k flattens peaks into a cheap-speaker crunch
function overdrive(k: number) {
  const curve = new Float32Array(1024)
  for (let i = 0; i < curve.length; i++) {
    const x = i / (curve.length - 1) * 2 - 1
    curve[i] = (1 + k) * x / (1 + k * Math.abs(x))
  }
  return curve
}

export const loadPitch = (ctx: AudioContext) => ctx.audioWorklet.addModule(pitchUrl)
