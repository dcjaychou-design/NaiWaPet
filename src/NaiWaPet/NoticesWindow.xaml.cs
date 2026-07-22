using System.Windows;
using NaiWaPet.Services;

namespace NaiWaPet;

internal sealed partial class NoticesWindow : Window
{
    public NoticesWindow()
    {
        InitializeComponent();
        VersionText.Text = $"版本 {OpenSourceNotices.Version}";
        NoticeTextBox.Text = OpenSourceNotices.LoadAll();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
