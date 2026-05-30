using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class AiSettingsPage : ContentPage
{
    private readonly AiSettingsViewModel _viewModel;

    public AiSettingsPage(AiSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }
}
