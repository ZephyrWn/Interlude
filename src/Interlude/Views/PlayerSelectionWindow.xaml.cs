using System.Windows;
using System.Windows.Input;
using Interlude.ViewModels;

namespace Interlude.Views;

public partial class PlayerSelectionWindow : Window
{
    private readonly PlayerSelectionViewModel _viewModel;

    public PlayerSelectionWindow(PlayerSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.SelectionCompleted += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshAsync();
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            return;
        }

        DragMove();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
