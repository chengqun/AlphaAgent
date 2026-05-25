using AlphaAgent.Maui.ViewModels;
using System.Diagnostics;

namespace AlphaAgent.Maui.Views;

public partial class MomentsPage : ContentPage
{
    public MomentsViewModel ViewModel { get; }

    public MomentsPage(MomentsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        BindingContext = viewModel;
        Debug.WriteLine("[MomentsPage] 构造函数执行完成");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine("[MomentsPage] OnAppearing 被调用");
        
        // 检查命令是否可用（注意 RelayCommand 生成的属性名去掉了 Async 后缀）
        if (ViewModel.AddMomentCommand != null)
        {
            Debug.WriteLine($"[MomentsPage] AddMomentCommand 已创建, CanExecute = {ViewModel.AddMomentCommand.CanExecute(null)}");
        }
        else
        {
            Debug.WriteLine("[MomentsPage] AddMomentCommand 为 null");
        }
        
        ViewModel?.LoadMoments();
    }
}