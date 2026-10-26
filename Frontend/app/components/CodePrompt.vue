<script setup lang="ts">
defineProps<{ code: number | null }>()
</script>

<template>
  <section aria-live="polite" class="prompt">
    <p>В игре откроется форма. Нажмите на код:</p>
    <p class="code">{{ code }}</p>
  </section>
</template>

<style scoped lang="less">
.prompt {
  display: grid;
  gap: 10px;
}

.code {
  margin: 0;

  color: var(--scolor-accent);
  font-size: 4rem;
  font-weight: 800;
  text-align: center;
  letter-spacing: 0.1em;
}
</style>
