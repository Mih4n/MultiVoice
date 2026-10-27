// mirrors VoiceMix on the server; its defaults are a dry, unchanged voice
export type VoiceMix = {
  Volume: number
  Lowpass: number
  Highpass: number
  ReverbWet: number
  ReverbDecay: number
  EchoDelay: number
  EchoFeedback: number
  EchoWet: number
  Distortion: number // 0..1 overdrive
  Compressor: number // 0..1 how hard the voice is squashed
  Pitch: number // semitones
  Room: boolean // room voice: no 3D, comes from everywhere
}
