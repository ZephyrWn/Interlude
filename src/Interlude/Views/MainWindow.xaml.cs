using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Interlude.Services;
using Interlude.ViewModels;

namespace Interlude.Views;

public partial class MainWindow : Window
{
    private readonly WindowCoordinator _windowCoordinator;
    private readonly WindowPlacementService _windowPlacementService;

    public MainWindow(
        MainViewModel viewModel,
        WindowCoordinator windowCoordinator,
        WindowPlacementService windowPlacementService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _windowCoordinator = windowCoordinator;
        _windowPlacementService = windowPlacementService;
        _windowPlacementService.Restore(this);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _windowPlacementService.Save(this);

        if (_windowCoordinator.ShouldCloseToTray())
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    public void SelectSettingsPage()
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SelectSettingsPage();
        }
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 ||
            e.LeftButton != MouseButtonState.Pressed ||
            IsInteractiveElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        DragMove();
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnGitHubLinkRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });

        e.Handled = true;
    }

    private static bool IsInteractiveElement(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is System.Windows.Controls.Primitives.ButtonBase ||
                source is System.Windows.Controls.TextBox ||
                source is System.Windows.Controls.ComboBox ||
                source is System.Windows.Controls.Primitives.ToggleButton)
            {
                return true;
            }

            source = System.Windows.Media.VisualTreeHelper.GetParent(source);
        }

        return false;
    }
}
