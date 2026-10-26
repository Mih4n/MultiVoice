<template>
  <div class="dropdown" ref="dropdownRef">
    <Input
      ref="input"
      class="100w"
      v-model="search"
      :placeholder="placeholder"
      @blur="closeDropdown"
      @click="openDropdown"
      @focus="openDropdown"
    />
    <teleport to="#teleports">
      <div
        ref="listRef"
        class="dropdown-list scrollable"
        :class="{ open: isOpen, above: placement.above }"
        :style="placement.style"
        @mousedown.prevent
      >
        <div class="scrollable-inner" :style="{ maxHeight: isOpen ? placement.maxHeight : '0px' }">
          <div
            v-for="(option, index) in filteredOptions"
            :key="index"
            class="option"
            @mousedown.prevent="selectOption(option)"
          >
            <h4>{{ getLabel(option) }}</h4>
          </div>
          <div v-if="filteredOptions.length === 0" class="no-options">
            <h4 class="white">{{ isSearching ? "Загрузка…" : "Ничего не найдено" }}</h4>
          </div>
          <div v-else-if="isSearching" class="no-options">
            <h6 class="white">Загрузка…</h6>
          </div>
        </div>
      </div>
    </teleport>
  </div>
</template>

<script lang="ts" generic="T" setup>
const model = defineModel<T>();

const props = withDefaults(
  defineProps<{
    options?: T[];
    getLabel?: (option: T) => string;
    placeholder?: string;
    /**
     * Server-side search. Called when the list opens and again, debounced, as the operator types;
     * whatever it brings in is expected to show up in `options`. Options already present are
     * filtered locally meanwhile, so the list never waits on the network for what it has.
     */
    search?: (term: string) => Promise<unknown>;
  }>(),
  {
    getLabel: (o: any) => {
      return o;
    },
  },
);

const emit = defineEmits<{
  (e: "select", value: any): void;
}>();

const search = ref("");
const isOpen = ref(false);
/**
 * Whether the text in the field is something the operator typed, as opposed to the label of the
 * option already selected. Without the distinction the field filters against its own value, so a
 * dropdown with a selection shows exactly one option — itself — and the other choices are
 * unreachable.
 */
const isFiltering = ref(false);
const pendingSearches = ref(0);
const isSearching = computed(() => pendingSearches.value > 0);

const dropdownRef = ref<HTMLElement>();
const listRef = ref<HTMLElement>();
const input = useTemplateRef("input");
const { height } = useElementSize(input as any);

const selectedLabel = computed(() => (model.value ? props.getLabel(model.value) : ""));

const filteredOptions = computed(() => {
  if (!props.options) return [];
  if (!isFiltering.value || !search.value) return props.options;

  const searchLower = search.value.toLowerCase();
  return props.options.filter((option) => {
    const label = props.getLabel(option);
    return label.toLowerCase().includes(searchLower);
  });
});

const runSearch = async (term: string) => {
  if (!props.search) return;

  pendingSearches.value++;
  try {
    await props.search(term);
  } catch {
    // A failed search leaves whatever is already cached on screen; the next keystroke retries.
  } finally {
    pendingSearches.value--;
  }
};

const debouncedSearch = useDebounceFn(runSearch, 350);

// The list lives in #teleports so no card or dialog with overflow can clip it, which means it
// is positioned against the viewport by hand: under the field when there is room, above it when
// the field sits too close to the bottom of the screen.
const GAP = 10;
const placement = reactive({
  above: false,
  maxHeight: "0px",
  style: {} as Record<string, string>,
});

const place = () => {
  const field = unrefElement(input as any) as HTMLElement | undefined;
  if (!field) return;

  const rect = field.getBoundingClientRect();
  const wanted = (height.value || rect.height) * 4;
  const below = window.innerHeight - rect.bottom - GAP * 2;
  const above = rect.top - GAP * 2;

  placement.above = below < wanted && above > below;
  placement.maxHeight = `${Math.max(0, Math.min(wanted, placement.above ? above : below))}px`;
  placement.style = {
    left: `${rect.left}px`,
    width: `${rect.width}px`,
    ...(placement.above
      ? { bottom: `${window.innerHeight - rect.top + GAP}px` }
      : { top: `${rect.bottom + GAP}px` }),
  };
};

useEventListener(window, "resize", () => isOpen.value && place());
// Capture, so scrolling inside a dialog or any other container moves the list with its field.
useEventListener(window, "scroll", () => isOpen.value && place(), { capture: true, passive: true });

const selectOption = (option: T) => {
  search.value = props.getLabel(option);
  isFiltering.value = false;

  model.value = option;
  emit("select", option);
  isOpen.value = false;
};

const openDropdown = () => {
  if (isOpen.value) return;

  // Opening shows everything; typing is what narrows it.
  isFiltering.value = false;
  place();
  isOpen.value = true;
  runSearch("");
};

// Anything in the field that isn't the current selection's label is the operator searching.
watch(search, (value) => {
  isFiltering.value = !!value && value !== selectedLabel.value;
  if (isOpen.value && isFiltering.value) debouncedSearch(value);
});

const closeDropdown = () => {
  // Delayed so a click on an option lands before the list goes away.
  setTimeout(() => {
    isOpen.value = false;
  }, 200);
};

const handleClickOutside = (event: MouseEvent) => {
  const target = event.target as Node;
  if (dropdownRef.value?.contains(target) || listRef.value?.contains(target)) return;

  isOpen.value = false;
};

useEventListener(window, "click", handleClickOutside);

watch(isOpen, (open) => {
  // Closing: an abandoned search would otherwise sit in the field looking like a selection. This
  // lives here rather than in closeDropdown because an outside click closes the list without
  // going through it.
  if (!open) {
    if (isFiltering.value) search.value = selectedLabel.value;
    isFiltering.value = false;
    return;
  }

  // Opening onto a search that already narrows to one match picks it, so the operator doesn't
  // have to click the only thing left.
  if (!isFiltering.value || filteredOptions.value.length !== 1) return;

  const option = filteredOptions.value[0];
  if (!option) return;

  model.value = option;
  search.value = props.getLabel(option);
});

watch(
  model,
  (newValue) => {
    if (newValue && props.options) {
      const selectedOption = props.options.find((option) => {
        return props.getLabel(option) === newValue;
      });
      if (selectedOption) {
        search.value = props.getLabel(selectedOption);
      }
    } else if (!newValue) {
      search.value = "";
    }
  },
  { immediate: true },
);
</script>

<style lang="less" scoped>
.dropdown {
  position: relative;
}

.dropdown-list {
  position: fixed;
  z-index: var(--z-index-dropdown);
  border-radius: 5px;
  background: var(--scolor-gradient);
  overflow: hidden;
  pointer-events: none;

  // Collapsed, this element has no height at all — so a visible border would be a hairline
  // floating under the field. It only gets one while it is open.
  border: 1px solid transparent;

  &.open {
    pointer-events: auto;
    border-color: var(--mcolor-overlay-white-10);
  }

  .scrollable-inner {
    overflow-y: auto;
    transition: max-height 0.3s ease;
  }

  .option {
    padding: 8px 12px;
    cursor: pointer;
    transition:
      background-color 0.2s,
      color 0.2s;
    color: oklch(100% 0 0);

    h4 {
      color: inherit;
    }

    &:hover {
      color: oklch(0% 0 0);
      background-color: var(--scolor-crystal);
    }
  }

  .no-options {
    padding: 8px 12px;
    text-align: center;
    color: oklch(100% 0 0);
    opacity: 0.7;
  }
}
</style>
