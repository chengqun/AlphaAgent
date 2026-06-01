using System;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.DTOs.AgentConfig;
using AlphaAgent.Abp.Application.Contracts.Services.AgentConfig;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace AlphaAgent.Abp.Application.Services.AgentConfig;

[Authorize]
public class AgentConfigAppService : CrudAppService<
    AppAgentConfig,
    AgentConfigDto,
    Guid,
    PagedAndSortedResultRequestDto,
    AgentConfigCreateDto,
    AgentConfigUpdateDto>,
    IAgentConfigAppService
{
    private readonly IdentityUserManager _userManager;

    public AgentConfigAppService(IRepository<AppAgentConfig, Guid> repository, IdentityUserManager userManager)
        : base(repository)
    {
        _userManager = userManager;
        GetPolicyName = AbpPermissions.AgentConfigs.Default;
        GetListPolicyName = AbpPermissions.AgentConfigs.Default;
        CreatePolicyName = AbpPermissions.AgentConfigs.Create;
        UpdatePolicyName = AbpPermissions.AgentConfigs.Update;
        DeletePolicyName = AbpPermissions.AgentConfigs.Delete;
    }

    protected override async Task<AgentConfigDto> MapToGetOutputDtoAsync(AppAgentConfig entity)
    {
        var dto = await base.MapToGetOutputDtoAsync(entity);
        if (entity.CreatorId.HasValue)
        {
            var user = await _userManager.FindByIdAsync(entity.CreatorId.Value.ToString());
            dto.CreatorUserName = user?.UserName;
        }
        return dto;
    }

    public async Task<ListResultDto<AgentConfigDto>> GetMyConfigAsync()
    {
        var userId = CurrentUser.GetId();
        var queryable = await Repository.GetQueryableAsync();
        var configs = await AsyncExecuter.ToListAsync(
            queryable.Where(c => c.CreatorId == userId)
                .OrderByDescending(c => c.IsActive)
                .ThenBy(c => c.AgentName)
        );

        // 用户没有 Agent 配置时，回退到 admin 的配置
        if (configs.Count == 0)
        {
            var adminRoleUsers = await _userManager.GetUsersInRoleAsync("admin");
            var adminId = adminRoleUsers.FirstOrDefault()?.Id;
            if (adminId.HasValue)
            {
                var adminConfigs = await AsyncExecuter.ToListAsync(
                    queryable.Where(c => c.CreatorId == adminId.Value && c.IsActive)
                        .OrderBy(c => c.AgentName)
                );
                return new ListResultDto<AgentConfigDto>(
                    adminConfigs.Select(MapToGetOutputDto).ToList()
                );
            }
        }

        return new ListResultDto<AgentConfigDto>(
            configs.Select(MapToGetOutputDto).ToList()
        );
    }

    public async Task<AgentConfigDto> GetMyActiveConfigAsync(string agentName)
    {
        var userId = CurrentUser.GetId();
        var queryable = await Repository.GetQueryableAsync();
        var config = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(c => c.CreatorId == userId && c.AgentName == agentName && c.IsActive)
        );

        // 用户没有配置时，回退到 admin 的配置
        if (config == null)
        {
            var adminRoleUsers = await _userManager.GetUsersInRoleAsync("admin");
            var adminId = adminRoleUsers.FirstOrDefault()?.Id;
            if (adminId.HasValue)
            {
                config = await AsyncExecuter.FirstOrDefaultAsync(
                    queryable.Where(c => c.CreatorId == adminId.Value && c.AgentName == agentName && c.IsActive)
                );
            }
        }

        if (config == null)
        {
            return new AgentConfigDto();
        }

        return MapToGetOutputDto(config);
    }

    public async Task<AgentConfigDto> SetMyConfigAsync(AgentConfigCreateDto input)
    {
        var userId = CurrentUser.GetId();
        var queryable = await Repository.GetQueryableAsync();
        var config = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(c => c.CreatorId == userId && c.AgentName == input.AgentName)
        );

        if (config == null)
        {
            config = new AppAgentConfig(
                input.AgentName,
                input.DefaultSystemPrompt,
                input.IsActive,
                input.LlmConfigId
            );

            await Repository.InsertAsync(config);
        }
        else
        {
            config.AgentName = input.AgentName;
            config.DefaultSystemPrompt = input.DefaultSystemPrompt;
            config.IsActive = input.IsActive;
            config.LlmConfigId = input.LlmConfigId;

            await Repository.UpdateAsync(config);
        }

        return MapToGetOutputDto(config);
    }

    public async Task ActivateConfigAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var queryable = await Repository.GetQueryableAsync();

        var target = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(c => c.Id == id)
        );

        if (target == null || target.CreatorId != userId)
        {
            throw new Volo.Abp.BusinessException("AlphaAgent:AgentConfigNotFound");
        }

        // 只取消同 AgentName 下的其他 IsActive
        var sameAgentConfigs = await AsyncExecuter.ToListAsync(
            queryable.Where(c => c.CreatorId == userId && c.AgentName == target.AgentName)
        );

        foreach (var c in sameAgentConfigs)
        {
            c.IsActive = (c.Id == id);
        }

        await Repository.UpdateManyAsync(sameAgentConfigs);
    }
}