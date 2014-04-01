using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Management;  // <=== Add Reference required!!
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Threading; 



namespace RestartExplorer
{
    public partial class Form1 : Form
    {
        //常数定义
        public const int REG_HOTKEY_NUM_DD = 820;//默认桌面快捷键注册码
        public const int REG_HOTKEY_NUM_SD = 821;//安全桌面快捷键注册码
        public const string REGKEY_AUTO_RESTARTSHELL = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
        public const int NO_INPUT = 1;
        public const int OK_INPUT = 0;

        //全局
        public Keys DD_HOTKEY = Keys.D;
        public Keys SD_HOTKEY = Keys.S;

        public Form1()
        {
            
            InitializeComponent();
            //start test
            //this.cddbtn.Text = "abc";
            //end test

            //设置输入框的MASK
            //this.ddhotkeybox.

            //隐藏修改快捷键的输入框
            this.ddhotkeybox.Hide();
            this.sdhotkeybox.Hide();

            //修改注册表值,杀掉explorer后系统不会自动拉起
            RegistryKey HKLM = Registry.LocalMachine;
            try
            {
                RegistryKey AutoRestartShell = HKLM.OpenSubKey(REGKEY_AUTO_RESTARTSHELL, true);
                AutoRestartShell.SetValue("AutoRestartShell", 0);
                AutoRestartShell.Close();
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }

            //单实例
            ThreadPool.RegisterWaitForSingleObject(Program.ProgramStarted, OnProgramStarted, null, -1, false); 
        }

        //当收到第二个进程的通知时，显示窗体  
        void OnProgramStarted(object state, bool timeout)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal; //注意：一定要在窗体显示后，再对属性进行设置  
        }

        //查找安全桌面或默认桌面的Explorer.exe的ID,flag=1:SD flag=0:DD
        public int FindExplorerID(int flag)
        {
            System.Diagnostics.Process[] explorers = System.Diagnostics.Process.GetProcessesByName("explorer");
            if (flag == 0)
            {
                foreach (Process ex in explorers)
                {
                    try
                    {
                        if (Process.GetProcessById(ex.Parent().Id).ProcessName != "SangforSDUI")
                        {
                            return ex.Id;
                        }
                    }
                    catch
                    {
                        return ex.Id;
                    }
                }
            }
            else
            {
                foreach (Process ex in explorers)
                {
                    try
                    {
                        if (Process.GetProcessById(ex.Parent().Id).ProcessName == "SangforSDUI") 
                        {
                            return ex.Id;
                        }
                    }
                    catch 
                    {
                        continue;
                    }
                }
            }
            return 0;
        }

        //重启默认桌面Explorer
        public void restartDD()
        {
            Process DDExplorer = Process.GetProcessById(this.FindExplorerID(0));
            if (DDExplorer.Id != 0)
            {
                DDExplorer.Kill();
                System.Threading.Thread.Sleep(500);
                Process.Start("explorer");
            }
        }
        //关闭安全桌面explorer
        public void closeSD()
        {
            Process SDExplorer = Process.GetProcessById(this.FindExplorerID(1));
            if (SDExplorer.Id != 0)
            {
                SDExplorer.Kill();
                //kill掉后，安全桌面会自己重新拉起
            }
        }
        //重启默认桌面Explorer按钮
        private void button1_Click(object sender, EventArgs e)
        {
            this.restartDD();
        }
        //关闭安全桌面explorer按钮
        private void button2_Click(object sender, EventArgs e)
        {
            this.closeSD();
        }



        //注册快捷键部分
        [DllImport("user32")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint control, Keys vk);
        [DllImport("user32")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        //load事件中注册快捷键
        //888未唯一标记， 1是WM_HOTKEY中的ALT，
        private void Form1_Load(object sender, EventArgs e)
        {
            RegisterHotKey(this.Handle, REG_HOTKEY_NUM_DD, 1 | 2, DD_HOTKEY);
            RegisterHotKey(this.Handle, REG_HOTKEY_NUM_SD, 1 | 2, SD_HOTKEY);
        }
        
        //截获消息
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x0312: //这个是window消息定义的 注册的热键消息 
                    if (m.WParam.ToString().Equals(REG_HOTKEY_NUM_DD.ToString()))  
                    {
                        this.restartDD();
                    }
                    else if (m.WParam.ToString().Equals(REG_HOTKEY_NUM_SD.ToString()))
                    {
                        this.closeSD();
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        //托盘
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.Visible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }

        //托盘菜单
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.notifyIcon1.Dispose();//释放托盘资源
            Application.Exit();//释放所有资源
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        //事件是在属性页面的闪电图标上点的。。。
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                this.notifyIcon1.BalloonTipTitle = "RestartExplorer";
                this.notifyIcon1.BalloonTipText = "托盘";
                this.notifyIcon1.ShowBalloonTip(1000);
            }
        }

        //注销快捷键
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnregisterHotKey(this.Handle, REG_HOTKEY_NUM_DD);
            UnregisterHotKey(this.Handle, REG_HOTKEY_NUM_SD);
        }



        //修改默认桌面快捷键的按钮
        private void cddbtn_Click(object sender, EventArgs e)
        {
            if (this.cddbtn.Text == "修改")
            {
                this.ddhotkeybox.Text = "";
                this.ddhotkeybox.Show();
                this.cddbtn.Text = "应用";
            }
            else if (this.cddbtn.Text == "应用")
            {
                //check input...
                if (checkInput(this.ddhotkeybox) == OK_INPUT)
                {
                    UnregisterHotKey(this.Handle, REG_HOTKEY_NUM_DD);
                    this.DD_HOTKEY = Keys.A + (this.ddhotkeybox.Text[0] - 'A');
                    RegisterHotKey(this.Handle, REG_HOTKEY_NUM_DD, 1 | 2, this.DD_HOTKEY);
                    this.label1.Text = @"热键 Ctrl+Alt+" + this.ddhotkeybox.Text;
                }
                this.ddhotkeybox.Hide();
                this.cddbtn.Text = "修改";
            }
        
        }
        //验证快捷键输入框的内容
        private int checkInput(TextBox tb)
        {
            if (tb.Text.Length == 0)
            {
                return NO_INPUT;
            }
            else 
            {
                tb.Text = tb.Text.ToUpper();
                return OK_INPUT;
            }
        }
        //修改安全桌面快捷键的按钮
        private void csdbtn_Click(object sender, EventArgs e)
        {
            if (this.csdbtn.Text == "修改")
            {
                this.sdhotkeybox.Text = "";
                this.sdhotkeybox.Show();
                this.csdbtn.Text = "应用";
            }
            else if (this.csdbtn.Text == "应用")
            {
                //check input...
                if (checkInput(this.sdhotkeybox) == OK_INPUT)
                {
                    UnregisterHotKey(this.Handle, REG_HOTKEY_NUM_SD);
                    this.SD_HOTKEY = Keys.A + (this.sdhotkeybox.Text[0] - 'A');
                    RegisterHotKey(this.Handle, REG_HOTKEY_NUM_SD, 1 | 2, this.SD_HOTKEY);
                    this.label3.Text = @"热键 Ctrl+Alt+" + this.sdhotkeybox.Text;
                }
                this.sdhotkeybox.Hide();
                this.csdbtn.Text = "修改";
            }
        }
    }

    //获取父进程ID的类
    public static class ProcessExtensions
    {
        private static string FindIndexedProcessName(int pid)
        {
            var processName = Process.GetProcessById(pid).ProcessName;
            var processesByName = Process.GetProcessesByName(processName);
            string processIndexdName = null;

            for (var index = 0; index < processesByName.Length; index++)
            {
                processIndexdName = index == 0 ? processName : processName + "#" + index;
                var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
                if ((int)processId.NextValue() == pid)
                {
                    return processIndexdName;
                }
            }

            return processIndexdName;
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
            return Process.GetProcessById((int)parentId.NextValue());
        }

        public static Process Parent(this Process process)
        {
            return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
        }
    } 


}
