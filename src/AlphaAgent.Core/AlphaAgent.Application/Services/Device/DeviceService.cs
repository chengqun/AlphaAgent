using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Application.Interfaces.Device;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Device;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Device;

public class DeviceService : IDeviceService
{
    private readonly IHttpClientService _httpClientService;

    public DeviceService(IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;
    }

    public async Task<ApiResponse<List<MyDeviceDto>>> GetMyDevicesAsync()
    {
        var response = await _httpClientService.GetAsync<List<MyDeviceDto>>("api/app/device/my-devices");
        return response != null
            ? new ApiResponse<List<MyDeviceDto>> { Success = true, Data = response }
            : new ApiResponse<List<MyDeviceDto>> { Success = false };
    }

    public async Task<ApiResponse<MyDeviceDto>> CreateDeviceAsync(string deviceName, string deviceType)
    {
        var payload = new { DeviceName = deviceName, DeviceType = deviceType };
        var response = await _httpClientService.PostAsync<MyDeviceDto>("api/app/device/device", payload);
        return response != null
            ? new ApiResponse<MyDeviceDto> { Success = true, Data = response }
            : new ApiResponse<MyDeviceDto> { Success = false };
    }
}
