<script setup lang="ts">
import { mdiHeadphones, mdiHeadphonesOff, mdiMicrophone, mdiMicrophoneOff } from '@mdi/js'

const props = defineProps<{ suspended: boolean, speaking: boolean, level: number, gameMuted: boolean, bannedUntil: number | null, devices: MediaDeviceInfo[] }>()
const micOn = defineModel<boolean>('micOn', { required: true })
const deafened = defineModel<boolean>('deafened', { required: true })
const deviceId = defineModel<string>('deviceId', { required: true })
const denoise = defineModel<boolean>('denoise', { required: true })
const sensitivity = defineModel<number>('sensitivity', { required: true })

// the slider is the activation threshold, like Discord: further right needs a louder voice; 0..1 is -100..0 dB
const threshold = computed({ get: () => 1 - sensitivity.value, set: (v) => { sensitivity.value = 1 - v } })
const talking = computed(() => micOn.value && !deafened.value)
const status = computed(() => {
  if (deafened.value) return 'Вы никого не слышите, и вас тоже не слышно'
  if (!micOn.value) return 'Микрофон выключен'
  if (props.bannedUntil === 0) return 'Администратор запретил вам говорить'
  if (props.bannedUntil) return `Администратор запретил вам говорить до ${new Date(props.bannedUntil).toLocaleString('ru')}`
  if (props.gameMuted) return 'Микрофон выключен командой /mute в игре'
  return props.speaking ? 'Вас слышно' : 'Тишина'
})

// DropDown works with labels, the mic is picked by deviceId
const DEFAULT_DEVICE = 'По умолчанию'
const labelOf = (d: MediaDeviceInfo, i: number) => d.label || `Микрофон ${i + 1}`
const deviceLabels = computed(() => [DEFAULT_DEVICE, ...props.devices.map(labelOf)])
const deviceLabel = computed({
  get: () => {
    const i = props.devices.findIndex((d) => d.deviceId === deviceId.value)
    return i < 0 ? DEFAULT_DEVICE : labelOf(props.devices[i]!, i)
  },
  set: (label) => { deviceId.value = props.devices.find((d, i) => labelOf(d, i) === label)?.deviceId ?? '' },
})
</script>

<template>
  <section aria-labelledby="settings-title">
    <h2 id="settings-title">Настройки</h2>
    <p v-if="suspended" class="hint">Нажмите в любом месте страницы, чтобы слышать игроков</p>

    <div class="toggles">
      <button
        class="toggle" :class="{ off: !talking }" :disabled="deafened"
        :aria-label="micOn ? 'Выключить микрофон' : 'Включить микрофон'" :title="micOn ? 'Выключить микрофон' : 'Включить микрофон'"
        @click="micOn = !micOn"
      >
        <Icon :path="talking ? mdiMicrophone : mdiMicrophoneOff" />
      </button>
      <button
        class="toggle" :class="{ off: deafened }"
        :aria-label="deafened ? 'Слушать игроков' : 'Не слушать игроков'" :title="deafened ? 'Слушать игроков' : 'Не слушать игроков'"
        @click="deafened = !deafened"
      >
        <Icon :path="deafened ? mdiHeadphonesOff : mdiHeadphones" />
      </button>
      <p class="status" aria-live="polite">{{ status }}</p>
    </div>

    <template v-if="talking">
      <div class="group">
        <p>Микрофон</p>
        <DropDown v-model="deviceLabel" :options="deviceLabels" />
      </div>

      <div class="group">
        <label for="sensitivity">Чувствительность</label>
        <div class="meter" :class="{ speaking }">
          <div class="level" :style="{ width: `${level * 100}%` }" />
          <input
            id="sensitivity" v-model.number="threshold" type="range" min="0.05" max="0.95" step="0.01"
            :aria-valuetext="`${Math.round(threshold * 100 - 100)} дБ`"
          >
        </div>
        <p class="muted small">Голос передаётся, когда полоса заходит за ползунок</p>
      </div>

      <Checkbox v-model="denoise">Шумоподавление</Checkbox>
    </template>
  </section>
</template>

<style scoped lang="less">
section {
  display: grid;
  gap: 16px;
}

.hint {
  padding: 10px 12px;

  color: oklch(0% 0 0);
  background: var(--scolor-accent);
  border-radius: 10px;
}

.toggles {
  display: flex;
  align-items: center;
  gap: 10px;
}

.toggle {
  width: 56px;
  height: 56px;
  flex: none;

  display: inline-grid;
  place-items: center;

  border-radius: 14px;
  cursor: pointer;

  color: oklch(0% 0 0);
  background: var(--scolor-accent);
  transition: background-color 0.2s ease;

  &:hover {
    background: var(--scolor-accent-hover);
  }

  &.off {
    color: oklch(100% 0 0);
    background: var(--scolor-error);
  }

  &:disabled {
    cursor: not-allowed;
    opacity: 0.6;
  }
}

.status {
  flex: 1;
  min-width: 0;
}

.group {
  display: grid;
  gap: 8px;
}

.small {
  font-size: 0.8125rem;
}

// Discord-style: a live level bar under the threshold slider
.thumb() {
  width: 12px;
  height: 24px;
  border: none;
  border-radius: 3px;
  background: oklch(100% 0 0);
}

.meter {
  position: relative;
  height: 24px;
  overflow: hidden;

  background: var(--scolor-overlay-white-10);
  border-radius: 5px;

  .level {
    position: absolute;
    inset: 0 auto 0 0;

    background: var(--scolor-overlay-white-30);
    transition: width 50ms linear;

    @media (prefers-reduced-motion: reduce) {
      transition: none;
    }
  }

  &.speaking .level {
    background: var(--scolor-accent);
  }

  input {
    position: absolute;
    inset: 0;
    height: 100%;
    margin: 0;

    appearance: none;
    background: transparent;
    cursor: pointer;
    outline: none;

    &::-webkit-slider-thumb {
      appearance: none;
      .thumb();
    }

    &::-moz-range-thumb {
      .thumb();
    }

    &::-webkit-slider-runnable-track {
      background: transparent;
    }

    &::-moz-range-track,
    &::-moz-range-progress {
      background: transparent;
    }
  }
}
</style>
