using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using System.Collections.Generic;
using Microsoft.QueryStringDotNET;
using Newtonsoft.Json.Linq;

namespace Notiwin {
    public class NotificationUtils {
       
        public class ToastFactory {
            protected IList<IToastButton> Buttons { get; set; }
            protected IList<IToastInput> Inputs { get; set; }

            public string Id { get; set; }
            public string Label { get; set; }
            public string Body { get; set; }
            public string Image { get; set; }
            public string AppName { get; set; }

            public ToastFactory() {
                Buttons = new List<IToastButton>();
                Inputs = new List<IToastInput>();
            }

            public ToastFactory AddReply() {

                Inputs.Add(new ToastTextBox("message") { PlaceholderContent = "Type a reply" });
                Buttons.Add(new ToastButton("Reply", BindId("reply", Id))
                {
                    ActivationType = ToastActivationType.Background,
                    TextBoxId = "message"
                });

                Buttons.Add(new ToastButton("Dismiss", BindId("dismiss", Id)) { ActivationType = ToastActivationType.Background });

                return this;
            }

            public ToastFactory AddActions(JArray actions) {
                foreach (JObject action in actions) {
                    Buttons.Add(new ToastButton((string)action["label"], BindId((string)action["trigger_key"], Id))
                    { ActivationType = ToastActivationType.Background });
                }
                return this;
            }

            public XmlDocument ToXml() {
                XmlDocument toastXml = new XmlDocument();
                ToastActionsCustom actions = new ToastActionsCustom();

                Type type = actions.GetType();
                type.GetProperty("Inputs").SetValue(actions, Inputs);

                if (Buttons.Count > 5 || Properties.Settings.Default.ContextMenuActions) {
                    IList<ToastContextMenuItem> menuItems = new List<ToastContextMenuItem>();
                    IList<IToastButton> exceptions = new List<IToastButton>();

                    foreach (ToastButton button in Buttons) {
                        if (button.TextBoxId?.Length > 0) {
                            exceptions.Add(button);
                            continue;
                        }

                        menuItems.Add(new ToastContextMenuItem(button.Content, button.Arguments)
                        {
                            ActivationType = button.ActivationType
                        });
                    }

                    if (exceptions.Count > 0) {
                        type.GetProperty("Buttons").SetValue(actions, exceptions);
                    }

                    type.GetProperty("ContextMenuItems").SetValue(actions, menuItems);
                } else {
                    type.GetProperty("Buttons").SetValue(actions, Buttons);
                }

                toastXml.LoadXml(new ToastContent()
                {
                    Launch = BindId("activate", Id),
                    Visual = new ToastVisual()
                    {
                        BindingGeneric = new ToastBindingGeneric()
                        {
                            Children =
                            {
                                new AdaptiveText() { Text = Label },
                                new AdaptiveText() { Text = Body }
                            },
                            Attribution = AppName != null ? new ToastGenericAttributionText()
                            {
                                Text = AppName
                            } : null,
                            AppLogoOverride = Image != null ? new ToastGenericAppLogo()
                            {
                                Source = Image,
                                HintCrop = Properties.Settings.Default.SquareIcons ? ToastGenericAppLogoCrop.Default : ToastGenericAppLogoCrop.Circle 
                            } : null
                        }
                    },
                    Audio = new ToastAudio() { Silent = Properties.Settings.Default.Silent },
                    Actions = actions
                }.GetContent());

                return toastXml;
            }
        }

        public void ShowNotification(string tag, XmlDocument xml) {
            var toast = new ToastNotification(xml);
            toast.ExpirationTime = DateTime.Now.AddDays(2);
            toast.Group = tag;
            toast.Failed += (sender, args) => {
                Console.WriteLine("Toast failed: " + args.ToString());
            };

            ToastNotificationManager.CreateToastNotifier(Constants.AppId).Show(toast);
        }

        public void ShowNotification(ToastFactory factory) {
            ShowNotification(factory.Id, factory.ToXml());
        }

        protected static string BindId(string key, string id) {
            return new QueryString()
            {
                { "action", key },
                { "notification_id", id }
            }.ToString();
        }

        public static IDictionary<string, string> ParseId(string qs) {
            QueryString test = QueryString.Parse(qs);
            return test.ToDictionary(k => k.Name, v => v.Value);
        }

        public void DismissNotification(string id) {
            if (!Properties.Settings.Default.NoDismiss) {
                ToastNotificationManager.History.RemoveGroup(id, Constants.AppId);
            }
        }

        

        
    }
}
