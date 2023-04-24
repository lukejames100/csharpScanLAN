using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;

namespace csharpScanLAN
{
    public partial class Form1 : Form
    {
        [DllImport("Iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(Int32 dest, Int32 host, ref Int64 mac, ref Int32 length);
        //private static extern int SendARP(int destIp, int srcIp, byte[] pMacAddr, ref uint phyAddrLen);
        [DllImport("ws2_32.dll")]
        private static extern Int32 inet_addr(string ip);

        delegate void WT(int n);
        Thread t;

        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        string selectip;//选中的ip地址
        string ipseg = "";
        public Form1()
        {
            InitializeComponent();
            myinit();
            myinit2();
            //初始化后台工作线程
            mybackinit();
        }

        private void myinit()
        {
            listView1.Items.Clear();
            listView1.Columns.Add("IP地址", listView1.Width * 20 / 100, HorizontalAlignment.Center);
            listView1.Columns.Add("MAC地址", listView1.Width * 40 / 100, HorizontalAlignment.Center);
            listView1.Columns.Add("子网掩码", listView1.Width * 20 / 100, HorizontalAlignment.Center);
            listView1.Columns.Add("网关", listView1.Width * 20/100, HorizontalAlignment.Center);
            listView1.View = System.Windows.Forms.View.Details;

           
        }

        private void myinit2()
        {
            listView2.Items.Clear();
            listView2.Columns.Add("IP地址", listView2.Width * 20 / 100, HorizontalAlignment.Center);
            listView2.Columns.Add("MAC地址", listView2.Width * 30 / 100, HorizontalAlignment.Center);
            listView2.Columns.Add("主机名", listView2.Width * 50 / 100, HorizontalAlignment.Center);
            listView2.View = System.Windows.Forms.View.Details;
        }

        private void mybackinit()
        {
            backgroundWorker.WorkerReportsProgress = true; ;
            backgroundWorker.DoWork+=backgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            //获取本机信息，ip mac mask
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection nics = mc.GetInstances();
            foreach (ManagementObject nic in nics)
            {
                if (Convert.ToBoolean(nic["ipEnabled"]) == true)
                {
                    ListViewItem it = new ListViewItem();
                    string ip = (nic["IPAddress"] as String[])[0];
                    it.SubItems[0].Text = ip;
                    //string ip = (nic["IPAddress"] as String[])[0];
                    string mac=nic["MacAddress"].ToString();
                    it.SubItems.Add(mac);
                    string ipsubnet = (nic["IPSubnet"] as String[])[0];
                    it.SubItems.Add(ipsubnet);
                    string ipgateway = null;
                    if (nic["DefaultIPGateway"] == null)
                        ipgateway = "null";
                    else
                        ipgateway = (nic["DefaultIPGateway"] as String[])[0];
                    it.SubItems.Add(ipgateway);
                    //Console.Write(ip + mac + ipsubnet);
                    listView1.Items.Add(it);
                }

                button1.Enabled = true;
            }
        }
        private string GetMacAddress(string hostip)
        {
            string Mac = "";
            try
            {
                Int32 ldest = inet_addr(hostip);
                Int64 macinfo = new Int64();
                Int32 len = 6;
                SendARP(ldest, 0, ref macinfo, ref len);
                string tmpmac = Convert.ToString(macinfo, 16).PadLeft(12, '0');
                Mac = tmpmac.Substring(0, 2).ToUpper();
                for (int i = 2; i < tmpmac.Length; i = i + 2)
                {
                    Mac = tmpmac.Substring(i, 2).ToUpper() + "-" + Mac;
                }
            }
            catch (Exception ee)
            {
                Mac = "获取mac失败：" + ee.Message;
            }
            return Mac;
        }

        private void EnumComputers(int n)
        {
            if (this.listView2.InvokeRequired)
            {
                WT d = new WT(EnumComputers);
                this.Invoke(d, new object[] { n });
            }
            else
            {
                try
                {
                    for (int i = 0; i <= 255; i++)
                    {
                        Ping myping;
                        myping = new Ping();
                        myping.PingCompleted += new PingCompletedEventHandler(myping_pingcompleted);
                        string pingip = ipseg+"." + n.ToString() + "." + i.ToString();
                        myping.SendAsync(pingip, 1000, null);
                    }
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                }
            }
        }

        static string GetNameFromIP(string ipa)
        {
            string machingname = "null";
            try
            {
                IPHostEntry hostentry = Dns.GetHostEntry(ipa);
                machingname = hostentry.HostName;
            }
            catch (Exception ee)
            {
                System.Console.WriteLine(ee.Message);
            }
            return machingname;
        }
        private void myping_pingcompleted(object sender, PingCompletedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            if (e.Reply.Status == IPStatus.Success)
            {
                sb.Append("ip:" + e.Reply.Address.ToString() + "\r\n");
                string otherip = e.Reply.Address.ToString();
                string mac = GetMacAddress(e.Reply.Address.ToString());
                string othermac = mac;
                string machinename = GetNameFromIP(otherip);
                ListViewItem it = new ListViewItem();
                it.SubItems[0].Text = otherip;
                it.SubItems.Add(othermac);
                it.SubItems.Add(machinename);
                listView2.Items.Add(it);

                sb.Append("mac:" + mac + "\r\n\r\n");
                //NumericUp
            }
            //界面显示操作，在这里添加一个ip找对应的电脑名称
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //获取选择的ip列，获取对应的ip地址
            selectip = listView1.FocusedItem.SubItems[0].Text;
            //读出ip第三段
            string ipstr = selectip;
            //找第二个点位置
            int pos1 = ipstr.IndexOf('.');
            int pos2 = ipstr.IndexOf('.', pos1 + 1);
            int pos3 = ipstr.IndexOf('.', pos2 + 1);
            string secstr = ipstr.Substring(pos2 + 1, pos3 - pos2 - 1);
            ipseg = ipstr.Substring(0, pos2);
            int n = int.Parse(secstr);

            backgroundWorker.RunWorkerAsync();
            button2.Enabled = false;
        }

        private string GetSubnetMask(string ipa)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation addr in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.ToString() == ipa)
                        return addr.IPv4Mask.ToString();
                }
            }
            return "null";

            IPAddress ip = IPAddress.Parse(ipa);
            byte[] maskbytes = ip.GetAddressBytes();
            string maskbinary = string.Join("", maskbytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            int prefixLength = maskbinary.IndexOf('0');
            if (prefixLength == -1)
            {
                prefixLength = 32;
            }
            string mask = string.Join(".", Enumerable.Range(0, 4).Select(i => (prefixLength >= (i + 1) * 8) ? "255" : ((prefixLength > i * 8) ? ((int)Math.Pow(2, prefixLength % 8) - 1).ToString() : "0")));
            return mask;
        }

        private string GetNetworkSegment(string ipaddres, string subnetmask)
        {
            IPAddress ip = IPAddress.Parse(ipaddres);
            IPAddress mask = IPAddress.Parse(subnetmask);
            byte[] ipbytes = ip.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();

            byte[] networkBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                networkBytes[i] = (byte)(ipbytes[i] & maskBytes[i]);

            }
            string networkaddress = string.Join(".", networkBytes);
            return networkaddress;
        }
        private string GetMac(string ipAddress)
        {
            return null;
        }
        
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //获取本机子网掩码
            string subnetmask=GetSubnetMask(selectip);
            int numm = 0;
            int attempts = 3;
            //获取本机网段
            string networkSegment = GetNetworkSegment(selectip, subnetmask);
            //创建一个列表，用于保存网段上所有的ip地址
            List<string> ipList = new List<string>();
            //创建一个3个参数的数组，ip，mac，主机名
            List<string[]> allList = new List<string[]>();
            for (int i = 1; i <= 255; i++)
            {
                //去掉子网掩码最后一个0
                string cutstr = networkSegment.Substring(0, networkSegment.Length - 1);
                string ipAddress = cutstr + i.ToString();
                Ping ping = new Ping();
                PingOptions options = new PingOptions();
                options.DontFragment = true;
                string data = "aaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 4000;
                for (int j = 0; j < attempts; j++)//失败就尝试再ping，延时1秒，然后继续，总共3次
                {
                    try
                    {
                        PingReply reply = ping.Send(ipAddress, timeout, buffer, options);
                        if (reply.Status == IPStatus.Success)
                        {
                            //获取mac地址
                            //GetMacByIP(ipAddress);
                            string mc = null;
                            string hostname = "null";
                            mc = GetMacAddress(ipAddress);

                            IPHostEntry he = Dns.GetHostEntry(ipAddress);
                            if (he.HostName != null)
                                hostname = he.HostName;

                            allList.Add(new string[] { ipAddress, mc, hostname });
                            ipList.Add(ipAddress);
                            break;
                        }
                        numm++;
                    }
                    catch (Exception ee)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                //在backgroundworker中报告进度
                int progress = (int)(((float)i / 255) * 100);
                backgroundWorker.ReportProgress(progress);
            }
            //将结果返回到RunrorkerCompleted事件中处理
            e.Result = allList;// ipList;
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //List<string> ipList = (List<string>)e.Result;
            List<string[]>iplist=(List<string[]>)e.Result;
            foreach (string[] ipmacname in iplist)
            {
                ListViewItem it=new ListViewItem();
                it.SubItems[0].Text = ipmacname[0];
               // listView2.Items[0].Text = ipmacname[0];
                it.SubItems.Add(ipmacname[1]);
                it.SubItems.Add(ipmacname[2]);
                //listView2.Items.Add(ipmacname[1]);
                //listView2.Items.Add(ipmacname[2]);
                listView2.Items.Add(it);
            }
            progressBar1.Value = 0;
        }
    }
}
