<script setup lang="ts">
import { mdiAccountVoice, mdiHeadphones, mdiMicrophone, mdiVolumeHigh, mdiVolumeOff } from '@mdi/js'
import type { PlayerVoice, SpeakerSetting } from '~/composables/useVoice'

const props = defineProps<{ players: Record<string, PlayerVoice>, settings: Record<string, SpeakerSetting> }>()
const emit = defineEmits<{ volume: [xuid: string, volume: number, save: boolean], mute: [xuid: string, muted: boolean] }>()

const STATES = {
  listening: { label: 'слушает', icon: mdiHeadphones },
  mic: { label: 'микрофон включён', icon: mdiMicrophone },
  speaking: { label: 'говорит', icon: mdiAccountVoice },
}
const sorted = computed(() => Object.values(props.players).sort((a, b) => a.Name.localeCompare(b.Name)))
const setting = (xuid: string) => props.settings[xuid] ?? { Volume: 1, Muted: false }
const percent = (xuid: string) => `${Math.round(setting(xuid).Volume * 100)}%`
const toggleMute = (xuid: string) => emit('mute', xuid, !setting(xuid).Muted)
const volumeOf = (e: Event) => Number((e.target as HTMLInputElement).value)
</script>

<template>
  <section aria-labelledby="players-title">
    <h2 id="players-title">Игроки в голосе <span class="muted">{{ sorted.length }}</span></h2>
    <p v-if="!sorted.length" class="muted">Пока никого нет</p>
    <ul>
      <li v-for="p in sorted" :key="p.Xuid" :class="{ muted: setting(p.Xuid).Muted }">
        <div class="who">
          <span class="state" :class="p.State" :title="STATES[p.State].label">
            <Icon :path="STATES[p.State].icon" />
            <span class="sr-only">{{ STATES[p.State].label }}</span>
          </span>
          <span class="name">{{ p.Name }}</span>
        </div>
        <div class="controls">
          <Button
            class="icon" :class="{ red: setting(p.Xuid).Muted }" role="button" tabindex="0"
            :aria-pressed="setting(p.Xuid).Muted" :aria-label="`Заглушить ${p.Name}`" :title="setting(p.Xuid).Muted ? 'Включить звук' : 'Заглушить'"
            @click="toggleMute(p.Xuid)" @keydown.enter.space.prevent="toggleMute(p.Xuid)"
          >
            <Icon :path="setting(p.Xuid).Muted ? mdiVolumeOff : mdiVolumeHigh" />
          </Button>
          <input
            :id="`volume-${p.Xuid}`" type="range" min="0" max="5" step="0.05"
            :value="setting(p.Xuid).Volume" :aria-label="`Громкость ${p.Name}`" :aria-valuetext="percent(p.Xuid)"
            @input="emit('volume', p.Xuid, volumeOf($event), false)"
            @change="emit('volume', p.Xuid, volumeOf($event), true)"
          >
          <output :for="`volume-${p.Xuid}`">{{ percent(p.Xuid) }}</output>
        </div>
      </li>
    </ul>
  </section>
</template>

<style scoped lang="less">
ul {
  display: grid;
  gap: 8px;
  margin: 0;
  padding: 0;
  list-style: none;
}

li {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 280px);
  align-items: center;
  gap: 12px;
  padding: 10px 12px;

  background: var(--scolor-overlay-white-10);
  border-radius: 10px;

  &.muted .name {
    text-decoration: line-through;
  }

  @media (max-width: 520px) {
    grid-template-columns: minmax(0, 1fr);
  }
}

.who {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.state {
  width: 36px;
  height: 36px;
  flex: none;

  display: inline-grid;
  place-items: center;

  color: oklch(100% 0 0 / 0.6);
  border-radius: 50%;

  &.mic {
    color: #fff;
  }

  &.speaking {
    color: #000;
    background: var(--scolor-accent);
    box-shadow: 0 0 0 3px oklch(from var(--scolor-accent) l c h / 0.35);
  }
}

.controls {
  display: flex;
  align-items: center;
  gap: 10px;
}

output {
  min-width: 3.25rem;
  text-align: right;
  font-variant-numeric: tabular-nums;
}
</style>
