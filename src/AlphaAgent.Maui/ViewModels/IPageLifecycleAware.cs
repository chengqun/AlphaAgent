namespace AlphaAgent.Maui.ViewModels;

public interface IPageLifecycleAware
{
    Task OnAppearingAsync();
    Task OnDisappearingAsync();
}