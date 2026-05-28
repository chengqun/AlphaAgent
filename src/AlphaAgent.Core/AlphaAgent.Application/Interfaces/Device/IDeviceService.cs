using AlphaAgent.Application.Dtos.Device;
using AlphaAgent.Application.Dtos.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Device;

public interface IDeviceService
{
    Task<ApiResponse<List<MyDeviceDto>>> GetMyDevicesAsync();
    Task<ApiResponse<MyDeviceDto>> CreateDeviceAsync(string deviceName, string deviceType);
}
