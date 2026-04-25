using FocusShelf.App.Repositories;
using FocusShelf.App.Services;
using FocusShelf.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FocusShelf.App.Views;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(
            new JsonFocusRepository(),
            new DispatcherTimerService(DispatcherQueue),
            new MotivationService(),
            DispatcherQueue);

        Root.DataContext = _viewModel;
        Root.Loaded += OnRootLoaded;
        Closed += OnClosed;
    }

    private async void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        Root.Loaded -= OnRootLoaded;
        await _viewModel.LoadAsync();
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await _viewModel.SaveAsync();
    }

    private void OnRemoveDoneItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { CommandParameter: string item })
        {
            return;
        }

        if (_viewModel.RemoveDoneItemCommand.CanExecute(item))
        {
            _viewModel.RemoveDoneItemCommand.Execute(item);
        }
    }
}
