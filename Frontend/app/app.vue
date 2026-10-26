<script setup lang="ts">
const auth = useAuth()
const voice = useVoice()
const { micOn, deafened, deviceId, denoise, sensitivity } = voice
watch(auth.status, (status) => { if (status === 'ready') voice.start() }, { immediate: true })
</script>

<template>
  <main>
    <h1>MultiVoice</h1>

    <p v-if="auth.status.value === 'connecting'">Подключение…</p>
    <LoginForm v-else-if="auth.status.value === 'login'" @submit="auth.requestCode" />
    <CodePrompt v-else-if="auth.status.value === 'waiting'" :code="auth.code.value" />
    <template v-else>
      <VoicePanel
        v-model:mic-on="micOn"
        v-model:deafened="deafened"
        v-model:device-id="deviceId"
        :devices="voice.devices.value"
        v-model:denoise="denoise"
        v-model:sensitivity="sensitivity"
        :name="auth.name.value"
        :suspended="voice.suspended.value"
        :speaking="voice.speaking.value"
        :game-muted="voice.gameMuted.value"
        @logout="auth.logout"
      />
      <PlayerList
        v-if="voice.live.value"
        :players="voice.players.value"
        :settings="voice.settings.value"
        @volume="voice.setVolume"
        @mute="voice.setMuted"
      />
    </template>

    <p v-for="message in [auth.error.value, voice.error.value].filter(Boolean)" :key="message" role="alert" class="error">{{ message }}</p>
  </main>
</template>

<style>
:root { color-scheme: light dark; font-family: system-ui, sans-serif; }
body { margin: 0; }
main { max-width: 28rem; margin: 4rem auto; padding: 0 1rem; display: grid; gap: 1rem; }
form, section { display: grid; gap: .5rem; }
input, button, select { font: inherit; padding: .5rem .75rem; }
.error { color: #d33; }
</style>
