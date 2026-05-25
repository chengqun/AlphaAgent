namespace AlphaAgent.Abp.Application.Contracts.Services.Security;

public class SecuritySyncOptions
{
    public const string SectionName = "SecuritySync";

    public string Url { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; } = 60;
}
