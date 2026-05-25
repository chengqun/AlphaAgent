using AlphaAgent.Abp.Application.Contracts.DTOs.Security;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AlphaAgent.Abp.Application.Contracts.Services.Security;

public interface ISecurityAppService : ICrudAppService<
    SecurityDto,
    int,
    PagedAndSortedResultRequestDto,
    SecurityCreateDto,
    SecurityUpdateDto>
{}
