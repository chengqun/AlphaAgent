using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class MePage : ContentPage
{
    private readonly MeViewModel _viewModel;

    public MePage(MeViewModel viewModel)
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

    private async void OnThemeToggled(object sender, ToggledEventArgs e)
    {
        await _viewModel.ToggleThemeAsync(e.Value);
    }
}
