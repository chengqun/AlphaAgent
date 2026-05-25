using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class ChatPage : ContentPage
{
    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ChatViewModel vm)
            await vm.OnAppearingAsync();
    }
}
