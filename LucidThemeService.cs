using System;
using Lucid.Theming;

namespace WTF
{
    public static class LucidThemeService
    {
        public static void Apply(AppLayout layout)
        {
            bool useDarkMode = ShouldUseDarkMode(layout);
            ThemeProvider.SetThemeWithAlias(useDarkMode ? "Dark" : "Light");
        }

        private static bool ShouldUseDarkMode(AppLayout layout)
        {
            if (layout == AppLayout.WindowsDarkMode)
                return true;

            if (layout == AppLayout.WindowsLightMode)
                return false;

            return IsWindowsAppDarkModeEnabled();
        }

        private static bool IsWindowsAppDarkModeEnabled()
        {
            try
            {
                using Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

                object value = key?.GetValue("AppsUseLightTheme");

                if (value is int appsUseLightTheme)
                {
                    return appsUseLightTheme == 0;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
