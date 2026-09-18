namespace HrSystem.Api.Dtos;

public record SidebarMenuItemDto(
    string GroupTitle,
    int GroupOrder,
    string Label,
    string Route,
    string Icon,
    string PermissionCode,
    int SortOrder);