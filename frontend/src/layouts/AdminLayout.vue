<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { getSidebarMenus, groupSidebarMenus, type SidebarMenuItem } from '@/services/sidebar'

const auth = useAuthStore()

const menus = ref<SidebarMenuItem[]>([])

const menuGroups = computed(() => groupSidebarMenus(menus.value))

onMounted(async () => {
  try {
    menus.value = await getSidebarMenus()
  } catch {
    menus.value = []
  }
})

function onLogout() {
  auth.logout()
}
</script>

<template>
  <div class="layout">
    <aside class="sidebar">
      <div class="brand">HR 系統</div>
      <nav>
        <div v-for="group in menuGroups" :key="group.title" class="menu-group">
          <p v-if="group.title" class="menu-title">{{ group.title }}</p>
          <router-link
            v-for="item in group.items"
            :key="item.to"
            :to="item.to"
            class="nav-item"
          >
            <span class="nav-icon">{{ item.icon }}</span>
            <span>{{ item.label }}</span>
          </router-link>
        </div>
      </nav>
    </aside>
    <div class="main">
      <header class="header">
        <div class="header-info">
          <span>{{ auth.user?.email }}</span>
          <span class="badge">{{ auth.user?.roleCode }}</span>
        </div>
        <button @click="onLogout">登出</button>
      </header>
      <main class="content">
        <router-view />
      </main>
    </div>
  </div>
</template>