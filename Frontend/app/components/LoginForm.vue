<script setup lang="ts">
const emit = defineEmits<{ submit: [nick: string] }>()
const nick = ref('')
const submit = () => { if (nick.value.trim()) emit('submit', nick.value) }
</script>

<template>
  <form @submit.prevent="submit">
    <h2>Вход</h2>
    <Input v-model="nick" class="w100" placeholder="Ник в Minecraft" />
    <Button class="w100" role="button" tabindex="0" @click="submit" @keydown.enter.space.prevent="submit">Получить код</Button>
  </form>
</template>

<style scoped lang="less">
form {
  display: grid;
  gap: 10px;
}
</style>
