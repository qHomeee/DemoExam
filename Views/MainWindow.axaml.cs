using System;
using Avalonia.Controls;
using DemoForMuhin.ViewModels;

namespace DemoForMuhin.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainViewModel viewModel)
            await viewModel.LoadProductsCommand.ExecuteAsync(null);
    }
}
