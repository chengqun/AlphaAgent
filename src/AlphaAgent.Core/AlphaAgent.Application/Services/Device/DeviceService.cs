using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Application.Interfaces.Device;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Device;
using AlphaAgent.Domain.Services.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Device;

public class DeviceService : IDeviceService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ITokenManager _tokenManager;

    public DeviceService(IHttpClientService httpClientService, ITokenManager tokenManager)
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

    public async Task<ApiResponse<List<MyDeviceDto>>> GetMyDevicesAsync()
    {
        await EnsureTokenAsync();
        var response = await _httpClientService.GetAsync<List<MyDeviceDto>>("api/app/device/my-devices");
        return response != null
            ? new ApiResponse<List<MyDeviceDto>> { Success = true, Data = response }
            : new ApiResponse<List<MyDeviceDto>> { Success = false };
    }

    public async Task<ApiResponse<MyDeviceDto>> CreateDeviceAsync(string deviceName, string deviceType)
    {
        await EnsureTokenAsync();
        var payload = new { DeviceName = deviceName, DeviceType = deviceType };
        var response = await _httpClientService.PostAsync<MyDeviceDto>("api/app/device/device", payload);
        return response != null
            ? new ApiResponse<MyDeviceDto> { Success = true, Data = response }
            : new ApiResponse<MyDeviceDto> { Success = false };
    }
}
