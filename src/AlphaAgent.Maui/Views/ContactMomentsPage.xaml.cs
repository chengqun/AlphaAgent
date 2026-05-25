using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class ContactMomentsPage : ContentPage, IQueryAttributable
{
    private ContactMomentsViewModel? _viewModel;

    public ContactMomentsPage(ContactMomentsViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _viewModel?.ResetLoadState();
        _viewModel?.ApplyQueryAttributes(query);
        _viewModel?.LoadMoments();
    }
}