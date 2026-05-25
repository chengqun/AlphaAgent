using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class ContactsPage : ContentPage
{
    public ContactsViewModel ViewModel { get; }

    public ContactsPage(ContactsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadContactsAsync();
    }

    // 点击页面空白处取消搜索焦点
    private void OnPageTapped(object sender, EventArgs e)
    {
        SearchBarControl?.Unfocus();
    }
}