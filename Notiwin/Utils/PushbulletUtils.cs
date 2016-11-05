using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Notiwin.Utils;

namespace Notiwin {
    class PushbulletUtils {
        public async static Task<string> ResolveUrlFromPackageName(string packageName) {
            using (HttpClient client = new HttpClient()) {
                var response = await client.GetAsync("https://update.pushbullet.com/android_mapping.json");
                string content = await response.Content.ReadAsStringAsync();
                JObject list = await Task.Run(() => JObject.Parse(content));

                try {
                    return (string)list[packageName];
                } catch { }
            }
            return null;
        }

        public async static Task SendEphemeral(JObject json) {
            Console.WriteLine("------SEND.REQUEST------");
            Console.WriteLine(json.ToString(Newtonsoft.Json.Formatting.Indented));
            Console.WriteLine("----END.SEND.REQUEST----");

            await SendEphemeral(json.ToString());
        }

        public async static Task SendEphemeral(string json) {
            using (HttpClient client = new HttpClient()) {
                HttpContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                content.Headers.Add("Access-Token", Properties.Settings.Default.Token);

                // TODO: implement encryption

                HttpResponseMessage response = await client.PostAsync("https://api.pushbullet.com/v2/ephemerals", content);

                string errorResp = await response.Content.ReadAsStringAsync();

                Console.WriteLine("------SEND.RESPONSE------");
                Console.WriteLine(errorResp);
                Console.WriteLine("----END.SEND.RESPONSE----");

                if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                    JObject result = await Task.Run(() => JObject.Parse(errorResp));
                    string error = result["error"]["type"] + ": " + result["error"]["message"];

                    throw new Exception(error);
                }
            }
        }

        public async static void RespondSms(string message, string thread_id, string source_device_iden, string source_user_iden) {
            // TODO: check if SMS is working

            using (HttpClient client = new HttpClient()) {
                HttpContent payload = new StringContent(new JObject() { ["key"] = source_device_iden + "_threads" }.ToString(), System.Text.Encoding.UTF8, "application/json");
                payload.Headers.Add("Authorization", "Bearer + " + Properties.Settings.Default.ApiKey);
                HttpResponseMessage response = await client.PostAsync("https://api.pushbullet.com/v3/get-permanent", payload);
                string resp = await response.Content.ReadAsStringAsync();

                Console.WriteLine("------SEND.SMS_PERMANENT------");
                Console.WriteLine(resp);
                Console.WriteLine("----END.SEND.SMS_PERMANENT----");

                JObject parsed = await Task.Run(() => JObject.Parse(resp));

                // TODO: implement encryption
               
                foreach(JObject thread in parsed?["data"]?["threads"]) {
                    if (((string) thread["id"]).Equals(thread_id)) {
                        foreach(JObject recipient in thread?["recipients"]) {
                            JObject body = new JObject()
                            {
                                ["type"] = "push",
                                ["push"] = new JObject()
                                {
                                    ["conversation_iden"] = (string) recipient["address"],
                                    ["message"] = message,
                                    ["package_name"] = "com.pushbullet.android",
                                    ["source_user_iden"] = source_user_iden,
                                    ["target_device_iden"] = source_device_iden,
                                    ["type"] = "messaging_extension_reply"
                                }
                            };

                            await SendEphemeral(body);
                        }
                    }
                }
            }
        }

        public static string ConvertBase64Image(string base64) {
            byte[] data = Convert.FromBase64String(base64);
            string target = Path.Combine(Path.GetTempPath(), HashUtils.ComputeHash(data) + ".jpg");
            File.WriteAllBytes(target, data);
            return target;
        }

        public async static Task<string> DownloadImage(string url) {
            using (HttpClient client = new HttpClient()) {
                var response = await client.GetAsync(url);
                byte[] content = await response.Content.ReadAsByteArrayAsync();

                string target = Path.Combine(Path.GetTempPath(), HashUtils.ComputeHash(content) + ".jpg");
                File.WriteAllBytes(target, content);

                return target;
            }
        }

        public async static void QuickReply(JObject push, string reply) {
            JObject body = new JObject()
            {
                ["type"] = "push",
                ["push"] = new JObject()
                {
                    ["type"] = "messaging_extension_reply",
                    ["source_user_iden"] = push["source_user_iden"],
                    ["target_device_iden"] = push["source_device_iden"],
                    ["package_name"] = push["package_name"],
                    ["conversation_iden"] = push["conversation_iden"],
                    ["message"] = reply
                }
            };

            await SendEphemeral(body);
        }


        public async static void DismissPush(JObject push, string trigger) {
            JObject body = new JObject()
            {
                ["type"] = "push",
                ["push"] = new JObject()
                {
                    ["notification_id"] = push["notification_id"],
                    ["package_name"] = push["package_name"],
                    ["source_user_iden"] = push["source_user_iden"],
                    ["type"] = "dismissal"
                }
            };

            if (trigger != null) {
                body["push"]["trigger_action"] = trigger;
            }

            await SendEphemeral(body);

        }
    }
}
