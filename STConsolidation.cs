using Epicor.ServiceModel.Channels;
using Erp.BO;
using Erp.Proxy.BO;
using Ice.Proxy.Lib;
using Stcl.CIFMIS.GetPaymentReference;
using Stcl.Global.GlobalSysInfo;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;

/**
 * Version		    Project     Author			    Date		    Purpose
 * 2.0.0.0		    CIFMIS      Pritesh Parmar		01/02/2017		Process for creating journals for ST Consolidation
 * 2.0.0.1          CIFMIS      Mahesh Deore        06/07/2017		upgrade to 10.1.400.1 to 10.1.600.5
 * 2.0.0.2          CIFMIS      Pritesh Parmar      28/08/2017		Inplement SSO & login functionality(security issue) as per Epicor configuration 
 * 2.0.0.3          CIFMIS      Pritesh Parmar      16/08/2018      17830 - MOFTZ - Show Stopper - ST Consolidation Entry not generating even after scheduler run
 * 2.0.0.4          CIFMIS      Amod Loharkar       11/12/2018		PBID 17766/18976, added SiteID filteration logic for Cost Center Segregation for MOFKL.
 * 2.0.0.5          CIFMIS      Pritesh Parmar      04/02/2019      21382 - SA/ST Consolidation to Parent Ministry
 * 2.0.0.6          CIFMIS      Pritesh Parmar      14/02/2019		21851 - ST Consolidation performance testing with more then 100 lines 
 * 2.0.0.7          CIFMIS      Juhi Kanojia        02/03/2020		25838 - St Consolidation - Scheduler for ST consolidation Failing
 * 2.0.0.8          CIFMIS      Juhi Kanojia        12/03/2020		29174 - ST Consolidation is creating partial entry in parent Ministry
 */

namespace Stcl.Scheduler.STConsolidation
{
    public class STConsolidation
    {
        #region "static variables declaration"
        static string SchedulerSwitch = string.Empty;
        static string EpicorUserID = string.Empty;
        static string EpiorUserPassword = string.Empty;
        static string AppSrvUrl = string.Empty; //This should be the url to your appserver
        static string EndpointBinding = string.Empty; //This is case sensitive. Valid values are "UsernameWindowsChannel", "Windows" and "UsernameSslChannel"
        static string StclServerLogPath = string.Empty;
        static string TreasuryCompany = string.Empty;
        static string ISSTAllocApplicable = string.Empty;
        static string IsSiteIDFilterApplicable = string.Empty;

        static string ConfigFile = "\\\\" + Convert.ToString(System.Configuration.ConfigurationManager.AppSettings["AppServerName"]) + @"\c$\" + Convert.ToString(System.Configuration.ConfigurationManager.AppSettings["ServerConfigFilePath"]);
        static StringBuilder LogMsg = new StringBuilder();
        static string CurrentFile = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
        static string EpicorConnection = string.Empty;
        static SqlConnection SqlCon = new SqlConnection();
        #endregion "static variables declaration"

        #region Main method
        static void Main(string[] args)
        {
            #region "executing config section"
            Guid SessionId = Guid.Empty;
            var ConnDet = GlobalSysFunctions.GetConnectionString(ConfigFile);
            EpicorConnection = ConnDet.Item1;
            AppSrvUrl = ConnDet.Item2;
            EndpointBinding = GlobalSysFunctions.GetChannel(ConfigFile);
            SqlCon = new SqlConnection(Convert.ToString(EpicorConnection));
            #endregion "executing config section"

            CurrentFile = Path.GetFileNameWithoutExtension(CurrentFile);
            AssignValues();

            if (ISSTAllocApplicable == "FALSE")
            {
                LogMsg.AppendLine(Environment.NewLine + "Sub Treasury Allocation is not applicable for this client, Please varify Stcl_Sysparam ISSTAllocApplicable");
                GlobalSysFunctions.ShowCallerInfoSch(false, Convert.ToString(LogMsg), CurrentFile, StclServerLogPath);
                LogMsg.Clear();
                return;
            }

            string CompanyName, PlantID, PlantName, WorkstationID, WorkstationDesc, EmployeeID, CountryGroupCode, CountryCode, TenantID;
            string AllocationLegalNumberID = string.Empty;
            string SqlStr = string.Empty;

            DataTable DtLegalNumCnfg = new DataTable();
            SessionModImpl SessionModImpl = CreateBusObj<SessionModImpl>(Guid.Empty, SessionModImpl.UriPath);
            LogMsg.AppendLine(" ******************************************");
            try
            {
                Guid sessionId = SessionModImpl.Login();
                SessionModImpl.SessionID = sessionId;
                LogMsg.AppendLine("SessionModImpl.Login / Set current SessionId to SessionModImpl.SessionID Success, DateTime: " + DateTime.Now);

                //To check sub treasury company only, Do not consolidate foreign mission transactions  
                string Sql2 = "SELECT key1 " +
                        " FROM Ice.UD01  WITH (NOLOCK) " +
                        " WHERE Company = '" + TreasuryCompany + "' " +
                        " AND CheckBox01 = 1 " +            //CheckBox01 = IsSubTreasury Flag
                        " AND CheckBox02 = 0 " +            //CheckBox02 = IsRAS Flag       //CheckBox03 = Active Flag
                        " AND CheckBox03 = 1 ";             //CheckBox04 = ST Consolidation Process happned only for checked sub treasry
                DataTable dtSTCompList = new DataTable();
                dtSTCompList.Reset();
                dtSTCompList = GetDataSet(Sql2).Tables[0];
                LogMsg.AppendLine("Total of Active Sub Treasury Company : " + dtSTCompList.Rows.Count);

                for (int STComp = 0; STComp < dtSTCompList.Rows.Count; STComp++)
                {
                    string STCompCode = Convert.ToString(dtSTCompList.Rows[STComp][0]);
                    LogMsg.AppendLine("Processing Sub Treasury : " + STCompCode + " -------------------------");

                    DataSet PendingSTConsolidations = new DataSet();
                    PendingSTConsolidations.Reset();
                    PendingSTConsolidations = GetPendingSTConsolidation(STCompCode);

                    DataTable dtGrpHed = new DataTable();
                    dtGrpHed.Reset();
                    dtGrpHed = PendingSTConsolidations.Tables[0];

                    DataTable dtDetail = new DataTable();
                    dtDetail.Reset();
                    dtDetail = PendingSTConsolidations.Tables[1];
                    LogMsg.AppendLine("No Of Rows in dtGrpHed : " + dtGrpHed.Rows.Count + ",       dtDetail : " + dtDetail.Rows.Count);

                    for (int GrpHed = 0; GrpHed < dtGrpHed.Rows.Count; GrpHed++)
                    {
                        //Wait 2 Sec
                        if (GrpHed > 0)
                        {
                            System.Threading.Thread.Sleep(2000);
                        }

                        LogMsg.AppendLine("Processing Group Row : " + (GrpHed + 1) + "/" + dtGrpHed.Rows.Count + " -----------------------");

                        string Company = Convert.ToString(dtGrpHed.Rows[GrpHed]["Company"]);
                        string GroupID = Convert.ToString(dtGrpHed.Rows[GrpHed]["GroupID"]);
                        string BookID = Convert.ToString(dtGrpHed.Rows[GrpHed]["BookID"]);
                        string JournalCode = Convert.ToString(dtGrpHed.Rows[GrpHed]["JournalCode"]);
                        Int32 RunNbr = Convert.ToInt32(dtGrpHed.Rows[GrpHed]["RunNbr"]);
                        string VoteCode = Convert.ToString(dtGrpHed.Rows[GrpHed]["VoteCode"]);
                        string ConsId = Convert.ToString(dtGrpHed.Rows[GrpHed]["ConsId"]);
                        Int32 ConsNo = Convert.ToInt32(dtGrpHed.Rows[GrpHed]["ConsNo"]);
                        string Description = Convert.ToString(dtGrpHed.Rows[GrpHed]["Description"]);
                        string CurrencyCode = Convert.ToString(dtGrpHed.Rows[GrpHed]["CurrencyCode"]);
                        string SiteID = Convert.ToString(dtGrpHed.Rows[GrpHed]["SiteID"]);
                        LogMsg.AppendLine("ST Company : " + STCompCode + ", MDA Company : " + Company + ",   GroupID : " + GroupID + ",   BookID : " + BookID + ",   RunNbr : " + RunNbr + ",   VoteCode : " + VoteCode + ",   JournalCode : " + JournalCode + ",  SiteID : " + SiteID);

                        string expresion = "Company = '" + Company + "' AND GroupID = '" + GroupID + "'";
                        DataRow[] foundRows = dtDetail.Select(expresion);
                        LogMsg.AppendLine("foundRows in detail table : " + foundRows.Length + " for the Company : " + Company + " GroupID : " + GroupID);
                        expresion = string.Empty;

                        DataTable tmpdt = new DataTable();
                        tmpdt.Reset();
                        tmpdt = foundRows.CopyToDataTable();
                        foundRows = null;

                        DateTime JEDate = Convert.ToDateTime(tmpdt.Rows[0]["JEDate"]);

                        #region GLJrnGrp
                        GLJrnGrpDataSet DsGLJrnGrp = new GLJrnGrpDataSet();
                        GLJrnGrpImpl GLJrnGrpImpls = CreateBusObj<GLJrnGrpImpl>(sessionId, GLJrnGrpImpl.UriPath);
                        Int32 FiscalYear = 0;
                        string FiscalYearSuffix = string.Empty;
                        Int32 FiscalPeriod = 0;
                        string FiscalCalendarID = string.Empty;
                        string Msg = string.Empty;
                        bool IsGroupExist = false;
                        bool IsHeaderExist = false;

                        try
                        {
                            SessionModImpl.SetCompany(Company, out CompanyName, out PlantID, out PlantName, out WorkstationID, out WorkstationDesc, out EmployeeID, out CountryGroupCode, out CountryCode, out TenantID);
                            LogMsg.AppendLine("GLJrnGrpImpls, CurrentSession.CompanyID : " + Company);

                            if (IsSiteIDFilterApplicable == "TRUE")
                            {
                                string SiteName = string.Empty;
                                SessionModImpl.SetPlant(SiteID, out SiteName);
                                LogMsg.AppendLine("SessionModImpl.SetPlant, SiteID : " + SiteID + ", SiteName : " + SiteName);
                            }

                            GetFiscalPeriod(Company, BookID, JEDate, out FiscalYear, out FiscalYearSuffix, out FiscalPeriod, out FiscalCalendarID);
                            LogMsg.AppendLine("GetFiscalPeriod Success, FiscalYear : " + FiscalYear + "  FiscalYearSuffix : " + FiscalYearSuffix + "  FiscalPeriod : " + FiscalPeriod + "  FiscalCalendarID : " + FiscalCalendarID);

                            if (FiscalYear == 0 || FiscalPeriod == 0 || FiscalCalendarID == string.Empty)
                            {
                                LogMsg.AppendLine("FiscalYear OR FiscalPeriod OR FiscalCalendarID Not Found In Current Compnay : " + Company);
                                continue;
                            }

                            bool MorePage = false;
                            string WhereCls = " Company = '" + Company + "' AND GroupID = '" + GroupID + "'";
                            DsGLJrnGrp = GLJrnGrpImpls.GetRows(WhereCls, 0, 0, out MorePage);
                            LogMsg.AppendLine("GLJrnGrpImpls.GetRows Success, DsGLJrnGrp.GLJrnGrp.Rows.Count : " + DsGLJrnGrp.GLJrnGrp.Rows.Count);
                            if (DsGLJrnGrp.GLJrnGrp.Rows.Count > 0)
                            {
                                IsGroupExist = true;
                                LogMsg.AppendLine("GLJrnGrpImpls.GetRows Success, GroupID Exists, Company : " + Company + "  GroupID " + GroupID);
                            }

                            if (IsGroupExist == false)
                            {
                                GLJrnGrpImpls.GetNewGLJrnGrp(DsGLJrnGrp);
                                string GrpCmp = DsGLJrnGrp.GLJrnGrp[0].Company;
                                LogMsg.AppendLine("GLJrnGrpImpls.GetNewGLJrnGrp Success @ Company : " + GrpCmp);

                                if (GrpCmp != Company)
                                {
                                    LogMsg.AppendLine("Current Company is " + Company + ", But Group created in " + GrpCmp + ", Which is invalid");
                                    return;
                                }

                                DsGLJrnGrp.GLJrnGrp[0].GroupID = GroupID;
                                DsGLJrnGrp.GLJrnGrp[0].FiscalYear = FiscalYear;
                                DsGLJrnGrp.GLJrnGrp[0].FiscalPeriod = FiscalPeriod;
                                DsGLJrnGrp.GLJrnGrp[0].FiscalYearSuffix = FiscalYearSuffix;
                                DsGLJrnGrp.GLJrnGrp[0].FiscalCalendarID = FiscalCalendarID;
                                DsGLJrnGrp.GLJrnGrp[0].JEDate = JEDate;
                                DsGLJrnGrp.GLJrnGrp[0].JournalCode = JournalCode;
                                DsGLJrnGrp.GLJrnGrp[0]["STConsNo_c"] = RunNbr;
                                DsGLJrnGrp.GLJrnGrp[0]["STCompany_c"] = STCompCode;
                                DsGLJrnGrp.GLJrnGrp[0]["SiteID_c"] = SiteID;

                                GLJrnGrpImpls.CheckFiscalYear(DsGLJrnGrp, GroupID, JEDate, out Msg);
                                LogMsg.AppendLine("GLJrnGrpImpls.CheckFiscalYear Success");

                                GLJrnGrpImpls.ChangeEntryMode("S", DsGLJrnGrp);
                                LogMsg.AppendLine("GLJrnGrpImpls.ChangeEntryMode Success");

                                GLJrnGrpImpls.ChangeBookID(BookID, DsGLJrnGrp);
                                LogMsg.AppendLine("GLJrnGrpImpls.ChangeBookID Success");

                                GLJrnGrpImpls.Update(DsGLJrnGrp);
                                LogMsg.AppendLine("GLJrnGrpImpls.Update Success");
                            }
                        }
                        catch (Exception ex)
                        {
                            //Record error
                            RecordErrorLog("GLJrnGrp", ex.Message.ToString(), Company, FiscalPeriod, FiscalPeriod, RunNbr);
                            LogMsg.AppendLine("GLJrnGrpImpls > GLJrnGrp Update Failed > " + ex.Message.ToString());
                            continue;
                        }
                        #endregion

                        #region GLJrnHed
                        GLJournalEntryDataSet DsGLJournalEntry = new GLJournalEntryDataSet();
                        GLJournalEntryImpl GLJournalEntryImpls = CreateBusObj<GLJournalEntryImpl>(sessionId, GLJournalEntryImpl.UriPath);
                        LogMsg.AppendLine("GLJournalEntry.GLJrnHed Success");

                        try
                        {
                            bool RequiredUserInput = false;
                            bool MorePage = false;

                            string WhereCls = " Company = '" + Company + "' AND GroupID = '" + GroupID + "' AND Posted = 0";
                            DsGLJournalEntry = GLJournalEntryImpls.GetRows(WhereCls, "", "", "", "", "", "", "", 0, 0, out MorePage);
                            LogMsg.AppendLine("GLJournalEntry.GetRows Success, DsGLJournalEntry.GLJrnHed.Rows.Count : " + DsGLJournalEntry.GLJrnHed.Rows.Count);
                            if (DsGLJournalEntry.GLJrnHed.Rows.Count > 0)
                            {
                                LogMsg.AppendLine("GLJournalEntry.GetRows Success, GLJrnHed Exists, Company : " + Company + "  GroupID " + GroupID);
                                IsHeaderExist = true;
                            }

                            if (IsHeaderExist == false)
                            {
                                GLJournalEntryImpls.GetNewGlJrnHedTran(DsGLJournalEntry, GroupID);
                                LogMsg.AppendLine("GLJournalEntry.GetNewGlJrnHedTran Success");

                                DsGLJournalEntry.GLJrnHed[0].FiscalYear = FiscalYear;
                                DsGLJournalEntry.GLJrnHed[0].JournalCode = JournalCode;
                                DsGLJournalEntry.GLJrnHed[0].FiscalPeriod = FiscalPeriod;
                                DsGLJournalEntry.GLJrnHed[0].Description = Description;
                                DsGLJournalEntry.GLJrnHed[0].JEDate = JEDate;
                                DsGLJournalEntry.GLJrnHed[0].FiscalCalendarID = FiscalCalendarID;
                                DsGLJournalEntry.GLJrnHed[0].CurrencyCode = CurrencyCode;
                                DsGLJournalEntry.GLJrnHed[0].FiscalYearSuffix = FiscalYearSuffix;
                                DsGLJournalEntry.GLJrnHed[0].CommentText = Description;
                                DsGLJournalEntry.GLJrnHed[0]["STConsNo_c"] = RunNbr;
                                DsGLJournalEntry.GLJrnHed[0]["STCompany_c"] = STCompCode;
                                DsGLJournalEntry.GLJrnHed[0]["IsSysGenerated_c"] = false;
                                DsGLJournalEntry.GLJrnHed[0]["Submitted4Appr_c"] = false;
                                DsGLJournalEntry.GLJrnHed[0]["Approved_c"] = false;
                                DsGLJournalEntry.GLJrnHed[0]["SiteID_c"] = SiteID;

                                GLJournalEntryImpls.PreUpdate(DsGLJournalEntry, out RequiredUserInput);
                                LogMsg.AppendLine("GLJournalEntry.GLJrnHed.PreUpdate Success");

                                GLJournalEntryImpls.Update(DsGLJournalEntry);
                                LogMsg.AppendLine("GLJournalEntry.GLJrnHed.Update Success");
                            }
                        }
                        catch (Exception ex)
                        {
                            //Record error
                            RecordErrorLog("GLJrnHed", ex.Message.ToString(), Company, FiscalPeriod, FiscalPeriod, RunNbr);
                            LogMsg.AppendLine("DsGLJournalEntry.GLJrnHed > Update Failed > " + ex.Message.ToString());

                            GLJrnGrpDataSet DsGLJrnGrpDelete = new GLJrnGrpDataSet();
                            GLJrnGrpImpl GLJrnGrpImplsDelete = CreateBusObj<GLJrnGrpImpl>(sessionId, GLJrnGrpImpl.UriPath);

                            DsGLJrnGrpDelete = GLJrnGrpImplsDelete.GetByID(GroupID);
                            LogMsg.AppendLine("GLJrnGrpDelete > GetByID Success, Company : " + Company + " GroupID : " + GroupID);

                            GLJrnGrpImplsDelete.CheckDocumentIsLocked(GroupID);
                            LogMsg.AppendLine("GLJrnGrpDelete > CheckDocumentIsLocked Success, Company : " + Company + " GroupID : " + GroupID);

                            DsGLJrnGrpDelete.GLJrnGrp[0].RowMod = "D";
                            DsGLJrnGrpDelete.GLJrnGrp[0].Delete();

                            GLJrnGrpImplsDelete.Update(DsGLJrnGrpDelete);
                            LogMsg.AppendLine("GLJrnGrpDelete > Update Method > Delete Success, Company : " + Company + " GroupID : " + GroupID);
                            continue;
                        }
                        #endregion

                        #region GLJrnDtlMnl
                        bool IsErrorFound = false;
                        int GLJrnDtlMnlRowIndex = 0;
                        for (int Dtl = 0; Dtl < tmpdt.Rows.Count; Dtl++)
                        {
                            try
                            {
                                if (DsGLJournalEntry.GLJrnDtlMnl.Rows.Count != 0)
                                {
                                    //Bcoz its updating on line number 0 to ........
                                    //Getting error message, G/L Account is required
                                    GLJrnDtlMnlRowIndex = DsGLJournalEntry.GLJrnDtlMnl.Rows.Count;
                                }
                                decimal TransAmtDtl = Convert.ToDecimal(tmpdt.Rows[Dtl]["TransAmt"]);
                                string SessionBookID = DsGLJournalEntry.GLJrnHed[0].BookID;
                                Int32 SessionFiscalYear = DsGLJournalEntry.GLJrnHed[0].FiscalYear;
                                string SessionFiscalYearSuffix = DsGLJournalEntry.GLJrnHed[0].FiscalYearSuffix;
                                string SessionJournalCode = DsGLJournalEntry.GLJrnHed[0].JournalCode;
                                Int32 SessionJournalNum = DsGLJournalEntry.GLJrnHed[0].JournalNum;
                                string SessionGroupID = DsGLJournalEntry.GLJrnHed[0].GroupID;

                                string Expresion = "Company = '" + Company + "' AND GroupID = '" + GroupID + "' AND GLAccount = '" + Convert.ToString(tmpdt.Rows[Dtl]["GLAccount"]) + "' AND TransAmt = " + TransAmtDtl;
                                DataRow[] RowExist = DsGLJournalEntry.GLJrnDtlMnl.Select(Expresion);
                                LogMsg.AppendLine("GLJournalEntry.GetNewGLJrnDtlMnl[" + GLJrnDtlMnlRowIndex + "], " + Expresion);
                                Expresion = string.Empty;

                                if (RowExist.Length > 0)
                                {
                                    LogMsg.AppendLine("GLJournalEntry.GetNewGLJrnDtlMnl[" + GLJrnDtlMnlRowIndex + "], Above GLJrnDtlMnl transaction already exist in system with same filters");
                                }

                                if (RowExist.Length == 0)
                                {
                                    GLJournalEntryImpls.GetNewGLJrnDtlMnl(DsGLJournalEntry, SessionBookID, SessionFiscalYear, SessionFiscalYearSuffix, SessionJournalCode, SessionJournalNum, 0);
                                    LogMsg.AppendLine("GLJournalEntry.GetNewGLJrnDtlMnl[" + GLJrnDtlMnlRowIndex + "] Success");

                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].Description = Description;
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].GroupID = SessionGroupID;

                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].GLAccount = Convert.ToString(tmpdt.Rows[Dtl]["GLAccount"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue1 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue1"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue2 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue2"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue3 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue3"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue4 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue4"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue5 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue5"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue6 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue6"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue7 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue7"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue8 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue8"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue9 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue9"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue10 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue10"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue11 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue11"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue12 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue12"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue13 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue13"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue14 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue14"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue15 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue15"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue16 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue16"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue17 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue17"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue18 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue18"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue19 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue19"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].SegValue20 = Convert.ToString(tmpdt.Rows[Dtl]["SegValue20"]);
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex]["STCompany_c"] = STCompCode;
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex]["STConsNo_c"] = RunNbr;
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex]["IsSTCons_c"] = true;
                                    DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].TransAmt = TransAmtDtl;
                                    if (TransAmtDtl > 0)
                                    {
                                        DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].TotDebit = TransAmtDtl;
                                    }
                                    else
                                    {
                                        DsGLJournalEntry.GLJrnDtlMnl[GLJrnDtlMnlRowIndex].TotCredit = Math.Abs(TransAmtDtl);
                                    }
                                    LogMsg.AppendLine("DsGLJournalEntry.GLJrnDtlMnl[" + GLJrnDtlMnlRowIndex + "] Values Assigned Success");

                                    GLJournalEntryImpls.Update(DsGLJournalEntry);
                                    LogMsg.AppendLine("GLJournalEntry.GLJrnDtlMnl[" + GLJrnDtlMnlRowIndex + "].Update Success");
                                    GLJrnDtlMnlRowIndex += 1;
                                }
                            }
                            catch (Exception ex2)
                            {
                                //Record error
                                IsErrorFound = true;
                                RecordErrorLog("GLJrnDtlMnl", ex2.Message.ToString(), Company, FiscalPeriod, FiscalPeriod, RunNbr);
                                LogMsg.AppendLine("DsGLJournalEntry.GLJrnDtlMnl > Update Failed > " + ex2.Message.ToString());

                                GLJournalEntryDataSet DsGLJournalEntryDelete = new GLJournalEntryDataSet();
                                GLJournalEntryImpl GLJournalEntryDelete = CreateBusObj<GLJournalEntryImpl>(sessionId, GLJournalEntryImpl.UriPath);

                                DsGLJournalEntryDelete = GLJournalEntryDelete.GetByGroupID(GroupID);
                                LogMsg.AppendLine("GLJournalEntryDelete > GetByGroupID Success, Company : " + Company + " GroupID : " + GroupID);

                                DsGLJournalEntryDelete.GLJrnHed[0]["IsSysGenerated_c"] = false;
                                DsGLJournalEntryDelete.GLJrnHed[0]["Submitted4Appr_c"] = false;
                                DsGLJournalEntryDelete.GLJrnHed[0]["Approved_c"] = false;
                                DsGLJournalEntryDelete.GLJrnHed[0].RowMod = "U";

                                GLJournalEntryDelete.Update(DsGLJournalEntryDelete);
                                LogMsg.AppendLine("GLJournalEntryDelete > Update Method > Delete Success, Company : " + Company + " GroupID : " + GroupID);

                                DsGLJournalEntryDelete.GLJrnHed[0].RowMod = "D";
                                DsGLJournalEntryDelete.GLJrnHed[0].Delete();

                                GLJournalEntryDelete.Update(DsGLJournalEntryDelete);
                                LogMsg.AppendLine("GLJournalEntryDelete > Delete Success, Company : " + Company + " GroupID : " + GroupID);

                                GLJrnGrpDataSet DsGLJrnGrpDelete = new GLJrnGrpDataSet();
                                GLJrnGrpImpl GLJrnGrpImplsDelete = CreateBusObj<GLJrnGrpImpl>(sessionId, GLJrnGrpImpl.UriPath);

                                DsGLJrnGrpDelete = GLJrnGrpImplsDelete.GetByID(GroupID);
                                LogMsg.AppendLine("GLJrnGrpDelete > GetByID Success, Company : " + Company + " GroupID : " + GroupID);

                                GLJrnGrpImplsDelete.CheckDocumentIsLocked(GroupID);
                                LogMsg.AppendLine("GLJrnGrpDelete > CheckDocumentIsLocked Success, Company : " + Company + " GroupID : " + GroupID);

                                DsGLJrnGrpDelete.GLJrnGrp[0].RowMod = "D";
                                DsGLJrnGrpDelete.GLJrnGrp[0].Delete();

                                GLJrnGrpImplsDelete.Update(DsGLJrnGrpDelete);
                                LogMsg.AppendLine("GLJrnGrpDelete > Update Method > Delete Success, Company : " + Company + " GroupID : " + GroupID);
                                break; //Do not change, Always use break
                            }
                        }
                        #endregion

                        if (IsErrorFound == false)
                        {
                            UpdateGLJrnDtl(STCompCode, VoteCode, GroupID, FiscalYear, FiscalPeriod, RunNbr);
                            LogMsg.AppendLine("UpdateGLJrnDtl Success, Record updated in sub treasury company");

                            DsGLJournalEntry.GLJrnHed[0]["IsSysGenerated_c"] = true;
                            DsGLJournalEntry.GLJrnHed[0]["Submitted4Appr_c"] = true;
                            DsGLJournalEntry.GLJrnHed[0]["Approved_c"] = true;
                            GLJournalEntryImpls.Update(DsGLJournalEntry);
                            
                            string GroupID1 = DsGLJrnGrp.GLJrnGrp[0].GroupID;
                            GLJrnGrpImpls.UnlockGroup(GroupID1);
                            LogMsg.AppendLine("GLJrnGrpImpls.UnlockGroup Success-------------");
                        }
                        GlobalSysFunctions.ShowCallerInfoSch(false, Convert.ToString(LogMsg), CurrentFile, StclServerLogPath);
                        LogMsg.Clear();
                    }
                }
                LogMsg.AppendLine("Process Successfully Completed...........");
            }
            catch (Exception ex)
            {
                LogMsg.AppendLine("Error : " + ex.Message);
            }
            finally
            {
                if (SessionModImpl.SessionID != Guid.Empty)
                {
                    SessionModImpl.Logout();
                    LogMsg.AppendLine("SessionModImpl logout Success, DateTime: " + DateTime.Now);
                }
                LogMsg.AppendLine("--------------------------------------------------------------");
                GlobalSysFunctions.ShowCallerInfoSch(false, Convert.ToString(LogMsg), CurrentFile, StclServerLogPath);
                LogMsg.Clear();
            }
        }
        #endregion

        #region GetDataSet
        public static DataSet GetDataSet(string sSql)
        {
            SqlCon = new SqlConnection(Convert.ToString(EpicorConnection));
            DataSet DsErp = new DataSet();
            SqlDataAdapter DaErp = new SqlDataAdapter();
            DaErp.SelectCommand = new SqlCommand(sSql, SqlCon);
            DsErp.Clear();
            DaErp.Fill(DsErp);
            return DsErp;
        }
        #endregion

        #region GetPendingSTConsolidation
        public static DataSet GetPendingSTConsolidation(string STCompanyCode)
        {
            SqlCon = new SqlConnection(Convert.ToString(EpicorConnection));
            using (SqlCommand cmddata = new SqlCommand("Stcl_CIFMIS_Global_GetPendingSTConsolidation", SqlCon))
            {
                cmddata.CommandTimeout = 0;
                cmddata.CommandType = CommandType.StoredProcedure;
                cmddata.Parameters.AddWithValue("@UserID", EpicorUserID);
                cmddata.Parameters.AddWithValue("@STCompCode", STCompanyCode);
                cmddata.CommandType = CommandType.StoredProcedure;
                using (SqlDataAdapter da = new SqlDataAdapter(cmddata))
                {
                    DataSet ds = new DataSet();
                    ds.Clear();
                    da.Fill(ds);
                    return ds;
                }
            }
        }
        #endregion

        #region UpdateGLJrnDtl
        public static void UpdateGLJrnDtl(string STCompany, string VoteCode, string GroupId, Int32 FiscYr, Int32 FiscPr, Int32 RunNbr)
        {
            SqlCon = new SqlConnection(Convert.ToString(EpicorConnection));
            using (SqlCommand cmddata = new SqlCommand("Stcl_CIFMIS_Global_UpdateSTConsolidation", SqlCon))
            {
                if (SqlCon.State == ConnectionState.Closed)
                { SqlCon.Open(); }
                cmddata.CommandType = CommandType.StoredProcedure;
                cmddata.Parameters.AddWithValue("@STComp", STCompany);
                cmddata.Parameters.AddWithValue("@VoteCode", VoteCode);
                cmddata.Parameters.AddWithValue("@GroupId", GroupId);
                cmddata.Parameters.AddWithValue("@FiscYr", FiscYr);
                cmddata.Parameters.AddWithValue("@FiscPr", FiscPr);
                cmddata.Parameters.AddWithValue("@RunNbr", RunNbr);
                cmddata.ExecuteNonQuery();
                if (SqlCon.State == ConnectionState.Open)
                { SqlCon.Close(); }
            }
        }
        #endregion

        #region GetFiscalPeriod
        public static void GetFiscalPeriod(string Company, string BookID, DateTime TrDate, out Int32 FiscalYear, out string FiscalYearSuffix, out Int32 FiscalPeriod, out string FiscalCalendarID)
        {
            string sql = string.Empty;
            FiscalYear = 0;
            FiscalPeriod = 0;
            FiscalYearSuffix = string.Empty;
            FiscalCalendarID = string.Empty;

            sql = "SELECT FiscalCalendarID " +
                 " FROM Erp.GLBook WITH (NOLOCK) " +
                 " WHERE GLBook.Company = '" + Company + "' " +
                 " AND GLBook.BookID = '" + BookID + "' ";
            DataTable dtFiscalCalendarID = new DataTable();
            dtFiscalCalendarID = GetDataSet(sql).Tables[0];
            if (dtFiscalCalendarID.Rows.Count > 0)
            {
                FiscalCalendarID = Convert.ToString(dtFiscalCalendarID.Rows[0][0]);
            }
            dtFiscalCalendarID = null;

            sql = " SELECT FiscalPer.FiscalYear ,FiscalPer.FiscalYearSuffix,FiscalPer.FiscalPeriod " +
                " FROM Erp.FiscalPer  WITH (NOLOCK) " +
                " WHERE FiscalPer.Company = '" + Company + "' " +
                " AND FiscalPer.FiscalCalendarID = '" + FiscalCalendarID + "' " +
                " AND FiscalPer.StartDate <= '" + TrDate.ToString("yyyy-MM-dd") + "' " +
                " AND FiscalPer.EndDate >='" + TrDate.ToString("yyyy-MM-dd") + "' ";
            DataTable dtFiscalYear = new DataTable();
            dtFiscalYear = GetDataSet(sql).Tables[0];
            if (dtFiscalYear.Rows.Count > 0)
            {
                FiscalYear = Convert.ToInt32(dtFiscalYear.Rows[0]["FiscalYear"]);
                FiscalYearSuffix = Convert.ToString(dtFiscalYear.Rows[0]["FiscalYearSuffix"]);
                FiscalPeriod = Convert.ToInt32(dtFiscalYear.Rows[0]["FiscalPeriod"]);
            }
            dtFiscalYear = null;
        }
        #endregion

        #region RecordErrorLog
        public static void RecordErrorLog(string errorCode, string errorDescription, string company, int fiscalYear, int fiscalPeriod, int runNo)
        {
            SqlCon = new SqlConnection(Convert.ToString(EpicorConnection));
            string sCmd = "INSERT INTO Stcl_ErrorLog (ErrorCode,ErrorRecorded,UserId,Company,FiscalYear,FiscalPeriod,RunNo) " +
                                            " VALUES (@ErrorCode,@ErrorRecorded,@UserId,@Company,@FiscalYear,@FiscalPeriod,@RunNo)";
            using (SqlCommand comm = new SqlCommand(sCmd, SqlCon))
            {
                if (SqlCon.State == ConnectionState.Closed)
                { SqlCon.Open(); }
                comm.Parameters.AddWithValue("@ErrorCode", errorCode);
                comm.Parameters.AddWithValue("@ErrorRecorded", errorDescription);
                comm.Parameters.AddWithValue("@UserId", EpicorUserID);
                comm.Parameters.AddWithValue("@Company", company);
                comm.Parameters.AddWithValue("@FiscalYear", fiscalYear);
                comm.Parameters.AddWithValue("@FiscalPeriod", fiscalPeriod);
                comm.Parameters.AddWithValue("@RunNo", runNo);
                comm.ExecuteNonQuery();
                if (SqlCon.State == ConnectionState.Open)
                { SqlCon.Close(); }
            }
        }
        #endregion

        #region T CreateBusObj<T>
        public static T CreateBusObj<T>(Guid sessionId, string uriPath) where T : ImplBase
        {
            try
            {
                int OperationTimeout = 300;
                // the next two values are only used if the binding is set to UsernameSslChannel
                bool ValidateWcfCertificate = false; // should the certificate be validated as coming from a known certificate authority
                string dnsIdentity = string.Empty; // if the idenitity of the certificate does not match the machine name used in the url, you can specify it here.

                Guid SessionGuid = sessionId != null ? sessionId : Guid.Empty;

                T BO = ImplFactory.CreateImpl<T>(
                        uriPath,
                        appServerUrl: AppSrvUrl,
                        submitUser: string.Empty,
                        endpointBinding: EndpointBinding,
                        sessionId: SessionGuid,
                        userId: EpicorUserID,
                        password: EpiorUserPassword,
                        operationTimeout: OperationTimeout,
                        validateWcfCertificate: ValidateWcfCertificate,
                        dnsIdentity: dnsIdentity,
                        licenseTypeId: Ice.License.LicensableUserCounts.DefaultUser);

                return BO;
            }
            catch (Exception ex)
            {
                throw new Ice.BLException(ex.Message.ToString());
            }
        }
        #endregion

        #region "Public Methods/Functions AssignValues"
        public static void AssignValues()
        {
            DataSet DsGlbData = new DataSet();
            string SysParamCodes = "SchedulerSwitch,EpicorUserID,EpicorUserPassword,StclServerLogPath,TreasuryCompany,ISSTAllocApplicable,IsSiteIDFilterApplicable";
            DsGlbData = GlobalSysFunctions.GetAllSysParamValues(EpicorConnection, SysParamCodes);

            LogMsg.Append(Environment.NewLine + "SysParamValues: ");
            for (int i = 0; i < DsGlbData.Tables[0].Rows.Count; i++)
            {
                LogMsg.Append(Environment.NewLine + Convert.ToString(i + 1) + ". " + Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]) + ": ");
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "SCHEDULERSWITCH")
                {
                    SchedulerSwitch = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]);
                    LogMsg.Append(SchedulerSwitch);
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "EPICORUSERID")
                {
                    EpicorUserID = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]);
                    LogMsg.Append(EpicorUserID);
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "EPICORUSERPASSWORD")
                {
                    EpiorUserPassword = Convert.ToString(EFTReference.Decrypt(Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"])));
                    if (EpiorUserPassword == string.Empty)
                    { LogMsg.Append("Password Is Empty"); }
                    else
                    { LogMsg.Append("Password Found"); }
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "STCLSERVERLOGPATH")
                {
                    StclServerLogPath = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]);
                    LogMsg.Append(StclServerLogPath);
                    StclServerLogPath = StclServerLogPath + "\\StclServerLog_" + DateTime.Now.Date.ToString("yyyyMMdd") + "_" + CurrentFile;
                    LogMsg.Append("   StclServerLogPath: " + StclServerLogPath);
                    if (DsGlbData.Tables[0].Rows.Count != SysParamCodes.Split(',').Length)
                    {
                        throw new Exception("AssignValues => GlobalSysFunctions => GetAllSysParamValues(): All values for provided SysParam codes are not in Database.");
                    }
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "TREASURYCOMPANY")
                {
                    TreasuryCompany = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]);
                    LogMsg.Append(TreasuryCompany);
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "ISSTALLOCAPPLICABLE")
                {
                    ISSTAllocApplicable = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]).ToUpper();
                    LogMsg.Append(ISSTAllocApplicable);
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "ISSITEIDFILTERAPPLICABLE")
                {
                    IsSiteIDFilterApplicable = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]).ToUpper();
                    LogMsg.Append(IsSiteIDFilterApplicable);
                    continue;
                }
            }
        }
        #endregion "Public Methods/Functions" 

    }
}
