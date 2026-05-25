using System;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.DTOs.Security;
using Volo.Abp.Application.Services;

namespace AlphaAgent.Abp.Application.Contracts.Services.Security;

public interface ISecurityClientSyncService : IApplicationService
{
    Task<SecuritySyncResultDto> GetUpdatesAsync(DateTime? after = null);
}