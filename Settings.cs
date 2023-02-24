namespace TimeToolbar
{
    public class Settings
    {
        public Settings()
        {
            this.RemoteTimeZoneId = "Pacific Standard Time";
            this.RemoteTimeZoneLabel = "PST";
        }
        public string RemoteTimeZoneId { get; set; }
        public string RemoteTimeZoneLabel { get; set; }
    }
}
