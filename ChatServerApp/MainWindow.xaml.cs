using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace ChatServerApp
{
    public partial class MainWindow : Window
    {
        private delegate void UpdateStatusCallback(string strMessage);
        private string ipAdress = "127.0.0.1";
        ChatServer mainServer;
        //private bool _ServRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            System.Windows.Threading.DispatcherTimer DispTimer = new System.Windows.Threading.DispatcherTimer();
            DispTimer.Tick += new EventHandler(DispTimer_Tick);
            DispTimer.Interval = new TimeSpan(0, 0, 1);
            DispTimer.Start();
        }

        private void btnListen_Click(object sender, RoutedEventArgs e)
        {
            //if (_ServRunning == false)
            //{
                string port = PortNo.Text.ToString();
                int portInt = Convert.ToInt32(port);
                if (portInt > 1023)
                {
                    PortNo.IsEnabled = false;
                    btnListen.IsEnabled = false;
                    btnListen.Content = "Server running";
                    IPAddress ipAddr = IPAddress.Parse(ipAdress);
                    mainServer = new ChatServer(ipAddr);
                    ChatServer.StatusChanged += new StatusChangedEventHandler(mainServer_StatusChanged);
                    mainServer.StartListening(portInt);
                    ServChatWindow.Items.Add("Waiting for clients...\n");
                }
                else MessageBox.Show("Port number must be greater than 1023!");
               //_ServRunning = true;
            //}
            //else
            //{
            //    mainServer.RequestStop();
            //    btnListen.Content = "Run server";
            //    btnListen.IsEnabled = true;
            //    PortNo.IsEnabled = true;
            //    _ServRunning = false;
            //}
        }

        public void mainServer_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            this.Dispatcher.Invoke(new UpdateStatusCallback(this.UpdateStatus), new object[] { e.EventMessage });
        }

        private void UpdateStatus(string strMessage)
        {
            ServChatWindow.Items.Add(strMessage);
            ServChatWindow.ScrollIntoView(ServChatWindow.Items[ServChatWindow.Items.Count - 1]);
        }

        private void ChatServet_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void DispTimer_Tick(object sender, EventArgs e)
        {
            users.Items.Clear();
            IDictionaryEnumerator en = ChatServer.htConnections.GetEnumerator();
            while (en.MoveNext())
            {
                string str = en.Value.ToString();
                if (!users.Items.Contains(str))
                {
                    users.Items.Add(str);
                    users.ScrollIntoView(users.Items[users.Items.Count - 1]);
                }
            }
        }
    }
}
