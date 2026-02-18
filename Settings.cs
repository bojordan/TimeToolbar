namespace TimeToolbar;

public class Settings
{
    public int XOffset { get; set; } = 5;
    public int YOffset { get; set; }
    public bool ShowCpuRam { get; set; } = true;
    public string ThemeOverride { get; set; } = "System";
    public bool Use24HourFormat { get; set; }
    public bool ShowBorder { get; set; }
    public int Monitor { get; set; }

    public TimeZoneSettings[]? TimeZones { get; set; }

    public class TimeZoneSettings
    {
        public string TimeZoneId { get; set; } = "";
        public string TimeZoneLabel { get; set; } = "";
    }
}
