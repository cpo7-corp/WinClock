using System;

namespace WinClock
{
    public class Alarm
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Alarm";
        public string Label { get; set; } = "";
        public DateTime? TargetDate { get; set; } // For specific date
        public TimeSpan TargetTime { get; set; } // Time of day
        public bool IsActive { get; set; } = true;
        public bool IsDaily { get; set; } = false;

        public bool ShouldTrigger(DateTime now)
        {
            if (!IsActive) return false;

            // Check time (ignoring seconds for 1-minute window, or exactly matching)
            // To prevent multiple triggers in the same minute, we might need a LastTriggered property.
            bool timeMatch = now.Hour == TargetTime.Hours && now.Minute == TargetTime.Minutes && now.Second == 0;

            if (!timeMatch) return false;

            if (IsDaily) return true;

            if (TargetDate.HasValue)
            {
                return now.Date == TargetDate.Value.Date;
            }

            return true; // Simple time of day alarm if no date is set and not daily? 
                         // Usually, if no date and not daily, it's a "one-time" alarm for today.
        }
    }
}
