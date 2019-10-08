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

        void FtpClient()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            byte[] recv_buf = new byte[8192];
            int recv_len = 0;
            int send_len = 0;
            recv_len = socket.Receive(recv_buf);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));
            socket.Send(Encoding.UTF8.GetBytes("USER QQ\r\n"));
            recv_len = socket.Receive(recv_buf);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));

            socket.Send(Encoding.UTF8.GetBytes("PASS 123\r\n"));
            recv_len = socket.Receive(recv_buf);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));

            socket.Send(Encoding.UTF8.GetBytes("opts utf8 on\r\n"));
            recv_len = socket.Receive(recv_buf);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));

            socket.Send(Encoding.UTF8.GetBytes("PWD\r\n"));
            recv_len = socket.Receive(recv_buf);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));

            socket.Send(Encoding.UTF8.GetBytes("CWD /\r\n"));
            recv_len = socket.Receive(recv_buf);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));

            socket.Send(Encoding.UTF8.GetBytes("TYPE A\r\n"));
            recv_len = socket.Receive(recv_buf);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));

            socket.Send(Encoding.UTF8.GetBytes("PASV\r\n"));
            recv_len = socket.Receive(recv_buf);
            string pasv_str = Encoding.UTF8.GetString(recv_buf, 0, recv_len);
            int index = pasv_str.IndexOf("(");
            int index1 = pasv_str.IndexOf(")", index);
            pasv_str = pasv_str.Substring(index+1, index1 - index-1);
            string[] s1 = pasv_str.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            string pasv_ip = string.Format("{0}.{1}.{2}.{3}"
                , s1[0]
                , s1[2]
                , s1[3]
                , s1[3]);
            int pasv_port = int.Parse(s1[4]) * 256 + int.Parse(s1[5]);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));
            Socket socket_pasv = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket_pasv.Connect(new IPEndPoint(IPAddress.Parse(pasv_ip), pasv_port));

            socket.Send(Encoding.UTF8.GetBytes("LIST -l\r\n"));
            recv_len = socket.Receive(recv_buf);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));
            recv_len = socket_pasv.Receive(recv_buf);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));
            recv_len = socket.Receive(recv_buf);
            System.Diagnostics.Trace.WriteLine(Encoding.UTF8.GetString(recv_buf, 0, recv_len));
        }

        bool SendCommand(Socket client, CQFtpResponse resp)
        {
            bool result = true;
            int send_len = client.Send(Encoding.UTF8.GetBytes(string.Format("{0}\r\n", resp.ToString())));
            return result;
        }


        bool SendContent(Socket client, string data)
        {
            bool result = true;
            int send_len = client.Send(Encoding.UTF8.GetBytes(string.Format("{0}\r\n", data)));
            return result;
        }

        Socket m_SocketServer;
        Socket m_SocketData;
        CQMainUI m_MainUI;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if(this.m_MainUI == null)
            {

                //this.FtpClient();


                this.DataContext = this.m_MainUI = new CQMainUI();

                Task.Factory.StartNew(() =>
                {
                    this.m_SocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    this.m_SocketServer.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3333));
                    this.m_SocketServer.Listen(10);
                    while(true)
                    {
                        Socket client = this.m_SocketServer.Accept();

                        client.Send(Encoding.UTF8.GetBytes("220 QQ FTP reade.\r\n"));
                        byte[] buf = new byte[client.ReceiveBufferSize];

                        while (true)
                        {
                            Array.Clear(buf, 0, buf.Length);
                            int recv_len = client.Receive(buf);
                            if (recv_len == 0)
                            {
                                break;
                            }
                            string str = Encoding.UTF8.GetString(buf, 0, recv_len);
                            List<CQFtpRequest> reqs = CQFtpRequest.Parse(str);
                            foreach (var req in reqs)
                            {
                                List<CQFtpResponse> resps = this.Process(req);
                                for (int i = 0; i < resps.Count; i++)
                                {
                                    if (resps[i].Status > 0)
                                    {
                                        int send_len = client.Send(Encoding.UTF8.GetBytes(string.Format("{0}\r\n", resps[i].ToString())));
                                    }
                                    else
                                    {
                                        if(this.m_Socket_PASV != null)
                                        {
                                            if(string.IsNullOrEmpty(resps[i].Content) == false)
                                            {
                                                int send_len = this.m_Socket_PASV.Send(Encoding.UTF8.GetBytes(string.Format("{0}\r\n", resps[i].ToString())));
                                            }
                                            if (resps[i].Download != null)
                                            {
                                                byte[] send_buf = new byte[client.SendBufferSize];
                                                while(true)
                                                {
                                                    int read_len = resps[i].Download.Read(send_buf, 0, send_buf.Length);
                                                    if(read_len > 0)
                                                    {
                                                        this.m_Socket_PASV.Send(send_buf, read_len, SocketFlags.None);
                                                    }
                                                    if(read_len != send_buf.Length)
                                                    {
                                                        break;
                                                    }
                                                }
                                            }
                                            
                                            this.m_Socket_PASV.Close();
                                            this.m_Socket_PASV = null;
                                        }
                                        
                                    }
                                }
                            }
                            if (reqs.Any(x => x.Command.ToUpperInvariant() == "QUIT") == true)
                            {
                                client.Shutdown(SocketShutdown.Both);
                                client.Close();
                                break;
                            }
                        }
                    }
                    
                }, TaskCreationOptions.LongRunning);
            }
        }

        Socket m_Socket_PASV;
        string m_Root = "D:";
        string m_CurrentPath = "/";
        List<CQFtpResponse> Process(CQFtpRequest req)
        {
            List<CQFtpResponse> resps = new List<CQFtpResponse>();
            CQFtpResponse resp = new CQFtpResponse();
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.m_MainUI.HandSharks.Add(req.OriginCommand);
                System.Diagnostics.Trace.WriteLine(req.OriginCommand);
            }));
            
            switch(req.Command.ToUpperInvariant())
            {
                case "USER":
                    {
                        resp.Status = 331;
                        resp.Content = "Guest login ok, send your complete e-mail address as password";
                        resps.Add(resp);
                    }
                    break;
                case "PASS":
                    {
                        resp.Status = 230;
                        resp.Content = "Guest login ok, access restrictions apply.";
                        resps.Add(resp);
                    }
                    break;
                case "SYST":
                    {
                        resp.Status = 215;
                        resp.Content = "Windows.";
                        resps.Add(resp);
                    }
                    break;
                case "PWD":
                    {
                        resp.Status = 257;
                        resp.Content = string.Format("\"{0}\" is the current directory."
                            , this.m_CurrentPath);
                        resps.Add(resp);
                    }
                    break;
                case "TYPE":
                    {
                        resp.Status = 200;
                        resp.Content = "Type set.";
                        resps.Add(resp);
                    }
                    break;
                case "SIZE":
                    {
                        string path = string.Format("{0}{1}"
                            , this.m_Root
                            , req.Content);
                        if(File.Exists(path) == true)
                        {
                            resp.Status = 213;
                            resp.Content = new FileInfo(path).Length.ToString();
                            resps.Add(resp);
                        }
                        else
                        {
                            resp.Status = 550;
                            resp.Content = "File not found";
                            resps.Add(resp);
                        }
                    }
                    break;
                case "CWD":
                    {
                        string path = string.Format("{0}{1}"
                            , this.m_Root
                            , req.Content);
                        if (Directory.Exists(path) == false)
                        {
                            //550 CWD failed. "/WMICodeCreator.cs": directory not found.
                            resp.Status = 550;
                            resp.Content = string.Format("CWD failed \"{0}\":  directory not found."
                                , req.Content);
                            resps.Add(resp);
                        }
                        else
                        {
                            this.m_CurrentPath = req.Content;
                            resp.Status = 250;
                            resp.Content = "CWD command successful.";
                            resps.Add(resp);
                        }
                        
                    }
                    break;
                case "PASV":
                    {
                        resp.Status = 227;
                        string ip = "127,0,0,1";
                        int port = 3335+DateTime.Now.Millisecond/10;
                        if(this.m_SocketData != null)
                        {
                            this.m_SocketData.Close();
                            this.m_SocketData = null;
                        }
                        this.m_SocketData = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        this.m_SocketData.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        this.m_SocketData.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
                        this.m_SocketData.Listen(10);

                        Task.Factory.StartNew(() =>
                        {
                            this.m_Socket_PASV = this.m_SocketData.Accept();
                        });
                        
                        string address = string.Format("{0},{1},{2}"
                            , ip
                            , port / 256
                            , port % 256);
                        resp.Content = string.Format("Entering Passive Mode ({0})"
                            , address);
                        resps.Add(resp);
                    }
                    break;
                case "LIST":
                case "LS":
                    {
                        string path = string.Format("{0}{1}"
                            , this.m_Root
                            , this.m_CurrentPath);
                        resp.Status = 150;
                        resp.Content = "Opening data channel for directory listing of \"/\"";
                        resps.Add(resp);

                        resp = new CQFtpResponse();
                        StringBuilder strb = new StringBuilder();

                        DirectoryInfo ddr = new DirectoryInfo(path);
                        DirectoryInfo[] dirs = ddr.GetDirectories();
                        foreach (var oo in dirs)
                        {
                            strb.Append(string.Format("drwxr-xr-x 1 ftp ftp              0 {0} {1} {2}:{3} {4}\r\n"
                                , System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(oo.CreationTimeUtc.Month)
                                , oo.CreationTimeUtc.Day
                                , oo.CreationTimeUtc.Hour
                                , oo.CreationTimeUtc.Minute
                                , oo.Name));
                        }
                        FileInfo[] files = ddr.GetFiles();
                        foreach (var oo in files)
                        {
                            strb.Append(string.Format("-r--r--r-- 1 ftp ftp              {0} {1} {2} {3}:{4} {5}\r\n"
                                , oo.Length
                                , System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(oo.CreationTimeUtc.Month)
                                , oo.CreationTimeUtc.Day
                                , oo.CreationTimeUtc.Hour
                                , oo.CreationTimeUtc.Minute
                                , oo.Name));
                        }
                        resp.Content = strb.ToString();
                        resps.Add(resp);

                        resp = new CQFtpResponse();
                        resp.Status = 226;
                        resp.Content = "Successfully transferred \"/\"";
                        resps.Add(resp);
                    }
                    break;
                case "QUIT":
                    {
                        resp.Status = 221;
                        resp.Content = "Goodbye";
                        resps.Add(resp);
                    }
                    break;
                case "OPTS":
                    {
                        resp.Status = 202;
                        resp.Content = "UTF8 mode is always enabled. No need to send this command.";
                        resps.Add(resp);
                    }
                    break;
                case "RETR":
                    {
                        string path = string.Format("{0}{1}"
                            , this.m_Root
                            , req.Content);
                        resp.Status = 150;
                        resp.Content = "Opening data channel for directory listing of \"/\"";
                        resps.Add(resp);

                        resp = new CQFtpResponse();
                        resp.Download = new FileStream(path, FileMode.Open);
                        resps.Add(resp);

                        resp = new CQFtpResponse();
                        resp.Status = 226;
                        resp.Content = "Successfully transferred \"/\"";
                        resps.Add(resp);
                    }
                    break;
                default:
                    {
                        resp.Status = 502;
                        resp.Content = "No support.";
                        resps.Add(resp);
                    }
                    break;
            }

            this.Dispatcher.Invoke(new Action(() =>
            {
                foreach (var oo in resps)
                {
                    this.m_MainUI.HandSharks.Add(oo.ToString());
                    System.Diagnostics.Trace.WriteLine(oo.ToString());
                }
                
            }));
            
            return resps;
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
                int index = s1[i].IndexOf(" ");
                if(index > 0)
                {

                    CQFtpRequest req = new CQFtpRequest() { OriginCommand = s1[i] };
                    req.Command = s1[i].Substring(0, index);
                    req.Content = s1[i].Remove(0, index);
                    reqs.Add(req);
                }
                else
                {
                    CQFtpRequest req = new CQFtpRequest() { Command = src.Replace("\r\n", ""), Content = "", OriginCommand = s1[i] };
                    reqs.Add(req);
                }
                //string[] s2 = s1[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                //if(s2.Length == 2)
                //{
                //    CQFtpRequest req = new CQFtpRequest() { Command = s2[0], Content = s2[1], OriginCommand=s1[i] };
                //    reqs.Add(req);
                //}
                //else
                //{
                //    CQFtpRequest req = new CQFtpRequest() { Command = src.Replace("\r\n", ""), Content = "", OriginCommand = src };
                //    reqs.Add(req);
                //}
                
            }
            return reqs;
        }

        public string Command { set; get; } = "";
        public string Content { set; get; }
        public string OriginCommand { set; get; }
    }

    public class CQFtpResponse
    {
        public Stream Download { set; get; }
        public int Status { set; get; }
        public string Content { set; get; }
        public override string ToString()
        {
            if(this.Status == 0)
            {
                return this.Content;
            }
            return string.Format("{0} {1}"
                , this.Status
                , this.Content);
        }
    }
}
