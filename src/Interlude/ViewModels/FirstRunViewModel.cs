using System.Windows.Input;
using Interlude.Infrastructure;
using Interlude.Services;

namespace Interlude.ViewModels;

public sealed class FirstRunViewModel : ObservableObject
{
    private readonly WindowCoordinator _windowCoordinator;

    public FirstRunViewModel(WindowCoordinator windowCoordinator)
    {
        _windowCoordinator = windowCoordinator;
        SelectPlayerCommand = new RelayCommand(SelectPlayer);
    }

    public event EventHandler<bool>? RequestClose;

    public ICommand SelectPlayerCommand { get; }

    private void SelectPlayer()
    {
        var completed = _windowCoordinator.ShowPlayerSelection();
        if (completed)
        {
            RequestClose?.Invoke(this, true);
        }
    }
}
