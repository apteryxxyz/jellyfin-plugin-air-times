using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.AirTimes;

/// <summary>
/// Represents a schedule.
/// </summary>
/// <param name="timeZone">Time zone the schedule is in.</param>
public class Schedule(TimeZoneInfo? timeZone = null)
{
  /// <summary>
  /// Gets the time zone of the schedule.
  /// </summary>
  public TimeZoneInfo TimeZone { get; } = timeZone ?? TimeZoneInfo.Utc;

  /// <summary>
  /// Gets the days of the week for the schedule.
  /// </summary>
  public IReadOnlyList<DayOfWeek> Days { get; private set; } = [];

  /// <summary>
  /// Gets or sets the time of day for the schedule.
  /// </summary>
  public TimeSpan Time { get; private set; }

  /// <inheritdoc/>
  public override string ToString() => $"{string.Join(", ", Days)} at {Time}";

  /// <summary>
  /// Create a schedule from another schedule.
  /// </summary>
  /// <param name="other">The other schedule to create from.</param>
  /// <param name="timeZone">Time zone to convert the schedule to.</param>
  /// <returns>Schedule.</returns>
  public static Schedule FromSchedule(Schedule other, TimeZoneInfo? timeZone = null)
  {
    // Determine the offset difference between the two time zones
    var targetTimeZone = timeZone ?? TimeZoneInfo.Utc;
    var offsetDiff = other.TimeZone.BaseUtcOffset - targetTimeZone.GetUtcOffset(DateTime.UtcNow);

    var shiftedTime = other.Time - offsetDiff;
    int dayShift = shiftedTime.Hours < 0 ? -1 : shiftedTime.Hours >= 24 ? 1 : 0;

    if (dayShift == 0)
    {
      return new(targetTimeZone) { Days = other.Days, Time = shiftedTime };
    }

    var shiftedDays = other.Days.Select(d => (DayOfWeek)(((int)d + dayShift + 7) % 7)).ToList();
    shiftedTime = shiftedTime.Add(TimeSpan.FromDays(dayShift));
    return new(targetTimeZone) { Days = shiftedDays, Time = shiftedTime };
  }

  /// <summary>
  /// Create a schedule from a list of dates.
  /// </summary>
  /// <param name="dates">List of DateTime objects.</param>
  /// <param name="timeZone">Optional time zone to use for the schedule.</param>
  /// <returns>Schedule.</returns>
  public static Schedule FromDateTimes(IList<DateTime> dates, TimeZoneInfo? timeZone = null)
  {
    return new Schedule(timeZone ?? TimeZoneInfo.Utc)
    {
      // Get the unique days from the list of dates
      Days = [.. dates.Select(d => d.DayOfWeek).Distinct()],
      // Assume the latest date is the most accurate
      Time = dates.Last().TimeOfDay
    };
  }
}
