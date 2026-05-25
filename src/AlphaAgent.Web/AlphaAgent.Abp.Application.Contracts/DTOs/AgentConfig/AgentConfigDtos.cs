using System;
using Volo.Abp.Application.Dtos;

namespace AlphaAgent.Abp.Application.Contracts.DTOs.AgentConfig;

public class AgentConfigDto : EntityDto<Guid>
{
    public string AgentName { get; set; } = "指标分析Agent";
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.5f;
    public bool IsActive { get; set; } = true;
    public Guid? CreatorId { get; set; }
    public string? CreatorUserName { get; set; }
}

public class AgentConfigCreateDto
{
    public string AgentName { get; set; } = "指标分析Agent";
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.5f;
    public bool IsActive { get; set; } = true;
}

public class AgentConfigUpdateDto
{
    public string AgentName { get; set; } = "指标分析Agent";
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.5f;
    public bool IsActive { get; set; } = true;
}