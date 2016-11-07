using Windows.Networking.Connectivity;

namespace Notiwin {
    static class NetworkUtils {
        public static event NetworkChangeHandler ConnectionChanged;

        static NetworkUtils() {
            NetworkInformation.NetworkStatusChanged += OnNetworkChange;
        }

        private static void OnNetworkChange(object sender) {
            ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();
            ConnectionChanged?.Invoke(profile?.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
        }

        public delegate void NetworkChangeHandler(bool isConnected);
    }
}
