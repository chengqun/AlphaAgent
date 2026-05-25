using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Auth;

namespace AlphaAgent.Maui.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IAuthService? _authService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _emailAddress = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRegisterEnabled = true;

    [ObservableProperty]
    private bool _hasError;

    public RegisterViewModel()
    {
    }

    public RegisterViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(EmailAddress)
            || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            StatusMessage = "请填写所有字段";
            HasError = true;
            return;
        }

        if (Password != ConfirmPassword)
        {
            StatusMessage = "两次输入的密码不一致";
            HasError = true;
            return;
        }

        IsLoading = true;
        IsRegisterEnabled = false;
        StatusMessage = "正在注册...";
        HasError = false;

        try
        {
            if (_authService != null)
            {
                var registerResult = await _authService.RegisterAsync(Username, EmailAddress, Password);
                if (registerResult.Success)
                {
                    // 注册成功，自动登录
                    var loginResult = await _authService.LoginAsync(Username, Password);
                    if (loginResult.Success && loginResult.Data != null)
                    {
                        await Shell.Current.GoToAsync("//ChatPage");
                    }
                    else
                    {
                        StatusMessage = "注册成功，请登录";
                        HasError = false;
                        await Shell.Current.GoToAsync("//LoginPage");
                    }
                }
                else
                {
                    StatusMessage = registerResult.Error ?? "注册失败";
                    HasError = true;
                }
            }
            else
            {
                await Task.Delay(1000);
                StatusMessage = "演示模式 - 注册成功";
                HasError = false;
                await Shell.Current.GoToAsync("//ChatPage");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"注册失败: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            IsRegisterEnabled = true;
        }
    }

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
