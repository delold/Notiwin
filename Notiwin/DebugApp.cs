using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using WebSocket4Net;
using SuperSocket.ClientEngine;
using Newtonsoft.Json;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Data.Xml.Dom;

using Microsoft.Win32;
using System.IO;

namespace Notiwin {
    public partial class DebugApp : Form {
        private WebsocketUtils websocket;

        public DebugApp(WebsocketUtils websocket) {
            this.websocket = websocket;

            InitializeComponent();
        }

        private void MainApp_Load(object sender, EventArgs e) {
            logView.Columns.Add("App Name", -2, HorizontalAlignment.Left);
            logView.Columns.Add("Title", -2, HorizontalAlignment.Left);
            logView.Columns.Add("Body", -2, HorizontalAlignment.Left);

            logView.Items.Clear();
            logView.Refresh();

            websocket.Data += OnData;
        }

        private void MainApp_FormClosing(object sender, FormClosingEventArgs e) {
            if (websocket != null) {
                websocket.Data -= OnData;
            }
        }

        private void OnData(dynamic payload) {
            string check = payload.type;

            if (check.Equals("push")) {
                string type = payload.push.type;
                string title = payload.push.title;
                string body = payload.push.body;
                string application_name = payload.push.application_name;

                ListViewItem item = new ListViewItem(application_name);
                item.SubItems.Add(title);
                item.SubItems.Add(body);


                if (type.Equals("mirror")) {
                    this.InsertLog(item);
                }
            }
        }

        private void connectBtn_Click(object sender, EventArgs e) {
            Console.WriteLine("Connecting");
        }

        public void InsertLog(ListViewItem item) {
            if (InvokeRequired) {
                Invoke(new InsertIntoListDelegate(InsertLog), item);
            } else {
                logView.Items.Add(item);
                logView.Refresh();
            }
        }

        private delegate void InsertIntoListDelegate(ListViewItem item);
    }
}
