// forwards raw mic samples to the main thread, where WebCodecs encodes them
class Capture extends AudioWorkletProcessor {
  process(inputs) {
    const channel = inputs[0][0]
    if (channel) this.port.postMessage(channel.slice())
    return true
  }
}
registerProcessor('capture', Capture)
