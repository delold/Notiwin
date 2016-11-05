using Microsoft.QueryStringDotNET;
using Microsoft.Win32;
using mshtml;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Notiwin {
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window {
        public event TokenAcceptHandler TokenAccept;
        public event TokenDenyHandler TokenDeny;
        public event ApiKeyHandler ApiKey;

        public LoginWindow() {
            InitializeComponent();

            LoginBrowser.Navigated += OnNavigate;
            LoginBrowser.LoadCompleted += OnLoadCompleted;
            LoginBrowser.Source = GetAuthorizeUrl();
        }

        private void OnLoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e) {
            if (((WebBrowser)sender).Source.ToString().Contains("pushbullet.com/authorize")) {
                mshtml.IHTMLDocument2 document = LoginBrowser.Document as mshtml.IHTMLDocument2;
                IHTMLStyleSheet style = document.createStyleSheet("", 0);

                style.addRule(".agree-page", "position:absolute;top:0;left:0;right:0;");
                style.addRule(".agree-page > div:nth-child(2)", "display:none;");
                style.addRule(".agree-page h1", "margin-top:100px !important;");
                style.addRule("#header img", "display:none;");
            }
        }

        private Uri GetAuthorizeUrl() {
            String qs = new QueryString()
            {
                { "client_id", "hwYmquKx86IMaTHJJ4Pd6DCO4OYv7w0E" },
                { "redirect_uri", "https://www.pushbullet.com/login-success" },
                { "response_type", "token" },
                { "scope", "everything" }
            }.ToString();

            return new Uri("https://www.pushbullet.com/authorize?" + qs);
        }

        private void OnNavigate(object sender, System.Windows.Navigation.NavigationEventArgs e) {
            String source = ((WebBrowser)sender).Source.ToString();

            if (source.Contains("pushbullet.com")) {
                string cookie = GetApiKeyFromCookie();
                if (cookie?.Length > 0) {
                    ApiKey?.Invoke(cookie);
                }
            }

            if (source.Contains("pushbullet.com/login-success")) {
                LoginBrowser.Visibility = Visibility.Hidden;

                if (source.Contains("access_token=")) {
                    string accessToken = source.Split(new string[] { "access_token=" }, StringSplitOptions.None).Last();
                    TokenAccept?.Invoke(accessToken);
                } else {
                    TokenDeny?.Invoke();
                }
            }
        }

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        protected static extern bool InternetGetCookieEx(string url, string cookieName, StringBuilder cookieData, ref int size, Int32 dwFlags, IntPtr lpReserved);

        public CookieCollection GetUriCookieContainer(Uri uri) {
            CookieContainer cookies = null;
            // Determine the size of the cookie
            int datasize = 8192 * 16;
            StringBuilder cookieData = new StringBuilder(datasize);
            if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, 0x2000, IntPtr.Zero)) {
                if (datasize < 0) {
                    return null;
                }

                // Allocate stringbuilder large enough to hold the cookie
                cookieData = new StringBuilder(datasize);
                if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, 0x2000, IntPtr.Zero)) {
                    return null;
                }
            }

            if (cookieData.Length > 0) {
                cookies = new CookieContainer();
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }
            return cookies.GetCookies(uri);
        }

        public string GetApiKeyFromCookie() {
            CookieCollection container = GetUriCookieContainer(new Uri("https://www.pushbullet.com"));
            return container["api_key"].Value;
        }

        static bool SetOption(int settingCode, int? option) {
            IntPtr optionPtr = IntPtr.Zero;
            int size = 0;
            if (option.HasValue) {
                size = sizeof(int);
                optionPtr = Marshal.AllocCoTaskMem(size);
                Marshal.WriteInt32(optionPtr, option.Value);
            }

            bool success = InternetSetOption(IntPtr.Zero, settingCode, optionPtr, size);

            if (optionPtr != IntPtr.Zero) Marshal.Release(optionPtr);
            return success;
        }

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        public delegate void TokenAcceptHandler(string token);
        public delegate void TokenDenyHandler();
        public delegate void ApiKeyHandler(string apikey);
    }
}
