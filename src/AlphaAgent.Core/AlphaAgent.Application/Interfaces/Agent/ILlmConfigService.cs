using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Agent;

namespace AlphaAgent.Application.Interfaces.Agent;

public interface ILlmConfigService
{
    Task<List<LlmConfigResponseDto>?> GetCachedConfigsAsync(Guid userId);
    Task<List<LlmConfigResponseDto>?> SyncFromServerAsync(Guid userId);
    Task SetConfigAsync(LlmConfigResponseDto config);
}