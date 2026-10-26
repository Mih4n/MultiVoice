<template>
  <div class="checkbox" @click="() => (checked = !checked)">
    <div class="box" :class="{ checked }" name="checkbox">
      <div class="check">
        <Check />
      </div>
    </div>
    <label for="checkbox" v-if="!!$slots.default">
      <div class="inline">
        <slot></slot>
      </div>
    </label>
  </div>
</template>

<script setup lang="ts">
import Check from "@/assets/images/svg/check.svg";
const checked = defineModel<boolean>({ default: false });
</script>

<style lang="less">
.checkbox {
  display: flex;
  align-items: center;
  gap: 8px;

  label > .inline {
    display: inline-flex;
    gap: 5px;
  }

  .box {
    width: 20px;
    height: 20px;
    border: 2px solid oklch(100% 0 0);
    border-radius: 2px;

    display: flex;

    .check {
      position: relative;

      left: -4.5px;
      bottom: 12px;

      path {
        stroke-dasharray: 1000;
        stroke-dashoffset: -1000;

        transition: all 0.3s cubic-bezier(1, 0, 0, 1);
      }
    }
  }

  .box.checked {
    .check {
      path {
        stroke-dashoffset: 0;
      }
    }
  }

  label {
    cursor: pointer;
    display: flex;
    user-select: none;
  }
}
</style>
