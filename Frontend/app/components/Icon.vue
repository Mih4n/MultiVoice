<script setup lang="ts">
// path is an @mdi/js icon
defineProps<{ path: string }>()
</script>

<template>
  <svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><path :d="path" fill="currentColor" /></svg>
</template>

<style scoped lang="less">
.icon {
  width: 24px;
  height: 24px;
  flex: none;
}
</style>
