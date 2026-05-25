using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Update;
using AlphaAgent.Domain.Abstractions.Enums;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Update;

public interface IUpdateService
{
    Task<ApiResponse<CheckUpdateResultDto>> CheckUpdateAsync(AppPlatform platform, int currentVersionCode);
}
