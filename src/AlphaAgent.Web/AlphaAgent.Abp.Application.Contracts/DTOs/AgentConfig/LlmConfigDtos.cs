using System;
using Volo.Abp.Application.Dtos;

namespace AlphaAgent.Abp.Application.Contracts.DTOs.AgentConfig;

public class LlmConfigDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public float Temperature { get; set; } = 0.5f;
    public bool IsDefault { get; set; } = false;
    public Guid? CreatorId { get; set; }
    public string? CreatorUserName { get; set; }
}

public class LlmConfigCreateDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public float Temperature { get; set; } = 0.5f;
    public bool IsDefault { get; set; } = false;
}
