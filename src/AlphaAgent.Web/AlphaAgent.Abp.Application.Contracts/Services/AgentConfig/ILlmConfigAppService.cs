using System;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.DTOs.AgentConfig;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AlphaAgent.Abp.Application.Contracts.Services.AgentConfig;

public interface ILlmConfigAppService : IApplicationService
{
    /// <summary>获取当前用户的所有 LLM 配置</summary>
    Task<ListResultDto<LlmConfigDto>> GetMyLlmConfigsAsync();

    /// <summary>新增或更新 LLM 配置（Id 有值则更新，否则新增）</summary>
    Task<LlmConfigDto> SetMyLlmConfigAsync(LlmConfigCreateDto input);

    /// <summary>设置默认 LLM 配置（其他配置取消默认）</summary>
    Task SetDefaultLlmConfigAsync(Guid id);

    /// <summary>删除 LLM 配置（检查是否有 Agent 在用）</summary>
    Task DeleteLlmConfigAsync(Guid id);
}