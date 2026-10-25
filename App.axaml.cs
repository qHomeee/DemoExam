using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DemoForMuhin.Services;
using DemoForMuhin.ViewModels;
using DemoForMuhin.Views;

namespace DemoForMuhin;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = CreateLoginWindow(desktop, new AuthService());
        base.OnFrameworkInitializationCompleted();
    }

    private static LoginWindow CreateLoginWindow(IClassicDesktopStyleApplicationLifetime desktop, AuthService authService)
    {
        var viewModel = new LoginViewModel(authService);
        viewModel.SignedIn += () => OpenCatalog(desktop, authService);
        viewModel.GuestRequested += () => OpenCatalog(desktop, authService);
        return new LoginWindow { DataContext = viewModel };
    }

    private static void OpenCatalog(IClassicDesktopStyleApplicationLifetime desktop, AuthService authService)
    {
        var viewModel = new MainViewModel(authService);
        viewModel.SignOutRequested += () =>
        {
            authService.Logout();
            SwitchWindow(desktop, CreateLoginWindow(desktop, authService));
        };
        SwitchWindow(desktop, new MainWindow { DataContext = viewModel });
    }

    private static void SwitchWindow(IClassicDesktopStyleApplicationLifetime desktop, Window nextWindow)
    {
        var previousWindow = desktop.MainWindow;
        desktop.MainWindow = nextWindow;
        // Новое окно открывается до закрытия старого, чтобы приложение не завершилось.
        nextWindow.Show();
        previousWindow?.Close();
    }
}
