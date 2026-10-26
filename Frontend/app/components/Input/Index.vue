<template>
  <div class="input" :class="{ multiline: type === 'textarea' }">
    <slot name="before" />
    <input
      class="white"
      v-model="model"
      v-if="type != 'textarea'"
      :type="type ?? 'text'"
      :placeholder="placeholder"
    />
    <textarea v-else class="white" v-model="model" :placeholder="placeholder" />
    <slot name="after" />
  </div>
</template>

<script lang="ts" setup>
defineProps<{ type?: string; placeholder?: string }>();
const model = defineModel<string>();
</script>

<style scoped lang="less">
.input {
  height: 40px;
  display: flex;

  background: var(--scolor-gradient);
  border-radius: 5px;

  &.w100 {
    width: 100%;
  }

  input {
    flex: 1;
    text-indent: 10px;
  }

  // A textarea is a block of prose, not a one-line field: it needs room to be worth typing into
  // and padding on every side rather than a single-line indent. The wrapper's fixed row height
  // has to go with it, otherwise the box is 40px tall no matter what is inside.
  &.multiline {
    height: auto;
    min-height: 96px;
    padding: 10px;
  }

  textarea {
    flex: 1;
    resize: vertical;
    min-height: 76px;

    font: inherit;
    border: none;
    outline: none;
    background: transparent;
  }

  input,
  textarea {
    &::placeholder {
      color: oklch(100% 0 0);
      opacity: 0.5;
    }
  }
}
</style>
