namespace AlphaAgent.Application.Dtos.Relationship;

public class GroupMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public bool IsAdmin { get; set; }
}