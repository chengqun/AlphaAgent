namespace AlphaAgent.Application.Dtos.Device;

public class MyDeviceDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string AuthorizationCode { get; set; } = string.Empty;
    public bool HasRelationship { get; set; }
}
