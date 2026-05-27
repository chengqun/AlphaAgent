using AlphaAgent.Maui.ViewModels;
using AlphaAgent.Maui.Events;

namespace AlphaAgent.Maui.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage(SplashViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        Loaded += async (_, _) =>
        {
            if (BindingContext is SplashViewModel splashViewModel)
            {
                splashViewModel.InitializationCompleted += OnInitializationCompleted;
                await splashViewModel.InitializeCommand.ExecuteAsync(null);
            }
        };

        Disappearing += (_, _) =>
        {
            if (BindingContext is SplashViewModel splashViewModel)
            {
                splashViewModel.InitializationCompleted -= OnInitializationCompleted;
            }
        };
    }

    private void OnInitializationCompleted(object? sender, SplashNavigationEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (BindingContext is SplashViewModel vm && vm.HasUpdate && vm.UpdateInfo != null)
            {
                await HandleUpdateAsync(vm, e);
            }
            else
            {
                NavigateToMain(e);
            }
        });
    }

    private async Task HandleUpdateAsync(SplashViewModel vm, SplashNavigationEventArgs e)
    {
        var updateInfo = vm.UpdateInfo!;
        var title = $"发现新版本 v{updateInfo.VersionName}";
        var message = string.IsNullOrEmpty(updateInfo.UpdateNote)
            ? "请更新到最新版本"
            : updateInfo.UpdateNote;

        if (updateInfo.IsForce)
        {
            // 强制更新：只有"立即更新"按钮
            await Shell.Current.DisplayAlert(title, message, "立即更新");
            await Launcher.OpenAsync(updateInfo.UpdateUrl);
            // 强制更新不导航，等待用户安装新版
        }
        else
        {
            // 非强制更新：提供"立即更新"和"稍后再说"
            var update = await Shell.Current.DisplayAlert(title, message, "立即更新", "稍后再说");
            if (update)
            {
                await Launcher.OpenAsync(updateInfo.UpdateUrl);
            }
            NavigateToMain(e);
        }
    }

    private static void NavigateToMain(SplashNavigationEventArgs e)
    {
        if (e.IsLoggedIn)
        {
            Shell.Current.GoToAsync("InitializingPage");
        }
        else
        {
            Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
