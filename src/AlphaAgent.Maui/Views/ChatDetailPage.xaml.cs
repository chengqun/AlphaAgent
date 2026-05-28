using AlphaAgent.Maui.ViewModels;

namespace AlphaAgent.Maui.Views;

public partial class ChatDetailPage : ContentPage, IQueryAttributable
{
    private ChatDetailViewModel? _viewModel;

    public ChatDetailPage(ChatDetailViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();

        viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _viewModel?.ApplyQueryAttributes(query);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is IPageLifecycleAware lifecycleAware)
            await lifecycleAware.OnAppearingAsync();

        // 等待 CollectionView 完成布局后再滚动到底部
        await ScrollToBottomAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is IPageLifecycleAware lifecycleAware)
            await lifecycleAware.OnDisappearingAsync();
    }

    private void OnMessagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            _ = ScrollToBottomAsync();
        }
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            // 给 CollectionView 一点时间完成布局渲染
            await Task.Delay(50);

            if (BindingContext is ChatDetailViewModel vm && vm.Messages.Count > 0)
            {
                var lastItem = vm.Messages[^1];
                MessagesList.ScrollTo(lastItem, position: ScrollToPosition.MakeVisible, animate: false);
            }
        }
        catch
        {
            // CollectionView 滚动可能因布局未完成而失败，忽略
        }
    }

    private async void OnEntryCompleted(object? sender, EventArgs e)
    {
        if (BindingContext is ChatDetailViewModel vm && vm.SendMessageCommand.CanExecute(null))
        {
            await vm.SendMessageCommand.ExecuteAsync(null);

            // 发送后取消焦点，收起键盘
            MessageEntry.Unfocus();
        }
    }
}