using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class DeviceManagementPage : ContentPage
{
    public DeviceManagementPage(DeviceManagementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void ContentPage_Loaded(object? sender, EventArgs e)
    {
        if (BindingContext is DeviceManagementViewModel vm)
        {
            _ = vm.LoadDevicesCommand.ExecuteAsync(null);
        }
    }
}
