using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class NewFriendsPage : ContentPage
{
    public NewFriendsViewModel ViewModel { get; }

    public NewFriendsPage(NewFriendsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnAppearing(object sender, EventArgs e)
    {
        ViewModel.LoadPendingRequestsCommand.Execute(null);
    }
}