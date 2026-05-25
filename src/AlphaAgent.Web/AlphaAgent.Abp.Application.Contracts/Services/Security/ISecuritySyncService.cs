using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AlphaAgent.Abp.Application.Contracts.Services.Security;

public interface ISecuritySyncService : IApplicationService
{
    Task<SecuritySyncResult> SyncFromExternalAsync();
}
