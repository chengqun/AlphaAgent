using System;
using AlphaAgent.Abp.Domain.Shared.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace AlphaAgent.Abp.Domain.Entities;

public class AppVersionConfig : FullAuditedAggregateRoot<Guid>
{
    public AppPlatform Platform { get; set; }
    public int VersionCode { get; set; }
    public string VersionName { get; set; } = string.Empty;
    public string UpdateUrl { get; set; } = string.Empty;
    public string UpdateNote { get; set; } = string.Empty;
    public bool IsForce { get; set; }

    public AppVersionConfig() { }

    public AppVersionConfig(
        AppPlatform platform,
        int versionCode,
        string versionName,
        string updateUrl,
        string updateNote,
        bool isForce = false)
    {
        Platform = platform;
        VersionCode = versionCode;
        VersionName = versionName;
        UpdateUrl = updateUrl;
        UpdateNote = updateNote;
        IsForce = isForce;
    }
}
