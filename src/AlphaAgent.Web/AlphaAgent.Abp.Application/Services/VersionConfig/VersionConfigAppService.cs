using System;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.DTOs.VersionConfig;
using AlphaAgent.Abp.Application.Contracts.Services.VersionConfig;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Domain.Shared.Enums;
using AlphaAgent.Abp.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AlphaAgent.Abp.Application.Services.VersionConfig;

[Authorize]
public class VersionConfigAppService : CrudAppService<AppVersionConfig, VersionConfigDto, Guid, PagedAndSortedResultRequestDto, VersionConfigCreateDto, VersionConfigUpdateDto>, IVersionConfigAppService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VersionConfigAppService(IRepository<AppVersionConfig, Guid> repository, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        : base(repository)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        GetPolicyName = AbpPermissions.VersionConfigs.Default;
        GetListPolicyName = AbpPermissions.VersionConfigs.Default;
        CreatePolicyName = AbpPermissions.VersionConfigs.Create;
        UpdatePolicyName = AbpPermissions.VersionConfigs.Update;
        DeletePolicyName = AbpPermissions.VersionConfigs.Delete;
    }

    [Authorize(AbpPermissions.VersionConfigs.Create)]
    public override Task<VersionConfigDto> CreateAsync(VersionConfigCreateDto input)
    {
        return base.CreateAsync(input);
    }

    [Authorize(AbpPermissions.VersionConfigs.Update)]
    public override Task<VersionConfigDto> UpdateAsync(Guid id, VersionConfigUpdateDto input)
    {
        return base.UpdateAsync(id, input);
    }

    [Authorize(AbpPermissions.VersionConfigs.Delete)]
    public override Task DeleteAsync(Guid id)
    {
        return base.DeleteAsync(id);
    }

    [Authorize(AbpPermissions.VersionConfigs.Default)]
    public override Task<VersionConfigDto> GetAsync(Guid id)
    {
        return base.GetAsync(id);
    }

    [Authorize(AbpPermissions.VersionConfigs.Default)]
    public override Task<PagedResultDto<VersionConfigDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        return base.GetListAsync(input);
    }

    [AllowAnonymous]
    public async Task<CheckUpdateResultDto> CheckUpdateAsync(CheckUpdateInputDto input)
    {
        var queryable = await Repository.GetQueryableAsync();
        var latestVersion = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(v => v.Platform == input.Platform)
                .OrderByDescending(v => v.VersionCode)
        );

        if (latestVersion == null || latestVersion.VersionCode <= input.CurrentVersionCode)
        {
            return new CheckUpdateResultDto { HasUpdate = false };
        }

        return new CheckUpdateResultDto
        {
            HasUpdate = true,
            VersionCode = latestVersion.VersionCode,
            VersionName = latestVersion.VersionName,
            UpdateUrl = latestVersion.UpdateUrl,
            UpdateNote = latestVersion.UpdateNote,
            IsForce = latestVersion.IsForce
        };
    }

    [AllowAnonymous]
    public async Task PublishAsync(VersionConfigPublishDto input)
    {
        var token = _configuration["VersionConfig:PublishToken"];
        if (string.IsNullOrWhiteSpace(token) || !_httpContextAccessor.HttpContext!.Request.Headers.TryGetValue("X-Publish-Token", out var requestToken) || requestToken != token)
        {
            throw new UnauthorizedAccessException("Invalid publish token");
        }

        var entity = new AppVersionConfig(
            (AppPlatform)input.Platform,
            input.VersionCode,
            input.VersionName,
            input.UpdateUrl,
            input.UpdateNote,
            input.IsForce
        );

        await Repository.InsertAsync(entity);
    }
}