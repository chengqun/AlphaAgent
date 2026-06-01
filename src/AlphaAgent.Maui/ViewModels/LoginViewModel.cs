using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Auth;

namespace AlphaAgent.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService? _authService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoginEnabled = true;

    [ObservableProperty]
    private bool _hasError;

    public LoginViewModel()
    {
    }

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "请输入用户名和密码";
            HasError = true;
            return;
        }

        IsLoading = true;
        IsLoginEnabled = false;
        StatusMessage = "正在登录...";
        HasError = false;

        try
        {
            if (_authService != null)
            {
                var response = await Task.Run(() => _authService.LoginAsync(Username, Password));
                if (response.Success && response.Data != null)
                {
                    StatusMessage = "登录成功";
                    HasError = false;
                    await Shell.Current.GoToAsync("InitializingPage");
                }
                else
                {
                    StatusMessage = response.Error ?? "用户名或密码错误";
                    HasError = true;
                }
            }
            else
            {
                await Task.Delay(1000);
                StatusMessage = "演示模式 - 登录成功";
                HasError = false;
                await Shell.Current.GoToAsync("//ChatPage");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"登录失败: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            IsLoginEnabled = true;
        }
    }

    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        await Shell.Current.GoToAsync("RegisterPage");
    }
}