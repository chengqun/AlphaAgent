using System.Collections.Generic;

namespace AlphaAgent.Application.Dtos.Relationship;

public class ContactBookDto
{
    public List<RelationshipDto> Devices { get; set; } = new List<RelationshipDto>();
    public List<RelationshipDto> Friends { get; set; } = new List<RelationshipDto>();
    public List<RelationshipDto> Groups { get; set; } = new List<RelationshipDto>();
    public List<RelationshipDto> Stocks { get; set; } = new List<RelationshipDto>();
}

public class ContactItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public int RelationshipType { get; set; }
}