using System.Windows;
using System.Windows.Input;
using Interlude.Models;
using Interlude.Services;

namespace Interlude.Views;

public partial class LanguageSelectionWindow : Window
{
    private readonly LocalizationService _localization;

    public LanguageSelectionWindow(LocalizationService localization)
    {
        InitializeComponent();
        _localization = localization;
    }

    private void OnChineseClick(object sender, RoutedEventArgs e)
    {
        SelectLanguage(AppLanguage.ChineseSimplified);
    }

    private void OnEnglishClick(object sender, RoutedEventArgs e)
    {
        SelectLanguage(AppLanguage.English);
    }

    private void SelectLanguage(string languageCode)
    {
        _localization.SetLanguage(languageCode, save: true);
        DialogResult = true;
        Close();
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
