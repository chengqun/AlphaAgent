using System;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.DTOs.AgentConfig;
using AlphaAgent.Abp.Application.Contracts.Services.AgentConfig;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace AlphaAgent.Abp.Application.Services.AgentConfig;

[Authorize]
public class LlmConfigAppService : ApplicationService, ILlmConfigAppService
{
    private readonly IRepository<AppLlmConfig, Guid> _repository;
    private readonly IdentityUserManager _userManager;

    public LlmConfigAppService(IRepository<AppLlmConfig, Guid> repository, IdentityUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    public async Task<ListResultDto<LlmConfigDto>> GetMyLlmConfigsAsync()
    {
        var userId = CurrentUser.GetId();
        var queryable = await _repository.GetQueryableAsync();
        var configs = await AsyncExecuter.ToListAsync(
            queryable.Where(c => c.CreatorId == userId)
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Name)
        );

        return new ListResultDto<LlmConfigDto>(
            configs.Select(MapToDto).ToList()
        );
    }

    public async Task<LlmConfigDto> SetMyLlmConfigAsync(LlmConfigCreateDto input)
    {
        var userId = CurrentUser.GetId();

        if (input.Id.HasValue && input.Id.Value != Guid.Empty)
        {
            // 更新
            var config = await _repository.GetAsync(input.Id.Value);
            if (config.CreatorId != userId)
                throw new BusinessException("AlphaAgent:LlmConfigNotFound");

            config.Name = input.Name;
            config.ModelName = input.ModelName;
            config.ApiKey = input.ApiKey;
            config.Endpoint = input.Endpoint;
            config.Temperature = input.Temperature;

            if (input.IsDefault && !config.IsDefault)
            {
                await ClearOtherDefaultsAsync(userId);
                config.IsDefault = true;
            }

            await _repository.UpdateAsync(config);
            return MapToDto(config);
        }
        else
        {
            // 新增
            var isFirst = !await _repository.AnyAsync(c => c.CreatorId == userId);
            var config = new AppLlmConfig(
                input.Name,
                input.ModelName,
                input.ApiKey,
                input.Endpoint,
                input.Temperature,
                input.IsDefault || isFirst  // 第一条自动设为默认
            );

            if (config.IsDefault)
                await ClearOtherDefaultsAsync(userId);

            await _repository.InsertAsync(config);
            return MapToDto(config);
        }
    }

    public async Task SetDefaultLlmConfigAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var config = await _repository.GetAsync(id);
        if (config.CreatorId != userId)
            throw new BusinessException("AlphaAgent:LlmConfigNotFound");

        await ClearOtherDefaultsAsync(userId);
        config.IsDefault = true;
        await _repository.UpdateAsync(config);
    }

    public async Task DeleteLlmConfigAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var config = await _repository.GetAsync(id);
        if (config.CreatorId != userId)
            throw new BusinessException("AlphaAgent:LlmConfigNotFound");

        // 检查是否有 Agent 在用
        var agentConfigRepo = LazyServiceProvider.LazyGetRequiredService<IRepository<AppAgentConfig, Guid>>();
        var agentQueryable = await agentConfigRepo.GetQueryableAsync();
        var inUse = await AsyncExecuter.AnyAsync(
            agentQueryable.Where(a => a.CreatorId == userId && a.LlmConfigId == id)
        );
        if (inUse)
            throw new BusinessException("AlphaAgent:LlmConfigInUse");

        // 如果删除的是默认配置，将第一条其他配置设为默认
        if (config.IsDefault)
        {
            var queryable = await _repository.GetQueryableAsync();
            var nextDefault = await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(c => c.CreatorId == userId && c.Id != id)
                    .OrderBy(c => c.CreationTime)
            );
            if (nextDefault != null)
            {
                nextDefault.IsDefault = true;
                await _repository.UpdateAsync(nextDefault);
            }
        }

        await _repository.DeleteAsync(config);
    }

    private async Task ClearOtherDefaultsAsync(Guid userId)
    {
        var queryable = await _repository.GetQueryableAsync();
        var defaults = await AsyncExecuter.ToListAsync(
            queryable.Where(c => c.CreatorId == userId && c.IsDefault)
        );
        foreach (var d in defaults)
            d.IsDefault = false;
        if (defaults.Count > 0)
            await _repository.UpdateManyAsync(defaults);
    }

    private LlmConfigDto MapToDto(AppLlmConfig entity)
    {
        var dto = new LlmConfigDto
        {
            Id = entity.Id,
            Name = entity.Name,
            ModelName = entity.ModelName,
            ApiKey = entity.ApiKey,
            Endpoint = entity.Endpoint,
            Temperature = entity.Temperature,
            IsDefault = entity.IsDefault,
            CreatorId = entity.CreatorId,
        };

        if (entity.CreatorId.HasValue)
        {
            var user = _userManager.FindByIdAsync(entity.CreatorId.Value.ToString()).Result;
            dto.CreatorUserName = user?.UserName;
        }

        return dto;
    }
}