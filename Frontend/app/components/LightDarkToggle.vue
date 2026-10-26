<template>
  <div
    class="light-dark-toggle"
    :title="label"
    :aria-label="label"
    @click="useTheme().toggleTheme()"
  >
    <DarkMode v-if="theme == 'dark'" />
    <LightMode v-else />
  </div>
</template>

<script lang="ts" setup>
import DarkMode from "@/assets/images/svg/dark_mode.svg";
import LightMode from "@/assets/images/svg/light_mode.svg";

const { theme } = useTheme();

const label = computed(() =>
  theme.value === "dark" ? "Включить светлую тему" : "Включить тёмную тему",
);
</script>

<style lang="less">
.light-dark-toggle {
  display: flex;
  cursor: pointer;

  > * {
    margin: auto;
    transition:
      width 0.3s ease,
      height 0.3s ease;
  }

  // Used where the toggle has to fit a collapsed rail rather than a full-width row.
  &.small > * {
    width: 20px;
    height: 20px;
  }
}
</style>
