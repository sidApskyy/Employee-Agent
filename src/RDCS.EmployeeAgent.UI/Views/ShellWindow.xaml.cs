using RDCS.EmployeeAgent.UI.ViewModels;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace RDCS.EmployeeAgent.UI.Views;

public partial class ShellWindow : Window
{
    private readonly ShellViewModel _viewModel;
    private System.Windows.Forms.NotifyIcon _notifyIcon = null!;
    private bool _forceClose = false;

    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += ShellWindow_Loaded;
        Closing += ShellWindow_Closing;
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "RDCS Employee Agent — Running"
        };

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Open", null, (s, e) => ShowWindow());
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (s, e) =>
        {
            _forceClose = true;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Application.Current.Shutdown();
        });

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowWindow();
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ShellWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_forceClose)
        {
            e.Cancel = true;
            Hide();
            _notifyIcon.ShowBalloonTip(
                3000,
                "RDCS Agent Still Running",
                "The agent is running in the background. Right-click the tray icon to exit.",
                System.Windows.Forms.ToolTipIcon.Info);
        }
    }

    private async void ShellWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeCommand.ExecuteAsync(null);
    }
}
