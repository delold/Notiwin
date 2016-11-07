using System;
using System.Threading.Tasks;
using SuperSocket.ClientEngine;
using WebSocket4Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Notiwin {
    public delegate void DataEventHandler(JObject data);
    public delegate void LoginErrorHandler();

    public class WebsocketUtils {
        public event LoginErrorHandler LoginError;
        public event DataEventHandler Data;

        private WebSocket webSocket;

        private readonly string ENDPOINT_URL = "wss://stream.pushbullet.com/websocket/";
        private string token;
        private int attempts = 0;

        private bool clean = false;

        public string Token {
            get { return token; }
            set { token = value; }
        }

        public void Connect() {
            attempts = 0;
            clean = false;

            CreateWebsocket();
        }

        public void Reconnect() {
            if (webSocket?.State == WebSocketState.Open) {
                webSocket.Close();
            } else {
                Connect();
            }
        }

        public void Disconnect() {
            clean = true;
            if (webSocket?.State == WebSocketState.Open) {
                webSocket.Close();
            }
        }

        public bool IsConnected() {
            return webSocket?.State == WebSocketState.Open;
        }

        private void SubscribeEvents() {
            webSocket.Opened += new EventHandler(OnOpened);
            webSocket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(OnMessage);
            webSocket.Error += new EventHandler<ErrorEventArgs>(OnError);
            webSocket.Closed += new EventHandler(onClosed);
        }

        private void UnsubscribeEvents() {
            webSocket.Opened -= new EventHandler(OnOpened);
            webSocket.MessageReceived -= new EventHandler<MessageReceivedEventArgs>(OnMessage);
            webSocket.Error -= new EventHandler<ErrorEventArgs>(OnError);
            webSocket.Closed -= new EventHandler(onClosed);
        }

        private async void CreateWebsocket() {
            Console.WriteLine("Attempt to create websocket");

            if (webSocket == null || webSocket.State == WebSocketState.Closed || webSocket.State == WebSocketState.None) {
                Console.WriteLine("Creating websocket");

                if (webSocket != null) {
                    UnsubscribeEvents();
                }

                // await (Task.Delay((int)Math.Pow(2, attempts) * 1000));
                // wait 5 seconds instead of exponential backoff
                // TODO: implement listener of network changes
                if (attempts > 0) {
                    
                    await (Task.Delay(5000));
                }

                attempts += 1;

                webSocket = new WebSocket(ENDPOINT_URL + token);
                SubscribeEvents();
                webSocket.Open();
            }
        }

        private void onClosed(object sender, EventArgs e) {
            Console.WriteLine("Closed");

            if (!clean) {
                CreateWebsocket();
            }

        }

        private void OnError(object sender, ErrorEventArgs e) {
            Console.WriteLine("Error");
            Console.WriteLine(e.ToString());

            if (e.Exception.Message.Contains("401") && e.Exception.Message.Contains("HTTP")) {
                LoginError?.Invoke();
                Disconnect();
                return;
            }

            if (!clean) {
                CreateWebsocket();
            }
        }

        private void OnMessage(object sender, MessageReceivedEventArgs e) {
            JObject result = JObject.Parse(e.Message);
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
            Data?.Invoke(result);
        }

        private void OnOpened(object sender, EventArgs e) {
            Console.WriteLine("Opened");
            attempts = 0;
        }


    }
}
