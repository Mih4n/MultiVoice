<script setup lang="ts">
import type { PlayerVoice, SpeakerSetting } from '~/composables/useVoice'

const props = defineProps<{ players: Record<string, PlayerVoice>, settings: Record<string, SpeakerSetting> }>()
const emit = defineEmits<{ volume: [xuid: string, volume: number, save: boolean], mute: [xuid: string, muted: boolean] }>()

const STATES = { listening: 'слушает', mic: 'микрофон включён', speaking: 'говорит' }
const sorted = computed(() => Object.values(props.players).sort((a, b) => a.Name.localeCompare(b.Name)))
const setting = (xuid: string) => props.settings[xuid] ?? { Volume: 1, Muted: false }
const percent = (xuid: string) => `${Math.round(setting(xuid).Volume * 100)}%`
const volumeOf = (e: Event) => Number((e.target as HTMLInputElement).value)
</script>

<template>
  <section aria-labelledby="players-title">
    <h2 id="players-title">Игроки в голосе</h2>
    <p v-if="!sorted.length">Пока никого нет</p>
    <ul>
      <li v-for="p in sorted" :key="p.Xuid">
        <span><b>{{ p.Name }}</b> — <span :class="p.State">{{ STATES[p.State] }}</span></span>
        <label :for="`volume-${p.Xuid}`">Громкость {{ p.Name }}</label>
        <span class="row">
          <input
            :id="`volume-${p.Xuid}`" type="range" min="0" max="5" step="0.05"
            :value="setting(p.Xuid).Volume" :aria-valuetext="percent(p.Xuid)"
            @input="emit('volume', p.Xuid, volumeOf($event), false)"
            @change="emit('volume', p.Xuid, volumeOf($event), true)"
          >
          <output :for="`volume-${p.Xuid}`">{{ percent(p.Xuid) }}</output>
          <button :aria-pressed="setting(p.Xuid).Muted" :aria-label="`Заглушить ${p.Name}`" @click="emit('mute', p.Xuid, !setting(p.Xuid).Muted)">
            {{ setting(p.Xuid).Muted ? 'Заглушён' : 'Заглушить' }}
          </button>
        </span>
      </li>
    </ul>
  </section>
</template>

<style scoped>
ul { list-style: none; padding: 0; margin: 0; display: grid; gap: 1rem; }
li { display: grid; gap: .25rem; }
.row { display: flex; gap: .5rem; align-items: center; }
.row input { flex: 1; }
output { min-width: 3.5rem; text-align: right; }
.speaking { color: #2a2; font-weight: bold; }
</style>
