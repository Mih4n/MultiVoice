<script setup lang="ts">
defineProps<{ name: string, live: boolean, gameMuted: boolean }>()
const muted = defineModel<boolean>('muted', { required: true })
const emit = defineEmits<{ start: [], logout: [] }>()
</script>

<template>
  <section>
    <p>Вы вошли как <b>{{ name }}</b></p>
    <button v-if="!live" @click="emit('start')">Включить голос</button>
    <template v-else>
      <button :aria-pressed="muted" @click="muted = !muted">{{ muted ? 'Включить микрофон' : 'Выключить микрофон' }}</button>
      <p v-if="gameMuted">Микрофон выключен командой /mute в игре</p>
    </template>
    <button class="link" @click="emit('logout')">Выйти</button>
  </section>
</template>

<style scoped>
.link { background: none; border: none; text-decoration: underline; cursor: pointer; justify-self: start; padding: 0; }
</style>
