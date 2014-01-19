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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string ipAdress = "127.0.0.1";
        private string UserName = "Unknown";
        private StreamWriter swSender;
        private StreamReader srReceiver;
        private TcpClient tcpServer;
        // Needed to update the form with messages from another thread
        private delegate void UpdateLogCallback(string strMessage);
        // Needed to set the form to a "disconnected" state from another thread
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
            // If we are not currently connected but awaiting to connect
            if (Connected == false)
            {
                // Initialize the connection
                InitializeConnection();
            }
            else // We are connected, thus disconnect
            {
                CloseConnection("LOGOUT");
            }
        }

        private void InitializeConnection()
        {
            // Parse the IP address from the TextBox into an IPAddress object
            ipAddr = IPAddress.Parse(ipAdress);
            // Start a new TCP connections to the chat server
            tcpServer = new TcpClient();
            tcpServer.Connect(ipAddr, 1986);

            // Helps us track whether we're connected or not
            Connected = true;
            // Prepare the form
            UserName = usrName.Text;

            // Disable and enable the appropriate fields
            usrName.IsEnabled = false;
            message.IsEnabled = true;
            sendButton.IsEnabled = true;
            connectButton.Content = "Odjavi se";

            // Send the desired username to the server
            swSender = new StreamWriter(tcpServer.GetStream());
            swSender.WriteLine(usrName.Text);
            swSender.Flush();

            // Start the thread for receiving messages and further communication
            thrMessaging = new Thread(new ThreadStart(ReceiveMessages));
            thrMessaging.Start();
        }

        private void ReceiveMessages()
        {
            // Receive the response from the server
            srReceiver = new StreamReader(tcpServer.GetStream());
            // If the first character of the response is 1, connection was successful
            string ConResponse = srReceiver.ReadLine();
            // If the first character is a 1, connection was successful
            if (ConResponse[0] == '1')
            {
                // Update the form to tell it we are now connected
                this.Dispatcher.Invoke(new UpdateLogCallback(this.UpdateLog), new object[] { "Uspjesno spojeno!" });
            }
            else // If the first character is not a 1 (probably a 0), the connection was unsuccessful
            {
                string Reason = "Not Connected: ";
                // Extract the reason out of the response message. The reason starts at the 3rd character
                Reason += ConResponse.Substring(2, ConResponse.Length - 2);
                // Update the form with the reason why we couldn't connect
                this.Dispatcher.Invoke(new CloseConnectionCallback(this.CloseConnection), new object[] { Reason });
                // Exit the method
                return;
            }
            // While we are successfully connected, read incoming lines from the server
            while (Connected)
            {
                if (srReceiver != null) // !!!!!!
                {
                    // Show the messages in the log TextBox
                    this.Dispatcher.Invoke(new UpdateLogCallback(this.UpdateLog), new object[] { srReceiver.ReadLine() });
                }
            }
        }

        // This method is called from a different thread in order to update the log TextBox
        private void UpdateLog(string strMessage)
        {
            if (!Connected) return;
            // Append text also scrolls the TextBox to the bottom each time
            listBoxMessage.Items.Add(strMessage + "\r\n");
        }

        // Closes a current connection
        private void CloseConnection(string Reason)
        {
            swSender.Flush();
            // Show the reason why the connection is ending
            listBoxMessage.Items.Add(Reason + "\r\n");
            // Enable and disable the appropriate controls on the form
            usrName.IsEnabled = true;
            message.IsEnabled = false;
            sendButton.IsEnabled = false;
            connectButton.Content = "Spoji";

            // Close the objects
            Connected = false;
            srReceiver.Close();
            //srReceiver.Dispose();
            thrMessaging.Abort();
            swSender.Close();
            //swSender.Dispose();
            tcpServer.Close();
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        // Sends the message typed in to the server
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
            // Close the objects
            Connected = false;
            if(srReceiver != null)
                srReceiver.Close();
            //srReceiver.Dispose();
            if(thrMessaging != null)
                thrMessaging.Abort();
            if(swSender != null)
                swSender.Close();
            //swSender.Dispose();
            if(tcpServer != null)
                tcpServer.Close();
        }

    }
}
