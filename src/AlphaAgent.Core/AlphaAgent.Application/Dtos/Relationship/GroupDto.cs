namespace AlphaAgent.Application.Dtos.Relationship;

public class GroupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public int MemberCount { get; set; }
}