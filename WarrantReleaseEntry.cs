using System;
using System.Linq;
using Erp;
using Ice;
using Epicor.Customization.Bpm;
using System.Globalization;
using Stcl.Global.GlobalSysInfo;
using Stcl.Global.GlobalProcedures;
using Stcl.Global.GetWarrantRelAvailBudget;
using Stcl.Global.ValidateGLAccount;
using Stcl.Global.ValidateGLJournalEntryAppr;
using System.Text;

/*
Version		Project     Author			    Date		    Purpose
1.0			CIFMIS      Pritesh Parmar		27/05/2015		Validation for Warrant Release Entry (GLJournalEntry)
2.0     	CIFMIS      Pritesh Parmar		22/12/2015		Added validation, Do not save transaction where amount is 0 (zero) 
                                                            If we do not validate this then it will update WRCompany_c to blank or '000'
3.0         CIFMIS      Pritesh Parmar		01/02/2016		Description/Reference number must be unique
2.0.0.0		CIFMIS      Mahesh Deore        09/05/2016	    Upgraded references from 10.0 to 10.1.400.8
2.0.0.1		CIFMIS      Pritesh Parmar      11/05/2016	    Incorporated 10.0 changes to 10.1
2.0.0.2		CIFMIS      Mahesh Deore        20/05/2016	    Changed the dll references from 10.1.400.9 changes to 10.1.400.1
2.0.0.3		CIFMIS      Pritesh Parmar      22/06/2016	    Now calculating SubBudgClass Value, Vote Code from selected GL Account
2.0.0.4 	CIFMIS      Mahesh Deore        22/07/2016	    Line Comment On Swtiches of Line Comment validations for WarrantReleaseEntry
2.0.0.5     CIFMIS      Pritesh Parmar      20/09/2016      Set ToUpper in every global values and comparision,
2.0.0.6     CIFMIS      Pritesh Parmar      14/03/2017     12873 - Performance Issue - Mainly All Transaction Forms 
2.0.0.7     CIFMIS      Mahesh Deore        04/07/2017      Upgrade from 10.1.400.1 to 10.1.600.5
2.0.0.8     CIFMIS      Rajesh              27/07/2017      Performance Issue - VSO Id - 14443
2.0.0.9     CIFMIS      Shekahr Chaudhary   09/08/2017      1) Changed AsEnumerable() with AsQueryable() for performance improvement for DB Objects.
                                                            2) "global constant" changed with "sys param" in exception message as suggested by Pritesh Parmar.
2.0.0.10    CIFMIS      Rajesh              21/Feb/2017      PBID - 16097 Task Id - 16146 
2.0.0.11    CIFMIS      Rajesh              26/02/2019      Bug Id- 21242 , Task Id - 21869
2.0.0.12    CIFMIS      Rajesh              12/04/2019      Bug Id- 22630 MOFKL- Warrant Release control  Number misbehaving due to WR  GL import scheduler
2.0.0.13    CIFMIS      Rajesh              14/05/2019      Bug Id- 22560 MOF: Exchequer Control Number Creation

 * * * */

namespace Stcl.CIFMIS.WarrantReleaseEntry
{
    public class WarrantReleaseEntry : ContextBoundBase<ErpContext>
    {
        private Erp.ErpContext dataContext;
        private static Erp.ErpContext IceDtContext = null;
#pragma warning disable CS0618 // Type or member is obsolete
        public WarrantReleaseEntry(ErpContext ctx) : base(ctx)
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
        string ErrorMessage = string.Empty;       

        public void UpdateBefore(Erp.Tablesets.GLJournalEntryTableset ds, Ice.Tablesets.ContextTableset ctxx, string WarrantRelBookID, string WRJrnCode)
        {
            StringBuilder LogMsg = new StringBuilder();

            try
            {
                LogMsg.AppendLine("UpdateBefore => Start.......");

                #region Get SessionBookId
                var bpmRow = ctxx.BpmData.FirstOrDefault();
                if (bpmRow != null && (!String.IsNullOrEmpty(bpmRow.ShortChar01) || !String.IsNullOrEmpty(bpmRow.ShortChar02)))
                {
                    SessionBookId = Convert.ToString(gblProc.GetSysParam(bpmRow.ShortChar01)).ToUpper();
                    SessionJournalCode = Convert.ToString(gblProc.GetSysParam(bpmRow.ShortChar02)).ToUpper();
                }
                else
                {
                    throw new BLException("CallContaxt Bpm data not found OR Invalid SessionBookId and SessionJournalCode, Please contact administrator");
                }

                LogMsg.AppendLine("UpdateBefore => SessionBookId : " + SessionBookId + "    SessionJournalCode : " + SessionJournalCode);

                string TreasuryCompany = Convert.ToString(gblProc.GetSysParam("TreasuryCompany")).ToUpper();
                string PayableAcct = Convert.ToString(gblProc.GetSysParam("PayableAcct")).ToUpper();
                string BudgetBookID = Convert.ToString(gblProc.GetSysParam("BudgetBookID")).ToUpper();
                string ISSBCSegValue = Convert.ToString(gblProc.GetSysParam("ISSBCSegValue")).ToUpper();
                string SubBudgClassSegNbr = Convert.ToString(gblProc.GetSysParam("SubBudgClassSegNbr"));
                string VoteSegNbr = Convert.ToString(gblProc.GetSysParam("VoteSegNbr"));
                string LineComment = Convert.ToString(gblProc.GetSysParam("LineComment")).ToUpper();
                Int16 CompanySegNbr = Convert.ToInt16(gblProc.GetSysParam("CompanySegNbr"));
                string IsVoteToMultipleCompanyApplicable = Convert.ToString(gblProc.GetSysParam("IsVoteToMultipleCompanyApplicable")).ToUpper();

                LogMsg.AppendLine("UpdateBefore => Global Constant Values Created");
                #endregion

                #region Validation of global constant values
                if (TreasuryCompany == string.Empty)
                {
                    throw new BLException("TreasuryCompany value does not found in sys param table, Please contact administrator");
                }

                if (PayableAcct == string.Empty)
                {
                    throw new BLException("PayableAcct value does not found in sys param table, Please contact administrator");
                }

                if (BudgetBookID == string.Empty)
                {
                    throw new BLException("BudgetBookID value does not found in sys param table, Please contact administrator");
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
                #endregion

                if (SessionBookId == WarrantRelBookID && SessionJournalCode == WRJrnCode)
                {
                    #region Code for validation for approval
                    ValdGLAppr.ValidateApprSetups(ds);
                    LogMsg.AppendLine("UpdateBefore => ValidateApprSetups > Code for validation for approval success");

                    ValdGLAppr.StopUpdateOfApprovedRecs(ds);
                    LogMsg.AppendLine("UpdateBefore => StopUpdateOfApprovedRecs > Code for validation for approval success");
                    #endregion

                    #region GLJrnHed Start

                    #region Description/Reference already used. Please use another Description/Reference.
                    string DescriptionRowModBlnk = string.Empty;
                    var GLJrnHedRow = (from ttGLJrnHed in ds.GLJrnHed.AsEnumerable()
                                       where ttGLJrnHed.Company == Session.CompanyID &&
                                           ttGLJrnHed.BookID.ToUpper() == SessionBookId &&
                                           String.IsNullOrEmpty(ttGLJrnHed.RowMod)
                                       select ttGLJrnHed).FirstOrDefault();
                    if (GLJrnHedRow != null)
                    {
                        DescriptionRowModBlnk = GLJrnHedRow.Description;
                    }
                    LogMsg.AppendLine("UpdateBefore => DescriptionRowModBlnk : " + DescriptionRowModBlnk);
                    #endregion

                    foreach (var DataGLJrnHedRow in (from ttGLJrnHedRow1 in ds.GLJrnHed.AsEnumerable()
                                                     where ttGLJrnHedRow1.Company == Session.CompanyID &&
                                                     ttGLJrnHedRow1.BookID.ToUpper() == SessionBookId &&
                                                        (string.Equals(ttGLJrnHedRow1.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(ttGLJrnHedRow1.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                     select ttGLJrnHedRow1))
                    {
                        #region Set the Central Payment Warrant Release. This flag determines if bank adjustments should be generated in the Treasury company or not.*/
                        string SubBudgetClass = Convert.ToString(DataGLJrnHedRow["SubBudgetCls_c"]);
                        LogMsg.AppendLine("UpdateBefore => SubBudgetClass : " + SubBudgetClass);
                        if (ISSBCSegValue.ToUpper() == "TRUE")
                        {
                            if (SubBudgetClass == string.Empty)
                            {
                                throw new BLException("Please Select Sub Budget Class");
                            }

                            if (Session.CompanyID.ToUpper() == TreasuryCompany && SessionBookId == WarrantRelBookID && !string.IsNullOrEmpty(SubBudgetClass))
                            {
                                var QryData = (Dbctx.UD100A.Where(t => t.Company == TreasuryCompany &&
                                                t.ChildKey1 == SubBudgetClass)).Select(t => new
                                                {
                                                    PymtFromTreasury = t.CheckBox01,
                                                    GenerateBankAdj = t.CheckBox02
                                                }).FirstOrDefault();
                                if (QryData == null)
                                {
                                    throw new BLException("Invalid Sub Budget Classification Setup, Selected sub budget class code and 'Account Segment Values' of sub budget class should be same ");
                                }
                                else
                                {
                                    DataGLJrnHedRow["PymtFromTreasury_c"] = QryData.PymtFromTreasury;
                                    DataGLJrnHedRow["GenerateBankAdj_c"] = QryData.GenerateBankAdj;
                                }
                            }
                            LogMsg.AppendLine("UpdateBefore => Set the Central Payment Warrant Release Flag seccess");
                        }
                        #endregion

                        string GroupId = DataGLJrnHedRow.GroupID;
                        int FiscalYear = DataGLJrnHedRow.FiscalYear;
                        int JournalNum = DataGLJrnHedRow.JournalNum;
                        string FiscalYearSuffix = DataGLJrnHedRow.FiscalYearSuffix;
                        string JournalCode = DataGLJrnHedRow.JournalCode.ToUpper();
                        string FiscalCalendarID = DataGLJrnHedRow.FiscalCalendarID;

                       

                        #region validate sudgetclass and its GLAccount
                        if (ISSBCSegValue.ToUpper() == "TRUE")
                        {                          
                            foreach (var DataGlJrnDtlMnlRow in (from ttGlJrnHedDtlMnlRow in Dbctx.GLJrnDtlMnl.AsQueryable()
                                                                where ttGlJrnHedDtlMnlRow.Company.ToUpper() == TreasuryCompany &&
                                                                        ttGlJrnHedDtlMnlRow.BookID.ToUpper() == WarrantRelBookID &&
                                                                        ttGlJrnHedDtlMnlRow.FiscalYear == FiscalYear &&
                                                                        ttGlJrnHedDtlMnlRow.FiscalYearSuffix == FiscalYearSuffix &&
                                                                        ttGlJrnHedDtlMnlRow.JournalCode.ToUpper() == JournalCode &&
                                                                        ttGlJrnHedDtlMnlRow.JournalNum == JournalNum &&
                                                                        ttGlJrnHedDtlMnlRow.FiscalCalendarID == FiscalCalendarID &&
                                                                        ttGlJrnHedDtlMnlRow.GroupID == GroupId &&
                                                                        ttGlJrnHedDtlMnlRow.TransAmt > 0
                                                                select new
                                                                {
                                                                    SBCSegmentValue = ttGlJrnHedDtlMnlRow.SubBudgetCls_c
                                                                }))
                                if (DataGlJrnDtlMnlRow != null)
                                {
                                    string SBCSegmentValue = DataGlJrnDtlMnlRow.SBCSegmentValue;
                                    LogMsg.AppendLine("UpdateBefore => 1 SBCSegmentValue : " + SBCSegmentValue + "     SubBudgetClass : " + SubBudgetClass);

                                    if (SBCSegmentValue != SubBudgetClass)
                                    {
                                        throw new BLException("Budget Class selected in the Journal Header and the account do not match. Please select account that has matching sub budget class.");
                                    }
                                }
                            LogMsg.AppendLine("UpdateBefore => Validated Sudgetclass and its GLAccount Success");
                        }
                        #endregion

                        #region Description/Reference already used. Please use another Description/Reference.
                        var GLJrnHedRow1 = (from ttGLJrnHed in Dbctx.GLJrnHed.AsQueryable()
                                            where ttGLJrnHed.Company == DataGLJrnHedRow.Company &&
                                                        ttGLJrnHed.BookID.ToUpper() == DataGLJrnHedRow.BookID.ToUpper() &&
                                                        ttGLJrnHed.Description.ToUpper() == DataGLJrnHedRow.Description.ToUpper()
                                            select ttGLJrnHed.Description).FirstOrDefault();
                        if (GLJrnHedRow1 != null)
                        {
                            if (DescriptionRowModBlnk != GLJrnHedRow1)
                            {
                                throw new BLException("Description/Reference already used. Please use another Description/Reference.");
                            }
                        }
                        LogMsg.AppendLine("UpdateBefore => Description/Reference already used. Please use another Description/Reference.");
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
                                //DataGLJrnHedRow["TrxCtrlNum_c"] = FiscalYear.ToString().Substring(2, 2) + "CR" + ObjBO.MaxNo;
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

                    #region Foreach loop for GLJrnDtlMnl-set OrigDebitAmount and  OrigGLAccount
                    foreach (var DataGlJrnDtlMnlRow in (from ttGlJrnHedDtlMnlRow in ds.GLJrnDtlMnl.AsEnumerable()
                                                        where ttGlJrnHedDtlMnlRow.BookID.ToUpper() == SessionBookId &&
                                                        String.IsNullOrEmpty(ttGlJrnHedDtlMnlRow.RowMod)
                                                        select ttGlJrnHedDtlMnlRow))
                    {
                        if (DataGlJrnDtlMnlRow != null)
                        {
                            ObjBO.OrigDebitAmount = Convert.ToDecimal(DataGlJrnDtlMnlRow.TotDebit);
                            ObjBO.OrigGLAccount = DataGlJrnDtlMnlRow.GLAccount.ToString();
                        }
                        LogMsg.AppendLine("UpdateBefore => Set OrigDebitAmount And OrigGLAccount Success");
                    }
                    #endregion

                    #region Validate GL Account
                    ValdGLAcct.Validate(ds);
                    LogMsg.AppendLine("UpdateBefore => Validate GL Account Success");
                    #endregion

                    #region GLJrnDtlMnl Line Comments Should Not Be Empty
                    foreach (var GLJrnDtlMnlRow in (from DataGLJrnDtlMnl in ds.GLJrnDtlMnl.AsEnumerable()
                                                    where DataGLJrnDtlMnl.Company.ToUpper() == TreasuryCompany &&
                                                            DataGLJrnDtlMnl.BookID.ToUpper() == WarrantRelBookID &&
                                                            (string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                            string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                    select DataGLJrnDtlMnl))
                        if (GLJrnDtlMnlRow != null)
                        {
                            #region Line Amount
                            decimal LineAmount = GLJrnDtlMnlRow.TotDebit;
                            if (LineAmount == 0 && GLJrnDtlMnlRow.SegValue1.ToUpper() != PayableAcct)
                            {
                                throw new BLException("Debit Line Amount can not be 0 (Zero) !");
                            }
                            #endregion

                            #region Update CommentText
                            string CommentText = GLJrnDtlMnlRow.CommentText;
                            if ((LineComment == "TRUE") && (string.IsNullOrEmpty(CommentText)))
                            {
                                throw new BLException("Line Comments Should Not Be Empty!");
                            }
                            #endregion

                            #region update WRCompany_c in GLJrnDtlMnl
                            if (GLJrnDtlMnlRow.SegValue1.ToUpper() == PayableAcct && GLJrnDtlMnlRow.TotCredit != 0)
                            {
                                GLJrnDtlMnlRow["WRCompany_c"] = TreasuryCompany;
                            }
                            LogMsg.AppendLine("UpdateBefore => Update WRCompany_c (" + TreasuryCompany + ") in GLJrnDtlMnl Success");
                            #endregion

                            LogMsg.AppendLine("UpdateBefore => GLJrnDtlMnl Line Comments Should Not Be Empty");
                        }
                    #endregion

                    #region GLJrnDtlMnl
                    foreach (var DataGLJrnDtlMnlJoinRow in (from DataGLJrnDtlMnl in ds.GLJrnDtlMnl.AsEnumerable()
                                                            where DataGLJrnDtlMnl.Company.ToUpper() == TreasuryCompany &&
                                                            DataGLJrnDtlMnl.BookID.ToUpper() == WarrantRelBookID &&
                                                            (string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                            string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase)) &&
                                                            DataGLJrnDtlMnl.TotDebit != 0 &&
                                                            DataGLJrnDtlMnl.SegValue1.ToUpper() != PayableAcct
                                                            select DataGLJrnDtlMnl
                                                            ))
                    {
                        if (DataGLJrnDtlMnlJoinRow != null)
                        {

                            #region validate sudgetclass and its GLAccount
                            if (ISSBCSegValue.ToUpper() == "TRUE")
                            {
                                string SubBudgetClass = string.Empty;

                                var GLJrnHedRow1 = (from ttGLJrnHed in Dbctx.GLJrnHed.AsQueryable()
                                                    where ttGLJrnHed.Company == DataGLJrnDtlMnlJoinRow.Company &&
                                                        ttGLJrnHed.BookID == DataGLJrnDtlMnlJoinRow.BookID &&
                                                        ttGLJrnHed.FiscalYear == DataGLJrnDtlMnlJoinRow.FiscalYear &&
                                                        ttGLJrnHed.FiscalYearSuffix == DataGLJrnDtlMnlJoinRow.FiscalYearSuffix &&
                                                        ttGLJrnHed.JournalCode.ToUpper() == DataGLJrnDtlMnlJoinRow.JournalCode.ToUpper() &&
                                                        ttGLJrnHed.JournalNum == DataGLJrnDtlMnlJoinRow.JournalNum &&
                                                        ttGLJrnHed.FiscalCalendarID == DataGLJrnDtlMnlJoinRow.FiscalCalendarID
                                                    select new
                                                    {
                                                        SubBudgetCls = ttGLJrnHed.SubBudgetCls_c
                                                    }).FirstOrDefault();
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
                                ObjBO.OrigDebitAmount = 0;
                            }
                            LogMsg.AppendLine("UpdateBefore => OrigDebitAmount : " + ObjBO.OrigDebitAmount);

                            GetWarrantRelAvailBudget ObjGetWarrantRelAvailBudget = new GetWarrantRelAvailBudget(IceDtContext);
                            ObjBO.AvailBudget = ObjGetWarrantRelAvailBudget.GetWarRelAvailBudget(ObjBO.Vote, TreasuryCompany, BudgetBookID, SessionBookId, DataGLJrnDtlMnlJoinRow.GLAccount, Convert.ToDateTime(DataGLJrnDtlMnlJoinRow.JEDate));
                            LogMsg.AppendLine("UpdateBefore => GetWarrantRelAvailBudget Object Created");

                            string VoteCompany = string.Empty;
                            Decimal AvialBudAmt = 0;
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

                            var CompanyRow = (from ttcompany in Dbctx.Company.AsQueryable()
                                              where ttcompany.Company1 == ObjBO.VoteCompany
                                              select ttcompany.Name).FirstOrDefault();
                            {
                                if (CompanyRow == null)
                                {
                                    throw new BLException(ObjBO.VoteCompany + " - Does not exist in company master");
                                }
                            }

                            string CurrenyCode = DataGLJrnDtlMnlJoinRow.CurrencyCode;

                            VoteCompany = ObjBO.VoteCompany;
                            AvialBudAmt = (ObjBO.AvailBudget + ObjBO.OrigDebitAmount - DataGLJrnDtlMnlJoinRow.TotDebit);
                            LogMsg.AppendLine("UpdateBefore => AvialBudAmt : " + AvialBudAmt + "  ObjBO.AvailBudget : " + ObjBO.AvailBudget + "  ObjBO.OrigDebitAmount : " + ObjBO.OrigDebitAmount + "  TotDebit : " + DataGLJrnDtlMnlJoinRow.TotDebit);

                            if (string.IsNullOrEmpty(ObjBO.VoteCompany))
                            {
                                ErrorMessage = "Vote Company definition for vote " + ObjBO.Vote.ToString() +
                                                " has not been completed. In order to proceed please complete the required setup in Form General Ledger --> Setup --> Vote Definitions";
                                throw new BLException(ErrorMessage);
                            }

                            if (AvialBudAmt < 0)
                            {
                                ErrorMessage = "Insufficient Budget available for Cash Allocation for the line : " + Convert.ToString(GlAccountDisp) +
                                                ", Available : " + CurrenyCode + " " + (ObjBO.AvailBudget + ObjBO.OrigDebitAmount).ToString("0,0.00", CultureInfo.InvariantCulture);
                                throw new BLException(ErrorMessage);
                            }
                            else
                            {
                                string Message = "Budget available for Cash Allocation for the line : " + Convert.ToString(GlAccountDisp) +
                                                  ", Available : " + CurrenyCode + " " + (ObjBO.AvailBudget + ObjBO.OrigDebitAmount).ToString("0,0.00", CultureInfo.InvariantCulture);
                                InfoMessage.Publish(Message, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual);
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
                GlobalSysFunctions.ShowCallerInfo("Warrant Release : " + LogMsg.ToString(),ex);
                throw new BLException("Warrant Release : " + ex.Message);
            }

        }
    }
}
