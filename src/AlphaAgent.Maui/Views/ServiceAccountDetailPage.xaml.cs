using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class ServiceAccountDetailPage : ContentPage, IQueryAttributable
{
    private ServiceAccountDetailViewModel? _viewModel;

    public ServiceAccountDetailPage(ServiceAccountDetailViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _viewModel?.ApplyQueryAttributes(query);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            await _viewModel.LoadAsync();
        }
    }
}
