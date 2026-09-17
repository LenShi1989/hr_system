namespace HrSystem.Api.Services;

public class WorkSchedule(IConfiguration config)
{
    public TimeSpan Start { get; } = Parse(config["Attendance:WorkStart"], new TimeSpan(9, 0, 0));
    public TimeSpan End { get; } = Parse(config["Attendance:WorkEnd"], new TimeSpan(18, 0, 0));

    private static TimeSpan Parse(string? value, TimeSpan fallback) =>
        TimeSpan.TryParse(value, out var parsed) ? parsed : fallback;
}