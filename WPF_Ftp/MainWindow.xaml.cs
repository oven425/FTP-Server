using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using System.IO;

namespace WPF_Ftp
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Socket m_SocketServer;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.m_SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.m_SocketServer.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3333));
            this.m_SocketServer.Listen(10);
            Socket client = this.m_SocketServer.Accept();

            client.Send(Encoding.UTF8.GetBytes("220 QQ FTP reade.\r\n"));
            byte[] buf = new byte[client.ReceiveBufferSize];
            int recv_len = client.Receive(buf);
            string str = Encoding.UTF8.GetString(buf);
            client.Send(Encoding.UTF8.GetBytes("331 Guest login ok, send your complete e-mail address as password.\r\n"));

            Array.Clear(buf, 0, buf.Length);
            recv_len = client.Receive(buf);
            str = Encoding.UTF8.GetString(buf);
            client.Send(Encoding.UTF8.GetBytes("230 Guest login ok, access restrictions apply.\r\n"));

            Array.Clear(buf, 0, buf.Length);
            recv_len = client.Receive(buf);
            str = Encoding.UTF8.GetString(buf);
            client.Send(Encoding.UTF8.GetBytes("215 SYST command\r\n"));

            Array.Clear(buf, 0, buf.Length);
            recv_len = client.Receive(buf);
            str = Encoding.UTF8.GetString(buf);
            string[] files = Directory.GetFiles("C:\\");
        }
    }
}
