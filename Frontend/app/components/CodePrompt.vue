<script setup lang="ts">
defineProps<{ code: number | null }>()
</script>

<template>
  <section aria-live="polite">
    <p>В игре откроется форма. Нажмите на код:</p>
    <p class="code">{{ code }}</p>
  </section>
</template>

<style scoped>
.code { font-size: 4rem; font-weight: 700; text-align: center; margin: 0; letter-spacing: .1em; }
</style>
