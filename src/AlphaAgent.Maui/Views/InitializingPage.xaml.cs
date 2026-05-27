using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class InitializingPage : ContentPage
{
    public InitializingPage(InitializingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        Loaded += async (_, _) =>
        {
            if (BindingContext is InitializingViewModel vm)
            {
                vm.InitializationCompleted += OnInitializationCompleted;
                await vm.InitializeCommand.ExecuteAsync(null);
            }
        };

        Disappearing += (_, _) =>
        {
            if (BindingContext is InitializingViewModel vm)
            {
                vm.InitializationCompleted -= OnInitializationCompleted;
            }
        };
    }

    private void OnInitializationCompleted(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(500);
            await Shell.Current.GoToAsync("//ChatPage");
        });
    }
}