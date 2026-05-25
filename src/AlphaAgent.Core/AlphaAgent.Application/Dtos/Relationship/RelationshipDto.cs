using System;

namespace AlphaAgent.Application.Dtos.Relationship;

public class RelationshipDto
{
    public Guid Id { get; set; }
    public int Type { get; set; }
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? DeviceType { get; set; }
    public string Initial => !string.IsNullOrEmpty(TargetName) ? TargetName[0].ToString().ToUpper() : "?";
}