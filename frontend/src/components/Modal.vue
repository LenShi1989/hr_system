<script setup lang="ts">
defineProps<{ title: string; open: boolean }>()
const emit = defineEmits<{ close: [] }>()
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="open" class="modal-mask" @click.self="emit('close')">
        <div class="modal">
          <header class="modal-head">
            <h3>{{ title }}</h3>
            <button type="button" class="btn-ghost" @click="emit('close')">✕</button>
          </header>
          <div class="modal-body">
            <slot />
          </div>
          <footer v-if="$slots.footer" class="modal-foot">
            <slot name="footer" />
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>