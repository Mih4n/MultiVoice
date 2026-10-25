// Dev test speaker: stands where <player> is now and beeps through the real routing/pipeline.
// node scripts/voicebot.mjs <player> [http://localhost:5000] [secret]   (needs ffmpeg with libopus, Voice:DevHub=true)
import { spawn } from 'node:child_process'
import * as signalR from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'

const [near, base = 'http://localhost:5000', secret = 'change-me'] = process.argv.slice(2)
if (!near) { console.error('usage: node scripts/voicebot.mjs <player> [url] [secret]'); process.exit(1) }

const conn = new signalR.HubConnectionBuilder().withUrl(base + '/dev').withHubProtocol(new MessagePackHubProtocol()).build()
await conn.start()
const bot = await conn.invoke('SpawnBot', secret, near)
console.log(`VoiceBot at ${bot.X.toFixed(1)} ${bot.Y.toFixed(1)} ${bot.Z.toFixed(1)} (${bot.DimensionId}), beeping. Ctrl+C to stop.`)

// 0.3 s beep every second; -re paces output in real time, small pages flush every 20 ms frame
const ffmpeg = spawn('ffmpeg', [
  '-loglevel', 'error', '-re', '-f', 'lavfi', '-i', 'sine=frequency=440:sample_rate=48000',
  '-af', "volume='if(lt(mod(t,1),0.3),0.8,0)':eval=frame", '-ac', '1',
  '-c:a', 'libopus', '-b:a', '24k', '-frame_duration', '20', '-application', 'voip',
  '-page_duration', '20000', '-f', 'ogg', '-',
], { stdio: ['ignore', 'pipe', 'inherit'] })

// minimal Ogg demuxer: yields Opus packets, skipping the OpusHead/OpusTags headers
let buf = Buffer.alloc(0), partial = Buffer.alloc(0), packets = 0
ffmpeg.stdout.on('data', (chunk) => {
  buf = Buffer.concat([buf, chunk])
  while (buf.length >= 27) {
    if (buf.toString('latin1', 0, 4) !== 'OggS') throw new Error('lost Ogg sync')
    const segments = buf[26]
    if (buf.length < 27 + segments) return
    const lacing = buf.subarray(27, 27 + segments)
    const size = lacing.reduce((a, b) => a + b, 0)
    if (buf.length < 27 + segments + size) return
    let offset = 27 + segments
    for (const len of lacing) {
      partial = Buffer.concat([partial, buf.subarray(offset, offset + len)])
      offset += len
      if (len < 255) {
        if (packets++ >= 2 && conn.state === 'Connected') conn.send('Voice', new Uint8Array(partial))
        partial = Buffer.alloc(0)
      }
    }
    buf = buf.subarray(offset)
  }
})

ffmpeg.on('exit', (code) => { console.error(`ffmpeg exited ${code}`); process.exit(1) })
process.on('SIGINT', async () => { ffmpeg.kill(); await conn.stop(); process.exit(0) })
