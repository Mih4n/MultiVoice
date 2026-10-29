import svgLoader from 'vite-svg-loader'

export default defineNuxtConfig({
  compatibilityDate: '2026-10-01',
  // mic, WebCodecs and Web Audio are browser-only
  ssr: false,
  css: ['~/assets/style/main.less'],
  modules: ['@vueuse/nuxt'],
  app: {
    head: {
      title: 'MultiVoice',
      htmlAttrs: { lang: 'ru' },
      // same theme pick as the MultiPunk site
      script: [
        {
          innerHTML: `
            (function () {
              const saved = localStorage.getItem('mp-theme');
              if (saved === 'dark' || saved === 'light') {
                document.documentElement.setAttribute('data-theme', saved);
                return;
              }
              const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
              document.documentElement.setAttribute('data-theme', prefersDark ? 'dark' : 'light');
            })();
          `,
          tagPosition: 'head',
        },
      ],
      link: [
        { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: 'anonymous' },
        { rel: 'stylesheet', href: 'https://fonts.googleapis.com/css2?family=Montserrat:ital,wght@0,100..900;1,100..900&display=swap' },
      ],
    },
  },
  vite: {
    plugins: [svgLoader({ defaultImport: 'component', svgo: false })],
    // the capture worklet bundles RNNoise; module output keeps import.meta.url, which a worklet has and `self` doesn't
    worker: { format: 'es' },
  },
  // empty: the user types the server in (the Android app is built this way)
  runtimeConfig: {
    public: { serverUrl: '' },
  },
  $development: {
    runtimeConfig: { public: { serverUrl: 'http://localhost:5000' } },
  },
})
