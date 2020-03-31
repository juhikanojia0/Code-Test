/*------------------------------------------------------------------------
    File        : 
    Purpose     : For Audit Log 
    Syntax      :
    Author(s)   : Pritesh Parmar
    Created     : 19-12-2015
    Notes       :
    Version     : 1.0.0.0
Revision History:
 * Version     Project		Author				Date			Purpose																
 * 1.0.0.0		CIFMIS		Pankaj M. Borse		07-Oct-2015		Update for System Audit Log                                         
 * 1.0.0.1		CIFMIS		Sangram Kulkarni	23-Dec-2015		Switch for ON/OFF System Audit Log      
 * 1.0.0.2		CIFMIS		Pritesh Parmar	    08-Jan-2016		Hendle exception in all the methods      
 * 1.0.0.3		CIFMIS		Shweta Parashar	    01-Jan-2016		Validate Special Character
 * 1.0.0.4		CIFMIS      Mahesh Deore        04/05/2016	    Upgraded references from 10.1.400.1 to 10.1.400.8
 * 2.0.0.5      CIFMIS      Mahesh Deore        12-05-2016      Upgraded references from 10.1.400.8 to 10.1.400.9
 * 2.0.0.6	    CIFMIS      Mahesh Deore        20/05/2016	    Changed the references from 10.1.400.9 changes to 10.1.400.1
 * 2.0.0.7	    CIFMIS      Pritesh Parmar      07/06/2016	    Resolved code review issues, removed unused references 
 * 2.0.0.8	    CIFMIS      Mahesh Deore        04-Apr-2017	    Create new common functions ShowCallerInfoSch(), GetConnectionString(), GetChannel() & GetAllSysParamValues()
 * 2.0.0.9      CIFMIS      Shekhar Chaudhari   01-Jun-2017     Modified logic to record log from file format to EventViewer. 
 *                                                              Changed logic in ShowCallerInfo() method
 *                                                              New Overloding method created ShowCallerInfo() with extra exception parameter,                                                           
 *                                                              WriteToEventLog(), CreateLog() methods added
 * 2.0.0.10     CIFMIS      Mahesh Deore          04/07/2017    Upgrade from 10.1.400.1 to 10.1.600.5
 * 2.0.0.11     CIFMIS       Rajesh               21/Feb/2018      PBID - 16096, Task Id - 16116
 * ------------------------------------------------------------------------------------------------------------------------*/

using Erp;
using Ice;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Configuration;
using System.Data.EntityClient;
using System.Text;

namespace Stcl.Global.GlobalSysInfo
{
	public class GlobalSysFunctions : ContextBoundBase<ErpContext>
    {
        private static Erp.ErpContext EContext = null;
#pragma warning disable CS0618 // Type or member is obsolete
        public GlobalSysFunctions(ErpContext ctx) : base(ctx)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            EContext = ctx; 
        }
        ErpContext Dbctx = new ErpContext();
        #region Variable declaration section

        public static string MacAddrDb = string.Empty, IPAddrDb = string.Empty, TimeZoneDb = string.Empty, DBUserNameDb = string.Empty, DBNameDb = string.Empty, HostNameDb = string.Empty;
		public static DateTime CurrDTDb = DateTime.Now;
        //public static Boolean IsException = false;
        public static Boolean IsActive = true;

        public static string CompanyID;
        public static string UserID;
        public static string SessionID;

        #endregion Variable declaration section

        #region Public Method/Function section
        //delegation to call non static method in static method
        public delegate void NonStaticMethodHandler(out string companyid, out string userid, out string sessionid);       

      	/*Get Device Type*/
		public static string GetDeviceType()
		{
			return "-";
		}

		/*Get Current TimeZone*/
		public static string GetCurrentTimeZone()
		{
			try
			{
				TimeZone LocalZone = TimeZone.CurrentTimeZone;
				return LocalZone.StandardName.ToString();
			}
			catch (Exception ex)
			{
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => GetCurrentTimeZone :", ex);
				throw new BLException("GlobalSysInfo => GetCurrentTimeZone : " + ex.Message);
			}
		}

		/*Get Windows Login User Details*/
		public static string GetWindowsLoginUserName()
		{
			try
			{
				WindowsIdentity wi = WindowsIdentity.GetCurrent();
				string Result = wi.Name;
				return Result;
			}
			catch (Exception ex)
			{
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => GetWindowsLoginUserName : ", ex);
				throw new BLException("GlobalSysInfo => GetWindowsLoginUserName : " + ex.Message);
			}
		}

		/*Get CPU ID*/
		public static string GetCPUID()
		{
			try
			{
				string CpuId = string.Empty;
				ManagementObjectSearcher ObjMos = new ManagementObjectSearcher("Select ProcessorID From Win32_processor");
				ManagementObjectCollection ObjMocList = ObjMos.Get();

				foreach (ManagementObject ObjMo in ObjMocList)
				{
					CpuId = ObjMo["ProcessorID"].ToString();
				}
				return CpuId;
			}
			catch (Exception ex)
			{
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => GetCPUID : ", ex);
				throw new BLException("GlobalSysInfo => GetCPUID : " + ex.Message);
			}
		}

		/*Get IP Address*/
		public static string GetIPAddress()
		{
			try
			{
				string HostName = Dns.GetHostName();
				IPHostEntry IPEntry = Dns.GetHostEntry(HostName);
				foreach (IPAddress IPAdd in IPEntry.AddressList)
				{
					if (IPAdd.AddressFamily.ToString() == "InterNetwork")
					{
						return IPAdd.ToString();
					}
				}
				return "-";
			}
			catch (Exception ex)
			{
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => GetIPAddress : ", ex);
				throw new BLException("GlobalSysInfo => GetIPAddress : " + ex.Message);
			}
		}

		/*Get MAC Address*/
		public static PhysicalAddress GetMacAddress()
		{
			try
			{
				foreach (NetworkInterface NetInt in NetworkInterface.GetAllNetworkInterfaces())
				{
					if (NetInt.NetworkInterfaceType == NetworkInterfaceType.Ethernet && NetInt.OperationalStatus == OperationalStatus.Up)
					{
						return NetInt.GetPhysicalAddress();
					}
				}
				return null;
			}
			catch (Exception ex)
			{
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => GetMacAddress : ", ex);
				throw new BLException("GlobalSysInfo => GetMacAddress : " + ex.Message);
			}
		}

		public static string GetApplicationUserName()
		{
			try
			{
				WindowsIdentity wi = WindowsIdentity.GetCurrent();
				string Result = wi.Name;
				return Result;
			}
			catch (Exception ex)
			{
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => GetApplicationUserName : ", ex);
				throw new BLException("GlobalSysInfo => GetApplicationUserName : " + ex.Message);
			}
		}
                        
		// Write Debug Info to Event Viewer //
        // Methods Used for Log information in Event Viewer//
        // ShowCallerInfo overloading method to work as it is in dlls where it used.
        public static void ShowCallerInfo(string message, [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            try
            {
                GlobalProcedures.GlobalProcedures obj = new GlobalProcedures.GlobalProcedures(EContext);
              
                //Delegation instance for GetSessionDetail method
                NonStaticMethodHandler handler = new NonStaticMethodHandler(obj.GetSessionDetail);
                handler(out CompanyID, out UserID, out SessionID);

                Stcl.Global.GlobalProcedures.GlobalProcedures GlobalProc = new Stcl.Global.GlobalProcedures.GlobalProcedures(EContext);
                string CSharpCodeServerLogSwitch = Convert.ToString(GlobalProc.GetSysParam("CSharpCodeServerLogSwitch"));

                if (CSharpCodeServerLogSwitch.Trim().ToUpper() == "OFF")
                {
                    IsActive = false;
                }

                if (IsActive)
                {
                    StringBuilder LogMessage = new StringBuilder();
                    string Log;
                    string Event;                 

                    StackTrace StackTrace = new System.Diagnostics.StackTrace();
                    StackFrame Frame = StackTrace.GetFrames()[1];
                    MethodInfo Method = (MethodInfo)Frame.GetMethod();
                    string MethodName = Method.Name;                   
                    Type MethodsClass = Method.DeclaringType;
                    string SourceClassName = Convert.ToString(MethodsClass);
                    String DateTimeNow = System.DateTime.Now.ToString();
                    LogMessage.AppendLine("DateTime : " + Convert.ToString(DateTimeNow));
                    LogMessage.Append(" | Company ID : " + Convert.ToString(CompanyID));
                    LogMessage.Append(" | UserID : " + Convert.ToString(UserID));
                    LogMessage.Append(" | SessionID : " + Convert.ToString(SessionID));
                    LogMessage.Append(" | Class File : " + SourceClassName);
                    LogMessage.Append(" | Method Name : " + Convert.ToString(MethodName));
                    LogMessage.AppendLine(" | Line No : " + Convert.ToString(callerLine));
                    LogMessage.AppendLine(" | Message : " + message.ToString());
                                        
                    Log = "EpicorERPLogInfo";
                    Event = "EpicorERPLogInfo";

                    WriteToEventLog(Log, Event, LogMessage.ToString(), "Information", Convert.ToString(MethodsClass));
                }
            }
            catch (Exception ex)
            {
                throw new BLException("GlobalSysInfo => ShowCallerInfo : " + ex.Message);
            }
        }

        //New ShowCallerInfo overloading method with boolean isException parameter to handle exception and information.        
        public static void ShowCallerInfo(string message,Exception ex, [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            try
            {               
                GlobalProcedures.GlobalProcedures objGlob = new GlobalProcedures.GlobalProcedures(EContext);
                
                //Delegation instance for GetSessionDetail method
                NonStaticMethodHandler handler1 = new NonStaticMethodHandler(objGlob.GetSessionDetail);
                handler1(out CompanyID, out UserID, out SessionID);
              
                StringBuilder LogMessage = new StringBuilder();
                string Log;
               
                StackTrace StackTrace = new System.Diagnostics.StackTrace();
                StackFrame Frame = StackTrace.GetFrames()[1];
                MethodInfo Method = (MethodInfo)Frame.GetMethod();
                string MethodName = Method.Name;
                Type MethodsClass = Method.DeclaringType;
                String DateTimeNow = System.DateTime.Now.ToString();
                LogMessage.AppendLine("DateTime : " + Convert.ToString(DateTimeNow));
                LogMessage.Append(" | Company ID : " + Convert.ToString(CompanyID));
                LogMessage.Append(" | UserID : " + Convert.ToString(UserID));
                LogMessage.Append(" | SessionID : " + Convert.ToString(SessionID));
                LogMessage.Append(" | Class File : " + Convert.ToString(MethodsClass));
                LogMessage.Append(" | Method Name : " + Convert.ToString(MethodName));
                LogMessage.AppendLine(" | Line No : " + Convert.ToString(callerLine));
                LogMessage.AppendLine(" | Message : " + message.ToString());

                Log = "EpicorERPLogInfo";
              
                if (ex.InnerException != null)
                {
                    LogMessage.AppendLine("" + System.Environment.NewLine + System.Environment.NewLine +
                        "Error Caught : " + ex.StackTrace + System.Environment.NewLine + System.Environment.NewLine + "## Error Detail ##" +
                        System.Environment.NewLine + "------------------" + System.Environment.NewLine + "------------------" +
                        System.Environment.NewLine + System.Environment.NewLine + ex.Message + System.Environment.NewLine + System.Environment.NewLine +
                        "## Inner Exception ##" + System.Environment.NewLine + "----------------" + System.Environment.NewLine + "----------------"
                        + System.Environment.NewLine + ex.InnerException.Message);
                }
                else
                {
                    LogMessage.AppendLine("" + System.Environment.NewLine + System.Environment.NewLine +
                        "Error Caught : " + ex.StackTrace + System.Environment.NewLine + System.Environment.NewLine + "## Error Detail ##" +
                        System.Environment.NewLine + "------------------" + System.Environment.NewLine + "------------------" +
                        System.Environment.NewLine + System.Environment.NewLine + ex.Message
                        + System.Environment.NewLine + System.Environment.NewLine + "## Source ##"
                         + System.Environment.NewLine
                        + "------------------"
                         + System.Environment.NewLine + "------------------" +
                        System.Environment.NewLine + ex.Source);
                }

                WriteToEventLog(Log, "Application Error ", LogMessage.ToString(), "Error", Convert.ToString(MethodsClass));

            }
            catch (Exception exception)
            {
                throw new BLException("GlobalSysInfo => ShowCallerInfo : " + exception.Message);
            }
        }
        
        public static void WriteToEventLog(string strLogName, string strSource, string strErrDetail, string type,string methodsClass)
        {
            System.Diagnostics.EventLog SQLEventLog = new System.Diagnostics.EventLog();
            int EventID = 0;
            try
            {
                if (!System.Diagnostics.EventLog.SourceExists(strLogName))
                {
                    CreateLog(strLogName);                   
                }

                SQLEventLog.Log = strLogName;

                if (!string.IsNullOrEmpty(methodsClass))
                {
                    SQLEventLog.Source = methodsClass;
                }
                else
                {
                    SQLEventLog.Source = strLogName;
                }                        
 
                if (type == "Information")
                {                   
                    SQLEventLog.WriteEntry(Convert.ToString(strSource) + Environment.NewLine
                                          + Convert.ToString(strErrDetail), EventLogEntryType.Information, EventID);                    
                }
                else
                {                    
                    SQLEventLog.WriteEntry(Convert.ToString(strSource) + Environment.NewLine
                                          + Convert.ToString(strErrDetail), EventLogEntryType.Error, EventID);                    
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(methodsClass))
                {
                    SQLEventLog.Source = methodsClass;
                }
                else
                {
                    SQLEventLog.Source = "EpicorERPLogException";
                }
                SQLEventLog.WriteEntry(Convert.ToString("INFORMATION: ") + Environment.NewLine
                                      + Convert.ToString(ex.Message), EventLogEntryType.Error);
            }
            finally
            {
                SQLEventLog.Dispose();
                SQLEventLog = null;
            }
        }

        public static bool CreateLog(string strLogName)
        {            
            bool Reasult = false;

            try
            {                
                System.Diagnostics.EventLog.CreateEventSource(strLogName, strLogName);
                System.Diagnostics.EventLog SQLEventLog = new System.Diagnostics.EventLog();

                SQLEventLog.Source = strLogName;
                SQLEventLog.Log = strLogName;
                SQLEventLog.Source = strLogName;
                
                Reasult = true;
            }
            catch
            {
                Reasult = false;
            }

            return Reasult;
        }
        // Methods called for Log information in Event Viewer//     

      
        //below function will used into Stcl Schedulers to write log
        public static void ShowCallerInfoSch(bool IsFirstIteration, string message, [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            try
            {
                string FileName = string.Empty;
                if (string.IsNullOrEmpty(callerName))
                {
                    throw new BLException("GlobalSysInfo => ShowCallerInfoSch : Caller file name is undefined.");
                }
                else
                {
                    FileName = callerFilePath + ".txt";
                    if (!System.IO.Directory.Exists(Path.GetDirectoryName(FileName)))
                    {
                        System.IO.Directory.CreateDirectory(FileName);
                    }
                }

                if (!System.IO.File.Exists(FileName))
                {
                    System.IO.File.Create(FileName).Dispose();
                }

                using (StreamWriter Writer = new StreamWriter(FileName, true))
                {
                    StackTrace StackTrace = new System.Diagnostics.StackTrace();
                    StackFrame Frame = StackTrace.GetFrames()[1];
                    MethodInfo Method = (MethodInfo)Frame.GetMethod();
                    string MethodName = Method.Name;
                    Type MethodsClass = Method.DeclaringType;
                    String DateTimeNow = System.DateTime.Now.ToString();
                    Writer.WriteLine();
                    if (IsFirstIteration == true)
                    {
                        Writer.WriteLine("Scheduler starts at DateTime : {0}", DateTimeNow);
                        Writer.Write("Class File : {0}", MethodsClass);
                        Writer.Write(" | Method Name : {0}", MethodName);
                    }
                    Writer.Write(message.ToString());
                    Writer.WriteLine();
                }
            }
            catch (Exception ex)
            {                
                throw new BLException("GlobalSysInfo => ShowCallerInfo : " + ex.Message);
            }
        }

        //below function will used to get connection string & App server Url from specified web config file path
        public static Tuple<string, string> GetConnectionString(string configFile)
        {

            string connectString = string.Empty;
            string EpicorConnection = string.Empty;
            string AppSrvUrl = string.Empty;

            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap() { ExeConfigFilename = configFile };
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            connectString = config.ConnectionStrings.ConnectionStrings["ErpContext"].ConnectionString;
            EntityConnectionStringBuilder builder = new EntityConnectionStringBuilder(connectString);

            SqlConnectionStringBuilder Build = new SqlConnectionStringBuilder(builder.ProviderConnectionString);
            Build.DataSource = System.Configuration.ConfigurationManager.AppSettings["AppServerName"].ToString();
            EpicorConnection = Build.ConnectionString;
            AppSrvUrl = @"net.tcp://" + System.Configuration.ConfigurationManager.AppSettings["AppServerName"].ToString() + @"/" + configFile.Split('\\')[configFile.Split('\\').Length - 3];
            return Tuple.Create(EpicorConnection, AppSrvUrl);//Tuple will return two string
        }

        //below function will used to get channel/EndPointBinding value from specified web config file path
        public static string GetChannel(string configFile)
        {
            DirectoryInfo di = new DirectoryInfo(configFile.Remove(configFile.Length - 10));
            FileInfo[] rgFiles = di.GetFiles("web.config");
            SqlCommand CmdXMLFileData = new SqlCommand();
            SqlDataAdapter DaXmlFileData = new SqlDataAdapter(CmdXMLFileData);
            DataSet DsXMLFileData = new DataSet();
            DataView dv = new DataView();
            string EndpointBinding = string.Empty;
            try
            {
                foreach (FileInfo fi in rgFiles)
                {
                    DsXMLFileData.ReadXml(fi.FullName);
                    if (DsXMLFileData.Tables.Count > 0)
                    {
                        if (DsXMLFileData.Tables.Contains("Add"))
                        {
                            DsXMLFileData.Tables["Add"].DefaultView.RowFilter = "BindingConfiguration <> '' AND scheme = 'net.tcp' AND binding='customBinding' ";
                            DataTable dt = (DsXMLFileData.Tables["Add"].DefaultView).ToTable();
                            if (dt.Rows.Count > 0)
                            {
                                if (Convert.ToString(dt.Rows[0]["bindingconfiguration"]) == "TcpCompressedUsernameWindowsChannel")
                                {
                                    EndpointBinding = "UsernameWindowsChannel";
                                }
                                else if (Convert.ToString(dt.Rows[0]["bindingconfiguration"]) == "TcpCompressedWindows")
                                {
                                    EndpointBinding = "Windows";
                                }
                                else
                                {
                                    ShowCallerInfo("Known bindingconfiguration not found, Please contact Administrator");
                                }
                            }
                            else
                            {
                                ShowCallerInfo("BindingConfiguration section not found, Please contact Administrator");
                            }
                        }
                    }
                }
                return EndpointBinding;
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => GetChannel : ", ex);
                throw new BLException("GlobalSysInfo => GetChannel : " + ex.Message);
            }
            finally
            {
                DsXMLFileData.Dispose();
                dv.Dispose();
            }
        }

        //below function will used to get all specified sysParam values in dataset format
        public static DataSet GetAllSysParamValues(string ErpConStr, string code)
        {
            try
            {
                SqlConnection Con = new SqlConnection(ErpConStr);
                SqlCommand Cmd = new SqlCommand();
                if (Con.State == ConnectionState.Closed)
                {
                    Con.Open(); 
                }
                Cmd.CommandText = "Stcl_CIFMIS_Global_GetSysParamData";
                Cmd.CommandType = CommandType.StoredProcedure;
                Cmd.Connection = Con;
                Cmd.Parameters.AddWithValue("@code", code);
                SqlDataAdapter Da = new SqlDataAdapter(Cmd);
                DataSet Ds = new DataSet();
                Da.Fill(Ds);
                if (Con.State == ConnectionState.Open)
                {
                    Con.Close();
                }
                if (Ds != null)
                {
                    if (Ds.Tables.Count > 0)
                    {
                        if (Ds.Tables[0].Rows.Count > 0)
                        {
                            return Ds;
                        }
                        else
                        {
                            GlobalSysFunctions.ShowCallerInfo("Stcl.Global.GlobalProcedures => GetAllSysParamValues(): Ds.Table[0].Rows.Count = 0");
                            return Ds;
                        }
                    }
                    else
                    {
                        GlobalSysFunctions.ShowCallerInfo("Stcl.Global.GlobalProcedures => GetAllSysParamValues(): Ds.Tables.Count = 0 (might be sp or table objects not there)");
                        return Ds;
                    }
                }
                else
                {
                    GlobalSysFunctions.ShowCallerInfo("Stcl.Global.GlobalProcedures => GetAllSysParamValues(): Ds = null (might be sp or table objects not there)");
                    return Ds;
                }
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => GetAllSysParamValues : ", ex);
                throw new BLException("GlobalSysInfo => GetAllSysParamValues : " + ex.Message);
            }
        }

		/* Following Function called within ShowCallerInfo method */	

		public static DateTime GetApplicationDateTime()
		{
			return DateTime.Now;
		}

		public static DateTime GetCreatedDateTime()
		{
			return DateTime.Now;
		}

		public static DateTime GetDatabaseDateTime()
		{
			return DateTime.Now;
		}

		public static string GetDatabaseUserName()
		{
			return "XYZ";
		}

		public static string GetTransactionAction()
		{
			return "Update";
		}

		public static DateTime GetWindowsDateTime()
		{
			return DateTime.Now;
		}

		/*Get MotherBoardId */
		public static string GetMotherBoardID()
		{
			try
			{
				////ManagementObjectCollection mbCol = new ManagementClass("Win32_BaseBoard").GetInstances();
				ManagementObjectCollection mbCol = new ManagementClass("Win32_BIOS").GetInstances();
				//Enumerating the list
				ManagementObjectCollection.ManagementObjectEnumerator mbEnum = mbCol.GetEnumerator();
				//Move the cursor to the first element of the list (and most probably the only one)
				mbEnum.MoveNext();
				//Getting the serial number of that specific motherboard
				return ((ManagementObject)(mbEnum.Current)).Properties["SerialNumber"].Value.ToString();
			}
			catch (Exception ex)
			{
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => GetMotherBoardID : ", ex);
				throw new BLException("GlobalSysInfo => GetMotherBoardID : " + ex.Message);
			}
		}

		/*Get MAC Address added by IP Address [Mahesh]*/
		public static string GetMacAddressFromIP(string ipAddress)
		{
			string macAddress = string.Empty;
			if (!IsHostAccessible(ipAddress)) return null;
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo();
				Process process = new Process();
				processStartInfo.FileName = "nbtstat";
				processStartInfo.RedirectStandardInput = false;
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.Arguments = "-a " + ipAddress;
				processStartInfo.UseShellExecute = false;
				process = Process.Start(processStartInfo);

				int Counter = -1;

				while (Counter <= -1)
				{
					Counter = macAddress.Trim().ToLower().IndexOf("mac address", 0);
					if (Counter > -1)
					{
						break;
					}
					macAddress = process.StandardOutput.ReadLine();
				}
				process.WaitForExit();
				macAddress = macAddress.Trim();
			}
			catch (Exception e)
			{
                GlobalSysFunctions.ShowCallerInfo("GetMacAddressFromIP > Failed because : ", e);
                Console.WriteLine("GetMacAddressFromIP > Failed because : " + e.ToString());
			}
			return macAddress;
		}

		
		#region added by Pankaj  on 12-10-2015 for System Audit Log
		public static DataTable GetSysAuditInfo()
		{
			try
			{
				DataTable DtSysInfo = new DataTable();
				DtSysInfo.Columns.Add("ApplicationDateTime_c", System.Type.GetType("System.DateTime"));
				DtSysInfo.Columns.Add("ApplicationIPAddress_c", System.Type.GetType("System.String"));
				DtSysInfo.Columns.Add("ApplicationMACAddress_c", System.Type.GetType("System.String"));
				DtSysInfo.Columns.Add("ApplicationTimeZone_c", System.Type.GetType("System.String"));
				DtSysInfo.Columns.Add("ApplicationMotherBoardId_c", System.Type.GetType("System.String"));
				DtSysInfo.Columns.Add("DatabaseDateTime_c", System.Type.GetType("System.DateTime"));
				DtSysInfo.Columns.Add("DatabaseIPAddress_c", System.Type.GetType("System.String"));
				DtSysInfo.Columns.Add("DatabaseMACAddress_c", System.Type.GetType("System.String"));
				DtSysInfo.Columns.Add("DatabaseTimeZone_c", System.Type.GetType("System.String"));
				DtSysInfo.Columns.Add("DatabaseUserName_c", System.Type.GetType("System.String"));
				DtSysInfo.Columns.Add("DatabaseName_c", System.Type.GetType("System.String"));
				DtSysInfo.Columns.Add("DatabaseHostName_c", System.Type.GetType("System.String"));
				GlobalSysFunctions ObjSys = new GlobalSysFunctions(EContext);

                //Keep below commented code as per discussion of Mahesh Deore 01-June-2017.
				////ObjSys.ExecSysAuditLogProc();
                ////DtSysInfo.Rows.Add(
                ////    GlobalSysFunctions.GetApplicationDateTime(),                // ApplicationDateTime_c
					
                ////    GlobalSysFunctions.GetIPAddress(),                          // ApplicationIPAddress_c
                ////    Convert.ToString(GlobalSysFunctions.GetMacAddress()),       // ApplicationMACAddress_c
                ////    GlobalSysFunctions.GetCurrentTimeZone(),                    // ApplicationTimeZone_c 
                ////    Convert.ToString(GlobalSysFunctions.GetMotherBoardID()),    // ApplicationMotherBoardId_c     
                ////    Convert.ToDateTime(CurrDTDb),                                                  // DatabaseDateTime_c
                ////    IPAddrDb,                                                  // DatabaseIPAddress_c
                ////    //GlobalSysFunctions.GetMacAddressFromIP(IPAddrDb).Replace("MAC Address = ", ""),
                ////    //Update By Shweta Parashar on 12-Jan-2016 ,given Error while  Mac Address is blank
                ////    string.IsNullOrEmpty(GlobalSysFunctions.GetMacAddressFromIP(IPAddrDb)) ? "" : GlobalSysFunctions.GetMacAddressFromIP(IPAddrDb).Replace("MAC Address = ", ""), // DatabaseMACAddress_c
                ////    TimeZoneDb,
                ////    DBUserNameDb,
                ////    DBNameDb,
                ////    HostNameDb
                ////    );
                DtSysInfo.Rows.Add(DateTime.Now.ToString(), "localhost", "-", "-", "-", DateTime.Now, "-", "-", "-", "-", "-", "-");

				return DtSysInfo;
			}
			catch (Exception ex)
			{
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => GetSysAuditInfo : ", ex);          
				throw new BLException("GlobalSysInfo => GetSysAuditInfo : " + ex.Message);
			}
		}

		public void ExecSysAuditLogProc()
		{
			try
			{
                foreach (var dbInfo in Dbctx.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_GetDBAuditInfo"))
                {
                    MacAddrDb = dbInfo.MacAddress.ToString();
                    IPAddrDb = !string.IsNullOrEmpty(dbInfo.ClientNetAddress) ? dbInfo.ClientNetAddress.ToString() : "";
                    TimeZoneDb = dbInfo.TimeZone.ToString();
                    CurrDTDb = dbInfo.CurrDateTime;
                    DBUserNameDb = dbInfo.DBUserName.ToString();
                    DBNameDb = dbInfo.DataBaseName.ToString();
                    HostNameDb = dbInfo.HostName.ToString();
                }
			}
			catch (Exception ex)
			{
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => ExecSysAuditLogProc : ", ex);          
				throw new BLException("GlobalSysInfo => ExecSysAuditLogProc : " + ex.Message);
			}

		}
		#endregion added by Pankaj  on 12-10-2015 for System Audit Log

		#region added by Shweta  on 11-01-2016 for Validate Special Character
		public static bool ValidateSpecialCharacter(string Value)
		{
			try
			{
				foreach (Char character in Value)
				{
					if (!Char.IsLetterOrDigit(character) && !Char.IsWhiteSpace(character))
					{
						Value = Convert.ToString(Value).Replace(Convert.ToString(character), "");
						return false;
					}
				}
				return true;
			}
			catch (Exception ex)
			{
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => ValidateSpecialCharacter : ", ex);      
				throw new BLException("GlobalSysInfo => ValidateSpecialCharacter : " + ex.Message);
			}

		}
        #endregion Ended by Shweta  on 11-01-2016 for Validate Special Character


        public static DataTable ConvertListToDataTable(List<dynamic> list)
        {
            DataTable dt = new DataTable();

            int i = 0;
            List<object> li = new List<object>();

            foreach (var data in list)
            {
                string CheckColName = string.Empty; //logically used to know the row is changed or not for column

                if (i == 0)
                {
                    string[] str = data.ToString().Replace("{", "").Replace("}", "").Split(',');

                    foreach (string col in str)
                    {
                        // adding columns to data table  
                        string colname = col.Substring(0, col.IndexOf("="));
                        string dataValue = col.Substring(col.IndexOf("=") + 1);

                        dt.Columns.Add(colname);

                        if (string.IsNullOrEmpty(CheckColName))
                        {
                            dt.Rows.Add(); //Add row first time
                            CheckColName = colname;
                        }

                        dt.Rows[0][colname] = dataValue;
                    }
                }
                else
                {
                    string[] str = data.ToString().Replace("{", "").Replace("}", "").Split(',');
                    foreach (string col in str)
                    {
                        string colname = col.Substring(0, col.IndexOf("="));
                        string dataValue = col.Substring(col.IndexOf("=") + 1);

                        if (string.IsNullOrEmpty(CheckColName))
                        {
                            dt.Rows.Add(); //Add row when i get increment
                            CheckColName = colname;
                        }
                        dt.Rows[i][colname] = dataValue;
                    }
                }
                CheckColName = string.Empty;
                i++;
            }
            return dt;
        }
        #endregion Public Method/Function section

        #region Private Method/Function section

        private const int PING_TIMEOUT = 1000;
        private static bool IsHostAccessible(string hostNameOrAddress)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(hostNameOrAddress, PING_TIMEOUT);
                return reply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => IsHostAccessible : ", ex);   
                throw new BLException("GlobalSysInfo => IsHostAccessible : " + ex.Message);
            }
        }

        private static IDictionary<string, object> GetParametersFromPreviousMethodCall()
        {
            try
            {
                var StackTrace = new StackTrace();
                var Frame = StackTrace.GetFrame(2);
                var Method = Frame.GetMethod();

                var Dictionary = new Dictionary<string, object>();
                foreach (var ParameterInfo in Method.GetParameters())
                    Dictionary.Add(ParameterInfo.Name, ParameterInfo.DefaultValue);
                return Dictionary;
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalSysInfo => IDictionary : ", ex);   
                throw new BLException("GlobalSysInfo => IDictionary : " + ex.Message);
            }
        }
        
        #endregion Private Method/Function Section
    }
}
