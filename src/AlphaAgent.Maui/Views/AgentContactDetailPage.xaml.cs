using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class AgentContactDetailPage : ContentPage
{
    private AgentContactDetailViewModel ViewModel => BindingContext as AgentContactDetailViewModel;

    public AgentContactDetailPage(AgentContactDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.LoadAgentInfoCommand.ExecuteAsync(null);
        }
    }
}