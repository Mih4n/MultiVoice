<script setup lang="ts">
const emit = defineEmits<{ submit: [url: string] }>()
const address = ref('')

// a bare domain means https; the app's WebView refuses plain http anyway
function submit() {
  const text = address.value.trim()
  try {
    const url = new URL(/^https?:\/\//.test(text) ? text : `https://${text}`)
    emit('submit', url.origin)
  } catch {}
}
</script>

<template>
  <form @submit.prevent="submit">
    <h2>Сервер</h2>
    <Input v-model="address" class="w100" placeholder="voice.example.com" />
    <Button class="w100" role="button" tabindex="0" @click="submit" @keydown.enter.space.prevent="submit">Подключиться</Button>
  </form>
</template>

<style scoped lang="less">
form {
  display: grid;
  gap: 10px;
}
</style>
