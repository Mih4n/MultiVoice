<script setup lang="ts">
import { mdiDoorOpen, mdiLogin, mdiPlus } from '@mdi/js'
import type { Room } from '~/composables/useVoice'

defineProps<{ room: Room | null }>()
const emit = defineEmits<{ create: [], join: [code: string], leave: [] }>()
const code = ref('')
const join = () => { if (code.value.trim()) emit('join', code.value.trim()) }
</script>

<template>
  <section aria-labelledby="room-title">
    <h2 id="room-title">Комната</h2>

    <template v-if="room">
      <p>Код: <span class="code">{{ room.code }}</span></p>
      <p class="muted small">Все в комнате слышат друг друга где угодно в мире. В игре: /room join {{ room.code }}</p>
      <ul>
        <li v-for="name in room.members" :key="name">{{ name }}</li>
      </ul>
      <Button class="red" role="button" tabindex="0" @click="emit('leave')" @keydown.enter.space.prevent="emit('leave')">
        <Icon :path="mdiDoorOpen" />
        Выйти из комнаты
      </Button>
    </template>

    <template v-else>
      <p class="muted small">Комната - голос слышен всем участникам без расстояния</p>
      <form class="join" @submit.prevent="join">
        <Input v-model="code" placeholder="Код комнаты" />
        <Button class="icon" role="button" tabindex="0" aria-label="Войти" title="Войти" @click="join" @keydown.enter.space.prevent="join">
          <Icon :path="mdiLogin" />
        </Button>
      </form>
      <Button role="button" tabindex="0" @click="emit('create')" @keydown.enter.space.prevent="emit('create')">
        <Icon :path="mdiPlus" />
        Создать комнату
      </Button>
    </template>
  </section>
</template>

<style scoped lang="less">
section {
  display: grid;
  gap: 12px;
}

.code {
  font-size: 1.25rem;
  letter-spacing: 0.15em;
  font-variant-numeric: tabular-nums;
}

.small {
  font-size: 0.8125rem;
}

ul {
  display: grid;
  gap: 4px;
  margin: 0;
  padding: 0;
  list-style: none;
}

li {
  padding: 8px 12px;

  background: var(--scolor-overlay-white-10);
  border-radius: 10px;
}

.join {
  display: flex;
  gap: 8px;

  .input {
    flex: 1;
    min-width: 0;
  }
}
</style>
