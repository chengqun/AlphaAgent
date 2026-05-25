using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class AddFriendPage : ContentPage
{
    public AddFriendPage(AddFriendViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}