using System;
using Volo.Abp.Application.Dtos;

namespace AlphaAgent.Abp.Application.Contracts.DTOs.AgentConfig;

public class AgentConfigDto : EntityDto<Guid>
{
    public string AgentName { get; set; } = "指标分析Agent";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? LlmConfigId { get; set; }
    public Guid? CreatorId { get; set; }
    public string? CreatorUserName { get; set; }
}

public class AgentConfigCreateDto
{
    public string AgentName { get; set; } = "指标分析Agent";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? LlmConfigId { get; set; }
}

public class AgentConfigUpdateDto
{
    public string AgentName { get; set; } = "指标分析Agent";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? LlmConfigId { get; set; }
}
