using Avalonia.Controls;
using VideoSlicer.ViewModels;

namespace VideoSlicer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Dispose();
        }
        base.OnClosing(e);
    }
}