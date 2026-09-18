import http from '@/services/http'

export interface SidebarMenuItem {
  groupTitle: string
  groupOrder: number
  label: string
  route: string
  icon: string
  permissionCode: string
  sortOrder: number
}

export interface SidebarGroup {
  title: string
  items: Array<{ label: string; to: string; icon: string }>
}

export async function getSidebarMenus(): Promise<SidebarMenuItem[]> {
  const { data } = await http.get<SidebarMenuItem[]>('/sidebar/menus')
  return data
}

export function groupSidebarMenus(menus: SidebarMenuItem[]): SidebarGroup[] {
  const groups: SidebarGroup[] = []
  for (const menu of menus) {
    const group = groups.find((g) => g.title === menu.groupTitle)
    if (group) {
      group.items.push({ label: menu.label, to: menu.route, icon: menu.icon })
    } else {
      groups.push({
        title: menu.groupTitle,
        items: [{ label: menu.label, to: menu.route, icon: menu.icon }],
      })
    }
  }
  return groups
}