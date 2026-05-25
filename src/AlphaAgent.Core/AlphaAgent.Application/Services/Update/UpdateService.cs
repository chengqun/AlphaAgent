using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Update;
using AlphaAgent.Application.Interfaces.Update;
using AlphaAgent.Domain.Abstractions.Enums;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Services.Auth;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Update;

public class UpdateService : IUpdateService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ITokenManager _tokenManager;

    public UpdateService(IHttpClientService httpClientService, ITokenManager tokenManager)
    {
        _httpClientService = httpClientService;
        _tokenManager = tokenManager;
    }

    private async Task EnsureTokenAsync()
    {
        var token = await _tokenManager.GetTokenByUsernameAsync(await _tokenManager.GetUsernameAsync() ?? string.Empty);
        if (token != null && !token.IsExpired())
        {
            _httpClientService.SetAuthorizationToken(token.AccessToken);
        }
    }

    public async Task<ApiResponse<CheckUpdateResultDto>> CheckUpdateAsync(AppPlatform platform, int currentVersionCode)
    {
        try
        {
            await EnsureTokenAsync();

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