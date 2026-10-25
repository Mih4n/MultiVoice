export default defineNuxtConfig({
  compatibilityDate: '2026-10-01',
  // mic, WebCodecs and Web Audio are browser-only
  ssr: false,
  runtimeConfig: {
    public: { serverUrl: 'http://localhost:5000' },
  },
})
