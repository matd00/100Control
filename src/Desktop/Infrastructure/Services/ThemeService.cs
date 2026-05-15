using System.Windows;
using MaterialDesignThemes.Wpf;

namespace Desktop.Infrastructure.Services;

public class ThemeService : IThemeService
{
    private readonly PaletteHelper _paletteHelper = new();
    private bool _isDarkMode;

    public bool IsDarkMode => _isDarkMode;

    public ThemeService()
    {
        // Detect current theme or default to Light
        var theme = _paletteHelper.GetTheme();
        _isDarkMode = theme.GetBaseTheme() == BaseTheme.Dark;
    }

    public void ToggleTheme()
    {
        SetTheme(!_isDarkMode);
    }

    public void SetTheme(bool isDark)
    {
        _isDarkMode = isDark;
        
        // 1. Update MaterialDesign Theme
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);

        // 2. Update Custom SaaS Colors
        UpdateCustomTheme(isDark);
    }

    private void UpdateCustomTheme(bool isDark)
    {
        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var themeSource = isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
        
        // Find and replace our custom theme dictionary
        // We look for a dictionary that has one of our known theme paths
        for (int i = 0; i < dictionaries.Count; i++)
        {
            var source = dictionaries[i].Source?.ToString();
            if (source != null && (source.Contains("Themes/LightTheme.xaml") || source.Contains("Themes/DarkTheme.xaml")))
            {
                dictionaries[i] = new ResourceDictionary 
                { 
                    Source = new Uri($"pack://application:,,,/Themes/{(isDark ? "Dark" : "Light")}Theme.xaml", UriKind.Absolute)
                };
                return;
            }
        }

        // If not found, add it
        dictionaries.Add(new ResourceDictionary 
        { 
            Source = new Uri($"pack://application:,,,/Themes/{(isDark ? "Dark" : "Light")}Theme.xaml", UriKind.Absolute)
        });
    }
}
