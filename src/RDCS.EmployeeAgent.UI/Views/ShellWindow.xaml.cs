using RDCS.EmployeeAgent.UI.ViewModels;
using System.Windows;

namespace RDCS.EmployeeAgent.UI.Views;

public partial class ShellWindow : Window
{
    private readonly ShellViewModel _viewModel;

    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += ShellWindow_Loaded;
    }

    private async void ShellWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeCommand.ExecuteAsync(null);
    }
}
