using Microsoft.Win32;
using Notiwin.ShellHelpers;
using Squirrel;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Notiwin {
    class RegistryUtils {

        public static void RegisterApp() {
            string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Start Menu\Programs\" + Constants.AppName + ".lnk";

            if (true || !File.Exists(shortcutPath)) {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                CreateShortcut(shortcutPath, exePath);
                RegisterComServer(exePath);
                ShowInActionCenter();
            }

            FixBrowserVersion();
        }

        public static void ReregisterApp() {
            UnregisterApp();
            RegisterApp();
        }

        public static void UnregisterApp() {
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

            if (p?.Length > 0) {
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

        private static void RegisterComServer(string exePath) {
            string regString = string.Format("SOFTWARE\\Classes\\CLSID\\{{{0}}}\\LocalServer32", typeof(NotificationActivator).GUID);
            var key = Registry.CurrentUser.CreateSubKey(regString);
            key.SetValue(null, exePath);
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
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string filename = Path.GetFileName(exePath);

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true)) {
                key?.SetValue(filename, 11001);
            }
        }

    }
}
