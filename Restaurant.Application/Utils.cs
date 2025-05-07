using System.ComponentModel;
using Restaurant.Domain;

namespace Restaurant.Application;

public static class Utils
{
    public static List<TimeSlot> GeneratePredefinedTimeSlots()
    {
        var slots = new List<TimeSlot>();
        var startTimeUtc = new TimeSpan(6, 30, 0); // 6:30 AM UTC (10:30 AM Tbilisi time)
        var endTimeUtc = new TimeSpan(18, 30, 0); // 6:30 PM UTC (10:30 PM Tbilisi time)
        var currentTime = startTimeUtc;

        while (currentTime <= endTimeUtc)
        {
            var slotEnd = currentTime.Add(TimeSpan.FromMinutes(90));

            slots.Add(new TimeSlot
            {
                Start = currentTime.ToString(@"hh\:mm"),
                End = slotEnd.ToString(@"hh\:mm")
            });

            currentTime = currentTime.Add(TimeSpan.FromMinutes(90 + 15)); // 90-minute slot + 15-minute gap
        }

        return slots;
    }
    
    public static string GetEnumDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
        return attribute == null ? value.ToString() : attribute.Description;
    }
    
    public static decimal CalculateTotalPrice<T>(IEnumerable<T> items, Func<T, decimal> priceSelector, Func<T, int> quantitySelector)
    {
        return items.Sum(item => priceSelector(item) * quantitySelector(item));
    }
}