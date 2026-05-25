using System;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.DTOs.AgentConfig;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AlphaAgent.Abp.Application.Contracts.Services.AgentConfig;

public interface IAgentConfigAppService : ICrudAppService<
    AgentConfigDto,
    Guid,
    PagedAndSortedResultRequestDto,
    AgentConfigCreateDto,
    AgentConfigUpdateDto>
{
    Task<ListResultDto<AgentConfigDto>> GetMyConfigAsync();
    Task<AgentConfigDto> GetMyActiveConfigAsync(string agentName);
    Task<AgentConfigDto> SetMyConfigAsync(AgentConfigCreateDto input);
    Task ActivateConfigAsync(Guid id);
}