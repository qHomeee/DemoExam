using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemoForMuhin.Services;

namespace DemoForMuhin.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService;

    public event Action? SignedIn;
    public event Action? GuestRequested;

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenGuestCommand))]
    private bool _isBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanInteract => !IsBusy;

    public LoginViewModel(AuthService authService) => _authService = authService;

    partial void OnLoginChanged(string value) => ErrorMessage = string.Empty;

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private async Task SignInAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Login))
        {
            ErrorMessage = "Введите логин.";
            return;
        }

        IsBusy = true;
        var success = false;
        try
        {
            success = await _authService.LoginAsync(Login);
            if (!success)
                ErrorMessage = "Пользователь не найден.";
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось выполнить вход. Проверьте подключение и подготовку базы данных, затем попробуйте ещё раз.";
        }
        finally
        {
            IsBusy = false;
        }

        if (success)
            SignedIn?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void OpenGuest()
    {
        _authService.Logout();
        GuestRequested?.Invoke();
    }
}
