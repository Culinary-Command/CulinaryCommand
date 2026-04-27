namespace CulinaryCommandSmartTaskMcp.Services
{
    public sealed class ServiceWindowClock
    {
        private static readonly Dictionary<string, TimeOnly> ServiceWindowStartTimes = new()
        {
            ["Breakfast"] = new TimeOnly(6, 0),
            ["Brunch"]    = new TimeOnly(10, 0),
            ["Lunch"]     = new TimeOnly(11, 0),
            ["Dinner"]    = new TimeOnly(16, 0),
            ["LateNight"] = new TimeOnly(21, 0),
            ["AllDay"]    = new TimeOnly(10, 0)
        };

        public DateTime ResolveServiceStartUtc(DateOnly serviceDate, string serviceWindow, TimeOnly? serviceTimeOverride)
        {
            var startTime = serviceTimeOverride
                ?? (ServiceWindowStartTimes.TryGetValue(serviceWindow, out var defaultStart)
                    ? defaultStart
                    : ServiceWindowStartTimes["AllDay"]);

            return new DateTime(serviceDate, startTime, DateTimeKind.Utc);
        }
    }
}