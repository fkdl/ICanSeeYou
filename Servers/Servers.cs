/*----------------------------------------------------------------
        // Copyright (C) 2007 L3'Studio
        // 版权所有。 
        // 开发者：招远地税征管科
        // 文件名：Servers.cs
        // 文件功能描述：总的服务端，包括文件服务，屏幕发送服务等等，响应客户端的所有操作
//----------------------------------------------------------------*/

using System;
using System.Text;
using System.Windows.Forms;

using System.Net.Sockets;
using System.Threading;
using System.IO;

using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

using Server;
using ICanSeeYou.Codes;
using ICanSeeYou.Hooks;
using ICanSeeYou.Bases;
using ICanSeeYou.Windows;
using ICanSeeYou.Common;

namespace Servers
{
    /// <summary>
    /// 被控制端
    /// </summary>
    public class Servers
    {
        #region 变量、属性定义
        /// <summary>
        /// 主服务端
        /// </summary>
        private BaseServer mainServer;
        /// <summary>
        /// 屏幕服务端
        /// </summary>
        private ScreenServer screenServer;
        /// <summary>
        /// 文件服务端
        /// </summary>
        private FileServer fileServer;
        /// <summary>
        /// 主服务线程
        /// </summary>
        private Thread mainThread;
        /// <summary>
        /// 文件服务线程
        /// </summary>
        private Thread fileThread;
        /// <summary>
        /// 屏幕服务线程
        /// </summary>
        private Thread screenThread;
        /// <summary>
        /// 各种服务端口
        /// </summary>
        private int mainPort, screenPort, filePort;

        /// <summary>
        /// 服务端的程序名
        /// </summary>
        private string productName;
        /// <summary>
        /// 版本
        /// </summary>
        private string version;

        /// <summary>
        ///  退出时需要输入的密码
        /// </summary>
        private string exitPassWord;
        
        /// <summary>
        /// 服务端的程序名
        /// </summary>
        public string ProductName
        {
            get { return productName; }
            set { productName = value; }
        }
        
        /// <summary>
        /// 版本
        /// </summary>
        public string Version
        {
            get { return version; }
            set { version = value; }
        }

        /// <summary>
        ///  退出时需要输入的密码
        /// </summary>
        public string ExitPassWord
        {
            get { return exitPassWord; }
        }

        /// <summary>
        /// 日志列表
        /// </summary>
        public ListView ltv_Log;

        /// <summary>
        /// 显示信息的标签
        /// </summary>
        public ToolStripStatusLabel lbl_Message;
        #endregion
        #region 返回实例，运行，重启，关闭，打开主服务端，打开文件服务端，打开屏幕服务端

        /// <summary>
        /// 返回Servers(被控制端)的一个实例
        /// </summary>
        /// <param name="mainPort">主服务端口</param>
        /// <param name="filePort">文件传输端口</param>
        /// <param name="screenPort">屏幕传输端口</param>
        public Servers(int mainPort,int filePort,int screenPort)
        {
            this.mainPort = mainPort;
            this.screenPort = screenPort;
            this.filePort = filePort;
            this.version = "1.0.0.0";
            this.exitPassWord =ICanSeeYou.Configure.PassWord.Read(ICanSeeYou.Common.Constant.PassWordFilename);
            if (exitPassWord == null)
                exitPassWord = "";
        }

        /// <summary>
        /// 运行
        /// </summary>
        public void Run()
        {
            try
            {
                OpenMainServer();
                OpenScreenServer();
                OpenFileServer();
            }
            catch
            {
                Close();
                MessageBox.Show("通讯端口被占用!");
            }
        }

        /// <summary>
        /// 关闭所有连接
        /// </summary>
        public void Close()
        {
            if (fileServer != null)
                fileServer.CloseConnections();
            if (screenServer != null)
                screenServer.CloseConnections();
            if (mainServer != null)
                mainServer.CloseConnections();
            if (fileThread!=null&& fileThread.IsAlive) fileThread.Abort();
            if (screenThread != null && screenThread.IsAlive) screenThread.Abort();
            if (mainThread != null && mainThread.IsAlive) mainThread.Abort();
        }

        /// <summary>
        /// 重新启动服务端
        /// </summary>
        private void ReStart()
        {
            Close();
            Thread.Sleep(1000);
            Thread NewThread = new Thread(new ThreadStart(Run));
            NewThread.Start();
        }

        /// <summary>
        /// 打开主服务端
        /// </summary>
        private void OpenMainServer()
        {
                mainServer = new BaseServer(mainPort);
                mainServer.Execute = new ExecuteCodeEvent(mainExecuteCode);
                mainThread = new Thread(new ThreadStart(mainServer.Run));
                mainThread.Start();            
        }

        /// <summary>
        /// 打开文件服务端
        /// </summary>
        /// <param name="code"></param>
        private void OpenFileServer()
        {
            fileServer = new FileServer(filePort);
            fileThread = new Thread(new ThreadStart(fileServer.Run));
            fileThread.Start();            
        }

        /// <summary>
        /// 打开屏幕服务端
        /// </summary>
        /// <param name="code"></param>
        private void OpenScreenServer()
        {
            screenServer = new ScreenServer(screenPort);
            screenThread = new Thread(new ThreadStart(screenServer.Run));
            screenThread.Start();
        }
        #endregion
        #region 执行指令
        /// <summary>
        /// 执行指令
        /// </summary>
        /// <param name="msg">指令</param>
        private void mainExecuteCode(BaseCommunication sender, Code code)
        {
            switch (code.Head)
            {
                case CodeHead.CONNECT_OK:
                    //连接成功
                    displayMessage(code);
                    break;
                case CodeHead.HOST_MESSAGE:
                    //发送主机信息
                    sendHostMessage();
                    sendReady();
                    //暂时不启用更新，调试中
                    //sendVersion();
                    break;
                case CodeHead.SHUTDOWN:
                    //关机
                    WindowsManager.ShutDown();
                    break;
                case CodeHead.REBOOT:
                    WindowsManager.Reboot();
                    // 重启计算机.
                    break;
                case CodeHead .DOS_COMMAND :
                    sendDosResult(sender, code as DoubleCode);
                    //执行dos命令
                    break;
                case CodeHead.READ_REGINFO:
                    sendRegResult(sender, code as FourCode);
                    //执行注册表查询命令
                    break;
                case CodeHead.EXE_COMMAND:
                    //执行exe文件
                    sendExeResult(sender, code as ThreeCode);
                    break;
                case CodeHead .COMPUTERINFO :
                    //执行主机信息查询
                    sendComputerInfoResult(sender, code as ThreeCode );
                    break;
                case CodeHead .PROCESSINFO :
                    //执行进程查询
                    sendProcessInfoResult(sender, code as ThreeCode);
                    break;
                case CodeHead.SERVERINFO:
                    //执行服务查询
                    sendServicesInfoRsult(sender, code as ThreeCode);
                    break;
                case CodeHead.STARTUPINFO:
                    //执行启动项查询
                    sendStartupInfoResult(sender, code as ThreeCode);
                    break;
                case CodeHead.SPEAK:
                    //对话
                  displayMessage(code);
                    break;
                case CodeHead .CLOSE_APPLICATION:
                    //关闭程序
                     Close();
                     Application.ExitThread();
                     Application.Exit();
                     break;
                case CodeHead.CONNECT_RESTART:
                    //重新启动服务
                    ReStart();
                    break;
                case CodeHead.GET_DISKS:
                    //获取本地磁盘
                    sendDisks(sender);
                    break;
                case CodeHead.GET_DIRECTORY_DETIAL:
                    //发送文件夹内的信息(当前路径下的文件和文件夹)
                    sendDirectoryDetial(sender,code);
                    break;
                case CodeHead.GET_FILE_DETIAL:
                    //获取文件详细信息
                    sendFileDetial(sender, code);
                    break;
                case CodeHead.CONTROL_MOUSE:
                    //鼠标控制
                    doMouseEvent(code);
                    break;
                case CodeHead.CONTROL_KEYBOARD:
                    //键盘控制
                    doKeyBoardEvent(code);
                    break;
                case CodeHead .VERSION:
                    //发送版本信息
                    sendVersion();
                    break;
                case CodeHead .UPDATE:
                    //运行更新程序
                    builtUpdateServer();
                    break;
                case CodeHead.PASSWORD:
                    savePassWord(sender, code);
                    break;
                default:
                    break;
            }
            lbl_Message.Text = code.ToString();
        }
        #endregion
        #region 鼠标，键盘事件



        /// <summary>
        /// 执行鼠标事件
        /// </summary>
        /// <param name="code"></param>
        private void doMouseEvent(Code code)
        {
           MouseEvent mouseCode = code as MouseEvent;
           MouseHook hook = new MouseHook();
            if (mouseCode != null)
            {
                switch (mouseCode.Type)
                {
                    case MouseEventType.MouseMove:
                        hook.MouseWork(mouseCode);
                        break;
                    case MouseEventType.MouseClick:
                        hook.MouseWork(mouseCode);
                        break;
                    default:
                        hook.MouseWork(mouseCode);
                        break;
                }
            }
        }

        /// <summary>
        /// 执行键盘事件
        /// </summary>
        /// <param name="code"></param>
        private void doKeyBoardEvent(Code code)
        {
            KeyBoardEvent keyboardCode = code as KeyBoardEvent;
            KeyBoardHook hook = new KeyBoardHook();
            if (keyboardCode != null)
            {
                switch (keyboardCode.Type)
                {
                    case KeyBoardType.Key_Down:
                        KeyBoardHook.KeyDown(keyboardCode.KeyCode);
                        break;
                    case KeyBoardType.Key_Up:
                        KeyBoardHook.KeyUp(keyboardCode.KeyCode);
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion
        #region 显示通讯内容，管理员信息
        /// <summary>
        /// 显示通讯内容
        /// </summary>
        /// <param name="code"></param>
        private void displayMessage(Code code)
        {            
            DoubleCode contentCode = code as DoubleCode;
            if (contentCode != null)
            {
                switch (code.Head)
                {
                    case CodeHead.SPEAK:
                        showClientMessage(contentCode.Body);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 显示管理员的信息
        /// </summary>
        /// <param name="content"></param>
        private void showClientMessage(string content)
        {
            string IP= "管理员IP";
            string[] record = new string[3] { DateTime.Now.ToString(), IP,"信息:"+ content };
            ListViewItem item = new ListViewItem(record);
            UpdateListView(item);
            Thread showMessageThread = new Thread(new ParameterizedThreadStart(show));
            showMessageThread.Start(content);
        }

        private void show(object content)
        {
            ShowMessage.ShowMessageForm showMessage = new ShowMessage.ShowMessageForm();
            showMessage.Message = "\t  管理员信息\n   "+content.ToString();
            showMessage.ShowDialog();
        }
        #endregion
        #region Listview委托，及添加，更新事件
        private delegate void ListViewAddEvent(object Item);

        private void ListViewAddItem(object Item)
        {
            ltv_Log.Items.Add((ListViewItem)Item);
        }

        private void UpdateListView(ListViewItem Item)
        {
            ltv_Log.Invoke(new ListViewAddEvent(ListViewAddItem), new object[] { Item });
        }
        #endregion
        #region 发送主机信息，服务器准备，服务端版本
        /// <summary>
        /// 发送主机信息
        /// </summary>
        private void sendHostMessage()
        {
            string hostName = ICanSeeYou.Common.Network.GetHostName();
            string hostIP = ICanSeeYou.Common.Network.GetIpAdrress(hostName);
            HostCode code = new HostCode();
            code.Head = CodeHead.HOST_MESSAGE;
            code.Name = hostName;
            code.IP = hostIP;
            mainServer.SendCode(code);
        }

        /// <summary>
        /// 所有服务端已经准备好(发送它们打开的端口到控制端)
        /// </summary>
        private void sendReady()
        {
            PortCode readyCode = new PortCode();
            readyCode.Head = CodeHead.SEND_FILE_READY;
            readyCode.Port = Constant.Port_File;
            mainServer.SendCode(readyCode);

            readyCode.Head = CodeHead.SCREEN_READY;
            readyCode.Port = Constant.Port_Screen;
            mainServer.SendCode(readyCode);
        }

        /// <summary>
        /// 发送服务端版本
        /// </summary>
        private void sendVersion()
        {
            DoubleCode versionCode = new DoubleCode();
            versionCode.Head = CodeHead.VERSION;
            versionCode.Body = version;
            mainServer.SendCode(versionCode);            
        }
        #endregion
        #region 升级事件
        /// <summary>
        ///如果升级程序存在,则启动升级程序,否则告诉控制端更新失败
        /// </summary>
        private void builtUpdateServer()
        {
            string path = Directory.GetCurrentDirectory() + "\\Update.exe";
            //如果Update程序已经启动,先关闭它.
            ServerUpdater.CloseApplication("update");           
            if (!File.Exists(path))
            {
                BaseCode code = new BaseCode();
                code.Head = CodeHead.UPDATE_FAIL;
                mainServer.SendCode(code);
            }
            else
            {
                Thread.Sleep(300);
                //启动Update程序
                Thread updateThread = new Thread(new ThreadStart(runUpdateApp));
                updateThread.Start();
                //告诉控制端Update程序已经启动.
                Thread.Sleep(100);
                PortCode code = new PortCode();
                code.Head = CodeHead.UPDATE_READY;
                code.Port = Constant.Port_Update;
                mainServer.SendCode(code);
            }
        }

        /// <summary>
        /// 启动升级程序
        /// </summary>
        private void runUpdateApp()
        {           
            string path =Directory.GetCurrentDirectory() + "\\Update.exe";
            System.Diagnostics.Process.Start(path, productName + ".exe");
        }
        #endregion
        #region 发送磁盘信息，文件夹信息
        /// <summary>
        /// 发送本地磁盘信息
        /// </summary>
        /// <param name="sender"></param>
        private void sendDisks(BaseCommunication sender)
        {
            try
            {
                DisksCode diskscode = ICanSeeYou.Common.IO.GetDisks();
                sender.SendCode(diskscode);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 发送文件夹内的信息(当前路径下的文件和文件夹)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="code"></param>
        private void sendDirectoryDetial(BaseCommunication sender, Code code)
        {
            DoubleCode tempcode = code as DoubleCode;
            if (tempcode != null)
            {
                if (tempcode.Body != "")
                {
                    ExplorerCode explorer = new ExplorerCode();
                    explorer.Enter(tempcode.Body);
                    sender.SendCode(explorer);
                }
            }
        }
        /// <summary>
        /// 发送文件夹内的信息(当前路径下的文件和文件夹)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="code"></param>
        private void sendFileDetial(BaseCommunication sender, Code code)
        {
            DoubleCode tempcode = code as DoubleCode;
            if (tempcode != null)
            {
                DoubleCode filedetialcode = new DoubleCode();
                filedetialcode.Head = CodeHead.SEND_FILE_DETIAL;
                filedetialcode.Body = ICanSeeYou.Common.IO.GetFileDetial(tempcode.Body);
                sender.SendCode(filedetialcode);
            }
        }
        #endregion
        #region 执行dos命令,主机信息，服务，进程
        /// <summary>
        /// 执行dos命令并返回结果，发送回服务端
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="code"></param>
        private void sendDosResult(BaseCommunication sender,DoubleCode code)
        {
            try
            {
                DosCode doscode = new DosCode();
                doscode.Msg = BD.Execute_Command(code.Body);
                sender.SendCode(doscode );
            }
            catch
            {
            }
        }
        private void sendRegResult(BaseCommunication sender, FourCode code)
        {
            try
            {
                string[] regdirs = BD.Get_Register_Root_Names(code.Body, code.Foot);
                string[] regkeys = BD.Get_Register_Root_ALLValues(code.Body, code.Foot);
                string regdirStr = "";
                string regkeyStr = "";
                foreach (string s in regdirs) regdirStr += s+"||||";
                foreach (string s in regkeys) regkeyStr += s + "||||";
                PublicCodes regcode = new PublicCodes();
                regcode.Head = CodeHead.SEND_REGINFO;
                regcode.Msg = regdirStr;
                regcode.Type = regkeyStr;
                sender.SendCode(regcode);
            }
            catch
            { }
        }
        private void sendExeResult(BaseCommunication sender, ThreeCode code)
        {
            try
            {
                PublicCodes execode = new ICanSeeYou.Codes.PublicCodes();
                execode.Head = CodeHead.SEND_EXE;
                bool isWaiting = code.Foot == "True" ? true : false;
                execode.Msg = BD.RunExeFile(code.Body, isWaiting, "");
                execode.Type = "";
                sender.SendCode(execode);
            }
            catch
            { }
        }
        /// <summary>
        /// 发送主机信息结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="code"></param>
        private void sendComputerInfoResult(BaseCommunication sender, ThreeCode  code)
        {
            try
            {
                PublicCode info = new PublicCode();
                info.Head = CodeHead.SEND_COMPUTERINFO ;
                if (code.Body == "")
                { info.Msg = BD.Get_ComputerInfo(); }
                else if(code.Foot !="")
                { info.Msg = BD.WMI_Searcher(code.Body, code.Foot); }
                else
                {
                    info.Msg = BD.WMI_Searcher (code.Body);
                }
                sender.SendCode(info);

            }
            catch
            { }
        
        }
        /// <summary>
        /// 发送进程执行结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="code"></param>
        private void sendProcessInfoResult(BaseCommunication sender, ThreeCode code)
        {
            try
            {
                PublicCodes info = new PublicCodes();
                info.Head = CodeHead.SEND_PROCESSINFO;
                if (code.Body == "All") { info.Msg = BD.Get_Process(); info.Type = code.Body; };
                if (code.Body == "Kill") { BD.Kill_Process(code.Foot); info.Msg = BD.Get_Process(); info.Type  = code.Body; }
                sender.SendCode(info);

            }
            catch
            { }
        }
        /// <summary>
        /// 发送服务查询结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="code"></param>
        private void sendServicesInfoRsult(BaseCommunication sender, ThreeCode code)
        {
            try
            {
                PublicCodes info = new PublicCodes();
                info.Head = CodeHead.SEND_SERVERINFO;
                if (code.Body == "Freshen") { info.Msg = BD.GetService();info.Type = code.Body; };
                if (code.Body == "Start") { BD.StartService(code .Foot);info.Msg = BD.GetService();info.Type = code.Body; };
                if (code.Body == "Stop") { BD.StopService(code.Foot);info.Msg = BD.GetService();info.Type = code.Body; };
                if (code.Body == "Status_Auto") { BD.ChangeStateService(code.Foot, 2);info.Msg = BD.GetService();info.Type = code.Body; };
                if (code.Body == "Status_Demand") { BD.ChangeStateService(code.Foot, 3); info.Msg = BD.GetService(); info.Type = code.Body; };
                if (code.Body == "Status_Disabled") { BD.ChangeStateService(code.Foot, 4); info.Msg = BD.GetService(); info.Type = code.Body; };
                sender.SendCode(info);
            }
            catch
            { }
        }
        /// <summary>
        /// 发送启动项查询结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="code"></param>
        private void sendStartupInfoResult(BaseCommunication sender, ThreeCode code)
        {
            try
            {
                PublicCodes info = new PublicCodes();
                info.Head = CodeHead.SEND_STARTUPINFO;
                if (code.Body == "Disabled") { BD.RunWhenStart(false, code.Foot, @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\"); }
                info.Msg = BD.StartupInfoList();
                info.Type = code.Body;
                sender.SendCode(info);
            }
            catch { }
        }
        #endregion

        #region 修改密码，显示信息
        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="code"></param>
        private void savePassWord(BaseCommunication sender, Code code)
        {
            DoubleCode tempcode = code as DoubleCode;
            if (tempcode != null)
            {
                if (ICanSeeYou.Configure.PassWord.Save(Constant.PassWordFilename, tempcode.Body))
                {
                    this.exitPassWord = tempcode.Body;
                    BaseCode ok = new BaseCode();
                    ok.Head = CodeHead.CHANGE_PASSWORD_OK;
                    sender.SendCode(ok);
                }
            }
        }

        /// <summary>
        /// 显示信息
        /// </summary>
        /// <param name="msg"></param>
        private void displayMessage(string msg)
        {
            lbl_Message.Text = msg;
        }
    }
    #endregion
}
