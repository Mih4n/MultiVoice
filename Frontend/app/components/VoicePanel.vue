<script setup lang="ts">
defineProps<{ name: string, suspended: boolean, speaking: boolean, gameMuted: boolean, devices: MediaDeviceInfo[] }>()
const micOn = defineModel<boolean>('micOn', { required: true })
const deafened = defineModel<boolean>('deafened', { required: true })
const deviceId = defineModel<string>('deviceId', { required: true })
const denoise = defineModel<boolean>('denoise', { required: true })
const sensitivity = defineModel<number>('sensitivity', { required: true })
const emit = defineEmits<{ logout: [] }>()

</script>

<template>
  <section>
    <p>Вы вошли как <b>{{ name }}</b></p>
    <p v-if="suspended">Нажмите в любом месте страницы, чтобы слышать игроков</p>
    <button :aria-pressed="deafened" @click="deafened = !deafened">{{ deafened ? 'Слушать игроков' : 'Не слушать игроков' }}</button>
    <p v-if="deafened">Вы никого не слышите, и вас тоже не слышно</p>
    <button v-else :aria-pressed="micOn" @click="micOn = !micOn">{{ micOn ? 'Выключить микрофон' : 'Включить микрофон' }}</button>
    <template v-if="micOn && !deafened">
      <label for="mic-device">Микрофон</label>
      <select id="mic-device" v-model="deviceId">
        <option value="">По умолчанию</option>
        <option v-for="d in devices" :key="d.deviceId" :value="d.deviceId">{{ d.label || 'Микрофон' }}</option>
      </select>
      <p v-if="gameMuted">Микрофон выключен командой /mute в игре</p>
      <p v-else>{{ speaking ? 'Вас слышно' : 'Тишина' }}</p>
      <label><input v-model="denoise" type="checkbox"> Шумоподавление</label>
      <label for="sensitivity">Чувствительность микрофона</label>
      <input id="sensitivity" v-model.number="sensitivity" type="range" min="0.05" max="0.95" step="0.05">
    </template>
    <button class="link" @click="emit('logout')">Выйти</button>
  </section>
</template>

<style scoped>
.link { background: none; border: none; text-decoration: underline; cursor: pointer; justify-self: start; padding: 0; }
</style>
