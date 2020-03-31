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
using System.Collections.Generic;
using System.Linq;

/*
Version		Project     Author			    Date		    Purpose
1.0			CIFMIS      Rajesh              24/04/2015		Generate Bank Adjustment for FOCR
2.0			CIFMIS      Pankaj M. Borse		23/10/2015		Decryption of Epicor user password 
3.0         CIFMIS      Pritesh Parmar      19/02/2016      Resolved code review issues,Implemented password encryption logic
4.0         CIFMIS      Pritesh Parmar      08/03/2016      To avoide duplicate group id
5.0         CIFMIS      Pritesh Parmar		08/03/2016		Delete group record if header is fail   
2.0.0.0     CIFMIS      Pritesh Parmar		10/05/2016		Incorporated 10.0 changes to 10.1  
2.0.0.1     CIFMIS      Shekhar Chaudhari	10/05/2016		Incorporated commitment control dynamic logic.
2.0.0.2		CIFMIS      Mahesh Deore        12/05/2016	    Upgraded references from 10.1.400.8 to 10.1.400.9
2.0.0.3     CIFMIS      Pritesh Parmar		23/05/2016		Downgrade references from 10.1.400.9 to 10.1.400.1 
2.0.0.4     CIFMIS      Pritesh Parmar		11/08/2016		Changed TranDate from DateTime.Now to Exchaquer ApplyDate, VSO No : 9011
2.0.0.5     CIFMIS      Pritesh Parmar		20/12/2016		Incorporated 10.0 changes to 10.1  
 *                                                          1) 9764 - Bank Adjustment transaction creates in one company but does not create in other company.
 *                                                          2) 9766 - Bank Adjustment Fiscal Year / Fiscal Periods are not updating correctly.
                                                            3) 9762 - Dummy Voucher : If user has changed Vote SBC Bank Setup after GL post and before Dummy Voucher Scheduler run.
2.0.0.6     CIFMIS      Mahesh Deore        26/05/2017		Implement SSO & login functionality (security issue) as per Epicor configuration 
                                                            & also code changes as per code review check list (13552)
2.0.0.7     CIFMIS      Mahesh Deore        06/07/2017		upgrade to 10.1.400.1 to 10.1.600.5
2.0.0.8     CIFMIS		Pritesh Parmar      23/02/2018      VSO Id = 16190, Migrate to 10.2.100.9                                                        
2.0.0.9     CIFMIS		Rajesh              07/12/2018      VSO Id = 19039 (incorporating OFC concept for OFC Bank A/c in Epicor) today. This is required for MOFKL and other clients on high priority. 
2.0.0.10     CIFMIS		Rajesh              14/12/2018      VSO Id = 19039, 19180, 19180,19181,19183, 19182, 19176, 19177, 19172,19173 
2.0.0.11    CIFMIS      Amod Loharkar       11/12/2018		PBID 17766/18976, added SiteID filteration logic for Cost Center Segregation for MOFKL.
2.0.0.12    CIFMIS      Amod Loharkar       15/02/2019      PBID 21821/21893, added setPlant method to store.
2.0.0.13    CIFMIS	    Amod Loharkar       18/04/2019      VSO Id - 22245 - Bank adjustment is not being created for OFC transactions in Parent company for SME
2.0.0.14    CIFMIS      Rajesh              07/08/2019      24240 - Post ERP 10.2 Upgrade Change request 11 - Warrant of Fund Allocation to Sub treasury
2.0.0.15    CIFMIS      Rajesh              03/09/2019      24240 - Post ERP 10.2 Upgrade Change request 11 - Warrant of Fund Allocation to Sub treasury(Reopened)
2.0.0.16    CIFMIS      Rajesh              11/09/2019      Resolved Bug Id - 24240, 24778, 24308, 23180
2.0.0.17    CIFMIS      Rajesh              11/09/2019      VSO ID - 24810 - IRMS Integration for Cash Receipts

 * * */

namespace Stcl.Scheduler.GenerateBankAdjustmentFOCRAfterPost
{
    class GenerateBankAdjustmentFOCRAfterPost
    {
        #region "Static Variable Declaration"
        static string SchedulerSwitch = string.Empty;
        static string StclServerLogPath = string.Empty;
        static string EpicorUserID = string.Empty;
        static string EpiorUserPassword = string.Empty;
        static string AppSrvUrl = string.Empty; // This should be the url to your appserver
        static string EndpointBinding = string.Empty; // This is case sensitive.  Valid values are "UsernameWindowsChannel", "Windows" and "UsernameSslChannel"
        static string CommitCtrlLvl = string.Empty;
        static string TreasuryCompany = string.Empty;
        static string OFCCashRecXfrBankFeeID = string.Empty;
        static string OFCTrCashRecAdjBankFeeID = string.Empty;
        static string ConfigFile = "\\\\" + Convert.ToString(System.Configuration.ConfigurationManager.AppSettings["AppServerName"]) + @"\c$\" + Convert.ToString(System.Configuration.ConfigurationManager.AppSettings["ServerConfigFilePath"]);
        static StringBuilder LogMsg = new StringBuilder();
        static string CurrentFile = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
        static string EpicorConnection = string.Empty;
        static SqlConnection SqlCon = new SqlConnection();
        static string IsSiteIDFilterApplicable = string.Empty;
        static string ISOFCApplicable = string.Empty;
        #endregion "Static Variable Declaration

        static void Main(string[] args)
        {
            #region "get Config details & also sysParams"
            Guid SessionId = Guid.Empty;
            var ConnDet = GlobalSysFunctions.GetConnectionString(ConfigFile);
            EpicorConnection = ConnDet.Item1;
            AppSrvUrl = ConnDet.Item2;
            EndpointBinding = GlobalSysFunctions.GetChannel(ConfigFile);
            SqlCon = new SqlConnection(Convert.ToString(EpicorConnection));
            CurrentFile = Path.GetFileNameWithoutExtension(CurrentFile);
            TreasuryCompany = Convert.ToString(GetSysParam("TreasuryCompany"));
            #endregion "get Config details & also sysParams"

            if (AssignValues())
            {
                if (SchedulerSwitch.ToUpper() == "ON")
                {
                    GlobalSysFunctions.ShowCallerInfoSch(true, Convert.ToString(LogMsg), CurrentFile, StclServerLogPath);
                    LogMsg.Clear();
                    //Below condition will allow scheudlar to take action if Commitment Control level is 3 and IsBudgetFromTreasury flag is true.
                    if (Convert.ToString(ISOFCApplicable) == "TRUE")
                    {
                        SqlConnection con = new SqlConnection(EpicorConnection);
                        SessionModImpl SessionModImpl = CreateBusObj<SessionModImpl>(Guid.Empty, SessionModImpl.UriPath);
                        LogMsg.AppendLine("Session Created : SessionModImpl");
                        try
                        {
                            #region Define variables
                            string CompanyName, PlantID, PlantName, WorkstationID, WorkstationDesc, EmployeeID, CountryGroupCode, CountryCode, TenantID;
                            #endregion Define variables

                            string DefaultSiteID = string.Empty;
                            string SiteID = string.Empty;
                            string SiteName = string.Empty;

                            #region Get Default SiteID
                            SqlCommand cmdDefaultSite = new SqlCommand();
                            cmdDefaultSite.CommandText = "SELECT DefaultPlant " +
                                                            " FROM Erp.XaSyst WITH (NOLOCK) " +
                                                            " WHERE Company = '" + TreasuryCompany + "'";
                            cmdDefaultSite.CommandType = CommandType.Text;
                            cmdDefaultSite.Connection = con;
                            if (con.State == ConnectionState.Closed)
                            { con.Open(); }
                            DefaultSiteID = Convert.ToString(cmdDefaultSite.ExecuteScalar());
                            LogMsg.AppendLine(" Default Site ID for Treasury Company: " + DefaultSiteID);
                            #endregion

                            #region "Get All Companies using Stcl_CIFMIS_Global_GetAllComp"
                            SqlCommand cmdComp = new SqlCommand();
                            cmdComp.CommandText = "Stcl_CIFMIS_Global_GetAllComp";
                            cmdComp.CommandType = CommandType.StoredProcedure;
                            cmdComp.Connection = con;
                            if (con.State == ConnectionState.Closed)
                            {
                                con.Open();
                            }
                            DataTable DtCompany = new DataTable();
                            SqlDataAdapter DaCompany = new SqlDataAdapter(cmdComp);
                            DaCompany.Fill(DtCompany);
                            #endregion "Get All Companies using Stcl_CIFMIS_Global_GetAllComp"

                            #region "ForEach loop of DtCompany starts"
                            foreach (DataRow CompanyRow in DtCompany.Rows)
                            {
                                #region "Get All FOCR Bank Adjustments using Stcl_CIFMIS_Global_GetFOCRBankAdjustmentAfterPost"
                                string MDACompany = Convert.ToString(CompanyRow["Company"]);
                                LogMsg.AppendLine("Processing for Company : " + MDACompany + "-------------------------");
                                SqlCommand cmd1 = new SqlCommand();
                                if (con.State == ConnectionState.Closed)
                                {
                                    con.Open();
                                }
                                cmd1.CommandText = "Stcl_CIFMIS_Global_GetFOCRBankAdjustmentAfterPost";
                                cmd1.CommandType = CommandType.StoredProcedure;
                                cmd1.Connection = con;
                                cmd1.Parameters.Clear();
                                cmd1.Parameters.AddWithValue("@company", MDACompany);
                                DataTable DtBankAdj = new DataTable();
                                SqlDataAdapter DaBankAdj = new SqlDataAdapter(cmd1);
                                DaBankAdj.Fill(DtBankAdj);
                                LogMsg.AppendLine("No of rows found to process : " + DtBankAdj.Rows.Count);
                                #endregion "Get All FOCR Bank Adjustments using Stcl_CIFMIS_Global_GetFOCRBankAdjustmentAfterPost"

                                #region "ForEach loop of DtBankAdj starts"
                                foreach (DataRow BankAdjRow in DtBankAdj.Rows)
                                {
                                    try
                                    {
                                        string Company = string.Empty;
                                        int FiscalYear = Convert.ToInt32(BankAdjRow["FiscalYear"]);
                                        int FiscalPeriod = Convert.ToInt32(BankAdjRow["FiscalPeriod"]);
                                        string SubBudgetCls = Convert.ToString(BankAdjRow["SubBudgetClass"]);
                                        string OrigGroupId = Convert.ToString(BankAdjRow["GroupID"]);
                                        int HeadNum = Convert.ToInt32(BankAdjRow["HeadNum"]);
                                        string DocType = Convert.ToString(BankAdjRow["DocType"]).ToUpper();

                                        string Comments = string.Empty;
                                        if (!string.IsNullOrEmpty(DocType) && (DocType == "ONACCOUNT" || DocType == "BFT" || DocType == "PAYINVIRMS"))
                                        {
                                            Comments = "BNKTRF";
                                        }
                                        else
                                        {
                                            Comments = Convert.ToString(BankAdjRow["InvoiceComment"]);

                                        }
                                        string TranRef = Convert.ToString(BankAdjRow["CheckRef"]);
                                        DateTime TranDate = Convert.ToDateTime(BankAdjRow["TranDate"]);
                                        string LegalNumber = Convert.ToString(BankAdjRow["LegalNumber"]);
                                        string RefTrxCtrlNum_c = Convert.ToString(BankAdjRow["SrcTrxCtrlNum_c"]);
                                        LogMsg.AppendLine("SubBudgetCls: " + SubBudgetCls + " OrigGroupId: " + OrigGroupId + " HeadNum: " + HeadNum);
                                        LogMsg.AppendLine("LegalNumber = " + LegalNumber + " Comments = " + Comments + " TranRef = " + TranRef + " FiscalYear = " + FiscalYear + " FiscalPeriod = " + FiscalPeriod + " TranDate = " + Convert.ToString(TranDate));

                                        ValidateBankFee(MDACompany, OFCCashRecXfrBankFeeID);
                                        LogMsg.AppendLine("ValidateBankFee Success > Ministry: OFCCashRecXfrBankFeeID = " + OFCCashRecXfrBankFeeID);

                                        ValidateBankFee(TreasuryCompany, OFCTrCashRecAdjBankFeeID);
                                        LogMsg.AppendLine("ValidateBankFee Success > Treasury: OFCTrCashRecAdjBankFeeID = " + OFCTrCashRecAdjBankFeeID);



                                        int Cnt = 0;
                                        bool IsBankTranCreated = false;
                                        #region "While Loop Start"
                                        bool IsError = false;
                                        while (Cnt != 2)
                                        {
                                            IsError = false;
                                            if (DtBankAdj.Rows.Count > 0)
                                            {
                                                if (SessionModImpl.SessionID == Guid.Empty)
                                                {
                                                    SessionId = SessionModImpl.Login();
                                                    SessionModImpl.SessionID = SessionId;
                                                    LogMsg.AppendLine("Session Login:  " + SessionModImpl.SessionID);
                                                    LogMsg.AppendLine("SessionModImpl.Login Successfully");
                                                }
                                            }
                                            bool RequiredUserInput = false;
                                            bool IsBankTranExist = false;
                                            string BankFeeID = OFCCashRecXfrBankFeeID;
                                            string BankAcctId = string.Empty;
                                            decimal TranAmt = 0;

                                            Cnt = Cnt + 1;
                                            if (Cnt == 1)
                                            {
                                                Company = MDACompany;
                                                SessionModImpl.SetCompany(Company, out CompanyName, out PlantID, out PlantName, out WorkstationID, out WorkstationDesc, out EmployeeID, out CountryGroupCode, out CountryCode, out TenantID);
                                                LogMsg.AppendLine("SetCompany Success, Company : " + Company + "-------------------------");
                                                SiteID = Convert.ToString(BankAdjRow["SiteID"]);
                                                TranAmt = -1 * Convert.ToDecimal(BankAdjRow["DocTranAmt"]);
                                                BankAcctId = Convert.ToString(BankAdjRow["BankAcctId"]);
                                                LogMsg.AppendLine("Ministry > BankAcctId: " + BankAcctId + " BankFeeID: " + BankFeeID + " TranAmt: " + Convert.ToString(TranAmt));
                                                LogMsg.AppendLine("Ministry > SiteID: " + SiteID);
                                                if (IsSiteIDFilterApplicable == "TRUE")
                                                {
                                                    SessionModImpl.SetPlant(SiteID, out SiteName);
                                                    LogMsg.AppendLine("Set Company : " + Company + ", Plant/Site Id: " + SiteID + " - " + SiteName);
                                                }
                                            }
                                            else
                                            {
                                                Company = TreasuryCompany;
                                                SiteID = DefaultSiteID;
                                                LogMsg.AppendLine("Treasury DefaultSiteID : " + SiteID);
                                                SessionModImpl.SetCompany(Company, out CompanyName, out PlantID, out PlantName, out WorkstationID, out WorkstationDesc, out EmployeeID, out CountryGroupCode, out CountryCode, out TenantID);
                                                LogMsg.AppendLine("SetCompany Success, Company : " + Company);
                                                if (IsSiteIDFilterApplicable == "TRUE")
                                                {
                                                    SessionModImpl.SetPlant(SiteID, out SiteName);
                                                    LogMsg.AppendLine("Set Company : " + Company + ", Plant/Site Id: " + SiteID + " - " + SiteName);
                                                }
                                                TranAmt = Convert.ToDecimal(BankAdjRow["DocTranAmt"]);
                                                BankAcctId = Convert.ToString(BankAdjRow["TrBankAcct_c"]);

                                                ValidateBankAcct(MDACompany, BankAcctId);
                                                BankFeeID = OFCTrCashRecAdjBankFeeID;

                                                if (BankAcctId == string.Empty)
                                                {
                                                    LogMsg.AppendLine("Please Check Sub Budget Classification and Vote SBC Bank Setup, Sub Budget Class: " + SubBudgetCls + " Vote Company: " + MDACompany);
                                                    continue;
                                                }
                                                LogMsg.AppendLine("Treasury > BankAcctId: " + BankAcctId + " BankFeeID: " + BankFeeID + " TranAmt: " + Convert.ToString(TranAmt));
                                            }

                                            System.Threading.Thread.Sleep(2000);

                                            string GroupID = Convert.ToString(DateTime.Now.Day).PadLeft(2, '0') + Convert.ToString(DateTime.Now.Hour).PadLeft(2, '0') + Convert.ToString(DateTime.Now.Minute).PadLeft(2, '0') + Convert.ToString(DateTime.Now.Second).PadLeft(2, '0');
                                            LogMsg.AppendLine("GroupID : " + GroupID);

                                            BankAdjEntryImpl BankAdjEntryImpls = CreateBusObj<BankAdjEntryImpl>(SessionId, BankAdjEntryImpl.UriPath);
                                            BankAdjEntryDataSet dsBankAdjEntry = new BankAdjEntryDataSet();
                                            string Sql1 = string.Empty;
                                            if (DocType != "STBANKADJ")
                                            {
                                                Sql1 = " SELECT GroupID " +
                                                              " FROM BankTran WITH(NOLOCK) " +
                                                              " WHERE Company = '" + Company + "' " +
                                                              " AND SrcCompany_c = '" + MDACompany + "' " +
                                                              " AND SrcTrxCtrlNum_c = '" + LegalNumber + "' " +
                                                              " AND CashHeadNum_c = " + HeadNum;
                                            }
                                            else
                                            {
                                                Sql1 = " SELECT GroupID " +
                                                              " FROM BankTran WITH(NOLOCK) " +
                                                              " WHERE Company = '" + Company + "' " +
                                                              " AND SrcCompany_c = '" + MDACompany + "' " +
                                                              " AND SrcTrxCtrlNum_c = '" + RefTrxCtrlNum_c + "' " +
                                                              " AND RefTrxCtrlNum_c <> '" + DocType + "' " +
                                                              " AND CashHeadNum_c = " + HeadNum;

                                            }
                                            IsBankTranExist = IsRowExists(Sql1);
                                            LogMsg.AppendLine("IsBankTranExist : " + Convert.ToString(IsBankTranExist) + " For Company : " + Company + ", SrcCompany : " + MDACompany + ", SrcTrxCtrlNum : " + LegalNumber + ", CashHeadNum : " + HeadNum);

                                            if (IsBankTranExist == false)
                                            {
                                                try
                                                {
                                                    BankAdjEntryImpls.GetNewBankGrp(dsBankAdjEntry);
                                                    LogMsg.AppendLine("GetNewBankGrp Success");

                                                    dsBankAdjEntry.BankGrp[0].GroupID = GroupID;
                                                    dsBankAdjEntry.BankGrp[0].TranDate = TranDate;
                                                    dsBankAdjEntry.BankGrp[0].FiscalYear = FiscalYear;
                                                    dsBankAdjEntry.BankGrp[0].FiscalPeriod = FiscalPeriod;
                                                    dsBankAdjEntry.BankGrp[0]["Description_c"] = Comments;
                                                    dsBankAdjEntry.BankGrp[0]["TranRef_c"] = TranRef;
                                                    dsBankAdjEntry.BankGrp[0]["SubBudgetCls_c"] = SubBudgetCls;
                                                    dsBankAdjEntry.BankGrp[0]["AutoPost_c"] = true;
                                                    dsBankAdjEntry.BankGrp[0]["IsSysGenerated_c"] = true;
                                                    LogMsg.AppendLine("updating SiteID : " + SiteID);
                                                    dsBankAdjEntry.BankGrp[0]["SiteID_c"] = SiteID;
                                                    LogMsg.AppendLine("BankGrp SiteID : " + dsBankAdjEntry.BankGrp[0]["SiteID_c"].ToString());
                                                    dsBankAdjEntry.BankGrp[0].RowMod = "A";
                                                    LogMsg.AppendLine("BankGrp Values Assigned Success");

                                                    BankAdjEntryImpls.ChangeGrpTranDate(TranDate, dsBankAdjEntry);
                                                    LogMsg.AppendLine("ChangeGrpTranDate Success");

                                                    BankAdjEntryImpls.ChangeGrpBankAcct(BankAcctId, dsBankAdjEntry);
                                                    LogMsg.AppendLine("ChangeGrpBankAcct Success");


                                                    BankAdjEntryImpls.Update(dsBankAdjEntry);
                                                    LogMsg.AppendLine("Update 1 Success");
                                                }
                                                catch (Exception ex)
                                                {
                                                    IsError = true;
                                                    LogMsg.AppendLine("BankGrp > Error: " + ex.Message);
                                                }
                                                finally
                                                {
                                                    if (IsError == false)
                                                    {
                                                        LogMsg.AppendLine("Successfully created transaction for Group ID : " + GroupID + " of Company " + Company + ".");
                                                    }
                                                    GlobalSysFunctions.ShowCallerInfoSch(false, Convert.ToString(LogMsg), CurrentFile, StclServerLogPath);
                                                    LogMsg.Clear();
                                                }
                                                if (IsError == true)
                                                {
                                                    LogMsg.AppendLine("Error occured for transaction for Group ID : " + GroupID + " of Company " + Company + ".");
                                                    continue;
                                                }
                                            }
                                            IsBankTranCreated = false;

                                            try
                                            {
                                                if (IsBankTranExist == false)
                                                {
                                                    BankAdjEntryImpls.GetNewBankTran1(dsBankAdjEntry, GroupID);
                                                    LogMsg.AppendLine("GetNewBankTran1 Success");

                                                    dsBankAdjEntry.BankTran[0].TranRef = TranRef;
                                                    dsBankAdjEntry.BankTran[0].BankFeeID = BankFeeID;
                                                    dsBankAdjEntry.BankTran[0]["IsSysGenerated_c"] = true;
                                                    dsBankAdjEntry.BankTran[0]["SrcTrxCtrlNum_c"] = LegalNumber;
                                                    dsBankAdjEntry.BankTran[0]["SrcCompany_c"] = MDACompany;
                                                    dsBankAdjEntry.BankTran[0]["Description_c"] = Comments;
                                                    dsBankAdjEntry.BankTran[0]["SubBudgetCls_c"] = SubBudgetCls;
                                                    dsBankAdjEntry.BankTran[0]["IsFOCR_c"] = true;
                                                    dsBankAdjEntry.BankTran[0]["CashHeadNum_c"] = HeadNum;
                                                    if (DocType == "STBANKADJ")
                                                    {
                                                        dsBankAdjEntry.BankTran[0]["WOFAllocNum_c"] = RefTrxCtrlNum_c;
                                                        //dsBankAdjEntry.BankTran[0]["SrcTrxCtrlNum_c"] = RefTrxCtrlNum_c;

                                                    }
                                                    dsBankAdjEntry.BankTran[0]["RefTrxCtrlNum_c"] = RefTrxCtrlNum_c;
                                                    dsBankAdjEntry.BankTran[0]["IsXfrToTreasury_c"] = true;
                                                    dsBankAdjEntry.BankTran[0]["SiteID_c"] = SiteID;
                                                    LogMsg.AppendLine("Bank Tran SiteID : " + dsBankAdjEntry.BankTran[0]["SiteID_c"].ToString());
                                                    dsBankAdjEntry.BankTran[0].RowMod = "A";

                                                    BankAdjEntryImpls.ChangeTranAmt("D", TranAmt, dsBankAdjEntry);
                                                    LogMsg.AppendLine("ChangeTranAmt Success");

                                                    BankAdjEntryImpls.OnChangeBankFeeID(BankFeeID, dsBankAdjEntry);
                                                    LogMsg.AppendLine("OnChangeBankFeeID Success");


                                                    BankAdjEntryImpls.Update(dsBankAdjEntry);
                                                    LogMsg.AppendLine("Update 2 Success");
                                                    LogMsg.AppendLine("DocType - " + DocType);

                                                    if (Company == TreasuryCompany)
                                                    {
                                                        string SqlStrUpd = string.Empty;
                                                        LogMsg.AppendLine("DocType in Treasury Company- " + DocType);

                                                        if (DocType == "BFT")
                                                        {
                                                            SqlStrUpd = "UPDATE  BankTran set IsXfrToTreasury_c = 1  " +
                                                                                        " WHERE Company = '" + MDACompany + "'" +
                                                                                        " AND GroupId = '" + OrigGroupId + "'  " +
                                                                                        " AND TranNum = " + Convert.ToInt32(RefTrxCtrlNum_c) + "" +
                                                                                        " AND HeadNum = " + HeadNum + "" +
                                                                                        " AND FiscalYear = " + FiscalYear + " ";

                                                        }
                                                        else if (DocType == "STBANKADJ")
                                                        {
                                                            SqlStrUpd = "UPDATE  BankTran set IsXfrToTreasury_c = 1  " +
                                                                                        " WHERE Company = '" + MDACompany + "'" +
                                                                                        " AND GroupId = '" + OrigGroupId + "'  " +
                                                                                        " AND WOFAllocNum_c = '" + RefTrxCtrlNum_c + "'  " +
                                                                                        " AND HeadNum = " + HeadNum + "" +
                                                                                        " AND FiscalYear = " + FiscalYear + " ";

                                                        }
                                                        else
                                                        {
                                                            SqlStrUpd = "UPDATE  CashHead set IsXfrToTreasury_c = 1 , IsXfrCompleted_c = 1 " +
                                                                                    " WHERE Company = '" + MDACompany + "'" +
                                                                                    " AND GroupId = '" + OrigGroupId + "'  " +
                                                                                    " AND HeadNum = " + HeadNum + "" +
                                                                                    " AND FiscalYear = " + FiscalYear + " ";

                                                        }
                                                        LogMsg.AppendLine("SqlStrUpd - " + SqlStrUpd);
                                                        SqlCommand CmdErp = new SqlCommand(SqlStrUpd, con);
                                                        if (con.State == ConnectionState.Closed)
                                                        { con.Open(); }
                                                        CmdErp.ExecuteNonQuery();
                                                        if (con.State == ConnectionState.Open)
                                                        { con.Close(); }
                                                        LogMsg.AppendLine("Updated IsXfrToTreasury_c = 1  Success");

                                                    }
                                                }
                                                IsBankTranCreated = true;
                                            }
                                            catch (Exception ex)
                                            {
                                                IsError = true;
                                                if (IsBankTranCreated == false)
                                                {
                                                    SubBudgetCls = string.Empty;
                                                    LogMsg.AppendLine("Transaction get failed for GroupID : " + GroupID);
                                                    LogMsg.AppendLine("Exception : " + ex.Message);

                                                    BankAdjEntryImpl BankAdjEntryImplsDelete = CreateBusObj<BankAdjEntryImpl>(SessionId, BankAdjEntryImpl.UriPath);
                                                    BankAdjEntryDataSet dsBankAdjDelete = new BankAdjEntryDataSet();
                                                    LogMsg.AppendLine("GroupID : " + GroupID);

                                                    BankAdjEntryImplsDelete.BeforeGetBankGrp(GroupID);
                                                    LogMsg.AppendLine("BeforeGetBankGrp Success");

                                                    dsBankAdjDelete = BankAdjEntryImplsDelete.GetByID(GroupID);
                                                    LogMsg.AppendLine("GetByID Success");

                                                    BankAdjEntryImplsDelete.CheckDocumentIsLocked(GroupID);
                                                    LogMsg.AppendLine("CheckDocumentIsLocked Success");

                                                    dsBankAdjDelete.BankGrp[0].ActiveUserID = string.Empty;
                                                    dsBankAdjDelete.BankGrp[0]["IsSysGenerated_c"] = false;
                                                    dsBankAdjDelete.BankGrp[0].RowMod = string.Empty;

                                                    BankAdjEntryImplsDelete.Update(dsBankAdjDelete);
                                                    LogMsg.AppendLine("Update > SET IsSysGenerated_c = False to Delete Bank Group");

                                                    dsBankAdjDelete.BankGrp[0].Delete();
                                                    BankAdjEntryImplsDelete.Update(dsBankAdjDelete);
                                                    LogMsg.AppendLine("Update > Transaction deleted successfully for Group Id: " + GroupID + " of company: " + Company + ".");
                                                    string SqlStrUpdDel = string.Empty;
                                                    string SqlStrUpd = string.Empty;

                                                    if (DocType == "BFT")
                                                    {
                                                        SqlStrUpdDel = "UPDATE  BankTran set IsXfrToTreasury_c = 0  " +
                                                                                    " WHERE Company = '" + MDACompany + "'" +
                                                                                    " AND GroupId = '" + OrigGroupId + "'  " +
                                                                                    " AND TranNum = " + Convert.ToInt32(RefTrxCtrlNum_c) + "" +
                                                                                    " AND HeadNum = " + HeadNum + "" +
                                                                                    " AND FiscalYear = " + FiscalYear + " ";

                                                    }
                                                    else if (DocType == "STBANKADJ")
                                                    {
                                                        SqlStrUpdDel = "UPDATE  BankTran set IsXfrToTreasury_c = 0  " +
                                                                                    " WHERE Company = '" + MDACompany + "'" +
                                                                                    " AND GroupId = '" + OrigGroupId + "'  " +
                                                                                    " AND WOFAllocNum_c = '" + RefTrxCtrlNum_c + "'  " +
                                                                                    " AND HeadNum = " + HeadNum + "" +
                                                                                    " AND FiscalYear = " + FiscalYear + " ";

                                                    }
                                                    else
                                                    {
                                                        SqlStrUpdDel = "UPDATE  CashHead set IsXfrToTreasury_c = 0 " +
                                                                                " WHERE Company = '" + MDACompany + "'" +
                                                                                " AND GroupId = '" + OrigGroupId + "'  " +
                                                                                " AND HeadNum = " + HeadNum + "" +
                                                                                " AND FiscalYear = " + FiscalYear + " ";

                                                    }



                                                    SqlCommand CmdErpDel = new SqlCommand(SqlStrUpdDel, con);
                                                    if (con.State == ConnectionState.Closed)
                                                    { con.Open(); }
                                                    CmdErpDel.ExecuteNonQuery();
                                                    if (con.State == ConnectionState.Open)
                                                    { con.Close(); }
                                                    LogMsg.AppendLine("Updated IsXfrToTreasury_c = 0  Success");


                                                }
                                            }
                                            finally
                                            {
                                                if (IsError == false)
                                                {
                                                    if (IsBankTranCreated == true)
                                                    {
                                                        BankAdjEntryImpls.LeaveBankGrp(GroupID);
                                                        LogMsg.AppendLine("Bank Adjustment generated successfully & also LeaveBankGrp success.");
                                                    }
                                                }
                                                GlobalSysFunctions.ShowCallerInfoSch(false, Convert.ToString(LogMsg), CurrentFile, StclServerLogPath);
                                                LogMsg.Clear();
                                            }
                                            if (IsError == true)
                                            {
                                                continue;
                                            }
                                        }
                                        #endregion "While Loop End"

                                        GlobalSysFunctions.ShowCallerInfoSch(false, Convert.ToString(LogMsg), CurrentFile, StclServerLogPath);
                                        LogMsg.Clear();
                                    }
                                    catch (Exception ex)
                                    {
                                        LogMsg.AppendLine("Error: " + ex.Message);
                                        continue;
                                    }
                                }
                                #endregion "ForEach loop of DtBankAdj ends"
                                LogMsg.AppendLine("Process completed for company: " + MDACompany);
                                GlobalSysFunctions.ShowCallerInfoSch(false, Convert.ToString(LogMsg), CurrentFile, StclServerLogPath);
                                LogMsg.Clear();
                            }
                            #endregion "ForEach loop of DtCompany ends"
                            LogMsg.AppendLine("-----------------------------------------------------------------------------------------------------------------");
                        }
                        catch (Exception ex)
                        {
                            LogMsg.AppendLine(Convert.ToString(ex.Message));
                        }
                        finally
                        {
                            if (con.State == ConnectionState.Open)
                            {
                                con.Close();
                            }
                            if (SessionModImpl.SessionID != Guid.Empty)
                            {
                                SessionModImpl.Logout();
                                LogMsg.AppendLine("SessionModImpl logout Success, DateTime: " + DateTime.Now);
                            }
                            GlobalSysFunctions.ShowCallerInfoSch(false, Convert.ToString(LogMsg), CurrentFile, StclServerLogPath);
                            LogMsg.Clear();
                        }
                    }
                }

            }

        }

        #region "Static methods"
        static void ValidateBankFee(string pCompany, string pBankFeeId)
        {
            SqlConnection conBF = new SqlConnection(EpicorConnection);
            try
            {
                string message = string.Empty;
                SqlDataReader rdr;
                SqlCommand cmdBF = new SqlCommand();
                if (conBF.State == ConnectionState.Closed)
                {
                    conBF.Open();
                }
                cmdBF.CommandText = "Stcl_CIFMIS_GloBal_ValidateBankFee";
                cmdBF.CommandType = CommandType.StoredProcedure;
                cmdBF.Parameters.AddWithValue("@company", pCompany);
                cmdBF.Parameters.AddWithValue("@bankFeeId", pBankFeeId);
                cmdBF.Connection = conBF;
                rdr = cmdBF.ExecuteReader();
                conBF.Close();
                ValidateGLControl(pCompany, pBankFeeId, "Bank Fee", "Bank Fee");
            }
            catch (Exception ex)
            {
                LogMsg.AppendLine(Convert.ToString(ex.Message));
            }
            finally
            {
                if (conBF.State == ConnectionState.Open)
                {
                    conBF.Close();
                }
            }
        }

        static void ValidateGLControl(string pCompany, string pKey1, string pGLControlType, string pGLAcctContext)
        {
            SqlConnection conGL = new SqlConnection(EpicorConnection);
            try
            {
                string message = string.Empty;
                SqlDataReader rdr;

                string mainBookID = string.Empty;
                string cOACode = string.Empty;

                SqlCommand cmdGL = new SqlCommand();
                if (conGL.State == ConnectionState.Closed)
                {
                    conGL.Open();
                }
                cmdGL.CommandText = "Stcl_CIFMIS_Global_ValiDateGLControl";
                cmdGL.CommandType = CommandType.StoredProcedure;
                cmdGL.Parameters.AddWithValue("@company", pCompany);
                cmdGL.Parameters.AddWithValue("@key1", pKey1);
                cmdGL.Parameters.AddWithValue("@glControlType", pGLControlType);
                cmdGL.Parameters.AddWithValue("@glAcctContext", pGLAcctContext);
                cmdGL.Connection = conGL;
                rdr = cmdGL.ExecuteReader();
            }
            catch (Exception ex)
            {
                LogMsg.AppendLine(Convert.ToString(ex.Message));
            }
            finally
            {
                if (conGL.State == ConnectionState.Open)
                {
                    conGL.Close();
                }
            }
        }

        static void GetCPOBankAcctID(string pgTreasuryCompany, string pCompany, string pSubBudgetCls, out string pCPOBankAcctID)
        {
            pCPOBankAcctID = string.Empty;
            string message = string.Empty;
            SqlConnection conCP = new SqlConnection(EpicorConnection);
            try
            {
                SqlCommand cmdCP = new SqlCommand();
                cmdCP.CommandText = "Stcl_CIFMIS_Global_GetCPOBankAcctIDDummyVcr";
                cmdCP.CommandType = CommandType.StoredProcedure;
                cmdCP.Parameters.AddWithValue("@company", pCompany);
                cmdCP.Parameters.AddWithValue("@subBudgetCls", pSubBudgetCls);
                SqlParameter cpoBankAcctId = new SqlParameter();
                cpoBankAcctId.ParameterName = "@cpoBankAcctId";
                cpoBankAcctId.DbType = DbType.String;
                cpoBankAcctId.Size = 10;
                cpoBankAcctId.Direction = ParameterDirection.Output;
                cmdCP.Parameters.Add(cpoBankAcctId);
                cmdCP.Connection = conCP;
                if (conCP.State == ConnectionState.Closed)
                {
                    conCP.Open();
                }
                cmdCP.ExecuteNonQuery();
                pCPOBankAcctID = Convert.ToString(cpoBankAcctId.Value);
                conCP.Close();
            }
            catch (Exception ex)
            {
                LogMsg.AppendLine(ex.Message);
            }
            finally
            {
                if (conCP.State == ConnectionState.Open)
                {
                    conCP.Close();
                }
            }
        }

        static void ValidateBankAcct(string pCompany, string pBankAcctID)
        {
            SqlConnection conBA = new SqlConnection(EpicorConnection);
            try
            {
                string message = string.Empty;
                SqlCommand cmdBA = new SqlCommand();
                cmdBA.CommandText = "Stcl_CIFMIS_Global_ValidateBankAcct";
                cmdBA.CommandType = CommandType.StoredProcedure;
                cmdBA.Parameters.AddWithValue("@company", pCompany);
                cmdBA.Parameters.AddWithValue("@bankAcctId", pBankAcctID);
                cmdBA.Connection = conBA;
                if (conBA.State == ConnectionState.Closed)
                {
                    conBA.Open();
                }
                cmdBA.ExecuteNonQuery();
                conBA.Close();
                ValidateGLControl(pCompany, pBankAcctID, "Bank Account", "Cash");
            }
            catch (Exception ex)
            {
                LogMsg.AppendLine(ex.Message);
            }
            finally
            {
                if (conBA.State == ConnectionState.Open)
                {
                    conBA.Close();
                }
            }
        }
        #endregion "Static methods"

        #region "Private Static methods"
        private static bool IsRowExists(string SqlQry)
        {
            SqlConnection Con = new SqlConnection(EpicorConnection);
            try
            {
                SqlCommand Cmd = new SqlCommand();
                Cmd.CommandText = SqlQry;
                Cmd.CommandType = CommandType.Text;
                Cmd.Connection = Con;
                if (Con.State == ConnectionState.Closed)
                {
                    Con.Open();
                }
                object value = Cmd.ExecuteScalar();
                if (value != null)
                {
                    LogMsg.AppendLine("IsRowExists = True");
                    return true;
                }
                else
                {
                    LogMsg.AppendLine("IsRowExists = False");
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Ice.BLException(ex.Message);
            }
            finally
            {
                if (Con.State == ConnectionState.Open)
                {
                    Con.Close();
                }
            }
        }

        private static string GetSysParam(string code)
        {
            SqlConnection con = new SqlConnection(EpicorConnection);
            try
            {
                SqlCommand cmd = new SqlCommand();
                if (con.State == ConnectionState.Closed)
                { con.Open(); }
                cmd.CommandText = "Stcl_CIFMIS_Global_GetGlobalData";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("@code", code);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.Fill(ds);
                if (con.State == ConnectionState.Open)
                { con.Close(); }
                if (ds != null)
                {
                    if (ds.Tables.Count > 0)
                    {
                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            return Convert.ToString(ds.Tables[0].Rows[0][1]);
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                throw new Ice.BLException(ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }
        #endregion "Private Static methods"

        #region "Public Static methods"
        public static T CreateBusObj<T>(Guid sessionId, string uriPath) where T : ImplBase
        {
            try
            {
                int operationTimeout = 300;
                // the next two values are only used if the binding is set to UsernameSslChannel
                bool validateWcfCertificate = false; // should the certificate be validated as coming from a known certificate authority
                string dnsIdentity = string.Empty; // if the idenitity of the certificate does not match the machine name used in the url, you can specify it here.

                Guid sessionGuid = sessionId != null ? sessionId : Guid.Empty;

                T BO = ImplFactory.CreateImpl<T>(
                        uriPath,
                        appServerUrl: AppSrvUrl,
                        submitUser: string.Empty,
                        endpointBinding: EndpointBinding,
                        sessionId: sessionGuid,
                        userId: EpicorUserID,
                        password: EpiorUserPassword,
                        operationTimeout: operationTimeout,
                        validateWcfCertificate: validateWcfCertificate,
                        dnsIdentity: dnsIdentity,
                        licenseTypeId: Ice.License.LicensableUserCounts.DefaultUser);

                return BO;
            }
            catch (Exception ex)
            {
                throw new Ice.BLException(ex.Message);
            }
        }

        public static bool AssignValues()
        {
            DataSet DsGlbData = new DataSet();
            string SysParamCodes = "SchedulerSwitch,StclServerLogPath,epicorUserID,epicorUserPassword,CommitCtrlLvl,OFCCashRecXfrBankFeeID,OFCTrCashRecAdjBankFeeID,IsSiteIDFilterApplicable,ISOFCApplicable";
            DsGlbData = Global.GlobalSysInfo.GlobalSysFunctions.GetAllSysParamValues(EpicorConnection, SysParamCodes);

            LogMsg.Append(Environment.NewLine + Environment.NewLine + "SysParamValues: ");
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
                    LogMsg.Append("Password Found");
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "COMMITCTRLLVL")
                {
                    CommitCtrlLvl = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]);
                    LogMsg.Append(CommitCtrlLvl);
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "STCLSERVERLOGPATH")
                {
                    StclServerLogPath = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]);
                    LogMsg.Append(StclServerLogPath);
                    StclServerLogPath = StclServerLogPath + "\\StclServerLog_" + DateTime.Now.Date.ToString("yyyyMMdd") + "_" + CurrentFile;
                    LogMsg.Append("   StclServerLogPath: " + StclServerLogPath);

                    #region "Checking Non-Existing SysParam Values & showing list which is not exists"
                    DataTable dtSysParamCodes = new DataTable();
                    dtSysParamCodes.Columns.Add("SysParamCode", typeof(string));
                    string[] SysParams = SysParamCodes.Split(',');
                    for (int k = 0; k < SysParams.Length; k++)
                    {
                        dtSysParamCodes.Rows.Add(new object[] { SysParams[k] });
                    }
                    IEnumerable<string> idsInDataTableA = dtSysParamCodes.AsEnumerable().Select(row => ((string)row["SysParamCode"]).ToUpper());
                    IEnumerable<string> idsInDataTableB = DsGlbData.Tables[0].AsEnumerable().Select(row => ((string)row["Code"]).ToUpper());
                    IEnumerable<string> difference = idsInDataTableA.Except(idsInDataTableB);
                    string StrNonExists = String.Join(", ", difference.Select(x => x.ToString()).ToArray());
                    if (String.IsNullOrEmpty(StrNonExists))
                    {
                        continue;
                    }
                    else
                    {
                        LogMsg.AppendLine(Environment.NewLine + "Invalid SysParams: '" + StrNonExists + "' does not found in SysParam data, Please contact system administrator.");
                        GlobalSysFunctions.ShowCallerInfoSch(true, Convert.ToString(LogMsg), CurrentFile, StclServerLogPath);
                        return false;
                    }
                    #endregion "Checking Non-Existing SysParam Values & showing list which is not exists"
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "OFCCASHRECXFRBANKFEEID")
                {
                    OFCCashRecXfrBankFeeID = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]);
                    LogMsg.Append(OFCCashRecXfrBankFeeID);
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "OFCTRCASHRECADJBANKFEEID")
                {
                    OFCTrCashRecAdjBankFeeID = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]);
                    LogMsg.Append(OFCTrCashRecAdjBankFeeID);
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "ISSITEIDFILTERAPPLICABLE")
                {
                    IsSiteIDFilterApplicable = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]).ToUpper();
                    LogMsg.Append(IsSiteIDFilterApplicable);
                    continue;
                }
                if ((Convert.ToString(DsGlbData.Tables[0].Rows[i]["Code"]).ToUpper()) == "ISOFCAPPLICABLE")
                {
                    ISOFCApplicable = Convert.ToString(DsGlbData.Tables[0].Rows[i]["Value"]).ToUpper();
                    LogMsg.Append(ISOFCApplicable);
                    continue;
                }
            }
            return true;
        }
        #endregion "Public Static methods"

    }
}
