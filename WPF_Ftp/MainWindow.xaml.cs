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
using System.Threading.Tasks;

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
        Socket m_SocketData;
        CQMainUI m_MainUI;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(this.m_MainUI == null)
            {
                this.DataContext = this.m_MainUI = new CQMainUI();

                Task.Factory.StartNew(() =>
                {
                    this.m_SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    this.m_SocketServer.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3333));
                    this.m_SocketServer.Listen(10);
                    Socket client = this.m_SocketServer.Accept();
                    client.Send(Encoding.UTF8.GetBytes("220 QQ FTP reade.\r\n"));
                    byte[] buf = new byte[client.ReceiveBufferSize];

                    //client.Send(Encoding.UTF8.GetBytes("331 Guest login ok, send your complete e-mail address as password.\r\n"));

                    //Array.Clear(buf, 0, buf.Length);
                    //int recv_len = client.Receive(buf);
                    //string str = Encoding.UTF8.GetString(buf);
                    //client.Send(Encoding.UTF8.GetBytes("230 Guest login ok, access restrictions apply.\r\n"));

                    //Array.Clear(buf, 0, buf.Length);
                    //recv_len = client.Receive(buf);
                    //str = Encoding.UTF8.GetString(buf);
                    //client.Send(Encoding.UTF8.GetBytes("215 SYST command\r\n"));

                    //Array.Clear(buf, 0, buf.Length);
                    //recv_len = client.Receive(buf);
                    //str = Encoding.UTF8.GetString(buf);
                    //string[] files = Directory.GetFiles("C:\\");

                    while (true)
                    {
                        Array.Clear(buf, 0, buf.Length);
                        int recv_len = client.Receive(buf);
                        if (recv_len == 0)
                        {
                            break;
                        }
                        string str = Encoding.UTF8.GetString(buf,0 , recv_len);
                        List<CQFtpRequest> reqs = CQFtpRequest.Parse(str);
                        foreach (var req in reqs)
                        {
                            CQFtpResponse resp = this.Process(req);
                            if(this.m_Socket_PASV != null)
                            {
                                //int send_len = client.Send(Encoding.ASCII.GetBytes(string.Format("{0}\r\n", resp.ToString())));
                                int send_len = this.m_Socket_PASV.Send(Encoding.ASCII.GetBytes(string.Format("{0}\r\n", resp.ToString())));
                            }
                            else
                            {
                                int send_len = client.Send(Encoding.ASCII.GetBytes(string.Format("{0}\r\n", resp.ToString())));
                            }
                            
                            
                        }
                        if (reqs.Any(x=>x.Command.ToUpperInvariant() == "QUIT") == true)
                        {
                            client.Shutdown(SocketShutdown.Both);
                            client.Close();
                            break;
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }
            
            
            
            
        }

        Socket m_Socket_PASV;
        CQFtpResponse Process(CQFtpRequest req)
        {
            CQFtpResponse resp = new CQFtpResponse();
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.m_MainUI.HandSharks.Add(req.OriginCommand);
            }));
            
            switch(req.Command.ToUpperInvariant())
            {
                case "USER":
                    {
                        resp.Status = 331;
                        resp.Content = "Guest login ok, send your complete e-mail address as password";
                    }
                    break;
                case "PASS":
                    {
                        resp.Status = 230;
                        resp.Content = "Guest login ok, access restrictions apply.";
                    }
                    break;
                case "SYST":
                    {
                        resp.Status = 215;
                        resp.Content = "Windows.";
                    }
                    break;
                case "PWD":
                    {
                        resp.Status = 257;
                        resp.Content = "\"/\" is the current directory.";
                    }
                    break;
                case "TYPE":
                    {
                        resp.Status = 200;
                        resp.Content = "Type set to I.";
                    }
                    break;
                case "SIZE":
                    {
                        resp.Status = 550;
                        resp.Content = "File not found";
                    }
                    break;
                case "CWD":
                    {
                        resp.Status = 250;
                        resp.Content = "CWD command successful.";
                    }
                    break;
                case "PASV":
                    {
                        resp.Status = 227;
                        string ip = "127,0,0,1";
                        int port = 3335;
                        this.m_SocketData = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        this.m_SocketData.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
                        this.m_SocketData.Listen(10);
                        Task.Factory.StartNew(() =>
                        {
                            
                            this.m_Socket_PASV = this.m_SocketData.Accept();
                            byte[] buf = new byte[this.m_Socket_PASV.ReceiveBufferSize];
                            while (true)
                            {
                                int recv_len = this.m_Socket_PASV.Receive(buf);
                                if(recv_len > 0)
                                {

                                }
                            }
                        });
                        
                        string address = string.Format("{0},{1},{2}"
                            , ip
                            , port / 256
                            , port % 256);
                        resp.Content = string.Format("Entering Passive Mode ({0})"
                            , address);
                    }
                    break;
                case "LIST":
                case "LS":
                    {
                        resp.Status = 150;
                        StringBuilder strb = new StringBuilder();
                        strb.AppendLine("Opening ASCII mode data connection");
                        //strb.AppendLine("D:\\");
                        //strb.AppendLine("BB.exe");
                        //strb.AppendLine("CC.bmp");
                        //strb.AppendLine("150 Opening ASCII mode data connection");
                        strb.AppendLine("drwxr - x-- - 193 root     igs         3584 Oct 13 07:05..");
                        strb.AppendLine("drwxr - xr - x   2 234      igs          512 Oct 13 07:17 dialup");
                        strb.AppendLine("drwxr - xr - x   2 234      igs          512 Oct 13 07:17 email");
                        strb.AppendLine("drwxr - xr - x   4 234      igs          512 Oct 13 07:18 ftp");
                        strb.AppendLine("drwxr - xr - x   5 234      igs          512 Oct 13 07:19 netscape");
                        strb.AppendLine("drwxr - xr - x   2 234      igs          512 Oct 13 07:18 telnet");
                        strb.AppendLine("drwxr - xr - x   2 234      igs          512 Oct 13 07:18 www");
                        strb.AppendLine("drwxr - xr - x   4 234      igs          512 Oct 13 07:18 zip");
                        strb.Append("226 Transfer complete.");
                        resp.Content = strb.ToString();
                    }
                    break;
                case "QUIT":
                    {
                        resp.Status = 221;
                        resp.Content = "Goodbye";
                    }
                    break;
                case "OPTS":
                    {
                        resp.Status = 202;
                        resp.Content = "UTF8 mode is always enabled. No need to send this command.";
                    }
                    break;
                default:
                    {
                        System.Diagnostics.Trace.WriteLine("");
                    }
                    break;
            }

            this.Dispatcher.Invoke(new Action(() =>
            {
                this.m_MainUI.HandSharks.Add(resp.ToString());
            }));
            
            return resp;
        }
     
    }

    public class CQFtpRequest
    {
        static public List<CQFtpRequest> Parse(string src)
        {
            List<CQFtpRequest> reqs = new List<CQFtpRequest>();
            string[] s1 = src.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for(int i=0; i<s1.Length; i++)
            {
                string[] s2 = s1[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if(s2.Length == 2)
                {
                    CQFtpRequest req = new CQFtpRequest() { Command = s2[0], Content = s2[1], OriginCommand=s1[i] };
                    reqs.Add(req);
                }
                else
                {
                    CQFtpRequest req = new CQFtpRequest() { Command = src.Replace("\r\n", ""), Content = "", OriginCommand = src };
                    reqs.Add(req);
                }
                
            }
            return reqs;
        }

        public string Command { set; get; } = "";
        public string Content { set; get; }
        public string OriginCommand { set; get; }
    }

    public class CQFtpResponse
    {
        public int Status { set; get; }
        public string Content { set; get; }
        public override string ToString()
        {

            return string.Format("{0} {1}"
                , this.Status
                , this.Content);
        }
    }
}
