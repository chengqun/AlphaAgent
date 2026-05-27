using System;

namespace AlphaAgent.Application.Dtos.Moment;

public class MomentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = "Text";
    public string Visibility { get; set; } = "Friends";
    public string? TargetId { get; set; }
}

public class CreateMomentDto
{
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Type { get; set; } = "Text";
    public string Visibility { get; set; } = "Friends";
    public int? StockId { get; set; }
}