using System;
using System.Windows.Forms;

namespace WTF
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppSettings settings = AppSettings.Load();

            if (settings.StartElevatedOnStartup && !IsRunningAsAdministrator())
            {
                if (TryRestartAsAdministrator())
                {
                    return;
                }
            }

            if (ShouldShowElevationPrompt(settings))
            {
                AppDialogs.ElevationPromptResult elevationPromptResult = AppDialogs.ShowElevationPrompt(settings);

                if (elevationPromptResult.DoNotShowAgain)
                {
                    settings.ShowElevationPromptOnStartup = false;
                    settings.Save();
                }

                if (elevationPromptResult.ShouldRestartElevated && TryRestartAsAdministrator())
                {
                    return;
                }
            }

            Application.Run(new MainForm());
        }

        private static bool ShouldShowElevationPrompt(AppSettings settings)
        {
            if (settings == null)
                return false;

            if (!settings.ShowElevationPromptOnStartup)
                return false;

            return !IsRunningAsAdministrator();
        }

        private static bool IsRunningAsAdministrator()
        {
            using System.Security.Principal.WindowsIdentity windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();

            if (windowsIdentity == null)
                return false;

            System.Security.Principal.WindowsPrincipal windowsPrincipal = new System.Security.Principal.WindowsPrincipal(windowsIdentity);
            return windowsPrincipal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        private static bool TryRestartAsAdministrator()
        {
            try
            {
                System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                System.Diagnostics.Process.Start(processStartInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}