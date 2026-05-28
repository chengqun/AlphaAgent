using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Update;
using AlphaAgent.Application.Interfaces.Update;
using AlphaAgent.Domain.Abstractions.Enums;
using AlphaAgent.Domain.Abstractions.Interfaces;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Update;

public class UpdateService : IUpdateService
{
    private readonly IHttpClientService _httpClientService;

    public UpdateService(IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;
    }

    public async Task<ApiResponse<CheckUpdateResultDto>> CheckUpdateAsync(AppPlatform platform, int currentVersionCode)
    {
        try
        {
            var input = new CheckUpdateInputDto
            {
                Platform = (int)platform,
                CurrentVersionCode = currentVersionCode
            };

            var result = await _httpClientService.PostAsync<CheckUpdateResultDto>("api/app/version-config/check-update", input);

            return result != null
                ? new ApiResponse<CheckUpdateResultDto> { Success = true, Data = result }
                : new ApiResponse<CheckUpdateResultDto> { Success = false, Error = "检查更新失败" };
        }
        catch (System.Exception ex)
        {
            return new ApiResponse<CheckUpdateResultDto> { Success = false, Error = ex.Message };
        }
    }
}