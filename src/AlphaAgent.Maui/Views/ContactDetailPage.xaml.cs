using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class ContactDetailPage : ContentPage, IQueryAttributable
{
    private ContactDetailViewModel? _viewModel;

    public ContactDetailPage(ContactDetailViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _viewModel?.ApplyQueryAttributes(query);
    }
}