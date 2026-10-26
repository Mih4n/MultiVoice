<template>
  <a class="button"><slot></slot></a>
</template>

<script lang="ts" setup></script>

<style lang="less">
a.button {
  padding: 5px;

  cursor: pointer;

  height: 40px;
  background-color: var(--scolor-accent);
  border-radius: 10px;
  box-sizing: border-box;

  color: oklch(0% 0 0);

  text-align: center;

  gap: 5px;
  display: flex;
  align-items: center;
  justify-content: center;

  transition: all 0.3s ease-in-out;

  &:hover {
    background-color: var(--scolor-accent-hover);
  }

  &.disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  &.bordered {
    border: 5px var(--scolor-crystal) solid;
    background: var(--scolor-gradient);
  }

  &.red {
    background-color: var(--scolor-error);
    color: oklch(100% 0 0);
  }

  &.w100 {
    width: 100%;
  }

  &.flex {
    flex: 1;
    display: flex;
  }
}
</style>
