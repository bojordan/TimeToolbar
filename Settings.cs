namespace TimeToolbar;

public class Settings
{
    public int XOffset { get; set; } = 5;
    public bool ShowCpuRam { get; set; } = true;

    public TimeZoneSettings[]? TimeZones { get; set; }

    public class TimeZoneSettings
    {
        public string TimeZoneId { get; set; } = "";
        public string TimeZoneLabel { get; set; } = "";
    }
}
