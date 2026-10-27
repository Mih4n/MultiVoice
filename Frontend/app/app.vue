<script setup lang="ts">
import { mdiLogout } from '@mdi/js'

const auth = useAuth()
const voice = useVoice()
const { micOn, deafened, deviceId, denoise, sensitivity } = voice
watch(auth.status, (status) => { if (status === 'ready') voice.start() }, { immediate: true })
</script>

<template>
  <header>
    <h1>MultiVoice</h1>
    <div class="who">
      <LightDarkToggle />
      <template v-if="auth.status.value === 'ready'">
        <span>{{ auth.name.value }}</span>
        <Button class="icon" role="button" tabindex="0" aria-label="Выйти" title="Выйти" @click="auth.logout" @keydown.enter.space.prevent="auth.logout">
          <Icon :path="mdiLogout" />
        </Button>
      </template>
    </div>
  </header>

  <main v-if="auth.status.value !== 'ready'" class="single">
    <div class="card">
      <p v-if="auth.status.value === 'connecting'">Подключение…</p>
      <LoginForm v-else-if="auth.status.value === 'login'" @submit="auth.requestCode" />
      <CodePrompt v-else :code="auth.code.value" />
    </div>
  </main>

  <main v-else class="layout">
    <PlayerList
      class="card"
      :players="voice.players.value"
      :settings="voice.settings.value"
      :admin="voice.admin.value"
      @volume="voice.setVolume"
      @mute="voice.setMuted"
      @ban="voice.ban"
      @unban="voice.unban"
    />
    <aside>
      <VoicePanel
        v-model:mic-on="micOn"
        v-model:deafened="deafened"
        v-model:device-id="deviceId"
        v-model:denoise="denoise"
        v-model:sensitivity="sensitivity"
        class="card"
        :devices="voice.devices.value"
        :suspended="voice.suspended.value"
        :speaking="voice.speaking.value"
        :level="voice.level.value"
        :game-muted="voice.gameMuted.value"
        :banned-until="voice.bannedUntil.value"
      />
      <RoomPanel
        class="card"
        :room="voice.room.value"
        @create="voice.createRoom"
        @join="voice.joinRoom"
        @leave="voice.leaveRoom"
      />
    </aside>
  </main>

  <Footer />

  <div class="errors">
    <p v-for="message in [auth.error.value, voice.error.value].filter(Boolean)" :key="message" role="alert" class="error">{{ message }}</p>
  </div>
</template>

<style scoped lang="less">
header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 20px 0;

  h1 {
    font-size: 1.5rem;
  }
}

.who {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;

  span {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 340px;
  align-items: start;
  gap: 20px;

  aside {
    display: grid;
    gap: 20px;
  }

  @media (max-width: 760px) {
    grid-template-columns: minmax(0, 1fr);

    // settings first on phones: the mic controls matter more than the list
    > :last-child {
      order: -1;
    }
  }
}

.single {
  width: 100%;
  max-width: 420px;
  margin: 40px auto 0;
}

main {
  margin-bottom: 60px;
}

.errors {
  position: fixed;
  left: 16px;
  right: 16px;
  bottom: 16px;

  display: grid;
  justify-items: center;
  gap: 8px;

  pointer-events: none;
}

.error {
  max-width: 100%;
  padding: 10px 16px;

  color: #fff;
  background: var(--scolor-error);
  border-radius: 10px;
}
</style>
