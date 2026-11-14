using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DemoForMuhin.Views;

public partial class MessageWindow : Window
{
    public MessageWindow() => InitializeComponent();

    private MessageWindow(string title, string icon, string message, bool confirm) : this()
    {
        Title = $"Чудо Обувь — {title}";
        MessageIcon.Text = icon;
        MessageText.Text = message;
        CancelButton.IsVisible = confirm;
        AcceptButton.Content = confirm ? "Подтвердить" : "ОК";
        AcceptButton.IsDefault = !confirm;
    }

    public static Task ErrorAsync(Window owner, string message) =>
        new MessageWindow("Ошибка", "⛔", message, false).ShowDialog(owner);

    public static Task InfoAsync(Window owner, string message) =>
        new MessageWindow("Информация", "ℹ", message, false).ShowDialog(owner);

    public static Task<bool> ConfirmAsync(Window owner, string message) =>
        new MessageWindow("Подтверждение", "⚠", message, true).ShowDialog<bool>(owner);

    private void Accept(object? sender, RoutedEventArgs e) => Close(true);
    private void Cancel(object? sender, RoutedEventArgs e) => Close(false);
}
