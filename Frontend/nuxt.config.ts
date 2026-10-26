export default defineNuxtConfig({
  compatibilityDate: '2026-10-01',
  // mic, WebCodecs and Web Audio are browser-only
  ssr: false,
  // the capture worklet bundles RNNoise; module output keeps import.meta.url, which a worklet has and `self` doesn't
  vite: { worker: { format: 'es' } },
  runtimeConfig: {
    public: { serverUrl: 'http://localhost:5000' },
  },
})
