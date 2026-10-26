// End-to-end check against a running server (dotnet run in Backend/MultiVoice.Server):
// fake plugin + fake browsers go through login, chunk routing and the effect pipeline.
// node scripts/smoke.mjs [http://localhost:5000] [secret]
import assert from 'node:assert/strict'
import * as signalR from '@microsoft/signalr'
import { MessagePackHubProtocol } from '@microsoft/signalr-protocol-msgpack'

const base = process.argv[2] ?? 'http://localhost:5000'
const secret = process.argv[3] ?? 'change-me'
const wait = (ms) => new Promise((r) => setTimeout(r, ms))
const player = (name, x, extra = {}) => ({ xuid: `x-${name}`, name, x, y: 64, z: 0, yaw: 0, pitch: 0, dimensionId: 'Overworld', inWater: false, inCave: false, muted: false, ...extra })
const hub = (path, options = {}, msgpack = true) => {
  const b = new signalR.HubConnectionBuilder().withUrl(base + path, options).configureLogging(signalR.LogLevel.None)
  return (msgpack ? b.withHubProtocol(new MessagePackHubProtocol()) : b).build()
}

// wrong secret: the hub drops the connection
const bad = hub('/api/minecraft', { headers: { 'X-Secret': 'nope' } }, false)
const badClosed = new Promise((r) => bad.onclose(r))
await bad.start()
await badClosed

// plugin speaks JSON, like signalrcore in the Endstone plugin
const plugin = hub('/api/minecraft', { headers: { 'X-Secret': secret } }, false)
const asks = [], voiceStates = {}
plugin.on('Ask', (name, codes) => asks.push({ name, codes }))
plugin.on('VoiceStates', (changes) => Object.assign(voiceStates, changes))
await plugin.start()
// plugin sends only changed players and the xuids that left
const snapshot = (changed, left = []) => plugin.invoke('Players', changed, left)

// Alice and Bob are 3 blocks apart across a chunk border (x=31 / x=34 with ChunkSize 32), Carol is far away
await snapshot([player('Alice', 31), player('Bob', 34, { inWater: true }), player('Carol', 500)])

async function login(name, pickCorrect = true) {
  const auth = hub('/api/auth')
  const token = new Promise((r) => auth.on('Confirmed', r))
  const rejected = new Promise((r) => auth.on('Rejected', () => r(null)))
  await auth.start()
  const code = await auth.invoke('RequestCode', name)
  await wait(200)
  const ask = asks.findLast((a) => a.name === name)
  assert.equal(ask.codes.length, 3)
  assert.ok(ask.codes.includes(code))
  await plugin.invoke('Confirm', `x-${name}`, pickCorrect ? code : ask.codes.find((x) => x !== code))
  const t = await Promise.race([token, rejected])
  return { auth, token: t }
}

async function voiceOf(token) {
  const voice = hub('/api/voice', { accessTokenFactory: () => token })
  const heard = [], self = [], players = {}, settings = {}
  voice.on('Voice', (...args) => heard.push(args))
  voice.on('Self', (...args) => self.push(args))
  voice.on('Settings', (s) => Object.assign(settings, s))
  voice.on('Players', (list) => list.forEach((p) => { players[p.Xuid] = p.State }))
  voice.on('PlayerState', (p) => { players[p.Xuid] = p.State })
  await voice.start()
  await wait(100)
  return { voice, heard, self, players, settings }
}

await assert.rejects(login('Nobody'), /не в игре/)

const alice = await login('Alice')
assert.equal(await alice.auth.invoke('Resume', alice.token), 'Alice')
const bob = await login('Bob')
const carol = await login('Carol', false)
assert.equal(carol.token, null, 'wrong code is rejected')
await assert.rejects(carol.auth.invoke('RequestCode', 'Carol'), /30 секунд/)

// a voice connection with a bad token gets dropped
const intruder = hub('/api/voice', { accessTokenFactory: () => 'bogus' })
const intruderClosed = new Promise((r) => intruder.onclose(r))
await intruder.start()
await intruderClosed

const A = await voiceOf(alice.token)
const B = await voiceOf(bob.token)

// voice states: connected = listening; the browser reports mic/speaking; the plugin and other sites get changes
assert.equal(B.players['x-Alice'], 'listening', 'snapshot lists Alice')
assert.equal(A.players['x-Bob'], 'listening', 'Alice is told Bob joined')
assert.equal(A.players['x-Alice'], undefined, 'own state is not listed')
await A.voice.invoke('State', true, true)
await wait(150)
assert.equal(B.players['x-Alice'], 'speaking')
assert.equal(voiceStates['x-Alice'], 'speaking')
await A.voice.invoke('State', true, false)
await wait(150)
assert.equal(voiceStates['x-Alice'], 'mic')
const say = async (bytes) => { await A.voice.invoke('Voice', new Uint8Array(bytes)); await wait(150) }

await say([1, 2, 3])
assert.equal(B.heard.length, 1, 'Bob hears Alice across the chunk border')
let [speaker, frame, , , , mix] = B.heard.at(-1)
assert.equal(speaker, 'Alice')
assert.deepEqual([...frame], [1, 2, 3])
assert.equal(mix.Volume, 1, 'within DistanceFade.from')
assert.equal(mix.Lowpass, 600, 'Bob is in water')
assert.equal(mix.ReverbWet, 0)
assert.equal(A.heard.length, 0, 'speaker does not hear herself')

// 18 blocks: DistanceFade(4, 32) -> (32 - 18) / 28 = 0.5; Alice deep in a cave -> reverb + echo
await snapshot([player('Alice', 0, { y: -10, inCave: true }), player('Bob', 18, { y: -10 })])
await say([4])
mix = B.heard.at(-1)[5]
assert.ok(Math.abs(mix.Volume - 0.5) < 1e-6, `volume ${mix.Volume}`)
assert.equal(mix.Lowpass, 20000)
assert.equal(mix.ReverbWet, 0.5)
assert.equal(mix.ReverbDecay, 2.5)
assert.ok(Math.abs(mix.EchoDelay - 0.3) < 1e-6)
assert.equal(A.self.at(-1)[1], -10, 'Self carries listener position')

// per-listener volume: Bob sets Alice to 300% (0..500%, clamped), then mutes her
await assert.rejects(B.voice.invoke('SetVolume', 'x-Bob', 2), /не в игре/, 'own volume is not a setting')
await B.voice.invoke('SetVolume', 'x-Alice', 3)
await say([8])
assert.ok(Math.abs(B.heard.at(-1)[5].Volume - 1.5) < 1e-6, 'pipeline 0.5 x 300%')
await B.voice.invoke('SetVolume', 'x-Alice', 99)
await say([8])
assert.ok(Math.abs(B.heard.at(-1)[5].Volume - 2.5) < 1e-6, 'clamped to 500%')
await B.voice.invoke('SetMuted', 'x-Alice', true)
const heardBeforeMute = B.heard.length
await say([8])
assert.equal(B.heard.length, heardBeforeMute, 'muted by the listener')
await B.voice.invoke('SetMuted', 'x-Alice', false)
await B.voice.invoke('SetVolume', 'x-Alice', 2)
B.heard.splice(2) // the counts below predate this block

// /mute in game
await snapshot([player('Alice', 31, { muted: true }), player('Bob', 34)])
await say([5])
assert.equal(B.heard.length, 2, 'muted speaker is not routed')
assert.equal(A.self.at(-1)[5], true)
assert.equal(voiceStates['x-Alice'], 'listening', '/mute means listening')

// out of range
await snapshot([player('Alice', 0), player('Bob', 40)])
await say([6])
assert.equal(B.heard.length, 2, '40 blocks is out of range')

// Carol leaves: she is no longer known; unchanged Alice/Bob stay routable
await snapshot([], ['x-Carol'])
await assert.rejects(alice.auth.invoke('RequestCode', 'Carol'), /не в игре/)
await snapshot([player('Alice', 0), player('Bob', 3)])
await say([7])
assert.equal(B.heard.length, 3, 'delta update keeps routing')

await A.voice.stop()
await wait(150)
assert.equal(voiceStates['x-Alice'], 'off', 'closing the site turns voice off')
assert.equal(B.players['x-Alice'], 'off')

// saved settings come back on the next connect (this one takes over Bob's session)
const B2 = await voiceOf(bob.token)
assert.deepEqual([B2.settings['x-Alice'].Volume, B2.settings['x-Alice'].Muted], [2, false])

await Promise.all([alice.auth, bob.auth, carol.auth, B.voice, B2.voice, plugin].map((c) => c.stop()))
console.log('smoke OK')
