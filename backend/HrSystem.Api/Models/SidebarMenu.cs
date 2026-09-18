namespace HrSystem.Api.Models;

public class SidebarMenu
{
    public long Id { get; set; }
    public string GroupTitle { get; set; } = string.Empty;
    public int GroupOrder { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}