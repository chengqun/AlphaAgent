using AlphaAgent.Maui.ViewModels;
using CommunityToolkit.Maui.Views;

namespace AlphaAgent.Maui.Views;

public partial class VideoChannelsPage : ContentPage
{
    private readonly VideoChannelsViewModel _viewModel;
    private double _startY;

    public VideoChannelsPage(VideoChannelsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        var swipeUp = new SwipeGestureRecognizer { Direction = SwipeDirection.Up };
        swipeUp.Swiped += async (_, _) => await _viewModel.NextVideoAsync();
        var swipeDown = new SwipeGestureRecognizer { Direction = SwipeDirection.Down };
        swipeDown.Swiped += async (_, _) => await _viewModel.PreviousVideoAsync();

        Player.GestureRecognizers.Add(swipeUp);
        Player.GestureRecognizers.Add(swipeDown);

        _viewModel.VideoChanged += OnVideoChanged;
    }

    private void OnVideoChanged(string? videoUrl)
    {
        if (string.IsNullOrEmpty(videoUrl))
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Player.Source = MediaSource.FromUri(videoUrl);
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Player.Stop();
        _ = _viewModel.OnDisappearingAsync();
    }
}
