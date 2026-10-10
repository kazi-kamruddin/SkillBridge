namespace SkillBridge.Helpers;

public static class AvailabilityHelper
{
    public static readonly string[] DayNames = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    public static string DescribeDays(int mask)
    {
        var days = Enumerable.Range(0, 7)
            .Where(day => (mask & (1 << day)) != 0)
            .Select(day => DayNames[day]);
        return string.Join(", ", days);
    }
}
