namespace Xams.Core.Attributes;

public class ServiceJobAttribute : Attribute
{
    public string Name { get; }
    public string Queue { get; }
    public TimeSpan TimeSpan { get; }
    public JobSchedule JobSchedule { get; }
    public DaysOfWeek DaysOfWeek { get; }
    public string Tag { get; }

    /// <summary>
    /// If JobSchedule is TimeOfDay, then TimeSpan is the time of day to run the job in UTC, ie: "18:13:59" for 6:13:59 PM UTC,
    /// otherwise specify Interval with hours:minutes:seconds
    /// </summary>
    /// <param name="name"></param>
    /// <param name="queue"></param>
    /// <param name="timeSpan"></param>
    /// <param name="jobSchedule"></param>
    /// <param name="daysOfWeek"></param>
    /// <param name="tag"></param>
    /// <exception cref="Exception"></exception>
    public ServiceJobAttribute(string name, string queue, string timeSpan, JobSchedule jobSchedule = JobSchedule.Interval, DaysOfWeek daysOfWeek = DaysOfWeek.All, string tag = "")
    {
        Name = name;
        Queue = queue;
        JobSchedule = jobSchedule;
        DaysOfWeek = daysOfWeek;
        Tag = tag;

        if (jobSchedule == JobSchedule.TimeOfDay)
        {
            if (DateTime.TryParse(timeSpan, out DateTime dt))
            {
                TimeSpan = dt.TimeOfDay;
            }
            else
            {
                throw new Exception($"Invalid DateTime on Job {name}: {timeSpan}");
            }
        }
        
        if (jobSchedule == JobSchedule.Interval)
        {
            if (TimeSpan.TryParse(timeSpan, out TimeSpan ts))
            {
                TimeSpan = ts;    
            }
            else
            {
                throw new Exception($"Invalid TimeSpan on Job {name}: {timeSpan}");
            }
        }
    }
}

public enum JobSchedule
{
    TimeOfDay,
    Interval
}

[Flags]
public enum DaysOfWeek
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekend = Saturday | Sunday,
    All = Weekdays | Weekend
}