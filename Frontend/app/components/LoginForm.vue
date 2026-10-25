<script setup lang="ts">
const emit = defineEmits<{ submit: [nick: string] }>()
const nick = ref('')
</script>

<template>
  <form @submit.prevent="emit('submit', nick)">
    <label for="nick">Ник в Minecraft</label>
    <input id="nick" v-model="nick" required autocomplete="username">
    <button type="submit">Получить код</button>
  </form>
</template>
