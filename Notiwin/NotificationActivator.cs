using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Notiwin.ShellHelpers;

namespace Notiwin {
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("75F485B2-6B22-4CD9-82F8-25DBEBF09DB5"), ComVisible(true)]
    public class NotificationActivator : INotificationActivationCallback {
        public static event ActionEventHandler Action;

        public void Activate(string appUserModelId, string invokedArgs, NOTIFICATION_USER_INPUT_DATA[] data, uint dataCount) {

            IDictionary<string, string> res = NotificationUtils.ParseId(invokedArgs);
            if (data?.Length > 0) {
                foreach (NOTIFICATION_USER_INPUT_DATA inputData in data) {
                    res.Add(inputData.Key, inputData.Value);
                }
            }

            Action?.Invoke(res);
        }

        public static void Initialize() {
            regService = new RegistrationServices();

            cookie = regService.RegisterTypeForComClients(
                typeof(NotificationActivator),
                RegistrationClassContext.LocalServer,
                RegistrationConnectionType.MultipleUse);
        }
        public static void Uninitialize() {
            if (cookie != -1 && regService != null) {
                regService.UnregisterTypeForComClients(cookie);
            }
        }

        private static int cookie = -1;
        private static RegistrationServices regService = null;
    }

    public delegate void ActionEventHandler(IDictionary<string, string> data);
}
