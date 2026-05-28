using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Device;
using AlphaAgent.Application.Interfaces.Relationship;
using AlphaAgent.Application.Dtos.Device;

namespace AlphaAgent.Maui.ViewModels;

public partial class DeviceManagementViewModel : ObservableObject
{
    private readonly IDeviceService? _deviceService;
    private readonly IRelationshipService? _relationshipService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _newDeviceName = string.Empty;

    [ObservableProperty]
    private string _newDeviceType = "Console";

    [ObservableProperty]
    private bool _isAdding;

    public ObservableCollection<MyDeviceDto> Devices { get; } = new();

    public DeviceManagementViewModel(IDeviceService? deviceService = null, IRelationshipService? relationshipService = null)
    {
        _deviceService = deviceService;
        _relationshipService = relationshipService;
    }

    [RelayCommand]
    private async Task LoadDevicesAsync()
    {
        if (_deviceService == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "加载中...";

            var response = await _deviceService.GetMyDevicesAsync();
            Devices.Clear();

            if (response.Success && response.Data != null)
            {
                foreach (var device in response.Data)
                {
                    Devices.Add(device);
                }
                StatusMessage = Devices.Count == 0 ? "暂无设备" : string.Empty;
            }
            else
            {
                StatusMessage = "加载失败";
            }
        }
        catch
        {
            StatusMessage = "加载失败";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddDeviceAsync()
    {
        if (_deviceService == null) return;
        if (string.IsNullOrWhiteSpace(NewDeviceName)) return;

        try
        {
            IsAdding = true;
            var response = await _deviceService.CreateDeviceAsync(NewDeviceName, NewDeviceType);
            if (response.Success)
            {
                NewDeviceName = string.Empty;
                StatusMessage = "设备已创建";
                await LoadDevicesAsync();
            }
            else
            {
                StatusMessage = "创建失败";
            }
        }
        catch
        {
            StatusMessage = "创建失败";
        }
        finally
        {
            IsAdding = false;
        }
    }

    [RelayCommand]
    private async Task CopyAuthCodeAsync(string authCode)
    {
        if (string.IsNullOrEmpty(authCode)) return;
        await Clipboard.Default.SetTextAsync(authCode);
    }

    [RelayCommand]
    private async Task RestoreContactAsync(string deviceId)
    {
        if (_relationshipService == null) return;

        try
        {
            var response = await _relationshipService.CreateRelationshipAsync(1, deviceId);
            if (response.Success)
            {
                var device = Devices.FirstOrDefault(d => d.DeviceId == deviceId);
                if (device != null)
                {
                    device.HasRelationship = true;
                    OnPropertyChanged(nameof(Devices));
                }
                StatusMessage = "已恢复联系人";
            }
            else
            {
                StatusMessage = "恢复失败";
            }
        }
        catch
        {
            StatusMessage = "恢复失败";
        }
    }
}
