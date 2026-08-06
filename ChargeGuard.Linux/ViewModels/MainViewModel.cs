using CommunityToolkit.Mvvm.ComponentModel;

namespace ChargeGuard.Linux.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";
}
