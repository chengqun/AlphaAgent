using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class DiscoveryPage : ContentPage
{
    public DiscoveryPage(DiscoveryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}