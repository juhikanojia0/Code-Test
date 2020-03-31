using System;
using System.Linq;
using Erp;
using Ice;
using Erp.Tablesets;
using Epicor.Customization.Bpm;
using System.Globalization;
using Stcl.Global.GlobalSysInfo;
using Stcl.Global.GlobalProcedures;
using Stcl.Global.GetWarrantRelAvailBudget;
using Stcl.Global.ValidateGLAccount;
using Stcl.Global.GetTotalAllocationAmt;
using Stcl.Global.GetUnallocatedBalance;
using Stcl.Global.ValidateGLJournalEntryAppr;
using System.Text;


/*
Version		Project     Author			    Date		    Purpose
1.0			CIFMIS      Sangram Kulkarni    24/02/2015		New project created for Warrant Release Withdrawal Entry
2.0.0.0		CIFMIS      Mahesh Deore        09/05/2016	    Upgraded references from 10.0 to 10.1.400.8
2.0.0.1		CIFMIS      Pritesh Parmar      11/05/2016	    Incorporated 10.0 changes to 10.1
2.0.0.2		CIFMIS      Mahesh Deore        20/05/2016	    Changed the dll references from 10.1.400.9 changes to 10.1.400.1
2.0.0.3		CIFMIS      Pritesh Parmar      22/06/2016	    Now calculating SubBudgClass Value, Vote Code from selected GL Account 
2.0.0.4 	CIFMIS      Mahesh Deore        22/07/2016	    Header & Line Comment On Swtiches of Header & Line Comment validations for WarrantReleaseWithdrawalEntry
2.0.0.5     CIFMIS      Pritesh Parmar      20/09/2016      Set ToUpper in every global values and comparision, 
2.0.0.6     CIFMIS      Pritesh Parmar      14/03/2017      12873 - Performance Issue - Mainly All Transaction Forms 
2.0.0.7     CIFMIS      Mahesh Deore        04/07/2017      Upgrade from 10.1.400.1 to 10.1.600.5
2.0.0.8     CIFMIS      Rajesh              27/07/2017      Performance Issue - VSO Id - 14443
2.0.0.9     CIFMIS      Shekahr Chaudhary   09/08/2017      1) Changed AsEnumerable() with AsQueryable() for performance improvement for DB Objects.
                                                            2) "global constant" changed with "sys param" in exception message as suggested by Pritesh Parmar.
2.0.0.10    CIFMIS      Rajesh              21/Feb/2017      PBID - 16097 Task Id - 16146 
2.0.0.11    CIFMIS      Rajesh              26/02/2019      Bug Id- 21242 , Task Id - 21869
2.0.0.12    CIFMIS      Rajesh              12/04/2019      Bug Id- 22630 MOFKL- Warrant Release control  Number misbehaving due to WR  GL import scheduler
2.0.0.13    CIFMIS      Rajesh              14/05/2019      Bug Id- 22561 MOF: Exchequer Withdrawal Control Number Creation
 * */

namespace Stcl.CIFMIS.WarrantReleaseWithdrawalEntry
{
    public class WarrantReleaseWithdrawalEntry : ContextBoundBase<ErpContext>
    {
        private Erp.ErpContext dataContext;
        private static Erp.ErpContext IceDtContext = null;
#pragma warning disable CS0618 // Type or member is obsolete
        public WarrantReleaseWithdrawalEntry(ErpContext ctx) : base(ctx)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            IceDtContext = ctx;
        }
        ErpContext Dbctx = new ErpContext();
        GlobalProcedures gblProc = new GlobalProcedures(IceDtContext);
        ValidateGLAccount ValdGLAcct = new ValidateGLAccount(IceDtContext);
        ValidateGLJournalEntryAppr ValdGLAppr = new ValidateGLJournalEntryAppr(IceDtContext);
        BusinessObject ObjBO = new BusinessObject();
        string SessionBookId = string.Empty;
        string SessionJournalCode = string.Empty;       

        public void UpdateBefore(Erp.Tablesets.GLJournalEntryTableset ds, Ice.Tablesets.ContextTableset ctxx, string warrantRelBookID, string wrwJrnCode)
        {
            StringBuilder LogMsg = new StringBuilder();
            try
            {
                #region Get SessionBookId / System Audit Information / Global Data
                var bpmRow = ctxx.BpmData.FirstOrDefault();
                if (bpmRow != null && !String.IsNullOrEmpty(bpmRow.ShortChar01))
                {
                    SessionBookId = Convert.ToString(gblProc.GetSysParam(bpmRow.ShortChar01)).ToUpper();
                    SessionJournalCode = Convert.ToString(gblProc.GetSysParam(bpmRow.ShortChar02)).ToUpper();
                }
                else
                {
                    throw new BLException("CallContaxt Bpm data not found OR Invalid SessionBookId and SessionJournalCode, Please contact administrator");
                }
                LogMsg.AppendLine("UpdateBefore => SessionBookId : " + SessionBookId + "    SessionJournalCode : " + SessionJournalCode);

                string ErrorMessage = string.Empty;
                string TreasuryCompany = Convert.ToString(gblProc.GetSysParam("TreasuryCompany")).ToUpper();
                string SubWarrantBookID = Convert.ToString(gblProc.GetSysParam("SubWarrantBookID")).ToUpper();
                string PayableAcct = Convert.ToString(gblProc.GetSysParam("PayableAcct")).ToUpper();
                string BudgetBookID = Convert.ToString(gblProc.GetSysParam("BudgetBookID")).ToUpper();
                string WRJrnCode = Convert.ToString(gblProc.GetSysParam("WarrantRelJrnCode")).ToUpper();
                string ISSBCSegValue = Convert.ToString(gblProc.GetSysParam("ISSBCSegValue")).ToUpper();
                string SubBudgClassSegNbr = Convert.ToString(gblProc.GetSysParam("SubBudgClassSegNbr"));
                string VoteSegNbr = Convert.ToString(gblProc.GetSysParam("VoteSegNbr"));
                string LineComment = Convert.ToString(gblProc.GetSysParam("LineComment")).ToUpper();
                string HeaderComment = Convert.ToString(gblProc.GetSysParam("HeaderComment")).ToUpper();
                Int16 CompanySegNbr = Convert.ToInt16(gblProc.GetSysParam("CompanySegNbr"));
                string IsVoteToMultipleCompanyApplicable = Convert.ToString(gblProc.GetSysParam("IsVoteToMultipleCompanyApplicable")).ToUpper();

                LogMsg.AppendLine("UpdateBefore => Global Constant Values Created");
                #endregion

                #region Validation of global constant values
                if (TreasuryCompany == string.Empty)
                {
                    throw new BLException("TreasuryCompany value does not found in sys param table, Please contact administrator");
                }

                if (SubWarrantBookID == string.Empty)
                {
                    throw new BLException("SubWarrantBookID value does not found in sys param table, Please contact administrator");
                }

                if (PayableAcct == string.Empty)
                {
                    throw new BLException("PayableAcct value does not found in sys param table, Please contact administrator");
                }

                if (BudgetBookID == string.Empty)
                {
                    throw new BLException("BudgetBookID value does not found in sys param table, Please contact administrator");
                }

                if (WRJrnCode == string.Empty)
                {
                    throw new BLException("WRJrnCode value does not found in sys param table, Please contact administrator");
                }

                if (ISSBCSegValue == string.Empty)
                {
                    throw new BLException("ISSBCSegValue value does not found in sys param table, Please contact administrator");
                }

                if (SubBudgClassSegNbr == string.Empty)
                {
                    throw new BLException("SubBudgClassSegNbr value does not found in sys param table, Please contact administrator");
                }

                if (VoteSegNbr == string.Empty)
                {
                    throw new BLException("VoteSegNbr value does not found in sys param table, Please contact administrator");
                }

                if (LineComment == string.Empty)
                {
                    throw new BLException("LineComment value does not found in sys param table, Please contact administrator");
                }

                if (HeaderComment == string.Empty)
                {
                    throw new BLException("HeaderComment value does not found in sys param table, Please contact administrator");
                }
                #endregion

                if (SessionBookId == warrantRelBookID && SessionJournalCode == wrwJrnCode)
                {
                    #region Code for validation for approval
                    ValdGLAppr.ValidateApprSetups(ds);
                    LogMsg.AppendLine("UpdateBefore => ValidateApprSetups > Code for validation for approval success");

                    ValdGLAppr.StopUpdateOfApprovedRecs(ds);
                    LogMsg.AppendLine("UpdateBefore => StopUpdateOfApprovedRecs > Code for validation for approval success");
                    #endregion

                    #region GLJrnHed Start
                    foreach (var DataGLJrnHedRow in (from ttGLJrnHedRow1 in ds.GLJrnHed.AsEnumerable()
                                                     where ttGLJrnHedRow1.Company == Session.CompanyID &&
                                                     ttGLJrnHedRow1.BookID.ToUpper() == SessionBookId &&
                                                       (string.Equals(ttGLJrnHedRow1.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                       string.Equals(ttGLJrnHedRow1.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                     select ttGLJrnHedRow1))
                    {
                        #region Set the Central Payment Warrant Release. This flag determines if bank adjustments should be generated in the Treasury company or not.*/
                        string SubBudgetClass = Convert.ToString(DataGLJrnHedRow["SubBudgetCls_c"]);
                        if (ISSBCSegValue.ToUpper() == "TRUE")
                        {
                            if (Session.CompanyID.ToUpper() == TreasuryCompany && SessionBookId == warrantRelBookID && !string.IsNullOrEmpty(SubBudgetClass))
                            {
                                var QryData = (Dbctx.UD100A.Where(t => t.Company == TreasuryCompany &&
                                    t.ChildKey1 == SubBudgetClass)).Select(t => new
                                    {
                                        PymtFromTreasury = t.CheckBox01,
                                        GenerateBankAdj = t.CheckBox02
                                    }).FirstOrDefault();
                                if (QryData != null)
                                {
                                    DataGLJrnHedRow["PymtFromTreasury_c"] = QryData.PymtFromTreasury;
                                    DataGLJrnHedRow["GenerateBankAdj_c"] = QryData.GenerateBankAdj;
                                }
                            }
                        }
                        LogMsg.AppendLine("UpdateBefore => Set the Central Payment Warrant Release Flag seccess");
                        #endregion

                        #region Update CommentText Header
                        string CommentText = DataGLJrnHedRow.CommentText;
                        if (HeaderComment == "TRUE" && string.IsNullOrEmpty(CommentText))
                        {
                            throw new BLException("Header comments should not be empty!");
                        }
                        #endregion

                        #region Validate Cash Allocation Dropdown Is Empty
                        string RefTrxCtrlNum = DataGLJrnHedRow["RefTrxCtrlNum_c"].ToString();
                        if (string.IsNullOrEmpty(RefTrxCtrlNum))
                        {
                            throw new BLException("Please select one from the dropdown list.");
                        }
                        #endregion

                        #region Update IsFullWithdrawal
                        string TrxCtrlNum = Convert.ToString(DataGLJrnHedRow["TrxCtrlNum_c"]);

                        bool IsPartialWithdrawal = false;

                        if (string.IsNullOrEmpty(TrxCtrlNum) && !string.IsNullOrEmpty(SubBudgetClass))
                        {
                            if (ISSBCSegValue.ToUpper() == "TRUE")
                            {
                                var QryData = (Dbctx.UD100A.AsQueryable().Where(t => t.Company.ToUpper() == TreasuryCompany
                                    && t.ChildKey1 == SubBudgetClass)).Select
                                    (t => new
                                    {
                                        IsPartialWithdraw = t.IsPartialWithdrawal_c,
                                    }).FirstOrDefault();
                                if (QryData != null)
                                {
                                    IsPartialWithdrawal = QryData.IsPartialWithdraw;
                                }
                            }
                        }
                        else
                        {
                            var ttGLJrnHed = (Dbctx.GLJrnHed.AsQueryable().Where(b => b.Company == Session.CompanyID &&
                                b.BookID.ToUpper() == warrantRelBookID &&
                                b.TrxCtrlNum_c == TrxCtrlNum).Select(
                            b => new
                            {
                                IsPartialWithdraw = b.IsPartialWithdrawal_c,
                            }).FirstOrDefault());
                            if (ttGLJrnHed != null)
                            {
                                IsPartialWithdrawal = ttGLJrnHed.IsPartialWithdraw;
                            }
                        }
                        DataGLJrnHedRow["IsPartialWithdrawal_c"] = IsPartialWithdrawal;

                        #endregion

                        string GroupId = DataGLJrnHedRow.GroupID;
                        int FiscalYear = DataGLJrnHedRow.FiscalYear;
                        int JournalNum = DataGLJrnHedRow.JournalNum;
                        string FiscalYearSuffix = DataGLJrnHedRow.FiscalYearSuffix;
                        string JournalCode = DataGLJrnHedRow.JournalCode.ToUpper();
                        string FiscalCalendarID = DataGLJrnHedRow.FiscalCalendarID;


                        #region PymtFromTreasury_c / Set JEDate from WarrantRelease
                        DateTime? WarrantRelJEDate = null;
                        string RefTrxCtrlNumber = Convert.ToString(DataGLJrnHedRow["RefTrxCtrlNum_c"]);
                        if (!string.IsNullOrEmpty(RefTrxCtrlNumber))
                        {
                            var ttGLJrnHed = (Dbctx.GLJrnHed.AsQueryable().Where(b => b.Company == Session.CompanyID &&
                                b.BookID.ToUpper() == warrantRelBookID &&
                                b.JournalCode.ToUpper() == WRJrnCode &&
                                b.TrxCtrlNum_c == RefTrxCtrlNumber).Select(
                                b => new
                                {
                                    JEDate = b.JEDate,
                                }).FirstOrDefault());
                            if (ttGLJrnHed != null)
                            {
                                WarrantRelJEDate = Convert.ToDateTime(ttGLJrnHed.JEDate);
                            }
                        }
                        #endregion

                        #region validate apply date
                        DateTime? SrcEndDate = null;
                        DateTime? DestEndDate = null;
                        if (WarrantRelJEDate != null)
                        {
                            foreach (var ReturnDateValue in Dbctx.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_GetFiscalPeriod " + "@company = {0},@acctType={1},@applyDate={2}", DataGLJrnHedRow.Company, "I", WarrantRelJEDate))
                            {
                                SrcEndDate = ReturnDateValue.EndDate;
                            }
                            foreach (var ReturnDateValue in Dbctx.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_GetFiscalPeriod " + "@company = {0},@acctType={1},@applyDate={2}", DataGLJrnHedRow.Company, "I", DataGLJrnHedRow.JEDate))
                            {
                                DestEndDate = ReturnDateValue.EndDate;
                            }

                            if (SrcEndDate != DestEndDate)
                            {
                                throw new BLException("The parent and the child transaction must be applied in the same financial year");
                            }
                            else
                            {
                                if (DataGLJrnHedRow.JEDate < WarrantRelJEDate)
                                {
                                    throw new BLException("The Withdrawal of Cash Allocation must be applied after Cash Allocation #" + WarrantRelJEDate + "#");
                                }
                            }
                        }

                        #endregion

                        #region validate budgetclass and its GLAccount
                        if (ISSBCSegValue.ToUpper() == "TRUE")
                        {
                            var DataGlJrnDtlMnlRow = (from ttGlJrnHedDtlMnlRow in Dbctx.GLJrnDtlMnl.AsQueryable()
                                                      where ttGlJrnHedDtlMnlRow.Company.ToUpper() == TreasuryCompany &&
                                                            ttGlJrnHedDtlMnlRow.BookID.ToUpper() == warrantRelBookID &&
                                                            ttGlJrnHedDtlMnlRow.FiscalYear == FiscalYear &&
                                                            ttGlJrnHedDtlMnlRow.FiscalYearSuffix == FiscalYearSuffix &&
                                                            ttGlJrnHedDtlMnlRow.JournalCode.ToUpper() == JournalCode &&
                                                            ttGlJrnHedDtlMnlRow.JournalNum == JournalNum &&
                                                            ttGlJrnHedDtlMnlRow.FiscalCalendarID == FiscalCalendarID &&
                                                            ttGlJrnHedDtlMnlRow.GroupID == GroupId &&
                                                            ttGlJrnHedDtlMnlRow.TransAmt < 0
                                                      select new
                                                      {
                                                          SubBudgetCls = ttGlJrnHedDtlMnlRow.SubBudgetCls_c
                                                      }).FirstOrDefault();
                            {
                                if (DataGlJrnDtlMnlRow != null)
                                {
                                    string SBCSegmentValue = DataGlJrnDtlMnlRow.SubBudgetCls;
                                    LogMsg.AppendLine("UpdateBefore => 1 SBCSegmentValue : " + SBCSegmentValue + "     SubBudgetClass : " + SubBudgetClass);
                                    if (SBCSegmentValue != SubBudgetClass)
                                    {
                                        throw new BLException("Budget Class selected in the Journal Header and the account do not match. Please select account that has matching sub budget class.");
                                    }
                                }
                            }
                            LogMsg.AppendLine("UpdateBefore => Validated Sudgetclass and its GLAccount Success");
                        }
                        #endregion
  
                        #region Get Max Transaction Control No
                        if (Convert.ToString(DataGLJrnHedRow["TrxCtrlNum_c"]) == "")
                        {
                            foreach (var DataGetMaxNo in Dbctx.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_GetMaxNo " + "@company = {0}, @journalcode = {1}", Session.CompanyID, JournalCode))
                            {
                                ObjBO.MaxNo = DataGetMaxNo.MaxNo;
                                ObjBO.Prefix = DataGetMaxNo.Prefix;
                            }
                            if (ObjBO.MaxNo > 0 && !string.IsNullOrEmpty(ObjBO.Prefix))
                            {
                                //DataGLJrnHedRow["TrxCtrlNum_c"] = FiscalYear.ToString().Substring(2, 2) + "CRW" + ObjBO.MaxNo;
                                DataGLJrnHedRow["TrxCtrlNum_c"] = FiscalYear.ToString().Substring(2, 2) + ObjBO.Prefix + ObjBO.MaxNo;
                                LogMsg.AppendLine("UpdateBefore => Get Max Transaction Control No Procedure Success");
                            }
                            else
                            {
                                LogMsg.AppendLine("UpdateBefore => TrxCtrlNum Or Prefix is Invalid, Validate Stcl_CIFMIS_Global_GetMaxNo. Session.CompanyID: " + Session.CompanyID + ", JournalCode: " + JournalCode);
                                throw new BLException("TrxCtrlNum Or Prefix is Invalid, Please Contact to Administrator.");
                            }

                        }
                        #endregion

                    }
                    #endregion GLJrnHed End

                    #region GLJrnDtlMnl Line Comments Should Not Be Empty
                    foreach (var GLJrnDtlMnlRow in (from DataGLJrnDtlMnl in ds.GLJrnDtlMnl.AsEnumerable()
                                                    where DataGLJrnDtlMnl.Company.ToUpper() == TreasuryCompany &&
                                                            DataGLJrnDtlMnl.BookID.ToUpper() == warrantRelBookID &&
                                                            (string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                            string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                    select DataGLJrnDtlMnl))
                        if (GLJrnDtlMnlRow != null)
                        {
                            #region Line Amount
                            decimal LineAmount = GLJrnDtlMnlRow.TotCredit;
                            if (LineAmount == 0 && GLJrnDtlMnlRow.SegValue1.ToUpper() != PayableAcct)
                            {
                                throw new BLException("Credit Line Amount can not be 0 (Zero) !");
                            }
                            #endregion
                            LogMsg.AppendLine("UpdateBefore => Credit Line Amount can not be 0 (Zero) !");

                            #region update WRCompany_c in GLJrnDtlMnl
                            if (GLJrnDtlMnlRow.SegValue1.ToUpper() == PayableAcct && GLJrnDtlMnlRow.TotDebit != 0)
                            {
                                GLJrnDtlMnlRow["WRCompany_c"] = TreasuryCompany;
                            }
                            LogMsg.AppendLine("UpdateBefore => Update WRCompany_c (" + TreasuryCompany + ") in GLJrnDtlMnl Success");
                            #endregion
                        }
                    #endregion

                    #region Foreach loop for GLJrnDtlMnl-set OrigDebitAmount and  OrigGLAccount
                    foreach (var DataGlJrnDtlMnlRow in (from ttGlJrnHedDtlMnlRow in ds.GLJrnDtlMnl.AsEnumerable()
                                                        where ttGlJrnHedDtlMnlRow.BookID.ToUpper() == SessionBookId &&
                                                              String.IsNullOrEmpty(ttGlJrnHedDtlMnlRow.RowMod)
                                                        select ttGlJrnHedDtlMnlRow))
                    {
                        if (DataGlJrnDtlMnlRow != null)
                        {
                            ObjBO.OrigCreditAmount = Convert.ToDecimal(DataGlJrnDtlMnlRow.TotCredit);
                            ObjBO.OrigGLAccount = DataGlJrnDtlMnlRow.GLAccount.ToString();
                        }
                        LogMsg.AppendLine("UpdateBefore => Set OrigDebitAmount And OrigGLAccount Success");
                    }
                    #endregion

                    #region Validate GL Account
                    ValdGLAcct.Validate(ds);
                    LogMsg.AppendLine("UpdateBefore => Validate GL Account Success");
                    #endregion

                    #region GLJrnDtlMnl
                    foreach (var DataGLJrnDtlMnlJoinRow in ((from DataGLJrnDtlMnl in ds.GLJrnDtlMnl.AsEnumerable()
                                                             where DataGLJrnDtlMnl.Company.ToUpper() == TreasuryCompany &&
                                                                    DataGLJrnDtlMnl.BookID.ToUpper() == warrantRelBookID &&
                                                                    (string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                                    string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase)) &&
                                                                    DataGLJrnDtlMnl.TotCredit != 0 &&
                                                                    DataGLJrnDtlMnl.SegValue1.ToUpper() != PayableAcct
                                                             select DataGLJrnDtlMnl
                                                             )))
                    {
                        if (DataGLJrnDtlMnlJoinRow != null)
                        {
                            string SubBudgetClass = string.Empty;

                            #region Update CommentText Line
                            string CommentText = DataGLJrnDtlMnlJoinRow.CommentText;
                            if (LineComment == "TRUE" && string.IsNullOrEmpty(CommentText))
                            {
                                throw new BLException("Line comments should not be empty!");
                            }
                            #endregion

                            #region validate sudgetclass and its GLAccount
                            if (ISSBCSegValue.ToUpper() == "TRUE")
                            {
                                var GLJrnHedRow1 = (from ttGLJrnHed in Dbctx.GLJrnHed.AsQueryable()
                                                    where ttGLJrnHed.Company == DataGLJrnDtlMnlJoinRow.Company &&
                                                        ttGLJrnHed.BookID.ToUpper() == DataGLJrnDtlMnlJoinRow.BookID.ToUpper() &&
                                                        ttGLJrnHed.FiscalYear == DataGLJrnDtlMnlJoinRow.FiscalYear &&
                                                        ttGLJrnHed.FiscalYearSuffix == DataGLJrnDtlMnlJoinRow.FiscalYearSuffix &&
                                                        ttGLJrnHed.JournalCode.ToUpper() == DataGLJrnDtlMnlJoinRow.JournalCode.ToUpper() &&
                                                        ttGLJrnHed.JournalNum == DataGLJrnDtlMnlJoinRow.JournalNum &&
                                                        ttGLJrnHed.FiscalCalendarID == DataGLJrnDtlMnlJoinRow.FiscalCalendarID
                                                    select new
                                                    {
                                                        SubBudgetCls = ttGLJrnHed.SubBudgetCls_c
                                                    }
                                                    ).FirstOrDefault();
                                if (GLJrnHedRow1 != null)
                                {
                                    SubBudgetClass = Convert.ToString(GLJrnHedRow1.SubBudgetCls);
                                }

                                string[] arrGL = DataGLJrnDtlMnlJoinRow.GLAccount.Split('|');
                                LogMsg.AppendLine("UpdateBefore => arrGL.Length : " + arrGL.Length);

                                string SBCSegmentValue = ISSBCSegValue.ToUpper() == "TRUE" ? arrGL[Convert.ToInt32(SubBudgClassSegNbr) - 1] : string.Empty;
                                LogMsg.AppendLine("UpdateBefore => 2 SBCSegmentValue : " + SBCSegmentValue + "     SubBudgetClass : " + SubBudgetClass);

                                if (SBCSegmentValue != SubBudgetClass)
                                {
                                    throw new BLException("Budget Class selected in the Journal Header and the account do not match. Please select account that has matching sub budget class.");
                                }
                            }
                            #endregion

                            string GlAccountDisp = string.Empty;

                            foreach (var DataGetGLAcctDisp in Dbctx.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_GetGLAcctDisp " + "@company = {0},@cOACode={1},@glAccount={2},@glAccountDisp={3}", DataGLJrnDtlMnlJoinRow.Company, DataGLJrnDtlMnlJoinRow.COACode, DataGLJrnDtlMnlJoinRow.GLAccount, GlAccountDisp))
                            {
                                GlAccountDisp = DataGetGLAcctDisp.GlAcctDisp;
                            }
                            LogMsg.AppendLine("UpdateBefore => GlAccountDisp Success ");

                            string[] arrGL1 = DataGLJrnDtlMnlJoinRow.GLAccount.Split('|');
                            LogMsg.AppendLine("UpdateBefore => arrGL.Length : " + arrGL1.Length);

                            string VoteCode = arrGL1[Convert.ToInt32(VoteSegNbr) - 1];

                            ObjBO.Vote = VoteCode;
                            LogMsg.AppendLine("UpdateBefore => VoteCode : " + VoteCode);

                            if (ObjBO.OrigGLAccount != DataGLJrnDtlMnlJoinRow.GLAccount)
                            {
                                ObjBO.OrigCreditAmount = 0;
                            }

                            GetWarrantRelAvailBudget ObjGetWarrantRelAvailBudget = new GetWarrantRelAvailBudget(IceDtContext);
                            ObjBO.AvailBudget = ObjGetWarrantRelAvailBudget.GetWarRelAvailBudget(ObjBO.Vote, TreasuryCompany, BudgetBookID, SessionBookId, DataGLJrnDtlMnlJoinRow.GLAccount, Convert.ToDateTime(DataGLJrnDtlMnlJoinRow.JEDate));
                            LogMsg.AppendLine("UpdateBefore => ObjGetWarrantRelAvailBudget Object Created,  ObjBO.AvailBudget : " + ObjBO.AvailBudget);

                            string CostCenter = string.Empty;
                            bool CostCenterAlloc = false;
                            if (DataGLJrnDtlMnlJoinRow.BookID.ToUpper() == warrantRelBookID)
                            {
                                CostCenterAlloc = true;
                                CostCenter = string.Empty;
                            }
                            int CountVoteCompany = 0;
                            foreach (var DataGetVote in Dbctx.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_GetVote " + "@vote = {0}", ObjBO.Vote))
                            {
                                ObjBO.VoteCompany = DataGetVote.VoteCompany;
                                CountVoteCompany = CountVoteCompany + 1;
                            }
                            LogMsg.AppendLine("UpdateBefore => CountVoteCompany : " + CountVoteCompany.ToString());
                            if (CountVoteCompany > 1 && IsVoteToMultipleCompanyApplicable == "TRUE")
                            {
                                ObjBO.VoteCompany = arrGL1[CompanySegNbr - 1];
                                LogMsg.AppendLine("UpdateBefore => Inside If-VoteCompany : " + ObjBO.VoteCompany.ToString());
                            }
                            DataGLJrnDtlMnlJoinRow["WRCompany_c"] = ObjBO.VoteCompany;

                            LogMsg.AppendLine("UpdateBefore => GetVote (" + ObjBO.VoteCompany + ") Procedure Success");

                            #region Validate existance of company in company master
                            var CompanyRow = (from ttcompany in Dbctx.Company.AsQueryable()
                                              where ttcompany.Company1 == ObjBO.VoteCompany
                                              select ttcompany.Name).FirstOrDefault();
                            {
                                if (CompanyRow == null)
                                {
                                    throw new BLException(ObjBO.VoteCompany + " - Does not exist in company master");
                                }
                            }
                            #endregion

                            var ttGLJrnHed1 = (Dbctx.GLJrnHed.AsQueryable().Where(b =>
                                b.Company == Session.CompanyID &&
                                b.BookID.ToUpper() == warrantRelBookID &&
                                b.FiscalYear == DataGLJrnDtlMnlJoinRow.FiscalYear &&
                                b.FiscalYearSuffix == DataGLJrnDtlMnlJoinRow.FiscalYearSuffix &&
                                b.JournalCode.ToUpper() == SessionJournalCode &&
                                b.JournalNum == DataGLJrnDtlMnlJoinRow.JournalNum &&
                                b.FiscalCalendarID == DataGLJrnDtlMnlJoinRow.FiscalCalendarID &&
                                b.GroupID == DataGLJrnDtlMnlJoinRow.GroupID).Select(
                            b => new
                            {
                                SubBudgetCls_c =  b.SubBudgetCls_c, 
                                RefTrxCtrlNum_c = b.RefTrxCtrlNum_c, 
                                IsPartialWithdrawal_c = b.IsPartialWithdrawal_c
                            }).FirstOrDefault());

                            string RefTrxCtrlNum = string.Empty;
                            bool IsPartialWithDrawal = false;
                            if (ttGLJrnHed1 != null)
                            {
                                SubBudgetClass = ttGLJrnHed1.SubBudgetCls_c;
                                RefTrxCtrlNum = ttGLJrnHed1.RefTrxCtrlNum_c;
                                IsPartialWithDrawal = ttGLJrnHed1.IsPartialWithdrawal_c;
                            }
                            LogMsg.AppendLine("UpdateBefore => SubBudgetClass : " + SubBudgetClass + "    RefTrxCtrlNum : " + RefTrxCtrlNum + "    IsPartialWithDrawal : " + IsPartialWithDrawal);

                            ////Get unallocated balance for Warrant relese i.e WR-SW
                            GetTotalAllocationAmt TotAllocAmt = new GetTotalAllocationAmt(IceDtContext);
                            decimal SWTrxAmt = TotAllocAmt.GetTotalAllocationAmount(ObjBO.VoteCompany, SubWarrantBookID, "", RefTrxCtrlNum, DataGLJrnDtlMnlJoinRow.GLAccount, Convert.ToDateTime(DataGLJrnDtlMnlJoinRow.JEDate), 1, 0);
                            LogMsg.AppendLine("UpdateBefore => SWTrxAmt : " + SWTrxAmt);

                            decimal UnallocatedTrxAmt = 0;
                            decimal UnallocatedBankBal = 0;
                            decimal UnallocatedCCBudget = 0;
                            bool CentralPayment = false;

                            GetUnallocatedBalance UnAllocBal = new GetUnallocatedBalance(IceDtContext);
                            UnAllocBal.GetUnallocatedBalanceAmount(ObjBO.VoteCompany, SubBudgetClass, CostCenter, RefTrxCtrlNum, DataGLJrnDtlMnlJoinRow.GLAccount, out UnallocatedTrxAmt, out UnallocatedBankBal, out UnallocatedCCBudget, CostCenterAlloc, out CentralPayment);
                            LogMsg.AppendLine("UpdateBefore => UnallocatedTrxAmt : " + UnallocatedTrxAmt + "    UnallocatedBankBal : " + UnallocatedBankBal + "    UnallocatedCCBudget : " + UnallocatedCCBudget + "    CentralPayment : " + CentralPayment);

                            string CurrenyCode = DataGLJrnDtlMnlJoinRow.CurrencyCode;
                            decimal AvialWithAmt = (UnallocatedTrxAmt + ObjBO.OrigCreditAmount);        /*- DataGLJrnDtlMnlJoinRow.TotCredit*/
                            LogMsg.AppendLine("UpdateBefore => CurrenyCode : " + CurrenyCode + "  AvialWithAmt : " + AvialWithAmt + "  CostCenterAlloc : " + CostCenterAlloc);

                            if ((CostCenterAlloc == true) && ((UnallocatedTrxAmt + ObjBO.OrigCreditAmount - DataGLJrnDtlMnlJoinRow.TotCredit) < 0))
                            {
                                ErrorMessage = "Insufficient Cash Allocation for line: " + Convert.ToString(GlAccountDisp) +
                                                  ", Available: " + CurrenyCode + " " + (UnallocatedTrxAmt + ObjBO.OrigCreditAmount).ToString("0,0.00", CultureInfo.InvariantCulture);
                                throw new BLException(ErrorMessage);
                            }
                            else if ((CostCenterAlloc == true) && ((UnallocatedTrxAmt + ObjBO.OrigCreditAmount - DataGLJrnDtlMnlJoinRow.TotCredit) >= 0))
                            {
                                if ((IsPartialWithDrawal == false) && (Convert.ToInt32(SWTrxAmt) > 0))
                                {
                                    ErrorMessage = "Subwarrant already issued for current warrant release." +
                                                   "Insufficient Cash Allocation for line : " + Convert.ToString(GlAccountDisp) +
                                                   ", Available : " + CurrenyCode + " " + (0.00).ToString("0,0.00", CultureInfo.InvariantCulture);
                                    throw new BLException(ErrorMessage);
                                }
                                else if ((IsPartialWithDrawal == false) && (Convert.ToInt32(SWTrxAmt) == 0) && (AvialWithAmt != DataGLJrnDtlMnlJoinRow.TotCredit))
                                {
                                    ErrorMessage = "You are only allowed to do full withdrawal! " + Convert.ToString(GlAccountDisp) +
                                                   ", Available       : " + CurrenyCode + " " + (UnallocatedTrxAmt + ObjBO.OrigCreditAmount).ToString("0,0.00", CultureInfo.InvariantCulture);
                                    throw new BLException(ErrorMessage);
                                }
                                else if ((IsPartialWithDrawal == false) && (Convert.ToInt32(SWTrxAmt) == 0) && (AvialWithAmt == DataGLJrnDtlMnlJoinRow.TotCredit))
                                {
                                    string Message = "Cash Allocation amount for line : " + Convert.ToString(GlAccountDisp) +
                                                            ", Available       : " + CurrenyCode + " " + (UnallocatedTrxAmt + ObjBO.OrigCreditAmount).ToString("0,0.00", CultureInfo.InvariantCulture);
                                    InfoMessage.Publish(Message, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual);
                                }
                                else if ((IsPartialWithDrawal == true) && (DataGLJrnDtlMnlJoinRow.TotCredit <= AvialWithAmt))
                                {
                                    string Message = "Cash Allocation amount for line : " + Convert.ToString(GlAccountDisp) +
                                                             ", Available       : " + CurrenyCode + " " + (UnallocatedTrxAmt + ObjBO.OrigCreditAmount).ToString("0,0.00", CultureInfo.InvariantCulture);
                                    InfoMessage.Publish(Message, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual);
                                }
                                else if ((IsPartialWithDrawal == true) && (DataGLJrnDtlMnlJoinRow.TotCredit > AvialWithAmt))
                                {
                                    ErrorMessage = "Insufficient Cash Allocation for line : " + Convert.ToString(GlAccountDisp) +
                                                   ", Available : " + CurrenyCode + " " + (UnallocatedTrxAmt + ObjBO.OrigCreditAmount).ToString("0,0.00", CultureInfo.InvariantCulture);
                                    throw new BLException(ErrorMessage);
                                }
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    throw new BLException("SessionBookId and SessionJournalCode does not match with global constant value, Please check global constant value");
                }
                LogMsg.AppendLine("UpdateBefore => End.......");
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("Warrant Release Withdrawal => " + LogMsg.ToString(),ex);
                throw new BLException("Warrant Release Withdrawal => " + ex.Message);
            }
        }
    }
}
