using AlphaAgent.Maui.Models;
using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class AgentChatDetailPage : ContentPage
{
    private AgentChatDetailViewModel ViewModel => BindingContext as AgentChatDetailViewModel;

    // 供 DataTemplate 内 RelativeSource 绑定使用
    public string AgentName => ViewModel?.AgentName ?? string.Empty;

    private DateTime _lastScrollTime = DateTime.MinValue;
    private readonly TimeSpan _scrollThrottle = TimeSpan.FromMilliseconds(150);

    public AgentChatDetailPage(AgentChatDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        viewModel.StreamingContentUpdated += OnStreamingContentUpdated;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AgentChatDetailViewModel.IsStockMode)
            || e.PropertyName == nameof(AgentChatDetailViewModel.StockName)
            || e.PropertyName == nameof(AgentChatDetailViewModel.AgentName))
        {
            UpdateTitle();
        }
    }

    private void UpdateTitle()
    {
        if (ViewModel == null) return;
        Title = ViewModel.IsStockMode ? ViewModel.StockName : ViewModel.AgentName;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateTitle();

        if (ViewModel != null)
        {
            await ViewModel.InitializeCommand.ExecuteAsync(null);
        }

        await ScrollToBottomAsync();
    }

    private void OnMessagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            ThrottledScrollToBottom();
        }
    }

    private void OnStreamingContentUpdated()
    {
        ThrottledScrollToBottom();
    }

    private void ThrottledScrollToBottom()
    {
        var now = DateTime.UtcNow;
        if (now - _lastScrollTime < _scrollThrottle)
            return;

        _lastScrollTime = now;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (ViewModel != null && ViewModel.Messages.Count > 0)
                {
                    var lastItem = ViewModel.Messages[^1];
                    AgentMessagesList.ScrollTo(lastItem, position: ScrollToPosition.End, animate: false);
                }
            }
            catch
            {
            }
        });
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            await Task.Delay(50);

            if (ViewModel != null && ViewModel.Messages.Count > 0)
            {
                var lastItem = ViewModel.Messages[^1];
                AgentMessagesList.ScrollTo(lastItem, position: ScrollToPosition.End, animate: false);
            }
        }
        catch
        {
        }
    }

    private void OnToolCardTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject bindable && bindable.BindingContext is ChatMessageItem item)
        {
            item.IsExpanded = !item.IsExpanded;
        }
    }

    private async void OnEntryCompleted(object? sender, EventArgs e)
    {
        if (ViewModel != null && ViewModel.SendMessageCommand.CanExecute(null))
        {
            await ViewModel.SendMessageCommand.ExecuteAsync(null);
        }
    }
}
