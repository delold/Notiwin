using Microsoft.Win32;
using Notiwin.ShellHelpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Notiwin {
    class InstallUtils {
        public static void RegisterApp() {
            string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Start Menu\Programs\" + Constants.AppName + ".lnk";

            if (true || !File.Exists(shortcutPath)) {
                CreateShortcut(shortcutPath, GetExePath());
                RegisterComServer();
                ShowInActionCenter();
            }

            FixBrowserVersion();
        }

        public static void ReregisterApp() {
            bool isStartup = IsRunningOnStartup();
            UnregisterApp();
            RegisterApp();
            RunOnStartup(isStartup);
        }

        public static void UnregisterApp() {
            // disable running on startup
            RunOnStartup(false);

            //revert COM server registration
            Registry.CurrentUser.DeleteSubKeyTree(string.Format("SOFTWARE\\Classes\\CLSID\\{{{0}}}", typeof(NotificationActivator).GUID));

            //revert ShowInActionCenter
            Registry.CurrentUser.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\" + Constants.AppId);

            //revert FixBrowserVersion
            Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true).DeleteValue(Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName));

            //delete shortcut
            File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Start Menu\Programs\" + Constants.AppName + ".lnk");
        }

        public static void KillOtherInstances() {
            Process[] processList = Process.GetProcessesByName(Assembly.GetExecutingAssembly().GetName().Name);
            Process currentProcess = Process.GetCurrentProcess();

            if (processList?.Length > 0) {
                foreach(Process instance in processList) {
                    if (instance.Id != currentProcess.Id) {
                        instance.Kill();
                    }
                }
            }
        }

        private static void CreateShortcut(string shortcutPath, string exePath) {
            IShellLinkW newShortcut = (IShellLinkW)new CShellLink();
            newShortcut.SetPath(exePath);

            IPropertyStore newShortcutProperties = (IPropertyStore)newShortcut;

            PropVariantHelper varAppId = new PropVariantHelper();
            varAppId.SetValue(Constants.AppId);
            newShortcutProperties.SetValue(PROPERTYKEY.AppUserModel_ID, varAppId.Propvariant);

            PropVariantHelper varToastId = new PropVariantHelper();
            varToastId.VarType = VarEnum.VT_CLSID;
            varToastId.SetValue(typeof(NotificationActivator).GUID);

            newShortcutProperties.SetValue(PROPERTYKEY.AppUserModel_ToastActivatorCLSID, varToastId.Propvariant);
            newShortcutProperties.Commit();

            IPersistFile newShortcutSave = (IPersistFile)newShortcut;
            
            newShortcutSave.Save(shortcutPath, true);
        }

        private static void RegisterComServer() {
            string regString = string.Format("SOFTWARE\\Classes\\CLSID\\{{{0}}}\\LocalServer32", typeof(NotificationActivator).GUID);
            var key = Registry.CurrentUser.CreateSubKey(GetExePath());
            key.SetValue(null, GetExePath());
        }

        private static void ShowInActionCenter() {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings", true)) {
                RegistryKey subKey = key.CreateSubKey(Constants.AppId);
                if (subKey != null && (int)subKey.GetValue("ShowInActionCenter", 0) != 1) {
                    subKey.SetValue("ShowInActionCenter", 1);
                }
            }
        }

        private static void FixBrowserVersion() {
            string filename = Path.GetFileName(GetExePath());

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true)) {
                key?.SetValue(filename, 11001);
            }
        }

        public static void RunOnStartup(bool enable) {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)) {
                if (enable) {
                    key.SetValue(Constants.AppName, '"' + GetExePath() + '"');
                } else {
                    key.DeleteValue(Constants.AppName);
                }
            }
        }

        public static bool IsRunningOnStartup() {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run")) {
                return key.GetValue(Constants.AppName) != null;
            }
        }

        private static string GetExePath() {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

    }
}
