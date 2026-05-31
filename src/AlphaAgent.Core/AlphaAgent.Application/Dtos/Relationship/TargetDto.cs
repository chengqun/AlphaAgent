namespace AlphaAgent.Application.Dtos.Relationship;

public class TargetDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TargetSecurityInfo? SecurityInfo { get; set; }
    public TargetServiceAccountInfo? ServiceAccountInfo { get; set; }
}

public class TargetSecurityInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SecurityType { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string BaseCode { get; set; } = string.Empty;
}

public class TargetServiceAccountInfo
{
    public string Category { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
}
