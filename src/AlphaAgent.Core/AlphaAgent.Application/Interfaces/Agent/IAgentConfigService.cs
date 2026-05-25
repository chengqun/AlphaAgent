using AlphaAgent.Application.Dtos.Agent;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Agent;

public interface IAgentConfigService
{
    Task<List<AgentConfigResponseDto>?> GetCachedConfigsAsync(Guid userId);
    Task<List<AgentConfigResponseDto>?> SyncFromServerAsync(Guid userId);
    Task EnsureDefaultConfigsAsync(Guid userId, List<AgentConfigResponseDto> existingConfigs);
    Task SetConfigAsync(AgentConfigResponseDto config);
}
