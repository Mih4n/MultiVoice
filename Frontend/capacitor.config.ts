import type { CapacitorConfig } from '@capacitor/cli'

// Android app: the generated site inside a WebView, kept alive in the background by VoiceService
const config: CapacitorConfig = {
  appId: 'com.multipunk.multivoice',
  appName: 'MultiVoice',
  webDir: '.output/public',
}

export default config
