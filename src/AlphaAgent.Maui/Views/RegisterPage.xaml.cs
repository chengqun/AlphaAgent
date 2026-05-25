using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}