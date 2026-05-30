using System;

namespace AlphaAgent.Application.Dtos.Agent;

public class LlmConfigResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public float Temperature { get; set; } = 0.5f;
    public bool IsDefault { get; set; } = false;
}
