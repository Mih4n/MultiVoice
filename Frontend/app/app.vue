<script setup lang="ts">
const auth = useAuth()
const voice = useVoice()
const { muted } = voice
</script>

<template>
  <main>
    <h1>MultiVoice</h1>

    <p v-if="auth.status.value === 'connecting'">Подключение…</p>
    <LoginForm v-else-if="auth.status.value === 'login'" @submit="auth.requestCode" />
    <CodePrompt v-else-if="auth.status.value === 'waiting'" :code="auth.code.value" />
    <VoicePanel
      v-else
      v-model:muted="muted"
      :name="auth.name.value"
      :live="voice.live.value"
      :game-muted="voice.gameMuted.value"
      @start="voice.start"
      @logout="auth.logout"
    />

    <p v-for="message in [auth.error.value, voice.error.value].filter(Boolean)" :key="message" role="alert" class="error">{{ message }}</p>
  </main>
</template>

<style>
:root { color-scheme: light dark; font-family: system-ui, sans-serif; }
body { margin: 0; }
main { max-width: 28rem; margin: 4rem auto; padding: 0 1rem; display: grid; gap: 1rem; }
form, section { display: grid; gap: .5rem; }
input, button { font: inherit; padding: .5rem .75rem; }
.error { color: #d33; }
</style>
