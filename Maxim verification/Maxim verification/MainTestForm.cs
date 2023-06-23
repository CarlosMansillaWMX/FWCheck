using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Runtime.InteropServices;
using Traceability;
using System.Runtime.Remoting.Contexts;
using Aspose.Cells.Revisions;
using PackToOrderStrap.Properties;

namespace Maxim_verification
{
    public partial class MainTestForm : Form
    {
        string diadelasemana = "";
        string dianum = "";
        string mes = "";
        string hora = "";
        string minutos = "";
        Form f2;
        Aspose.Cells.Workbook wb1 = new Aspose.Cells.Workbook();
        SFC_System wmx_ts = new SFC_System();
        public static string ws = "";
        public static string ws1 = "";
        public string fwverRevL = "";
        public string fwverRevG = "";
        public string fwRuggles = "";
        public string fwMaxim = "";
        public string fwNordic = "";
        public string path = "script_start.txt";
        public string path1 = "script_end.txt";
        public string smLpath = "";
        public string smGpath = "";
        public string Maxpath =  "";
        public string Rugpath = "";
        public string Norpath = "";
        public string portnrf = "";
        public string file = @"c:\Report_Recharge\";
        
        public string file2 = @"c:\HostReport_Recharge\";
        
        public string csvt = "";
        public string csv = "";
        public MainTestForm(LoginForm f2)
        {

            InitializeComponent();
            tbuser.Text = LoginForm.user;
            this.f2 = f2;
            timer1.Enabled = true;
            ws = (Settings.Default.testStationNumber + 1).ToString();
            ws1 = Settings.Default.ScriptEnable.ToString();
            serialPort1.PortName = Settings.Default.Portname;
            tbCategory.Text = LoginForm.Category;
            fwverRevL = Settings.Default.SMFWverRevL;
            fwverRevG = Settings.Default.SMFWverRevG;
            fwMaxim = Settings.Default.MaximFWver;
            fwNordic = Settings.Default.NordicFWVer;
            portnrf = Settings.Default.PortNRF2;
            if(fwNordic.Contains("17.2.1.0"))
            {
                Norpath= ".\\510-000013_Nordic\\boylston-17_2_1_0_bootloader_app_softdevice-dfu.zip";
            }
            if (fwNordic.Contains("17.2.2.0"))
            {
                Norpath = ".\\510-000013_Nordic\\boylston-17_2_2_0_bootloader_app_softdevice-dfu.zip";
            }
            if (fwMaxim.Contains("41.11.4.0"))
            {
                Maxpath = ".\\510-000009_Maxim\\510-000009-41.11.4.0.bin";
            }
            else if(fwMaxim.Contains("42.11.7.0"))
            {
                Maxpath = ".\\510-000009_Maxim\\510-000009-42.11.7.0.bin";
            }
            if (fwverRevL.Contains("3.9.0.0"))
            {
                smLpath= ".\\510-000011_Ship_Moder\\510-000011-3.9.0.0.bin";
            }
            if (fwverRevG.Contains("3.8.0.0"))
            {
                smGpath = ".\\510-000011_Ship_Moder\\harvard_ship-3.8.0.0-87917cd4fd8109cd7d1359b7766e74cc.bin";
            }
            if(fwRuggles.Contains("172.11.2.1"))
            {
                Rugpath = ".\\510-000015_Ruggles\\510-000015-172.11.2.1.bin";
            }
            
            if (LoginForm.offline.Contains("offline"))
            {
                tbCategory.Text = tbCategory.Text + " offline";
                tbCategory.BackColor = Color.Red;
            }
            else
            {
                tbCategory.Text = tbCategory.Text + " Online Station " + ws;
                tbCategory.BackColor = Color.Green;
            }
        }
        public void CreateRow(string testName, string testOutput, string lowerLimit, string upperLimit, string result, string status)
        {
            int n = dataGridView1.Rows.Add();
            //colocacion de informacion 
            dataGridView1.Rows[n].Cells[0].Value = testName;
            dataGridView1.Rows[n].Cells[1].Value = testOutput;
            dataGridView1.Rows[n].Cells[2].Value = lowerLimit;
            dataGridView1.Rows[n].Cells[3].Value = upperLimit;
            dataGridView1.Rows[n].Cells[4].Value = result;
            if (status.Contains("OK"))
            {
                dataGridView1.Rows[n].DefaultCellStyle.BackColor = Color.Green;
            }
            else
            {
                dataGridView1.Rows[n].DefaultCellStyle.BackColor = Color.Red;
            }
        }
        public int CheckNFCCon()
        {
            int conect = 0;
            int i = 0;
            string VV1 = "";
            while (true)
            {

                serialPort1.DiscardOutBuffer();
                serialPort1.DiscardInBuffer();
                serialPort1.WriteLine("X05,01,24,4,68A3");
                try
                {
                    VV1 = "";
                    VV1 = serialPort1.ReadLine();
                }
                catch (Exception)
                {
                    conect = conect + 1;
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "no response";
                }
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + VV1;
                serialPort1.DiscardOutBuffer();
                serialPort1.DiscardInBuffer();
                serialPort1.WriteLine("W01,sync");
                try
                {
                    VV1 = "";
                    VV1 = serialPort1.ReadLine();
                }
                catch (Exception)
                {
                    conect = conect + 1;
                }
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + VV1;
                if (VV1 == "W02,ack")
                {
                    i = 7;
                    conect = 0;
                    break;

                }
                else
                {
                    Thread.Sleep(1000);
                    i = i + 1;
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "\r" + i.ToString();
                }
                if (i == 10)
                {
                    conect = 1;
                    break;
                }
            }
            return conect;
        }
        public void Disablequietmode()
        {
            string loco = "";
            int i = 0;
            string VV1 = "";
            string[] subs = new string[6] { "", "", "", "", "", "" };
            while (true)
            {
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "m085 2";
                if (VV1 != "")
                {

                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + VV1;
                    serialPort1.DiscardOutBuffer();
                    serialPort1.DiscardInBuffer();
                    serialPort1.WriteLine("m085 2");
                    try
                    {
                        VV1 = serialPort1.ReadLine();
                    }
                    catch (Exception) { }
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + VV1;
                }
                if (VV1.Length > 1)
                    subs = VV1.Split(',');
                if (subs.Length > 2)
                {
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[2];
                    if (subs[2].Length >= 12)
                    {
                        tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[2].Substring(0, 12);
                        loco = subs[2].Substring(0, 12);
                        tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + loco;
                    }
                }
                if (loco == ":m085 002 OK")
                {
                    i = 7;
                    break;
                }
                else
                {
                    i++;
                    Thread.Sleep(1000);
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "\r" + i.ToString();
                    VV1 = "Attemp" + i.ToString();
                }
                if (i == 15)
                {
                    break;
                }
            }
        }
        public void disableshipmode()
        {
            int i = 0;
            string VV1 = "";
            
            tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Disable Shipmode";
            while (true)
            {
                VV1 = "";
                serialPort1.DiscardOutBuffer();
                serialPort1.DiscardInBuffer();
                serialPort1.WriteLine("bp");

                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "bp";
                int j = 0;
                while (true)
                {
                    try
                    {
                        VV1 = serialPort1.ReadLine();
                    }
                    catch (TimeoutException) { }
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" +VV1;
                    if (VV1.Contains("Sent BLE_PAIRING_SUCCESS_SIG from menu"))
                        break;

                    j++;
                    if (j == 6)
                        break;
                }

                if (VV1.Contains("Sent BLE_PAIRING_SUCCESS_SIG from menu"))
                {
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "bp disabled";


                    tbpass.BackColor = Color.Green;
                    break;
                }
                i++;
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Attemp" + i.ToString();
                if (i == 6)
                {
                    break;
                }



            }
        }
        public string GetSN(string SN)
        {
            string VV1 = "";
            string[] subs = new string[6] { "", "", "", "", "", "" };
            string[] subs2 = new string[6] { "", "", "", "", "", "" };
            int i = 0;
            while (true)
            {
                subs = new string[6] { "", "", "", "", "", "" };
                subs2 = new string[6] { "", "", "", "", "", "" };

                try
                {
                    serialPort1.DiscardOutBuffer();
                    serialPort1.DiscardInBuffer();
                    serialPort1.WriteLine("m017");

                }
                catch (TimeoutException) { }
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "m017";
                int j = 0;
                while (true)
                {
                    try
                    {
                        subs[0] = serialPort1.ReadLine();
                        tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0];
                        if (subs[0].Length > 0)
                            subs2 = subs[0].Split(' ');
                        if (subs2.Length > 2)
                        {
                            if (subs2[2].Length >= 9)
                                VV1 = subs2[2].Substring(0, 9);
                        }
                        tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + VV1;
                    }
                    catch (TimeoutException) { }
                    if (VV1.Contains(SN))
                    {
                        break;
                    }
                    else if (subs[0].Contains(SN))
                    {
                        VV1 = SN;
                        break;
                    }
                    if (j == 6)
                        break;
                    j++;
                }

                if (VV1.Contains(SN))
                {
                    i = 7;
                    break;
                }
                i++;
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Attemp" + i.ToString();
                if (i == 6)
                {
                    break;
                }
            }
            return VV1;
        }
        public int GETFG()
        {
            int i = 0;
            string VV1 = "";
            string[] subs = new string[6] { "", "", "", "", "", "" };
            string[] subs2 = new string[6] { "", "", "", "", "", "" };
            int FG = 0;
            while (true)
            {
                VV1 = "";
                subs = new string[6] { "", "", "", "", "", "" };
                subs2 = new string[6] { "", "", "", "", "", "" };
                string[] subs3 = new string[6] { "", "", "", "", "", "" };
                serialPort1.DiscardOutBuffer();
                serialPort1.DiscardInBuffer();
                serialPort1.WriteLine("m007");
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "m007";
                int j = 0;
                while (j < 5)
                {

                    try
                    {

                        subs[0] = serialPort1.ReadLine();

                    }
                    catch (TimeoutException) { }

                    FG = 0;
                    if (subs[0].Length > 0)
                        subs2 = subs[0].Split(' ');
                    if (subs2.Length < 5 && subs2.Length > 3)
                    {
                        if (subs2[3].Length >= 5)
                        {
                            subs3 = subs2[3].Split('.');
                            VV1 = subs3[0];
                            FG = int.Parse(VV1);
                        }
                    }
                    try
                    {
                        tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0] + $"{Environment.NewLine}" + "try1" + j.ToString() + $"{Environment.NewLine}" + VV1 + "vv1";
                    }
                    catch (Exception) { }
                    if (FG > 0 && FG <= 100)
                    {
                        i = 7;
                        break;
                    }
                    j = j + 1;
                    //tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0] + $"{Environment.NewLine}" + "try1" + j.ToString() + $"{Environment.NewLine}" + VV1 + "vv1";


                }
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "try2" + i.ToString();
                if (i == 4)
                    break;
                if (i == 7)
                    break;
                i = i + 1;
            }
            return FG;
        }
        public string getHID()
        {
            int i = 0;
            string VV1 = "";
            string revision = "";
            string[] subs = new string[6] { "", "", "", "", "", "" };
            string[] subs2 = new string[6] { "", "", "", "", "", "" };
            // loop to check Sensor REV ID
            while (true)
            {
                subs = new string[6] { "", "", "", "", "", "" };
                subs2 = new string[6] { "", "", "", "", "", "" };
                serialPort1.DiscardOutBuffer();
                serialPort1.DiscardInBuffer();
                serialPort1.WriteLine("m084");
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "m084";
                int j = 0;
                while (true)
                {
                    try
                    {
                        subs[0] = serialPort1.ReadLine();
                    }
                    catch (TimeoutException) { }
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0];
                    if (subs[0].Contains("Rev G"))
                        break;
                    if (subs[0].Contains("Rev L"))
                        break;

                    j++;
                    if (j == 6)
                        break;
                }
                // TODO only handle cases Rev G and Rev L
                if (subs[0].Contains("Rev L"))
                {
                    revision = "Rev L";
                    break;
                }
                else if (subs[0].Contains("Rev G"))
                {
                    revision = "Rev G";
                    break;
                }
                i++;
                Thread.Sleep(500);
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Attemp" + i.ToString();
                if (i == 6)
                {
                    revision = "No Response";
                    break;
                }
            }
            return revision;
        }
        public string getsmver(string version)
        {
            int i = 0;
            string[] subs = new string[6] { "", "", "", "", "", "" };
            string[] subs2 = new string[6] { "", "", "", "", "", "" };
            string fw = "";


            while (true)
            {
                subs = new string[6] { "", "", "", "", "", "" };
                subs2 = new string[6] { "", "", "", "", "", "" };
                serialPort1.DiscardOutBuffer();
                serialPort1.DiscardInBuffer();
                serialPort1.WriteLine("m058");
                int j = 0;
                int ka = 0;
                int ga = 0;
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "m058";
                while (true)
                {
                    try
                    {
                        subs[0] = serialPort1.ReadLine();
                    }
                    catch (TimeoutException) { }
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0];                    
                    if (subs[0].Contains(version))
                    {
                    break;
                    }
                    if (subs[0].Contains("3."))
                    {
                        break;
                    }
                    j++;
                    if (j == 6)
                        break;
                }
                if (subs[0].Contains(version))
                    break;
                if (subs[0].Contains("3."))
                    break;
                if (i == 6)
                {
                    break;
                }
                i++;
            }
            return subs[0];
        }
        public string getnordicversion(string version)
        {
            int i = 0;
            string[] subs = new string[6] { "", "", "", "", "", "" };
            string[] subs2 = new string[6] { "", "", "", "", "", "" };
            string fw = "";


            while (true)
            {
                subs = new string[6] { "", "", "", "", "", "" };
                subs2 = new string[6] { "", "", "", "", "", "" };
                serialPort1.DiscardOutBuffer();
                serialPort1.DiscardInBuffer();
                serialPort1.WriteLine("m015");
                int j = 0;
                int ka = 0;
                int ga = 0;
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "m015";
                while (true)
                {
                    try
                    {
                        subs[0] = serialPort1.ReadLine();
                    }
                    catch (TimeoutException) { }
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0];
                    if (subs[0].Contains(version))
                    {
                        break;
                    }
                    if (subs[0].Contains("17."))
                    {
                        break;
                    }
                    j++;
                    if (j == 6)
                        break;
                }
                if (subs[0].Contains(version))
                    break;
                if (subs[0].Contains("17."))
                    break;
                if (i == 6)
                {
                    break;
                }
                i++;
            }
            return subs[0];
        }
        public string getmaximver(string version)
        {
            {
                int i = 0;
                string[] subs = new string[6] { "", "", "", "", "", "" };
                string[] subs2 = new string[6] { "", "", "", "", "", "" };
                string fw = "";


                while (true)
                {
                    subs = new string[6] { "", "", "", "", "", "" };
                    subs2 = new string[6] { "", "", "", "", "", "" };
                    serialPort1.DiscardOutBuffer();
                    serialPort1.DiscardInBuffer();
                    serialPort1.WriteLine("m001");
                    int j = 0;
                    int ka = 0;
                    int ga = 0;
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "m001";
                    while (true)
                    {
                        try
                        {
                            subs[0] = serialPort1.ReadLine();
                        }
                        catch (TimeoutException) { }
                        tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0];
                        if (subs[0].Contains(version))
                        {
                            break;
                        }
                        if (subs[0].Contains("41.") || subs[0].Contains("42."))
                        {
                            break;
                        }
                        j++;
                        if (j == 6)
                            break;
                    }
                    if (subs[0].Contains(version))
                        break;
                    if (subs[0].Contains("41.")|| subs[0].Contains("42."))
                        break;
                    if (i == 6)
                    {
                        break;
                    }
                    i++;
                }
                return subs[0];
            }
        }
        public void update(string Chip, string fwfile)
        {
                string chippath = "";
                if(Chip.Contains("Maxim"))
                {
                    chippath = ".\\510-000009_Maxim\\serial_upgrade.py";
                }
                else if(Chip.Contains("Shipmode"))
                {
                    chippath = ".\\510-000011_Ship_Moder\\serial_upgrade.py";
                }
                else if(Chip.Contains("Ruggles"))
                {
                    chippath = ".\\510-000015_Ruggles\\serial_upgrade.py";
                }
                int ka = 0;
                int ga = 0;
                
                Thread.Sleep(1000);
                Process process1 = new Process();
                ProcessStartInfo processInfo = new ProcessStartInfo();
                int w = int.Parse(ws1);
                if (w == 1)
                {
                    processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }
                else
                {
                    processInfo.WindowStyle = ProcessWindowStyle.Normal;
                }
                processInfo.FileName = "C:\\Windows\\system32\\cmd.exe";
                processInfo.Arguments = $"/C \"py -3 {chippath} -p {serialPort1.PortName} {fwfile}\"";
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + processInfo.Arguments.ToString();
                process1.StartInfo = processInfo;
                process1.Start();
                process1.WaitForExit();
                process1.Close();
                Thread.Sleep(1000);              
        }
        public string putonquietmode()
        {
            string VV1 = "";
            string[] subs = new string[6] { "", "", "", "", "", "" };            
            int i = 0;
            while (true)
            {
                try
                {
                    serialPort1.DiscardOutBuffer();
                    serialPort1.DiscardInBuffer();
                    serialPort1.WriteLine("m085 1");
                }catch (TimeoutException) { }
                int j = 0;
                while (true)
                {
                    try
                    {
                        subs[0] = serialPort1.ReadLine();
                    }
                    catch (TimeoutException) { }

                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0] + $"{Environment.NewLine}" + subs[1] + $"{Environment.NewLine}" + subs[3];
                    if (subs[0].Contains("m085 002 OK"))
                    {
                        break;
                    }
                    if (j == 6)
                        break;
                    j++;

                }
                if (subs[0].Contains("m085 002 OK"))
                {

                    break;
                }
                else
                {
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0] + $"{Environment.NewLine}" + subs[1] + $"{Environment.NewLine}" + "Try\n" + subs[2];
                    i = i + 1;
                }
                if (i == 6)
                {
                    break;
                }
            }
            return subs[0];
        }
        public string putonshipmode()
        {
            int i = 0;
            string VV1 = "";
            while (true)
            {
                
                string[] subs = new string[30] { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
                string[] subs2 = new string[6] { "", "", "", "", "", "" };
                string[] subs3 = new string[6] { "", "", "", "", "", "" };
                string[] subs4 = new string[6] { "", "", "", "", "", "" };
                try
                {
                    serialPort1.DiscardOutBuffer();
                    serialPort1.DiscardInBuffer();
                    serialPort1.WriteLine("m038");
                }
                catch (TimeoutException) { }
                int j = 0;
                while (true)
                {
                    try
                    {
                        VV1 = serialPort1.ReadLine();
                    }

                    catch (TimeoutException) { }
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + VV1;
                    if (VV1.Contains("m038 002 OK"))
                    {
                        break;
                    }
                    j = j + 1;
                    if (j == 30)
                    {
                        break;
                    }
                }

                if (VV1.Contains("m038 002 OK") || i == 6)
                {
                    break;
                }
                i = i + 1;
            }
            return VV1;
        }
        public string getnordicaddres()
        {
            string VV1 = "";
            string VV2 = "";
            string[] subs = new string[6] { "", "", "", "", "", "" };
            string[] subs2 = new string[6] { "", "", "", "", "", "" };
            string[] subs3 = new string[6] { "", "", "", "", "", "" };
            string fw = "";
            int i = 0;
            while (true)
            {
                try
                {
                    serialPort1.DiscardOutBuffer();
                    serialPort1.DiscardInBuffer();
                    serialPort1.WriteLine("bm");
                }
                catch (TimeoutException) { }
                int j = 0;
                while (true)
                {
                    try
                    {
                        subs[0] = serialPort1.ReadLine();
                    }
                    catch (TimeoutException) { }

                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0] + $"{Environment.NewLine}" + subs[1] + $"{Environment.NewLine}" + subs[3];
                    if (subs[0].Contains("BLE: Nordic MAC Address"))
                    {
                        break;
                    }
                    if (j == 9)
                        break;
                    j++;

                }
                if (subs[0].Contains("BLE: Nordic MAC Address"))
                {

                    break;
                }
                else
                {
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0] + $"{Environment.NewLine}" + subs[1] + $"{Environment.NewLine}" + "Try\n" + subs[2];
                    i = i + 1;
                }
                if (i == 6)
                {
                    break;
                }
            }
            VV1 = subs[0];
            if (VV1.Length > 10)
            {
                try
                {
                    subs2 = VV1.Split(' ');
                    if (subs2.Length >= 3)
                    {
                        fw = subs2[4];
                        if (fw.Length >= 15)
                        {
                            fw = fw.Substring(0, 17);                          
                        }
                    }
                }
                catch (Exception) { }
            }
            if (fw.Length >= 10)
            {
                subs3 = fw.Split(':');
                VV2 = subs3[5];
                i = Convert.ToInt32(VV2, 16);
                i = i + 1;
                string hex = i.ToString("X");
                VV2 = subs3[0] + ":" + subs3[1] + ":" + subs3[2] + ":" + subs3[3] + ":" + subs3[4] + ":" + hex;
            }
            return VV2;
        }
        public void updatenordic( string fwfile)
        {          
            int ka = 0;
            int ga = 0;            
            string VV1= getnordicaddres();
            if (putbootmode())
            {
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Start Nordic Update";
                Thread.Sleep(1000);
                Process process1 = new Process();
                ProcessStartInfo processInfo = new ProcessStartInfo();
                int w = int.Parse(ws1);
                if (w == 1)
                {
                    processInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }
                else
                {
                    processInfo.WindowStyle = ProcessWindowStyle.Normal;
                }
                processInfo.FileName = "C:\\Windows\\system32\\cmd.exe";
                processInfo.Arguments = $"/C \"python -m whoop_nordic_update dfu ble --conn-ic-id NRF52 --address {VV1} --port {portnrf} --package {fwfile}\"";
                process1.StartInfo = processInfo;
                process1.Start();
                process1.WaitForExit();
                Thread.Sleep(1000);
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "End Nordic Update";
            }
            else
            {
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "bootmode not available";
            }
        }
        public bool putbootmode()
        {
            bool boot = false;

            int i = 0;
            string[] subs = new string[6] { "", "", "", "", "", "" };
            string[] subs2 = new string[6] { "", "", "", "", "", "" };
            string fw = "";


            while (true)
            {
                subs = new string[6] { "", "", "", "", "", "" };
                subs2 = new string[6] { "", "", "", "", "", "" };
                serialPort1.DiscardOutBuffer();
                serialPort1.DiscardInBuffer();
                serialPort1.WriteLine("bU");
                int j = 0;
                int ka = 0;
                int ga = 0;
                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "bU";
                while (true)
                {
                    try
                    {
                        subs[0] = serialPort1.ReadLine();
                    }
                    catch (TimeoutException) { }
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0];
                    if (subs[0].Contains("BLE: Bootloader; mode entry complete."))
                    {
                        break;
                    }
                    j++;
                    if (j == 6)
                        break;
                }
                if (subs[0].Contains("BLE: Bootloader; mode entry complete."))
                    break;
                
                if (i == 6)
                {
                    break;
                }
                i++;
            }
            if (subs[0].Contains("BLE: Bootloader; mode entry complete."))
                boot=true;
            return boot;
        }
        public string saveresults(string sw1,string Teststart,string SN)
        {
            string pathString = file + "Recharge_" + DateTime.Now.ToString("MM_dd_yyyy") + "_" + "FT_Strap" + ".csv";
            string pathString2 = file2 + "log_Recharge_" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm") + "_" + SN + ".txt";
            string log = tbcom.Text;
            csvt = "";
            csv = "";
            string csvl = "";
            tbtime.Text = sw1;
            int i = 0;
            if (dataGridView1.Rows.Count > 1)
            {


                for (i = 0; i < (dataGridView1.Rows.Count - 2); i++)
                {
                    csvt = csvt + dataGridView1.Rows[i].Cells[0].Value + ",";
                    csv = csv + dataGridView1.Rows[i].Cells[1].Value + ",";
                }
            }
            string now = DateTime.Now.ToString("MM/dd/yyyy HH\\:mm\\:ss");
            csvl= this.Text + "," + Teststart + "," +now+ "," + sw1 + "" + "," + SN + "," + dataGridView1.Rows[dataGridView1.Rows.Count - 2].Cells[1].Value + "," + csv + "Test Station #" + ws + "," + LoginForm.userid + "," + "15" + "\n";
            csvt = "Test_Program_Version" + "," + "Test_Start_Time" + "," + "Test_End_Time" + "," + "Test_Duration" + "," + "Serial_Number" + "," + "Final_Result" + "," + csvt + "Workstation" + "," + "Employee_ID" + "," + "Number_of_Results" + "\n";
            csv = csvt + this.Text + "," + Teststart + "," + now + "," + sw1 + "" + "," + SN + "," + dataGridView1.Rows[dataGridView1.Rows.Count - 2].Cells[1].Value + "," + csv + "Test Station #" + ws + "," + LoginForm.userid + "," + "15" + "\n";

            if (File.Exists(pathString))
            {
                csvt = "";
                csvt = File.ReadAllText(pathString);
                File.Delete(pathString);
            }
            csvl = csvt + csvl;
            using (StreamWriter sw = File.CreateText(pathString))
            {
                sw.Write(csvl);
            }
            File.WriteAllText(pathString2, log);
            return csv;
        }
        private void button2_Click(object sender, EventArgs e)
        {
                
                dataGridView1.Rows.Clear();
                string Teststart = DateTime.Now.ToString("MM/dd/yyyy HH\\:mm\\:ss");
                tbcom.Text = "";
                panel1.BackColor = Color.SteelBlue;
                string[] subs = new string[6] { "", "", "", "", "", "" };
                tbcom.Text = "";
                string loco = "";
                string SN = tbsn.Text.ToUpper();
                string revision = "";
                string VV1 = ""; //TODO Variable Naming - please rename so we know what it is for. It looks like you use it to store strap responses, if so just name if "strapResponse" or something like that.
                int conect = 0;
                string[] subs2 = new string[6] { "", "", "", "", "", "" };
                int i = 0;
                int t = 0;
                string revision2 = "";
                string fw = "";                
                int ga = 0;
                int ka = 0;
                Stopwatch sw1 = new Stopwatch();
                int ship = 0;
                sw1.Start();
                
                string response1 = "";
                //
                if (LoginForm.offline.Contains("online"))
                {
                    response1 = wmx_ts.IsSerialNumberAvailable(SN, "Strap Re-Flash");
                }
                else
                {
                    response1 = "OK";
                }
           
                if (SN.Length == 9 && response1.Contains("OK"))
                {
                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Check NFC Conection" + $"{Environment.NewLine}" + "X05,01,24,4,68A3" + $"{Environment.NewLine}" + "W01,sync";
                    serialPort1.Open();
                    conect = CheckNFCCon();
                    if (conect == 0)
                    {
                        
                        tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Unlock Quiet mode";
                                           
                        CreateRow("NFC_Conection", "Pass", "Pass", "Pass", "Pass", "OK");
                        i = 0;
                        VV1 = "";
                        tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Disable Shipmode";
                        Disablequietmode();
                        //function to obtain Fuel Gauge Report
                        disableshipmode();          
                        VV1 = "";
                        i = 0;
                        t = 0;
                        tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Serial Number Eeprom Process";
                        VV1 = GetSN(SN);
                        
                        if (SN == VV1 && SN != "")
                        {
                            CreateRow("Eeprom_Serial_Number", VV1,SN,SN, "Pass", "OK");
                            t = t + 1;
                            tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Serial Number Eeprom Process" + $"{Environment.NewLine}" + "m017";


                            
                            int FG = 0;
                            
                            t = 0;
                            i = 0;
                            tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Check Fuel Gauge";
                            //function to obtain Fuel Gauge Report
                            FG = GETFG();
                            bool Chargetest =false;
                            if (FG <= 100&& FG>1)
                            {
                                Chargetest = true;
                                CreateRow("Fuel_Gauge_Report",FG.ToString(), "1", "100", "Pass", "OK");
                                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0] + $"{Environment.NewLine}" + subs[1] + $"{Environment.NewLine}" + subs[2] + $"{Environment.NewLine}" + subs[3] + $"{Environment.NewLine}" + FG.ToString();
                            }
                            else 
                            {
                                CreateRow("Fuel_Gauge_Report", FG.ToString(), "1", "100", "Pass", "not ok");
                            }
                            /*if (!LoginForm.offline.Contains("online"))
                            {
                                Chargetest = true;  
                            }*/
                            //if FG is higher than 95 chargetest is true
                            if (Chargetest)
                            {
                                tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "Revision ID";
                            //function to obtain Sensor Revision
                            revision = getmaximver(fwMaxim);
                            if (revision.Contains(fwMaxim))
                            {
                                revision = fwMaxim;
                            }
                            else
                            {


                                if (revision.Length >= 10)
                                {
                                    subs = revision.Split(' ');
                                    try
                                    {
                                        if (subs.Length >= 3)
                                        {
                                            revision = subs[subs.Length-1];
                                            if (revision.Length >= 7)
                                            {
                                                subs2 = revision.Split('.');
                                                for (i = 0; i <= subs2.Length - 1; i++)
                                                {
                                                    if (subs2[i].Contains("\r"))
                                                    {
                                                        int ihn = int.Parse(subs2[i]);
                                                        subs2[i] = ihn.ToString();
                                                    }
                                                }
                                            }

                                        }
                                        revision = subs2[0] + "." + subs2[1] + "." + subs2[2] + "." + subs2[3];

                                    }
                                    catch (Exception) { }
                                }
                            }
                            revision2 = getnordicversion(fwNordic);
                            Boolean firmwarecheck = false;
                            if (revision.Contains(fwMaxim) && revision2.Contains(fwNordic))
                            {
                                firmwarecheck = true;
                            }
                            else if(revision.Contains("41.11.8.0") && revision2.Contains(fwNordic))
                            {
                                firmwarecheck = true;
                            }
                            else
                            {
                                firmwarecheck = false;
                            }
                                if (firmwarecheck) 
                                {
                                    CreateRow("Maxim_Firmware_version", revision,fwMaxim, "41.11.8.0", "Pass", "OK");
                                    CreateRow("Nordic_Firmware_Version",fwNordic, fwNordic,fwNordic, "Pass", "OK");
                                    i = 0;
                                    bool checksmv = false;
                                    tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + "";
                                    
                                    //if the shipmode fw version is according to fwverRevL the variable checksmv will be true
                                    if (true)
                                    {                                        
                                        bool quietmodeenable = false;
                                        //function to put on quiet mode
                                       // VV1 = putonquietmode();
                                        if (VV1.Contains("m085 002 OK"))
                                        {
                                        quietmodeenable = true;
                                        }
                                        else
                                    {
                                        quietmodeenable = false;
                                    }
                                        if (true)
                                        {
                                            CreateRow("Quiet_Mode", "Not_Applied", "Pass", "Pass", "Pass", "OK");                                            
                                            tbcom.Text = tbcom.Text + $"{Environment.NewLine}" + subs[0] + $"{Environment.NewLine}" + subs[1] + $"{Environment.NewLine}" + subs[2];
                                            //VV1 = putonshipmode();
                                            bool shipmodeenable = false;
                                            if (VV1.Contains("m038 002 OK"))
                                            {
                                                shipmodeenable = true;
                                            }
                                        else
                                        {
                                            shipmodeenable = false;
                                        }
                                            if (true)
                                            {
                                                CreateRow("Ship_Mode", "Not_Applied", "Pass", "Pass", "Pass", "OK");
                                                CreateRow("Test_Result", "Pass", "Pass", "Pass", "Pass", "OK");   
                                            /*
                                                serialPort1.DiscardOutBuffer();
                                                serialPort1.DiscardInBuffer();
                                                serialPort1.WriteLine("K");
                                                Thread.Sleep(6000);
                                                serialPort1.DiscardOutBuffer();
                                                serialPort1.DiscardInBuffer();
                                                serialPort1.WriteLine("m001");
                                                try
                                                {
                                                    VV1 = "";
                                                    VV1 = serialPort1.ReadLine();
                                                }
                                                catch (Exception ex) { tbcom.Text = tbcom.Text + ex.ToString() + VV1; }
                                            */
                                                tbpass.Text = "Pass";
                                                tbpass.BackColor = Color.Green;
                                                panel1.BackColor = Color.Green;

                                            }
                                            else
                                            {
                                                CreateRow("Ship_Mode", "", "Pass", "Pass", "Fail", "not");
                                                CreateRow("Test_Result", "Ship_Mode_Failure", "Pass", "Pass", "Fail", "not");
                                                tbpass.Text = "Fail";
                                                tbpass.BackColor = Color.Red;
                                                panel1.BackColor = Color.Red;
                                            }
                                        }
                                        else
                                        {
                                            CreateRow("Quiet_Mode", "Fail", "Pass", "Pass", "Fail", "not");
                                            CreateRow("Ship_Mode", "", "Pass", "Pass", "Fail", "not");
                                            CreateRow("Test_Result", "Quiet_Mode_Failure", "Pass", "Pass", "Fail", "not");
                                            tbpass.Text = "Fail";
                                            tbpass.BackColor = Color.Red;
                                            panel1.BackColor = Color.Red;
                                    }
                                    }
                                    else
                                    {
                                        if (VV1.Length > 10)
                                        {
                                            try
                                            {
                                                subs2 = VV1.Split(' ');
                                                if (subs2.Length >= 3)
                                                {
                                                    fw = subs2[2];
                                                    if (fw.Length >= 7)
                                                    {
                                                        fw = fw.Substring(0, 7);
                                                    }
                                                }
                                            }
                                            catch (Exception) { }
                                        }
                                        
                                        CreateRow("Quiet_Mode", "", "Pass", "Pass", "Fail", "not");
                                        CreateRow("Ship_Mode", "", "Pass", "Pass", "Fail", "not");
                                        CreateRow("Test_Result", "Fail_Get_Firmware", "Pass", "Pass", "Fail", "not");
                                        tbpass.Text = "Fail";                                        
                                        tbpass.BackColor = Color.Red;
                                        panel1.BackColor = Color.Red;
                                        MessageBox.Show("Falla del Strap");
                                        
                                    }
                                }                            
                                
                                else
                                {
                                //after 6 Attemps we assume No Response in Sensor revision
                                    //Q08,:m001 009 42.11.7.0
                                   
                                subs = new string[6] { "", "", "", "", "", "" };
                                subs2 = new string[6] { "", "", "", "", "", "" };
                                if (revision2.Contains(fwNordic))
                                {
                                    revision2= fwNordic;
                                }
                                else
                                { 
                                    if (revision2.Length >= 10)
                                    {
                                        subs = revision2.Split(' ');
                                        try
                                        {
                                            if (subs.Length >3)
                                            {
                                                revision2 = subs[subs.Length-1];
                                                if (revision2.Length >= 7)
                                                {
                                                    subs2 = revision2.Split('.');
                                                    for (i = 0; i <= subs2.Length - 1; i++)
                                                    {
                                                        if (subs2[i].Contains("\r"))
                                                        {
                                                            int ihn = int.Parse(subs2[i]);
                                                            subs2[i] = ihn.ToString();
                                                        }
                                                    }
                                                }

                                            }
                                            revision2 = subs2[0] + "." + subs2[1] + "." + subs2[2] + "." + subs2[3];

                                        }
                                        catch (Exception) { }
                                    }
                                }
                                
                                CreateRow("Check_Maxim", revision,fwMaxim, fwMaxim, "Fail", "no");
                                
                                    CreateRow("Check_Nordic", revision2, fwNordic, fwNordic, "Fail", "no");
                                    CreateRow("Quiet_Mode", "", "Pass", "Pass", "Fail", "no");
                                    CreateRow("Ship_Mode", "", "Pass", "Pass", "Fail", "no");
                                if(!revision2.Contains(fwNordic)&& revision.Contains(fwMaxim))
                                {
                                    CreateRow("Test_Result", "Nordic_version_invalid", "Pass", "Pass", "Fail", "no");
                                }
                                else if(revision2.Contains(fwNordic) && !revision.Contains(fwMaxim))
                                {
                                    CreateRow("Test_Result", "Maxim_version_invalid", "Pass", "Pass", "Fail", "no");
                                }
                                else
                                {
                                    CreateRow("Test_Result", "Maxim_Nordic_Firmware_Invalid", "Pass", "Pass", "Fail", "no");
                                }
                                    tbpass.Text = "Fail";
                                    tbpass.BackColor = Color.Red;
                                    panel1.BackColor = Color.Red;                                    
                                    MessageBox.Show("Falla del Strap");
                                }                                                            
                            }
                            else
                            {
                                CreateRow("Check_Maxim", "", fwMaxim, fwMaxim, "Fail", "no");                                
                                CreateRow("Quiet_Mode", "", "Pass", "Pass", "Fail", "no");
                                CreateRow("Ship_Mode", "", "Pass", "Pass", "Fail", "no");
                                CreateRow("Test_Result", "Fuel_Gauge", "Pass", "Pass", "Fail", "no");
                                tbpass.Text = "Fail";
                                tbpass.BackColor = Color.Red;
                                panel1.BackColor = Color.Red;                                
                                MessageBox.Show("Volver a carga");
                            }
                        }
                        else
                        {
                        // extract all results entry into the function CreateRow()
                            CreateRow("Eeprom_Serial_Number", VV1, SN,SN, "Fail", "no");
                            CreateRow("Fuel_Gauge_Report", "", "95", "100", "Fail", "no");
                            CreateRow("Check_Maxim", "", fwMaxim, fwMaxim, "Fail", "no");
                            CreateRow("Quiet_Mode", "", "Pass", "Pass", "Fail", "no");
                            CreateRow("Ship_Mode", "", "Pass", "Pass", "Fail", "no");
                            CreateRow("Test_Result", "EEPROM_GET_SN_INVALID", "Pass", "Pass", "Fail", "no");
                            tbpass.Text = "Fail";
                            tbpass.BackColor = Color.Red;
                            panel1.BackColor = Color.Red;                            
                            MessageBox.Show("Falla del Strap");
                        }

                    }
                    else
                    {
                        // extract all results entry into the function CreateRow()
                        tbpass.Text = "Fail";
                        tbpass.BackColor = Color.Red;
                        panel1.BackColor = Color.Red;
                        //adicion de nuevo renglon
                        CreateRow("NFC_Conection", "Fail", "Pass", "Pass", "Fail", "no");
                        CreateRow("Eeprom_Serial_Number", VV1, SN, SN, "Fail", "no");
                        CreateRow("Fuel_Gauge_Report", "", "95", "100", "Fail", "no");
                        CreateRow("Check_Maxim", "", fwMaxim, fwMaxim, "Fail", "no");
                        CreateRow("Quiet_Mode", "", "Pass", "Pass", "Fail", "no");
                        CreateRow("Ship_Mode", "", "Pass", "Pass", "Fail", "no");
                        CreateRow("Test_Result", "NFC_BAD_Comunication", "Pass", "Pass", "Fail", "no");                        
                        MessageBox.Show("Falla del Strap");
                    }
                    serialPort1.Close();

                    sw1.Stop();            
                    string timelapsed=sw1.Elapsed.ToString("hh\\:mm\\:ss");
                    csv = saveresults(timelapsed, Teststart, SN);
                    if(LoginForm.offline.Contains("offline"))
                    {
                        ship = 5;
                    }
                    if (ship != 5)
                    {
                        try
                        {
                            wmx_ts.SaveTestResult(csv, "Strap Re-Flash");
                        }
                        catch (Exception Ex) {; MessageBox.Show(Ex.ToString(), "Salir", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1); }
                    }

                }
                else
                {
                    if (response1.Contains("OK"))
                    {
                        MessageBox.Show("Por favor escanear correctamente el numero de serie", "Salir", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                    }
                    else if (SN.Length == 9 && SN != "")
                    {
                        MessageBox.Show(response1);
                    }
                    else
                    {
                        MessageBox.Show("Por favor escanear correctamente el numero de serie", "Salir", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                    }
                }
                tbsn.Text = "";
                tbsn.Select();
                
        }
            
        
    

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Desea Salir?", "Salir", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == System.Windows.Forms.DialogResult.Yes)
                Application.Exit();
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string dia = Application.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.DayOfWeek).ToString();
            diadelasemana = dia;
            string day = DateTime.Now.Day.ToString("00");
            dianum = day;
            string mes1 = DateTime.Now.Month.ToString("00");
            mes = mes1;
            string hora1 = DateTime.Now.Hour.ToString("00");
            hora = hora1;

            string minutos1 = DateTime.Now.Minute.ToString("00");
            minutos = minutos1;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            serialPort1.Open();
            int i = CheckNFCCon();
            Disablequietmode();
            disableshipmode();
            string nordic = getnordicversion(fwNordic);
            if(!nordic.Contains("17.2.1.0"))
            {
                updatenordic(Norpath);
            }
            serialPort1.Close();
        }
    }
}
