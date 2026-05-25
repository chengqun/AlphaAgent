using System;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.DTOs.VersionConfig;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AlphaAgent.Abp.Application.Contracts.Services.VersionConfig;

public interface IVersionConfigAppService : ICrudAppService<
    VersionConfigDto,
    Guid,
    PagedAndSortedResultRequestDto,
    VersionConfigCreateDto,
    VersionConfigUpdateDto>
{
    Task<CheckUpdateResultDto> CheckUpdateAsync(CheckUpdateInputDto input);
    Task PublishAsync(VersionConfigPublishDto input);
}