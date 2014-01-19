using System;
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;


namespace ChatApplication
{
    public partial class MainWindow : Window
    {
        private string ipAdress = "127.0.0.1";
        private string UserName = "Unknown";
        private StreamWriter swSender;
        private StreamReader srReceiver;
        private TcpClient tcpServer;
        private delegate void UpdateLogCallback(string strMessage);
        private delegate void CloseConnectionCallback(string strReason);
        private Thread thrMessaging;
        private IPAddress ipAddr;
        private bool Connected;


        public MainWindow()
        {
            InitializeComponent();
            sendButton.IsEnabled = false;
            message.IsEnabled = false;
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (Connected == false)
            {
                InitializeConnection();
            }
            else
            {
                CloseConnection("Loged out!");
            }
        }

        private void InitializeConnection()
        {
            string clientPort = ClientPort.Text.ToString();
            int CportInt = Convert.ToInt32(clientPort);
            if (CportInt > 1023)
            {
                ipAddr = IPAddress.Parse(ipAdress);
                tcpServer = new TcpClient();
                try
                {
                    tcpServer.Connect(ipAddr, CportInt);
                }
                catch (System.Exception exc)
                {
                    MessageBox.Show("Could not connect to the server!\n" + exc.Message);
                    return;
                }
                Connected = true;
                UserName = usrName.Text;

                message.IsEnabled = true;
                sendButton.IsEnabled = true;
                ClientPort.IsEnabled = false;
                usrName.IsEnabled = false;
                connectButton.Content = "Log out";

                swSender = new StreamWriter(tcpServer.GetStream());
                swSender.WriteLine(usrName.Text);
                swSender.Flush();

                thrMessaging = new Thread(new ThreadStart(ReceiveMessages));
                thrMessaging.Start();
            }
            else MessageBox.Show("Port number must be greater than 1023!");
        }

        private void ReceiveMessages()
        {
            srReceiver = new StreamReader(tcpServer.GetStream());
            string ConResponse = srReceiver.ReadLine();
            if (ConResponse[0] == '1')
            {
                this.Dispatcher.Invoke(new UpdateLogCallback(this.UpdateLog), new object[] { "Connected!" });
            }
            else
            {
                string Reason = "Not Connected: ";
                Reason += ConResponse.Substring(2, ConResponse.Length - 2);
                this.Dispatcher.Invoke(new CloseConnectionCallback(this.CloseConnection), new object[] { Reason });
                return;
            }

            while (Connected)
            {
                if (srReceiver != null)
                {
                    this.Dispatcher.Invoke(new UpdateLogCallback(this.UpdateLog), new object[] { srReceiver.ReadLine() });
                }
            }
        }

        private void UpdateLog(string strMessage)
        {
            if (!Connected) return;
            ChatWindow.Items.Add(strMessage);
            ChatWindow.ScrollIntoView(ChatWindow.Items[ChatWindow.Items.Count - 1]);
        }

        private void CloseConnection(string Reason)
        {
            swSender.Flush();
            ChatWindow.Items.Add("\n" + Reason + "\n");
            ChatWindow.ScrollIntoView(ChatWindow.Items[ChatWindow.Items.Count - 1]);

            usrName.IsEnabled = true;
            ClientPort.IsEnabled = true;
            message.IsEnabled = false;
            sendButton.IsEnabled = false;
            connectButton.Content = "Log in";

            srReceiver.Close();
            thrMessaging.Abort();
            swSender.Close();
            tcpServer.Close();
            Connected = false;
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            if (message.LineCount >= 1)
            {
                swSender.WriteLine(message.Text);
                swSender.Flush();
                message.Text = null;
            }
            message.Text = "";
        }

        private void message_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(srReceiver != null)
                srReceiver.Close();
            if(thrMessaging != null)
                thrMessaging.Abort();
            if(swSender != null)
                swSender.Close();
            if(tcpServer != null)
                tcpServer.Close();
            Connected = false;
        }
    }
}
