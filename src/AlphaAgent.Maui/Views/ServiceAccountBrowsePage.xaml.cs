using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class ServiceAccountBrowsePage : ContentPage
{
    private ServiceAccountBrowseViewModel? _viewModel;

    public ServiceAccountBrowsePage(ServiceAccountBrowseViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null && _viewModel.ServiceAccounts.Count == 0)
        {
            await _viewModel.LoadRecommendedAsync();
        }
    }
}
