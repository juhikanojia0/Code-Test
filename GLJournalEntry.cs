/*------------------------------------------------------------------------
    File        : C-IFMIS-E905702-Generic\ServerObjects\SoftTech\GLJournalEntry.p
    Purpose     : Common file for all GL transaction. Also used to call sub project
    Syntax      :
    Author(s)   : Pritesh Parmar
    Created     : 22-01-2015
    Notes       :
    Version     : 1.0.0.0
Revision History:
Version		Project     Author			    Date		    Purpose										                        Task					PBI/Bug
1.0         CIFMIS      Mahesh Deore        13/10/2015      System Audit Log Information saving			
1.1         CIFMIS      Shweta Parashar     28/10/2015      BookID & Journal Code Dynamic				                        PMORALG
1.2         CIFMIS      Sangram Kulkarni    01/12/2015      Added CarryForwardBudget     				                        PMORALG
1.3         CIFMIS      Sangram Kulkarni    01/12/2015      Added REvenue Budget Entry     				                        PMORALG
1.4         CIFMIS      Sangram Kulkarni    04/01/2016      Added Non Zero Debit/Credit Line Validation Code 		            PMORALG
2.0.0.0     CIFMIS      Sangram Kulkarni    04/02/2016      Upgrade to 10.1 
2.0.0.1     CIFMIS      Shekhar Chaudhary   4th,May 2016    Upgraded references from 10.1.400.1 to 10.1.400.8
2.0.0.2     CIFMIS      PRITESH PARMAR      12/05/2016      INCORPORATED 10.0 CHANGES TO 10.1 AND RESOLVED CODE REVIEW ISSUES  
2.0.0.3     CIFMIS      Shekhar Chaudhary   12th,May 2016   Upgraded references from 10.1.400.8 to 10.1.400.9
2.0.0.4     CIFMIS      Pritesh Parmar      13th,May 2016   Commitment Control Level added in WR, WRW, NRT & GL Commitment
2.0.0.5     CIFMIS      Shweta  Parashar    23/05/2016      Embassys - In budget Entry - system is not allow to post            Embassys                7266/7254 
                                                            transaction without approved 
2.0.0.6     CIFMIS      Shweta  Parashar    24/06/2016      Embassys - Introduce Budget withdrawal & resolve Salary Journal     Embassys                8007/7994                                                                                                     
2.0.0.7     CIFMIS      Pritesh Parmar      04/07/2016      VSO No : 8033, System is allowing to create Sub warrant & WOF Entry along with withdrawls , Realloaction from other than MDA
2.0.0.8     CIFMIS      Pritesh Parmar      08/07/2016      Legal Number is required if legal number setup is done 
2.0.0.9     CIFMIS      Shweta  Parashar    17/07/2016      Case Senstivity issue                                                                       8575
2.0.0.10    CIFMIS      Pritesh Parmar      02/08/2016      Implemeted ST Allocation Logic, 
                                                            In ST Company WOF transaction can be posted only after FOCR generated and posted from ST Company against WOF Ref number. 
2.0.0.11    CIFMIS      Pritesh Parmar      11/08/2016      Added validation of Exchaquer Beforpost 
2.0.0.12    CIFMIS      Pritesh Parmar      20/09/2016      Set ToUpper in every global values and comparision,
2.0.0.13    CIFMIS      Mahesh Deore        13/12/2016      solve Bug ID 9268, SBC was not showing to non-treasury user while doing SW
2.0.0.14    CIFMIS      Pritesh Parmar      19/12/2016      INCORPORATED 10.0 CHANGES TO 10.1
 *                                                          9762 - If user has changed Vote SBC Bank Setup after GL post and before Exchequer Bank Adj Scheduler run.
2.0.0.15    CIFMIS      Pritesh Parmar      14/03/2017      12873 - Performance Issue - Mainly All Transaction Forms 
2.0.0.16    CIFMIS		Mahesh Deore        07-Jul-2017     Upgrade from 10.1.400.1 to 10.1.600.5
2.0.0.17    CIFMIS		Mahesh Deore        24-Jul-2017     Code changes for performance issue
2.0.0.18    CIFMIS		Rajesh Tiwari       23-Feb-2018     Upgrade 10.2 PB ID - 16097 TaskID - 16176
2.0.0.19    CIFMIS		Rajesh Tiwari       23-Feb-2018     VSO Bug Id - 17120 - MOF: Vote SBC Bank Set up and Bank FeeID not to match
2.0.0.20    CIFMIS	    Pritesh Parmar      12/09/2018      VSO Id 18160 - Upgrade from 10.2.100.9 to 10.2.200.12 
2.0.0.21    CIFMIS      Amod Loharkar       14/12/2018      VSO Id - 17766/18906-added SiteID filteration logic for Cost Center Segregation for MOFKL.
2.0.0.22    CIFMIS      Pritesh Parmar      28/01/2019      VSO Id - 21357 - Sub-Treasury/Sub Accountancy/Foreign Mission Allocation Bank Adjustment required before posting allocation
2.0.0.23    CIFMIS      Amod Loharkar       08/02/2019      VSO Id - 18182 - Post WOF Allocation - added Validation for foreign Mission before posting
2.0.0.24    CIFMIS      Pritesh Parmar      05/04/2019      VSO Id - 22619 - MOFKL- Payroll Demo - Issues to be resolved 
2.0.0.25    CIFMIS      Shon Jambhale       07/05/2019      VSO Id - 22578 - MOF: Journal Detail Tracker Created/Last Edit users
2.0.0.26     CIFMIS      Rajesh             23/08/2019      VOS ID - 24240 - Post ERP 10.2 Upgrade Change request 11 - Warrant of Fund Allocation to Sub treasury
2.0.0.27     CIFMIS      Mahesh             11/10/2019      25336 - Budget Reallocation Error : Value for either too large or too small for an Int16
2.0.0.28     CIFMIS      Rajesh             22/11/2019      25707 - To enhance system to put Validations and control in the System
2.0.0.29    CIFMIS      Mahesh Deore        27-12-2019      27343 - Payroll Interface : System is not reverting back the process on any validation failure
2.0.0.30    CIFMIS      Mahesh Deore        30-12-2019      27399 - Payroll Interface : System is not allowing to submit for approval process of Payroll Journal Entry Commitment Transaction with sysparam- IsAutoSubmitAppr4PayrollGL , IsAutoApproval4PayrollGL, IsAutoPosting4PayrollGL is False
2.0.0.31    CIFMIS      Rajesh              13-02-2020      28486 - Payroll Interface : System is not allowing to submit for approval process of Payroll Journal Entry Commitment Transaction with sysparam- IsAutoSubmitAppr4PayrollGL , IsAutoApproval4PayrollGL, IsAutoPosting4PayrollGL is False
 * * * * * * * * ----------------------------------------------------------------------*/

using System;
using System.Linq;
using System.Data;
using Erp;
using Ice;
using Erp.Tablesets;
using Stcl.Global.GlobalSysInfo;
using Stcl.Global.GlobalProcedures;
using Stcl.CIFMIS.WarrantReleaseEntry;
using Stcl.CIFMIS.WarrantReleaseWithdrawalEntry;
using Stcl.CIFMIS.SubWarrantEntry;
using Stcl.CIFMIS.SubWarrantWithdrawalEntry;
using Stcl.CIFMIS.AllocationEntry;
using Stcl.CIFMIS.AllocationWithdrawal;
using Stcl.CIFMIS.AllocationReallocationEntry;
using Stcl.CIFMIS.GLCommitment;
using Stcl.CIFMIS.BudgetReallocationEntry;
using Stcl.CIFMIS.BudgetEntry;
using Stcl.CIFMIS.NationalRevenueTransferEntry;
using Stcl.CIFMIS.BudgetSupplimentaryEntry;
using Stcl.CIFMIS.CarryForwardBudgetEntry;
using Stcl.CIFMIS.RevenueBudgetEntry;
using Stcl.CIFMIS.BudgetWithdrawalEntry;
using System.Text;

namespace Stcl.Bpm.GlJournalEntry
{

    public partial class GLJournalEntry : ContextBoundBase<ErpContext>
    {
        private static Erp.ErpContext IceDtContext = null; // dataContext;
        public GLJournalEntry(ErpContext ctx) : base(ctx)
        {
            IceDtContext = ctx;

        }
        GlobalProcedures glbProc = new GlobalProcedures(IceDtContext);
        BusinessObject ObjBO = new BusinessObject();

        string SessionBookId = string.Empty;
        string SessionJournalCode = string.Empty;
        StringBuilder LogMsg = new StringBuilder();

        public static string SegmentCode = string.Empty;
        public static string SegmentName = string.Empty;

        public void UpdateBefore(Erp.Tablesets.GLJournalEntryTableset ds, Ice.Tablesets.ContextTableset ctxx)
        {
            try
            {
                LogMsg.AppendLine("UpdateBefore => Start.......");

                #region Get SessionBookId / System Audit Information / Global Data
                // Get first BpmData row from callcontext
                var bpmRow = ctxx.BpmData.FirstOrDefault();
                // in this example just copy contents of ShortChar01 to ShortChar02,  ShortChar01 is being set in UI to show passing of 
                // Call context data between UI form and BPM.
                if (bpmRow != null && !String.IsNullOrEmpty(bpmRow.ShortChar01))
                {
                    /*Updated by :  Shweta Parashar Updated Date : 28-10-2015*/
                    SessionBookId = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar01)).ToUpper();
                    SessionJournalCode = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar02)).ToUpper();
                    //End Updated By Shweta Parashar
                }
                else
                {
                    bool IsPayrollJrn = false;
                    foreach (var GLJrnHedRow in (from DataGLJrnH in ds.GLJrnHed.AsEnumerable()
                                                 where DataGLJrnH.Company == Session.CompanyID &&
                                                        (string.Equals(DataGLJrnH.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                        string.Equals(DataGLJrnH.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                 select new
                                                 {
                                                     IsPayrollJournal = Convert.ToBoolean(DataGLJrnH["IsPayrollJournal_c"])
                                                 }))
                        if (GLJrnHedRow != null)
                        {
                            LogMsg.AppendLine("inside GLJrnHedRow");
                            IsPayrollJrn = GLJrnHedRow.IsPayrollJournal;
                        }
                    foreach (var GLJrnDtlMnlRow1 in (from DataGLJrnDtlMnl in ds.GLJrnDtlMnl.AsEnumerable()
                                                     where DataGLJrnDtlMnl.Company == Session.CompanyID &&
                                                            (string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                            string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                     select new
                                                     {
                                                         IsPayrollJournal = Convert.ToBoolean(DataGLJrnDtlMnl["IsPayrollJournal_c"])
                                                     }))
                        if (GLJrnDtlMnlRow1 != null)
                        {
                            LogMsg.AppendLine("inside GLJrnDtlMnlRow1");
                            IsPayrollJrn = GLJrnDtlMnlRow1.IsPayrollJournal;
                        }
                    if (IsPayrollJrn)
                    {
                        SessionBookId = Convert.ToString(glbProc.GetSysParam("MainBookID")).ToUpper();
                        SessionJournalCode = Convert.ToString(glbProc.GetSysParam("GLJournalCode")).ToUpper();
                        LogMsg.AppendLine("Payroll Transaction execution");
                    }
                }
                LogMsg.AppendLine("UpdateBefore => SessionBookId : " + SessionBookId + "    SessionJournalCode : " + SessionJournalCode);


                string ErrorMessage = string.Empty;
                string CommitCtrlLvl = Convert.ToString(glbProc.GetSysParam("CommitCtrlLvl"));
                string TreasuryCompany = Convert.ToString(glbProc.GetSysParam("TreasuryCompany")).ToUpper();
                string BudgetBookID = Convert.ToString(glbProc.GetSysParam("BudgetBookID")).ToUpper();
                string WarrantRelBookID = Convert.ToString(glbProc.GetSysParam("WarrantRelBookID")).ToUpper();
                string SubWarrantBookID = Convert.ToString(glbProc.GetSysParam("SubWarrantBookID")).ToUpper();
                string AllocAcctsBookID = Convert.ToString(glbProc.GetSysParam("AllocAcctsBookID")).ToUpper();
                string RevBudgetBookID = Convert.ToString(glbProc.GetSysParam("RevBudgetBookID")).ToUpper();
                string PayableAcct = Convert.ToString(glbProc.GetSysParam("PayableAcct")).ToUpper();
                string MainBookID = Convert.ToString(glbProc.GetSysParam("MainBookID")).ToUpper();
                string ImprestBookID = Convert.ToString(glbProc.GetSysParam("ImprestBookID")).ToUpper();
                string NRXfrBookID = Convert.ToString(glbProc.GetSysParam("NRXfrBookID")).ToUpper();
                string NASubWarrantHolder = Convert.ToString(glbProc.GetSysParam("NASubWarrantHolder")).ToUpper();
                string WRJrnCode = Convert.ToString(glbProc.GetSysParam("WarrantRelJrnCode")).ToUpper();
                string WRWJrnCode = Convert.ToString(glbProc.GetSysParam("WarrantRelWJrnCode")).ToUpper();
                string SWJrnCode = Convert.ToString(glbProc.GetSysParam("SWJrnCode")).ToUpper();
                string SWWJrnCode = Convert.ToString(glbProc.GetSysParam("SWWJrnCode")).ToUpper();
                string AAJrnCode = Convert.ToString(glbProc.GetSysParam("AAJrnCode")).ToUpper();
                string AAWJrnCode = Convert.ToString(glbProc.GetSysParam("AAWJrnCode")).ToUpper();
                string AARJrnCode = Convert.ToString(glbProc.GetSysParam("AARJrnCode")).ToUpper();
                string BRJrnCode = Convert.ToString(glbProc.GetSysParam("BRJrnCode")).ToUpper();
                string BAJrnCode = Convert.ToString(glbProc.GetSysParam("BAJrnCode")).ToUpper();
                string BSJrnCode = Convert.ToString(glbProc.GetSysParam("BSJrnCode")).ToUpper();
                string CFJrnCode = Convert.ToString(glbProc.GetSysParam("CFJrnCode")).ToUpper();
                string RVJrnCode = Convert.ToString(glbProc.GetSysParam("RVJrnCode")).ToUpper();
                string BWJrnCode = Convert.ToString(glbProc.GetSysParam("BWJrnCode")).ToUpper();

                string BudgetTranDocTypeID = Convert.ToString(glbProc.GetSysParam("BudgetTranDocTypeID")).ToUpper();
                string BudgetSuppTranDocTypeID = Convert.ToString(glbProc.GetSysParam("BudgetSuppTranDocTypeID")).ToUpper();
                string BudgetCarryForwardTranDocTypeID = Convert.ToString(glbProc.GetSysParam("BudgetCarryForwardTranDocTypeID")).ToUpper();
                string BudgetRevenueTranDocTypeID = Convert.ToString(glbProc.GetSysParam("BudgetRevenueTranDocTypeID")).ToUpper();
                string BudgetWithdrawalTranDocTypeID = Convert.ToString(glbProc.GetSysParam("BudgetWithdrawalTranDocTypeID")).ToUpper();
                string BudgetReallocTranDocTypeID = Convert.ToString(glbProc.GetSysParam("BudgetReallocTranDocTypeID")).ToUpper();

                string WarrantRelTranDocTypeID = Convert.ToString(glbProc.GetSysParam("WarrantRelTranDocTypeID")).ToUpper();
                string WarrantRelWithTranDocTypeID = Convert.ToString(glbProc.GetSysParam("WarrantRelWithTranDocTypeID")).ToUpper();

                string SubWarrantTranDocTypeID = Convert.ToString(glbProc.GetSysParam("SubWarrantTranDocTypeID")).ToUpper();
                string SubWarrantWithdrawTranDocTypeID = Convert.ToString(glbProc.GetSysParam("SubWarrantWithdrawTranDocTypeID")).ToUpper();

                string AllocationTranDocTypeID = Convert.ToString(glbProc.GetSysParam("AllocationTranDocTypeID")).ToUpper();
                string WOFWithdrawalTranDocTypeID = Convert.ToString(glbProc.GetSysParam("WOFWithdrawalTranDocTypeID")).ToUpper();
                string AllocReallocTranDocTypeID = Convert.ToString(glbProc.GetSysParam("AllocReallocTranDocTypeID")).ToUpper();

                string GLCommitmentTranDocTypeID = Convert.ToString(glbProc.GetSysParam("GLCommitmentTranDocTypeID")).ToUpper();
                string NRXfrTranDocTypeID = Convert.ToString(glbProc.GetSysParam("NRXfrTranDocTypeID")).ToUpper();

                string IsSiteIDFilterApplicable = Convert.ToString(glbProc.GetSysParam("IsSiteIDFilterApplicable")).ToUpper();
                string SubCostCenterSegNbr = Convert.ToString(glbProc.GetSysParam("SubCostCenterSegNbr"));
                string NACostCenter = Convert.ToString(glbProc.GetSysParam("NACostCenter")).ToUpper();
                LogMsg.AppendLine("UpdateBefore => Global Constant Values Created");
                #endregion

                #region Validate GL Account for Site ID
                if (IsSiteIDFilterApplicable == "TRUE")
                {
                    foreach (var GLJrnDtlMnlRow1 in (from DataGLJrnDtlMnl in ds.GLJrnDtlMnl.AsEnumerable()
                                                     where DataGLJrnDtlMnl.Company == Session.CompanyID &&
                                                             ((DataGLJrnDtlMnl.BookID.ToUpper() == MainBookID)) &&
                                                            (string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                            string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                     select new
                                                     {
                                                         IsPayrollJournal = Convert.ToBoolean(DataGLJrnDtlMnl["IsPayrollJournal_c"]),
                                                         CostCenter = Convert.ToString(DataGLJrnDtlMnl["SegValue" + SubCostCenterSegNbr]).ToUpper()
                                                     }))
                        if (GLJrnDtlMnlRow1 != null)
                        {
                            if ((!string.IsNullOrEmpty(GLJrnDtlMnlRow1.CostCenter) && (GLJrnDtlMnlRow1.CostCenter != NACostCenter && GLJrnDtlMnlRow1.CostCenter != Session.PlantID)) && GLJrnDtlMnlRow1.IsPayrollJournal == false)
                            {
                                {
                                    LogMsg.AppendLine("UpdateBefore => Invalid GL Account for current SiteID : " + Session.PlantID);
                                    throw new BLException("Invalid GL Account for current SiteID : " + Session.PlantID);
                                }
                            }
                        }
                }
                #endregion


                #region System generated record can't be updated / deleted, Added by Pritesh Parmar on 13/01/2016, Common for ALL GL transaction
                bool Submitted4ApprRowModBlnk = false;
                string LegalNumberRowModBlnk = string.Empty;

                foreach (var DataGLJrnHedRow in (from ttGLJrnHedRow in ds.GLJrnHed.AsEnumerable()
                                                 where ttGLJrnHedRow.Company == Session.CompanyID &&
                                                   ttGLJrnHedRow.RowMod == string.Empty
                                                 select new { LegalNumber = ttGLJrnHedRow.LegalNumber, Submitted4Appr = Convert.ToBoolean(ttGLJrnHedRow["Submitted4Appr_c"]) }))
                    if (DataGLJrnHedRow != null)
                    {
                        LegalNumberRowModBlnk = DataGLJrnHedRow.LegalNumber;
                        Submitted4ApprRowModBlnk = DataGLJrnHedRow.Submitted4Appr;
                    }
                LogMsg.AppendLine("UpdateBefore => Submitted4ApprRowModBlnk : " + Submitted4ApprRowModBlnk.ToString() + "   LegalNumberRowModBlnk : " + LegalNumberRowModBlnk.ToString());

                foreach (var DataGLJrnHedRow in (from ttGLJrnHedRow in ds.GLJrnHed.AsEnumerable()
                                                 where ttGLJrnHedRow.Company == Session.CompanyID &&
                                                   (string.Equals(ttGLJrnHedRow.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(ttGLJrnHedRow.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                 select new { TranDocTypeID = ttGLJrnHedRow.TranDocTypeID, ttGLJrnHedRow.GroupID, Submitted4Appr = ttGLJrnHedRow["Submitted4Appr_c"] }))
                    if (DataGLJrnHedRow != null)
                    {
                        string TranDocTypeID = Convert.ToString(DataGLJrnHedRow.TranDocTypeID).ToUpper();
                        string GroupId = DataGLJrnHedRow.GroupID;
                        LogMsg.AppendLine("UpdateBefore => TranDocTypeID : " + TranDocTypeID);
                        LogMsg.AppendLine("UpdateBefore => GroupId : " + GroupId);

                        var DbGlJrnHed = (from ttglJrnHed in Db.GLJrnHed.AsQueryable()
                                          where ttglJrnHed.Company == Session.CompanyID
                                                && ttglJrnHed.GroupID == GroupId
                                                && ttglJrnHed.Posted == false
                                                && ttglJrnHed.IsSysGenerated_c == true
                                          select new
                                          {
                                              DbIsSyGenerated = ttglJrnHed.IsSysGenerated_c,
                                              DbSubmitted4Appr = ttglJrnHed.Submitted4Appr_c,
                                              DbIsPayrollJournal = ttglJrnHed.IsPayrollJournal_c
                                          }
                                         ).FirstOrDefault();
                        if (DbGlJrnHed != null)
                        {
                            if (DbGlJrnHed.DbSubmitted4Appr == true)
                            {
                                LogMsg.AppendLine("UpdateBefore => DbIsSysGen : " + DbGlJrnHed.DbIsSyGenerated.ToString() + " DBSubmitted4Appr : " + DbGlJrnHed.DbSubmitted4Appr.ToString());
                                if (DbGlJrnHed.DbIsSyGenerated == true && DbGlJrnHed.DbIsPayrollJournal == false)
                                {
                                    throw new BLException("System generated transaction can not be updated / deleted");
                                }
                                else if (DbGlJrnHed.DbIsSyGenerated == true && DbGlJrnHed.DbIsPayrollJournal == true && DbGlJrnHed.DbSubmitted4Appr == Convert.ToBoolean(DataGLJrnHedRow.Submitted4Appr))
                                {
                                    throw new BLException("System generated transaction can not be updated / deleted");
                                }

                            }
                        }



                        if (SessionBookId == BudgetBookID && SessionJournalCode == BAJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => BudgetTranDocTypeID : " + BudgetTranDocTypeID);
                            if (BudgetTranDocTypeID == string.Empty || TranDocTypeID != BudgetTranDocTypeID)
                            {
                                throw new BLException("Budget Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == BudgetBookID && SessionJournalCode == BWJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => BudgetWithdrawalTranDocTypeID : " + BudgetWithdrawalTranDocTypeID);
                            if (BudgetWithdrawalTranDocTypeID == string.Empty || TranDocTypeID != BudgetWithdrawalTranDocTypeID)
                            {
                                throw new BLException("Budget Withdrawal Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == BudgetBookID && SessionJournalCode == CFJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => BudgetCarryForwardTranDocTypeID : " + BudgetCarryForwardTranDocTypeID);
                            if (BudgetCarryForwardTranDocTypeID == string.Empty || TranDocTypeID != BudgetCarryForwardTranDocTypeID)
                            {
                                throw new BLException("Budget Carry Forward Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == BudgetBookID && SessionJournalCode == RVJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => BudgetRevenueTranDocTypeID : " + BudgetRevenueTranDocTypeID);
                            if (BudgetRevenueTranDocTypeID == string.Empty || TranDocTypeID != BudgetRevenueTranDocTypeID)
                            {
                                throw new BLException("Budget Revenue Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == BudgetBookID && SessionJournalCode == BSJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => BudgetSuppTranDocTypeID : " + BudgetSuppTranDocTypeID);
                            if (BudgetSuppTranDocTypeID == string.Empty || TranDocTypeID != BudgetSuppTranDocTypeID)
                            {
                                throw new BLException("Budget Supplementary Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == BudgetBookID && SessionJournalCode == BRJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => BudgetReallocTranDocTypeID : " + BudgetReallocTranDocTypeID);
                            if (BudgetReallocTranDocTypeID == string.Empty || TranDocTypeID != BudgetReallocTranDocTypeID)
                            {
                                throw new BLException("Budget Reallocation Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == WarrantRelBookID && SessionJournalCode == WRJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => WarrantRelTranDocTypeID : " + WarrantRelTranDocTypeID);
                            if (WarrantRelTranDocTypeID == string.Empty || TranDocTypeID != WarrantRelTranDocTypeID)
                            {
                                throw new BLException("Warrant Release Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == WarrantRelBookID && SessionJournalCode == WRWJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => WarrantRelWithTranDocTypeID : " + WarrantRelWithTranDocTypeID);
                            if (WarrantRelWithTranDocTypeID == string.Empty || TranDocTypeID != WarrantRelWithTranDocTypeID)
                            {
                                throw new BLException("Warrant Release Withdrawal Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == SubWarrantBookID && SessionJournalCode == SWJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => SubWarrantTranDocTypeID : " + SubWarrantTranDocTypeID);
                            if (SubWarrantTranDocTypeID == string.Empty || TranDocTypeID != SubWarrantTranDocTypeID)
                            {
                                throw new BLException("Sub Warrant Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == SubWarrantBookID && SessionJournalCode == SWWJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => SubWarrantWithdrawTranDocTypeID : " + SubWarrantWithdrawTranDocTypeID);
                            if (SubWarrantWithdrawTranDocTypeID == string.Empty || TranDocTypeID != SubWarrantWithdrawTranDocTypeID)
                            {
                                throw new BLException("Sub Warrant Withdrawal Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == AllocAcctsBookID && SessionJournalCode == AAJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => AllocationTranDocTypeID : " + AllocationTranDocTypeID);
                            if (AllocationTranDocTypeID == string.Empty || TranDocTypeID != AllocationTranDocTypeID)
                            {
                                throw new BLException("Allocation Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == AllocAcctsBookID && SessionJournalCode == AAWJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => WOFWithdrawalTranDocTypeID : " + WOFWithdrawalTranDocTypeID);
                            if (WOFWithdrawalTranDocTypeID == string.Empty || TranDocTypeID != WOFWithdrawalTranDocTypeID)
                            {
                                throw new BLException("Allocation Withdrawal Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == AllocAcctsBookID && SessionJournalCode == AARJrnCode)
                        {
                            LogMsg.AppendLine("UpdateBefore => AllocReallocTranDocTypeID : " + AllocReallocTranDocTypeID);
                            if (AllocReallocTranDocTypeID == string.Empty || TranDocTypeID != AllocReallocTranDocTypeID)
                            {
                                throw new BLException("Allocation Reallocation Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == MainBookID)
                        {
                            LogMsg.AppendLine("UpdateBefore => GLCommitmentTranDocTypeID : " + GLCommitmentTranDocTypeID);
                            if (GLCommitmentTranDocTypeID == string.Empty || TranDocTypeID != GLCommitmentTranDocTypeID)
                            {
                                throw new BLException("GLCommitment Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        if (SessionBookId == NRXfrBookID)
                        {
                            LogMsg.AppendLine("UpdateBefore => NRXfrTranDocTypeID : " + NRXfrTranDocTypeID);
                            if (NRXfrTranDocTypeID == string.Empty || TranDocTypeID != NRXfrTranDocTypeID)
                            {
                                throw new BLException("National Revenue Transfer Document Type does not exist in global constant OR Incorrect. Please verify global constant setup and transaction document type setup.");
                            }
                        }

                        LogMsg.AppendLine("UpdateBefore => TranDocTypeID validation success");
                        //End
                    }

                foreach (var DataGLJrnHedRow in (from ttGLJrnHedRow in ds.GLJrnHed.AsEnumerable()
                                                 where ttGLJrnHedRow.Company == Session.CompanyID &&
                                                   (string.Equals(ttGLJrnHedRow.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(ttGLJrnHedRow.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(ttGLJrnHedRow.RowMod, IceRow.ROWSTATE_DELETED, StringComparison.OrdinalIgnoreCase))
                                                 select new
                                                 {
                                                     JournalNum = ttGLJrnHedRow.JournalNum,
                                                     LegalNumber = ttGLJrnHedRow.LegalNumber,
                                                     TranDocTypeID = ttGLJrnHedRow.TranDocTypeID,
                                                     IsSysGenerated = Convert.ToBoolean(ttGLJrnHedRow["IsSysGenerated_c"]),
                                                     Submitted4Appr = Convert.ToBoolean(ttGLJrnHedRow["Submitted4Appr_c"]),
                                                 }))
                    if (DataGLJrnHedRow != null)
                    {
                        bool IsSysGen = DataGLJrnHedRow.IsSysGenerated;
                        bool Submitted4Appr = DataGLJrnHedRow.Submitted4Appr;
                        string LegalNumber = Convert.ToString(DataGLJrnHedRow.LegalNumber);
                        int JournalNum = Convert.ToInt32(DataGLJrnHedRow.JournalNum);
                        string TranDocTypeID = Convert.ToString(DataGLJrnHedRow.TranDocTypeID);
                        LogMsg.AppendLine("UpdateBefore => IsSysGen : " + IsSysGen.ToString()  + "     Submitted4Appr : " + Submitted4Appr.ToString() + "     LegalNumber : " + LegalNumber + "     JournalNum : " + JournalNum + "     TranDocTypeID : " + TranDocTypeID);
                        if (IsSysGen == true && Submitted4Appr == false)
                        {
                            if (LegalNumberRowModBlnk == LegalNumber)
                            {
                                if (Submitted4ApprRowModBlnk == Submitted4Appr)
                                {
                                    throw new BLException("System generated transaction can not be updated / deleted");
                                }
                            }
                        }
                        LogMsg.AppendLine("UpdateBefore => Submitted4ApprRowModBlnk : " + Submitted4ApprRowModBlnk.ToString() + "   LegalNumberRowModBlnk : " + LegalNumberRowModBlnk.ToString());

                    }


                foreach (var GLJrnDtlMnlRow in (from DataGLJrnDtlMnl in ds.GLJrnDtlMnl.AsEnumerable()
                                                where DataGLJrnDtlMnl.Company == Session.CompanyID &&
                                                       (string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                       string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase) ||
                                                       string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_DELETED, StringComparison.OrdinalIgnoreCase))
                                                select DataGLJrnDtlMnl))
                    if (GLJrnDtlMnlRow != null)
                    {
                        var GLJrnHedRow = (from ttGLJrnHedRow in Db.GLJrnHed.AsQueryable()
                                           where ttGLJrnHedRow.Company == GLJrnDtlMnlRow.Company &&
                                           ttGLJrnHedRow.GroupID == GLJrnDtlMnlRow.GroupID &&
                                           ttGLJrnHedRow.Posted == false &&
                                           ttGLJrnHedRow.IsSysGenerated_c == true
                                           select new
                                           {
                                               IsSysGenerated = ttGLJrnHedRow.IsSysGenerated_c
                                           }
                                           ).FirstOrDefault();
                            if (GLJrnHedRow != null)
                            {
                                if (GLJrnHedRow.IsSysGenerated == true)
                                {
                                    throw new BLException("System generated transaction can not be updated / deleted");
                                }
                            }
                    }
                LogMsg.AppendLine("UpdateBefore => System generated transaction can not be updated / deleted");
                #endregion

                #region Do not allow to changed transaction control number once gl line is entred - Pritesh Parmar on 21/09/2015
                foreach (var DataGLJrnHedRow in (from ttGLJrnHedRow in ds.GLJrnHed.AsEnumerable()
                                                 where ttGLJrnHedRow.Company == Session.CompanyID &&
                                                 ttGLJrnHedRow.BookID.ToUpper() == SessionBookId &&
                                                 string.Equals(ttGLJrnHedRow.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase)
                                                 select new
                                                 {
                                                     Company = ttGLJrnHedRow.Company,
                                                     BookID = ttGLJrnHedRow.BookID.ToUpper(),
                                                     FiscalYear = ttGLJrnHedRow.FiscalYear,
                                                     FiscalYearSuffix = ttGLJrnHedRow.FiscalYearSuffix,
                                                     JournalCode = ttGLJrnHedRow.JournalCode.ToUpper(),
                                                     JournalNum = ttGLJrnHedRow.JournalNum,
                                                     FiscalCalendarID = ttGLJrnHedRow.FiscalCalendarID,
                                                     MainTrxCtrlNum = Convert.ToString(ttGLJrnHedRow["MainTrxCtrlNum_c"]),
                                                     RefTrxCtrlNum = Convert.ToString(ttGLJrnHedRow["RefTrxCtrlNum_c"])
                                                 }))
                {
                    string MainTrxCtrlNumOld = string.Empty;
                    string RefTrxCtrlNumOld = string.Empty;

                    string MainTrxCtrlNumNew = DataGLJrnHedRow.MainTrxCtrlNum;
                    string RefTrxCtrlNumNew = DataGLJrnHedRow.RefTrxCtrlNum;

                    var DataGLJrnHedRow1 = (Db.GLJrnHed.AsQueryable().Where(
                                            tt => tt.Company == DataGLJrnHedRow.Company &&
                                            tt.BookID.ToUpper() == DataGLJrnHedRow.BookID &&
                                            tt.FiscalYear == DataGLJrnHedRow.FiscalYear &&
                                            tt.FiscalYearSuffix == DataGLJrnHedRow.FiscalYearSuffix &&
                                            tt.JournalCode.ToUpper() == DataGLJrnHedRow.JournalCode &&
                                            tt.JournalNum == DataGLJrnHedRow.JournalNum &&
                                            tt.FiscalCalendarID == DataGLJrnHedRow.FiscalCalendarID).
                                              Select(tt => new
                                              {
                                                  MainTrxCtrlNum = tt.MainTrxCtrlNum_c,
                                                  RefTrxCtrlNum = tt.RefTrxCtrlNum_c
                                              }).FirstOrDefault());
                    {
                        if (DataGLJrnHedRow1 != null)
                        {
                            MainTrxCtrlNumOld = DataGLJrnHedRow1.MainTrxCtrlNum;
                            RefTrxCtrlNumOld = DataGLJrnHedRow1.RefTrxCtrlNum;
                        }
                    }

                    if (MainTrxCtrlNumOld != MainTrxCtrlNumNew || RefTrxCtrlNumOld != RefTrxCtrlNumNew)
                    {
                        var DataGLJrnDtlMnlRowCnt = (from ttGLJrnDtlMnl in Db.GLJrnDtlMnl.AsQueryable()
                                                     where ttGLJrnDtlMnl.Company == DataGLJrnHedRow.Company &&
                                                     ttGLJrnDtlMnl.BookID == DataGLJrnHedRow.BookID &&
                                                     ttGLJrnDtlMnl.FiscalYear == DataGLJrnHedRow.FiscalYear &&
                                                     ttGLJrnDtlMnl.FiscalYearSuffix == DataGLJrnHedRow.FiscalYearSuffix &&
                                                     ttGLJrnDtlMnl.JournalCode == DataGLJrnHedRow.JournalCode &&
                                                     ttGLJrnDtlMnl.JournalNum == DataGLJrnHedRow.JournalNum &&
                                                     ttGLJrnDtlMnl.FiscalCalendarID == DataGLJrnHedRow.FiscalCalendarID
                                                     select ttGLJrnDtlMnl.Company).Count();
                        {
                            if (DataGLJrnDtlMnlRowCnt > 0)
                            {
                                throw new BLException("Please delete all the GL lines before updating transaction control number");
                            }
                        }
                    }
                }
                #endregion Do not allow to changed transaction control number once gl line is entred

                #region Warrant Release Entry
                if (SessionBookId == WarrantRelBookID && SessionJournalCode == WRJrnCode && CommitCtrlLvl == "3")
                {
                    WarrantReleaseEntry wr = new WarrantReleaseEntry(IceDtContext);
                    wr.UpdateBefore(ds, ctxx, WarrantRelBookID, WRJrnCode);
                    LogMsg.AppendLine("Warrant Release Entry of UpdateBefore Success=> WarrantRelBookID : " + WarrantRelBookID + "    WRJrnCode : " + WRJrnCode);
                }
                #endregion

                #region Warrant Release Withdrawal
                if (SessionBookId == WarrantRelBookID && SessionJournalCode == WRWJrnCode && CommitCtrlLvl == "3")
                {
                    WarrantReleaseWithdrawalEntry wrw = new WarrantReleaseWithdrawalEntry(IceDtContext);
                    wrw.UpdateBefore(ds, ctxx, WarrantRelBookID, WRWJrnCode);
                    LogMsg.AppendLine("Warrant Release Withdrawal of UpdateBefore Success=> WarrantRelBookID : " + WarrantRelBookID + "    WRWJrnCode : " + WRWJrnCode);
                }
                #endregion

                #region Sub Warrant
                if (SessionBookId == SubWarrantBookID && SessionJournalCode == SWJrnCode)
                {
                    SubWarrantEntry sw = new SubWarrantEntry(IceDtContext);
                    sw.UpdateBefore(ds, ctxx, SubWarrantBookID, SWJrnCode);
                    LogMsg.AppendLine("Sub Warrant of UpdateBefore Success=> SubWarrantBookID : " + SubWarrantBookID + "    SWJrnCode : " + SWJrnCode);

                }
                #endregion

                #region Sub Warrant Withdrawal
                if (SessionBookId == SubWarrantBookID && SessionJournalCode == SWWJrnCode)
                {
                    SubWarrantWithdrawalEntry sww = new SubWarrantWithdrawalEntry(IceDtContext);
                    sww.UpdateBefore(ds, ctxx, SubWarrantBookID, SWWJrnCode);
                    LogMsg.AppendLine("Sub Warrant Withdrawal of UpdateBefore Success=> SubWarrantBookID : " + SubWarrantBookID + "    SWWJrnCode : " + SWWJrnCode);
                }
                #endregion

                #region Allocation to item entry
                if (SessionBookId == AllocAcctsBookID)
                {
                    AllocationEntry aa = new AllocationEntry(IceDtContext);
                    aa.UpdateBefore(ds, ctxx, AllocAcctsBookID, AAJrnCode);
                    LogMsg.AppendLine("Allocation to item entry of UpdateBefore Success=> AllocAcctsBookID : " + AllocAcctsBookID + "    AAJrnCode : " + AAJrnCode);
                }
                #endregion

                #region Allocation to item withdrawal
                if (SessionBookId == AllocAcctsBookID)
                {
                    AllocationWithdrawal aaw = new AllocationWithdrawal(IceDtContext);
                    aaw.UpdateBefore(ds, ctxx, AllocAcctsBookID, AAWJrnCode);
                    LogMsg.AppendLine("Allocation to item withdrawal of UpdateBefore Success=> AllocAcctsBookID : " + AllocAcctsBookID + "    AAWJrnCode : " + AAWJrnCode);
                }
                #endregion

                #region Allocation Reallocation Entry

                if (SessionBookId == AllocAcctsBookID)
                {
                    AllocationReallocationEntry aar = new AllocationReallocationEntry(IceDtContext);
                    aar.UpdateBefore(ds, ctxx, AllocAcctsBookID, AARJrnCode);
                    LogMsg.AppendLine("Allocation Reallocation Entry of UpdateBefore Success=> AllocAcctsBookID : " + AllocAcctsBookID + "    AARJrnCode : " + AARJrnCode);
                }
                #endregion

                #region GL Commitment Control
                if (SessionBookId == MainBookID || SessionBookId == ImprestBookID)//Removed CommitCtrlvl =3 after discussion with pritesh.
                {
                    GLCommitment GLComm = new GLCommitment(IceDtContext);
                    GLComm.UpdateBefore(ds, ctxx, SessionBookId);
                    LogMsg.AppendLine("GL Commitment Control of UpdateBefore Success=> MainBookID : " + MainBookID + "    ImprestBookID : " + ImprestBookID);
                }
                #endregion

                #region Budget Reallocation Entry
                if (SessionBookId == BudgetBookID && SessionJournalCode == BRJrnCode)
                {
                    BudgetReallocationEntry br = new BudgetReallocationEntry(IceDtContext);
                    br.UpdateBefore(ds, ctxx, BudgetBookID, BRJrnCode);
                    LogMsg.AppendLine("Budget Reallocation Entry of UpdateBefore Success=> BudgetBookID : " + BudgetBookID + "    BRJrnCode : " + BRJrnCode);
                }
                #endregion

                #region BudgetWithdrawal Entry Added by shweta Parashar on 21-June-2016
                if (SessionBookId == BudgetBookID && SessionJournalCode == BWJrnCode)
                {
                    BudgetWithdrawalEntry br = new BudgetWithdrawalEntry(IceDtContext);
                    br.UpdateBefore(ds, ctxx, BudgetBookID, BWJrnCode);
                    LogMsg.AppendLine("Budget Withdrawal Entry of UpdateBefore Success=> BudgetBookID : " + BudgetBookID + "    BWJrnCode : " + BWJrnCode);
                }
                #endregion

                #region Budget Entry
                if (SessionBookId == BudgetBookID && SessionJournalCode == BAJrnCode)
                {
                    BudgetEntry br = new BudgetEntry(IceDtContext);
                    br.UpdateBefore(ds, ctxx, BudgetBookID, BAJrnCode);
                    LogMsg.AppendLine("Budget Entry of UpdateBefore Success=> BudgetBookID : " + BudgetBookID + "    BAJrnCode : " + BAJrnCode);
                }
                #endregion

                #region National Revenue Transfer Entry
                if (SessionBookId == NRXfrBookID && CommitCtrlLvl == "3")
                {
                    NationalRevenueTransferEntry nrt = new NationalRevenueTransferEntry(IceDtContext);
                    nrt.UpdateBefore(ds, ctxx, NRXfrBookID);
                    LogMsg.AppendLine("National Revenue Transfer Entry of UpdateBefore Success=> NRXfrBookID : " + NRXfrBookID);
                }
                #endregion

                #region  Budget Supplimentary Entry
                if (SessionBookId == BudgetBookID && SessionJournalCode == BSJrnCode)
                {
                    BudgetSupplimentaryEntry bs = new BudgetSupplimentaryEntry(IceDtContext);
                    bs.UpdateBefore(ds, ctxx, BudgetBookID, BSJrnCode);
                    LogMsg.AppendLine("Budget Supplimentary Entry of UpdateBefore Success=> BudgetBookID : " + BudgetBookID + "    BSJrnCode : " + BSJrnCode);
                }
                #endregion

                #region  Carry Forward Budget Entry
                if (SessionBookId == BudgetBookID && SessionJournalCode == CFJrnCode)
                {
                    CarryForwardBudgetEntry bs = new CarryForwardBudgetEntry(IceDtContext);
                    bs.UpdateBefore(ds, ctxx, BudgetBookID, CFJrnCode);
                    LogMsg.AppendLine("Carry Forward Budget Entry of UpdateBefore Success=> BudgetBookID : " + BudgetBookID + "    CFJrnCode : " + CFJrnCode);
                }
                #endregion

                #region  Revenue Budget Entry
                if (SessionBookId == RevBudgetBookID && SessionJournalCode == RVJrnCode)
                {
                    RevenueBudgetEntry rv = new RevenueBudgetEntry(IceDtContext);
                    rv.UpdateBefore(ds, ctxx, RevBudgetBookID, RVJrnCode);
                    LogMsg.AppendLine("Revenue Budget Entry of UpdateBefore Success=> BudgetBookID : " + RevBudgetBookID + "    CFJrnCode : " + RVJrnCode);
                }
                #endregion

                #region GLJrnDtlMnl Line Amounts Should Not Be Zero

                foreach (var GLJrnDtlMnlRow in (from DataGLJrnDtlMnl in ds.GLJrnDtlMnl.AsEnumerable()
                                                where DataGLJrnDtlMnl.Company == Session.CompanyID &&
                                                       ((DataGLJrnDtlMnl.BookID.ToUpper() == BudgetBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == BAJrnCode) ||
                                                       (DataGLJrnDtlMnl.BookID.ToUpper() == BudgetBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == BSJrnCode) ||
                                                       (DataGLJrnDtlMnl.BookID.ToUpper() == BudgetBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == CFJrnCode) ||
                                                       (DataGLJrnDtlMnl.BookID.ToUpper() == SubWarrantBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == SWJrnCode) ||
                                                       (DataGLJrnDtlMnl.BookID.ToUpper() == AllocAcctsBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == AAJrnCode)
                                                       ) &&
                                                       (string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                       string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                select new
                                                {
                                                    TotDebit = DataGLJrnDtlMnl.TotDebit,
                                                    SegValue1 = DataGLJrnDtlMnl.SegValue1
                                                }))
                    if (GLJrnDtlMnlRow != null)
                    {
                        #region Line Amount
                        decimal LineAmount = GLJrnDtlMnlRow.TotDebit;
                        if (LineAmount == 0 && GLJrnDtlMnlRow.SegValue1.ToUpper() != PayableAcct)
                        {
                            throw new BLException("Debit Line Amount can not be 0 (Zero) !");
                        }
                        #endregion
                        LogMsg.AppendLine("UpdateBefore => GLJrnDtlMnl Debit Line Amount can not be 0 (Zero) !");
                    }

                foreach (var GLJrnDtlMnlRow in (from DataGLJrnDtlMnl in ds.GLJrnDtlMnl.AsEnumerable()
                                                where DataGLJrnDtlMnl.Company == Session.CompanyID &&
                                                       ((DataGLJrnDtlMnl.BookID.ToUpper() == RevBudgetBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == RVJrnCode) ||
                                                       (DataGLJrnDtlMnl.BookID.ToUpper() == SubWarrantBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == SWWJrnCode) ||
                                                       (DataGLJrnDtlMnl.BookID.ToUpper() == AllocAcctsBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == AAWJrnCode) ||
                                                       (DataGLJrnDtlMnl.BookID.ToUpper() == BudgetBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == BWJrnCode)) && //Added by shweta Parashar on 21-June-2016
                                                       (string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                       string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                select new
                                                {
                                                    TotCredit = DataGLJrnDtlMnl.TotCredit,
                                                    SegValue1 = DataGLJrnDtlMnl.SegValue1
                                                }))
                    if (GLJrnDtlMnlRow != null)
                    {
                        #region Line Amount
                        decimal LineAmount = GLJrnDtlMnlRow.TotCredit;
                        if (LineAmount == 0 && GLJrnDtlMnlRow.SegValue1.ToUpper() != PayableAcct)
                        {
                            throw new BLException("Credit Line Amount can not be 0 (Zero) !");
                        }
                        #endregion
                        LogMsg.AppendLine("UpdateBefore => GLJrnDtlMnl Credit Line Amount can not be 0 (Zero) !");
                    }


                foreach (var GLJrnDtlMnlRow in (from DataGLJrnDtlMnl in ds.GLJrnDtlMnl.AsEnumerable()
                                                where DataGLJrnDtlMnl.Company == Session.CompanyID &&
                                                        ((DataGLJrnDtlMnl.BookID.ToUpper() == BudgetBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == BRJrnCode) ||
                                                       (DataGLJrnDtlMnl.BookID.ToUpper() == AllocAcctsBookID && DataGLJrnDtlMnl.JournalCode.ToUpper() == AARJrnCode)) &&
                                                       (string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                       string.Equals(DataGLJrnDtlMnl.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                select new
                                                {
                                                    TotDebit = DataGLJrnDtlMnl.TotDebit,
                                                    TotCredit = DataGLJrnDtlMnl.TotCredit,
                                                    SegValue1 = DataGLJrnDtlMnl.SegValue1
                                                }))
                    if (GLJrnDtlMnlRow != null)
                    {
                        #region Line Amount
                        decimal DebitLineAmount = GLJrnDtlMnlRow.TotDebit;
                        decimal CreditLineAmount = GLJrnDtlMnlRow.TotCredit;
                        LogMsg.AppendLine("UpdateBefore => DebitLineAmount : " + DebitLineAmount + "    CreditLineAmount : " + CreditLineAmount);
                        if (CreditLineAmount == 0 && DebitLineAmount == 0 && GLJrnDtlMnlRow.SegValue1.ToUpper() != PayableAcct)
                        {
                            throw new BLException("Debit / Credit Line Amount can not be 0 (Zero) !");
                        }
                        #endregion
                    }
                #endregion

                #region Assign SiteID
                var DataGLJrnHedRowVal = (from ttGLJrnHedRow in ds.GLJrnHed.AsEnumerable()
                                          where ttGLJrnHedRow.Company == Session.CompanyID &&
                                          (string.Equals(ttGLJrnHedRow.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(ttGLJrnHedRow.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                          select ttGLJrnHedRow).FirstOrDefault();
                if (DataGLJrnHedRowVal != null)
                {
                    DataGLJrnHedRowVal["SiteID_c"] = Session.PlantID;
                    #region Update Last Updated User ID                       
                    DataGLJrnHedRowVal["LastModifyBy_c"] = Session.UserID;
                    LogMsg.AppendLine("Session User ID : " + Session.UserID);                    
                    #endregion

                }
                LogMsg.AppendLine("UpdateBefore => SiteID Updated : " + Session.PlantID);
                #endregion

                //#region Assign System Audit Information, Common for ALL, Added by mahesh
                //AuditLog(ds);
                //LogMsg.AppendLine("UpdateBefore => Global Constant Values Assign To GLJrnHed");
                //#endregion System Audit Information                

                LogMsg.AppendLine("UpdateBefore => End.......");
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());

            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GLJournalEntry => " + LogMsg.ToString(), ex);
                throw new BLException("GLJournalEntry => " + ex.Message);
            }
        }

        #region CheckDocumentIsLocked - Deletion is not allowed, Once submit for approval is done    - Pritesh Parmar
        public void CheckDocumentIsLocked(ref System.String keyValue, ref System.String keyValue2, ref System.String keyValue3, ref System.String keyValue4, ref System.String keyValue5, Ice.Tablesets.ContextTableset context)
        {
            try
            {
                string GroupId = keyValue;
                string BookID = keyValue2.ToUpper();
                int FiscalYear = Convert.ToInt16(keyValue3);
                string JournalCode = keyValue4.ToUpper();
                int JournalNum = Convert.ToInt16(keyValue5);

                foreach (var GLJrnHedRow in (from ttGLJrnHed in Db.GLJrnHed.AsQueryable()
                                             where ttGLJrnHed.Company == Session.CompanyID &&
                                             ttGLJrnHed.GroupID == GroupId &&
                                             ttGLJrnHed.BookID.ToUpper() == BookID &&
                                             ttGLJrnHed.FiscalYear == FiscalYear &&
                                             ttGLJrnHed.JournalCode.ToUpper() == JournalCode &&
                                             ttGLJrnHed.JournalNum == JournalNum
                                             select new
                                             {
                                                 Submitted4Appr = ttGLJrnHed.Submitted4Appr_c
                                             }))
                {
                    LogMsg.AppendLine("IsSubmitForApproval Start.......");
                    bool IsSubmitForApproval = GLJrnHedRow.Submitted4Appr;
                    if (IsSubmitForApproval == true)
                    {
                        throw new BLException("Deletion is not allowed, Once submit for approval is done...");
                    }
                    LogMsg.AppendLine("IsSubmitForApproval End.......");
                }
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GLJournalEntry => CheckDocumentIsLocked : " + LogMsg.ToString(), ex);
                throw new BLException("GLJournalEntry => CheckDocumentIsLocked : " + ex.Message);
            }
        }
        #endregion Validation - Deletion is not allowed, Once submit for approval is done    - Pritesh Parmar

        #region Update - Update GroupId to APSyst table
        public void Update(Erp.Tablesets.GLJrnGrpTableset ds, Ice.Tablesets.ContextTableset context)
        {
            try
            {
                LogMsg.AppendLine("Update => Update GroupId to APSyst table Start ........");
                foreach (var GLJrnGrpRow in (from ttGLJrnGrp in ds.GLJrnGrp.AsEnumerable()
                                             where ttGLJrnGrp.Company == Session.CompanyID &&
                                             string.Equals(ttGLJrnGrp.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase)
                                             select ttGLJrnGrp))
                {
                    if (GLJrnGrpRow != null)
                    {
                        string GroupId = GLJrnGrpRow.GroupID;
                        string BookId = GLJrnGrpRow.BookID.ToUpper();
                        int FiscalYear = GLJrnGrpRow.FiscalYear;

                        LogMsg.AppendLine("Update => Update GroupId to APSyst table End > Inside GLJrnGrpRow........");

                        foreach (var GLJrnHedRow in (from ttGLJrnHed in Db.GLJrnHed.AsQueryable()
                                                     where ttGLJrnHed.Company == Session.CompanyID &&
                                                     ttGLJrnHed.BookID.ToUpper() == BookId &&
                                                     ttGLJrnHed.FiscalYear == FiscalYear &&
                                                     ttGLJrnHed.GroupID == GroupId
                                                     select new
                                                     {
                                                         Submitted4Appr = ttGLJrnHed.Submitted4Appr_c
                                                     }))
                        {
                            LogMsg.AppendLine("Update => Update GroupId to APSyst table End > Inside GLJrnHedRow........");

                            bool IsSubmitForApproval = GLJrnHedRow.Submitted4Appr;
                            if (IsSubmitForApproval == true)
                            {
                                throw new BLException("Deletion is not allowed, Once submit for approval is done...");
                            }
                        }
                    }
                }
                LogMsg.AppendLine("Update => Update GroupId to APSyst table End ........");
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GLJournalEntry => GLJrnGrp Update : " + LogMsg.ToString(), ex);
                throw new BLException("GLJournalEntry => GLJrnGrp Update :  " + ex.Message);
            }
        }
        #endregion

        #region GetNewGLJrnGrp - Auto Increment Group Id - Pritesh Parmar
        public void GetNewGLJrnGrp(Erp.Tablesets.GLJrnGrpTableset ds, Ice.Tablesets.ContextTableset ctxx)
        {

            try
            {
                LogMsg.AppendLine("Start ........");

                string Company = Session.CompanyID;
                string BookId = string.Empty;
                string GroupId = string.Empty;

                var bpmRow = ctxx.BpmData.FirstOrDefault();

                if (bpmRow.ShortChar10 == "GetSBC")
                {
                    LogMsg.AppendLine("SBC Checking Company : " + Company + ", Convert.ToString(bpmRow.ShortChar09) : " + Convert.ToString(bpmRow.ShortChar09));
                    GetSBCByTrxCtrlNum(Company, Convert.ToString(bpmRow.ShortChar09));
                    bpmRow.ShortChar09 = SegmentCode;
                    bpmRow.ShortChar10 = SegmentName;
                    LogMsg.AppendLine("Values return by function=> SegmentCode: " + bpmRow.ShortChar09 + " SegmentName: " + bpmRow.ShortChar10);
                }
                else
                {
                    if (bpmRow != null && !String.IsNullOrEmpty(bpmRow.ShortChar01))
                        BookId = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar01)).ToUpper();

                    LogMsg.AppendLine("ContextTableset Success ........");

                    var GLJrnGrpRow = (from ttGLJrnGrp in ds.GLJrnGrp.AsEnumerable()
                                       where ttGLJrnGrp.Company == Company &&
                                       string.Equals(ttGLJrnGrp.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase)
                                       select ttGLJrnGrp).FirstOrDefault();
                    if (GLJrnGrpRow != null)
                    {
                        //Added By Pritesh Parmar on 01/07/2016, VSO No : 8033, System is allowing to create Sub warrant & WOF Entry along with withdrawls , Realloaction from other than MDA
                        LogMsg.AppendLine("Update => Current Company : " + Company + ",   BookId : " + BookId);

                        var GLBookRow = (from ttGLBook in Db.GLBook.AsQueryable()
                                         where ttGLBook.Company == Session.CompanyID &&
                                         ttGLBook.BookID.Equals(BookId, StringComparison.OrdinalIgnoreCase) //Code Updated by Shweta parashar on 14-07-2016   bugg-8575
                                         select ttGLBook.BookID).Count();
                        if (GLBookRow == 0)
                        {
                            throw new BLException("Invalid BookID, You are not allow to do this transaction from current company");
                        }
                        //End Pritesh Parmar

                        LogMsg.AppendLine("Inside GLJrnGrpRow ........");
                        foreach (var DataGroupId in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_AutomaticGroupCreation " + "@company = {0}, @tranType = {1}", Company, "GL"))
                        {
                            GroupId = DataGroupId.MaxNo;
                        }
                        GLJrnGrpRow.GroupID = GroupId.ToString();

                        #region Assign Site ID
                        GLJrnGrpRow["SiteID_c"] = Session.PlantID;
                        #endregion

                        LogMsg.AppendLine("GroupID Assign ........");
                    }
                }
                LogMsg.AppendLine("End ........");
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GLJournalEntry => GetNewGLJrnGrp : " + LogMsg.ToString(), ex);
                throw new BLException("GLJournalEntry => GetNewGLJrnGrp : " + ex.Message);
            }
        }
        #endregion Auto Increment Group Id - Pritesh Parmar

        #region GetByID - Invalid GroupId For Current BookID
        public void GetByID(ref System.String groupID, Erp.Tablesets.GLJrnGrpTableset result, Ice.Tablesets.ContextTableset ctxx)
        {
            try
            {
                LogMsg.AppendLine("Start ........");

                string BookId = string.Empty;
                string JournalCode = string.Empty;
                string IsSiteIDFilterApplicable = Convert.ToString(glbProc.GetSysParam("IsSiteIDFilterApplicable")).ToUpper();
                var bpmRow = ctxx.BpmData.FirstOrDefault();

                if (bpmRow != null && !String.IsNullOrEmpty(bpmRow.ShortChar01))
                    BookId = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar01)).ToUpper();
                JournalCode = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar02)).ToUpper();
                string GrpId = groupID.ToString();

                LogMsg.AppendLine("ContextTableset Success ........");
                LogMsg.AppendLine("BookId : " + BookId + "    JournalCode : " + JournalCode + "    GrpId : " + GrpId);

                //Updated by Pritesh Parmar on 28/06/2016 , Issue Resolved VSO:7254, Any GL transaction is created from its respective form 
                //and trying to get data of the group created in another GL form by manually entering the group id and System is fetching the data 
                var QryData = (from p in Db.GLJrnGrp.AsQueryable()
                               where p.Company == Session.CompanyID
                               && p.BookID.ToUpper() == BookId
                               && p.JournalCode.ToUpper() == JournalCode
                               && p.GroupID == GrpId
                               //added by amod on 05/11/2018 for cost center segragation.
                               && ((IsSiteIDFilterApplicable == "TRUE" && p.SiteID_c == Session.PlantID) || (IsSiteIDFilterApplicable == "FALSE"))
                               select p.Company).Count();
                if (QryData == 0)
                {
                    if (IsSiteIDFilterApplicable == "TRUE")
                    {
                        throw new BLException("Invalid groupId for current BookID : " + BookId + " and JournalCode : " + JournalCode + " Or SiteID :" + Session.PlantID);
                    }
                    else
                    {
                        throw new BLException("Invalid groupId for current BookID : " + BookId + " and JournalCode : " + JournalCode);
                    }
                }

                LogMsg.AppendLine("End ........");
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GLJournalEntry => GLJrnGrp GetByID : " + LogMsg.ToString(), ex);
                throw new BLException("GLJournalEntry => GLJrnGrp GetByID : " + ex.Message);
            }
        }
        #endregion

        #region GetList GLJrnGrpList
        public void GetList(ref System.String whereClause, ref System.Int32 pageSize, ref System.Int32 absolutePage, ref System.Boolean morePages, Erp.Tablesets.GLJrnGrpListTableset result, Ice.Tablesets.ContextTableset ctxx)
        {
            try
            {
                LogMsg.AppendLine("Start ........");
                string MainBookID = Convert.ToString(glbProc.GetSysParam("MainBookID")).ToUpper();
                string BookID = string.Empty;
                string JournalCode = string.Empty;
                string IsSiteIDFilterApplicable = Convert.ToString(glbProc.GetSysParam("IsSiteIDFilterApplicable")).ToUpper();
                var bpmRow = ctxx.BpmData.FirstOrDefault();
                if (bpmRow != null && !String.IsNullOrEmpty(bpmRow.ShortChar01))
                    BookID = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar01)).ToUpper();
                JournalCode = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar02)).ToUpper();
                LogMsg.AppendLine("ContextTableset Success ........");

                if (BookID != MainBookID)
                {
                    whereClause = " BookID = '" + BookID + "' AND JournalCode = '" + JournalCode + "' AND Posted = 0";
                }
                else if (BookID == MainBookID)
                {
                    whereClause = " BookID = '" + BookID + "' AND Posted = 0 ";
                }
                if (IsSiteIDFilterApplicable.ToString() == "TRUE")
                {
                    whereClause = whereClause + " AND SiteID_c = '" + Session.PlantID + "'";
                }
                LogMsg.AppendLine("End ........");
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GLJournalEntry => GLJrnGrp GetList : " + LogMsg.ToString(), ex);
                throw new BLException("GLJournalEntry => GLJrnGrp GetList : " + ex.Message);
            }
        }
        #endregion

        #region GetByGroupID - Invalid GroupId For Current BookId
        public void GetByGroupID(ref System.String GroupID, Erp.Tablesets.GLJournalEntryTableset result, Ice.Tablesets.ContextTableset ctxx)
        {
            try
            {
                LogMsg.AppendLine("Start ........");
                string BookId = string.Empty;
                string JournalCode = string.Empty;
                string IsSiteIDFilterApplicable = Convert.ToString(glbProc.GetSysParam("IsSiteIDFilterApplicable")).ToUpper();
                var bpmRow = ctxx.BpmData.FirstOrDefault();

                if (bpmRow != null && !String.IsNullOrEmpty(bpmRow.ShortChar01))
                    BookId = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar01)).ToUpper();
                JournalCode = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar02)).ToUpper();

                LogMsg.AppendLine("ContextTableset Success ........");

                string GrpId = GroupID.ToString();
                var QryData = from p in Db.GLJrnGrp.AsQueryable()
                              where p.Company == Session.CompanyID &&
                              p.BookID.ToUpper() == BookId &&
                              p.JournalCode.ToUpper() == JournalCode &&
                              ((IsSiteIDFilterApplicable == "TRUE" && p.SiteID_c == Session.PlantID) || (IsSiteIDFilterApplicable == "FALSE")) &&
                              p.GroupID == GrpId
                              select p;
                foreach (var item in QryData)
                {
                    LogMsg.AppendLine("Inside QryData ........");

                    if (item.BookID.ToString() != BookId)
                    {
                        if (IsSiteIDFilterApplicable == "TRUE")
                        {
                            throw new BLException("Invalid GroupId For Current BookId and SiteID : " + Session.PlantID);
                        }
                        else
                        {
                            throw new BLException("Invalid GroupId For Current BookId");
                        }
                    }
                }
                LogMsg.AppendLine("End ........");
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GLJournalEntry => GetByGroupID : " + LogMsg.ToString(), ex);
                throw new BLException("GLJournalEntry => GetByGroupID : " + ex.Message);
            }
        }
        #endregion

        #region CheckBeforePost - Posting is not allowed, Approval is pending
        public void CheckBeforePost(ref System.String groupID, Ice.Tablesets.ContextTableset ctxx)
        {
            try
            {
                LogMsg.AppendLine("Start ........");
                string BookId = string.Empty;
                string JournalCode = string.Empty;
                bool PayFound = false;
                bool BankAdjFound = false;
                bool IsForeignMission = false;
                string ForeignMissionAllocationCurrencyCode = string.Empty;
                string BaseCurrencyCode = string.Empty;
                DateTime AllocationDate = DateTime.Now;
                string STCompany = string.Empty;
                var bpmRow = ctxx.BpmData.FirstOrDefault();

                if (bpmRow != null && !String.IsNullOrEmpty(bpmRow.ShortChar01))
                    BookId = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar01)).ToUpper();
                JournalCode = Convert.ToString(glbProc.GetSysParam(bpmRow.ShortChar02)).ToUpper();
                LogMsg.AppendLine("ContextTableset Success ........");
                LogMsg.AppendLine("AllocationDate :" + Convert.ToString(AllocationDate));
                if (!string.IsNullOrEmpty(BookId))
                {
                    string GroupId = groupID;
                    var GLJrnHedRow = (from ttGLJrnHed in Db.GLJrnHed.AsQueryable()
                                       where ttGLJrnHed.Company == Session.CompanyID &&
                                       ttGLJrnHed.BookID.ToUpper() == BookId &&
                                       ttGLJrnHed.GroupID == GroupId &&
                                       ttGLJrnHed.Posted == false
                                       select new
                                       {
                                           Company = ttGLJrnHed.Company,
                                           BookID = ttGLJrnHed.BookID.ToUpper(),
                                           JournalNum = ttGLJrnHed.JournalNum,
                                           FiscalYear = ttGLJrnHed.FiscalYear,
                                           FiscalYearSuffix = ttGLJrnHed.FiscalYearSuffix,
                                           JournalCode = ttGLJrnHed.JournalCode.ToUpper(),
                                           FiscalCalendarID = ttGLJrnHed.FiscalCalendarID,
                                           JEDate = ttGLJrnHed.JEDate,
                                           Approved = ttGLJrnHed.Approved_c,
                                           InitiateImprestWithdrawal = ttGLJrnHed.InitiateImprestWithdrawal_c,
                                           IsSysGenerated = ttGLJrnHed.IsSysGenerated_c,
                                           STCompany = ttGLJrnHed.STCompany_c,
                                           TrxCtrlNum = ttGLJrnHed.TrxCtrlNum_c,
                                           FundFromPrevYr = ttGLJrnHed.FundFromPrevYr_c,
                                           RateGrpCode = ttGLJrnHed.RateGrpCode
                                       });
                    foreach (var GLJrnHed in GLJrnHedRow)
                    {
                        LogMsg.AppendLine("Inside GLJrnHedRow ........");
                        bool Approved = Convert.ToBoolean(GLJrnHed.Approved);
                        bool InitiateImprestWithdrawal = Convert.ToBoolean(GLJrnHed.InitiateImprestWithdrawal);

                        if (Approved == false)      //Removed by mahesh as per code changes done by Shweta Bug ref -- 7266 && JournalCode != BAJrnCode
                        {
                            throw new BLException("Posting is not allowed, Approval is pending");
                        }
                        else if (Approved == true && InitiateImprestWithdrawal == true)
                        {
                            throw new BLException("Posting is not allowed, Imprest has Initiated for Withdrawal");
                        }

                        //Added By Pritesh Parmar on 02/08/2016,  In ST Company WOF transaction can be posted only after FOCR generated and posted from ST Company against WOF Ref number.
                        string TreasuryCompany = Convert.ToString(glbProc.GetSysParam("TreasuryCompany")).ToUpper();
                        string AllocAcctsBookID = Convert.ToString(glbProc.GetSysParam("AllocAcctsBookID")).ToUpper();

                        if (GLJrnHed.BookID.ToUpper() == AllocAcctsBookID && GLJrnHed.STCompany != string.Empty)
                        {
                            LogMsg.AppendLine("GLJrnHed.STCompany : " + GLJrnHed.STCompany);

                            /**Validate GL Book exists and is active in ST.**/
                            var GLBookRow = (from ttGLBook in Db.GLBook.AsQueryable()
                                             where ttGLBook.Company == GLJrnHed.STCompany &&
                                             ttGLBook.BookID.ToUpper() == GLJrnHed.BookID &&
                                             ttGLBook.Inactive == false
                                             select ttGLBook.BookID).Count();
                            if (GLBookRow == 0)
                            {
                                throw new BLException("Cannot post. Invalid Book in Sub Treasury.");
                            }
                            LogMsg.AppendLine("Validate GL Book exists and is active in ST Success");

                            /**Validate Fiscal Period and in ST.**/
                            var GLBookRowFiscalCalendarID = (from ttGLBook in Db.GLBook.AsQueryable()
                                                             where ttGLBook.Company == GLJrnHed.STCompany &&
                                                             ttGLBook.BookID.ToUpper() == GLJrnHed.BookID
                                                             select ttGLBook.FiscalCalendarID).FirstOrDefault();
                            if (GLBookRowFiscalCalendarID != null)
                            {
                                var FiscalPerRowCnt = (from ttFiscalPer in Db.FiscalPer.AsQueryable()
                                                       where ttFiscalPer.Company == GLJrnHed.STCompany &&
                                                       ttFiscalPer.FiscalCalendarID == GLBookRowFiscalCalendarID &&
                                                       ttFiscalPer.StartDate <= GLJrnHed.JEDate &&
                                                       ttFiscalPer.EndDate >= GLJrnHed.JEDate
                                                       select ttFiscalPer.Company).Count();
                                if (FiscalPerRowCnt == 0)
                                {
                                    throw new BLException("Cannot Post. No valid fiscal period found in Sub Treasury.");
                                }
                            }
                            LogMsg.AppendLine("Validate Fiscal Period and in ST Success");

                            /**Validate earliest apply date has been set for the transaction to get through to ST.**/
                            var EADTypeRowEarliestApplyDate = (from ttEADType in Db.EADType.AsQueryable()
                                                               where ttEADType.Company == GLJrnHed.STCompany &&
                                                               ttEADType.EADType1 == "GJ"
                                                               select ttEADType.EarliestApplyDate).FirstOrDefault();
                            if (EADTypeRowEarliestApplyDate != null)
                            {
                                LogMsg.AppendLine("GLJrnHed.JEDate : " + GLJrnHed.JEDate);
                                LogMsg.AppendLine("EADTypeRowEarliestApplyDate : " + EADTypeRowEarliestApplyDate);

                                if (GLJrnHed.JEDate < EADTypeRowEarliestApplyDate)
                                {
                                    throw new BLException("Cannot post. Invalid Earliest Apply Date in Sub Treasury.");
                                }
                            }
                            else
                            {
                                var EADCompRowEarliestApplyDate = (from ttEADType in Db.EADComp.AsQueryable()
                                                                   where ttEADType.Company == GLJrnHed.STCompany
                                                                   select ttEADType.EarliestApplyDate).FirstOrDefault();
                                if (EADCompRowEarliestApplyDate != null)
                                {
                                    LogMsg.AppendLine("GLJrnHed.JEDate : " + GLJrnHed.JEDate);
                                    LogMsg.AppendLine("EADCompRowEarliestApplyDate : " + EADCompRowEarliestApplyDate);

                                    if (GLJrnHed.JEDate < EADCompRowEarliestApplyDate)
                                    {
                                        throw new BLException("Cannot post. Invalid Earliest Apply Date in Sub Treasury.");
                                    }
                                }
                                else
                                {
                                    throw new BLException("Cannot Post. Earliest Apply Date has not been set for the Sub Treasury.");
                                }
                            }
                            LogMsg.AppendLine("Validate earliest apply date has been set for the transaction to get through to ST Success");

                            /*Validate vote definition setup */
                            var UD01RowCnt = (from ttUD01 in Db.UD01.AsQueryable()
                                              where ttUD01.Company.ToUpper() == TreasuryCompany &&
                                              ttUD01.Key1.ToUpper() == GLJrnHed.STCompany.ToUpper()
                                              select ttUD01.Company).Count();
                            if (UD01RowCnt == 0)
                            {
                                throw new BLException("Cannot Post. Vote code for company " + GLJrnHed.STCompany + " is not yet defined, Review vote definition setup.");
                            }
                            LogMsg.AppendLine("Validate vote definition setup Success");

                            /**START - Validate GL Accounts exists and is active in ST.**/
                            foreach (var GLJrnDtlMnlRow in (from ttGLJrnDtlMnl in Db.GLJrnDtlMnl.AsQueryable()
                                                            where ttGLJrnDtlMnl.Company == GLJrnHed.Company &&
                                                            ttGLJrnDtlMnl.BookID.ToUpper() == GLJrnHed.BookID &&
                                                            ttGLJrnDtlMnl.FiscalYear == GLJrnHed.FiscalYear &&
                                                            ttGLJrnDtlMnl.FiscalYearSuffix == GLJrnHed.FiscalYearSuffix &&
                                                            ttGLJrnDtlMnl.JournalCode.ToUpper() == GLJrnHed.JournalCode &&
                                                            ttGLJrnDtlMnl.JournalNum == GLJrnHed.JournalNum &&
                                                            ttGLJrnDtlMnl.FiscalCalendarID == GLJrnHed.FiscalCalendarID
                                                            select new
                                                            {
                                                                COACode = ttGLJrnDtlMnl.COACode.ToUpper(),
                                                                GLAccount = ttGLJrnDtlMnl.GLAccount,
                                                                FMCurrencyCode = ttGLJrnDtlMnl.CurrencyCodeAcct,
                                                                BaseCurrencyCode = ttGLJrnDtlMnl.CurrencyCode,
                                                                JEDate = ttGLJrnDtlMnl.JEDate
                                                            }))
                                if (GLJrnDtlMnlRow != null)
                                {
                                    STCompany = Convert.ToString(GLJrnHed.STCompany);

                                    ForeignMissionAllocationCurrencyCode = GLJrnDtlMnlRow.FMCurrencyCode;
                                    BaseCurrencyCode = GLJrnDtlMnlRow.BaseCurrencyCode;
                                    AllocationDate = Convert.ToDateTime(GLJrnDtlMnlRow.JEDate);

                                    LogMsg.AppendLine("STCompany : " + STCompany + "       GLAccount : " + GLJrnDtlMnlRow.GLAccount);
                                    LogMsg.AppendLine("ForeignMissionAllocationCurrencyCode : " + ForeignMissionAllocationCurrencyCode + "       BaseCurrencyCode : " + BaseCurrencyCode + " AllocationDate : " + Convert.ToString(AllocationDate));

                                    var GLAccountRowCnt = (from ttGLAccount in Db.GLAccount.AsQueryable()
                                                           where ttGLAccount.Company == STCompany &&
                                                           ttGLAccount.COACode.ToUpper() == GLJrnDtlMnlRow.COACode &&
                                                           ttGLAccount.GLAccount1 == GLJrnDtlMnlRow.GLAccount &&
                                                           ttGLAccount.Active == true
                                                           select ttGLAccount.Company).Count();
                                    if (GLAccountRowCnt == 0)
                                    {
                                        throw new BLException("Cannot post. GL Account : " + GLJrnDtlMnlRow.GLAccount + " has not been created in Sub Treasury.");
                                    }
                                }
                            LogMsg.AppendLine("Validate GL Accounts exists and is active in ST Success");
                            /**END - Validate GL Accounts exists and is active in ST.**/

                            if (GLJrnHed.FundFromPrevYr == false)
                            {
                                var VoteDefnRow = (from ttVoteDefn in Db.UD01.AsQueryable()
                                                   where ttVoteDefn.Company.ToUpper() == TreasuryCompany &&
                                                        ttVoteDefn.Key1 == Session.CompanyID &&
                                                        (ttVoteDefn.CheckBox01 == true || ttVoteDefn.CheckBox05 == true) &&
                                                        ttVoteDefn.CheckBox03 == true
                                                   select ttVoteDefn.Company).FirstOrDefault();
                                if (VoteDefnRow != null)
                                {
                                    LogMsg.AppendLine("Inside VoteDefnRow ........");
                                    ValidateSTAllocationBeforePost(Session.CompanyID, GroupId, TreasuryCompany, BookId, out PayFound, out BankAdjFound);

                                    if (PayFound == false)
                                    {
                                        throw new BLException("Payment corresponding to this Warrant of Funds Allocation not found.");
                                    }

                                    if (BankAdjFound == false)
                                    {
                                        throw new BLException("Bank Adjustment corresponding to this Warrant of Funds Allocation not found.");
                                    }
                                }
                                LogMsg.AppendLine("VoteDefnRow Success");
                            }
                            /*  Validate ST Company Base Currency */
                            var VoteDefRow = (from ttVoteDefn in Db.UD01.AsQueryable()
                                              where ttVoteDefn.Company.ToUpper() == TreasuryCompany &&
                                                   ttVoteDefn.Key1 == GLJrnHed.STCompany &&
                                                   ttVoteDefn.CheckBox05 == true &&
                                                   ttVoteDefn.CheckBox03 == true
                                              select new { ttVoteDefn.Company, ttVoteDefn.CheckBox05 }).FirstOrDefault();
                            if (VoteDefRow != null)
                            {
                                IsForeignMission = VoteDefRow.CheckBox05;
                            }

                            LogMsg.AppendLine(" IsForeignMission : " + Convert.ToString(IsForeignMission) + " ForeignMissionAllocationCurrencyCode : " + Convert.ToString(ForeignMissionAllocationCurrencyCode));
                            if (IsForeignMission == true)
                            {

                                // For FM, Destination company has base currency as the WOF Allocation currency
                                var FMCurrencyRow = (from ttCurrRow in Db.Currency.AsQueryable()
                                                     where ttCurrRow.Company == GLJrnHed.STCompany &&
                                                     ttCurrRow.BaseCurr == true
                                                     select new { ttCurrRow.CurrencyCode }
                                                     ).FirstOrDefault();
                                if (FMCurrencyRow != null)
                                {
                                    if (ForeignMissionAllocationCurrencyCode != FMCurrencyRow.CurrencyCode)
                                    {
                                        LogMsg.AppendLine(" ForeignMissionAllocationCurrencyCode : " + Convert.ToString(ForeignMissionAllocationCurrencyCode) + " FMCurrencyRow.CurrencyCode : " + FMCurrencyRow.CurrencyCode);
                                        throw new BLException(" Allocation Currency differs than the Foreign Mission Company Base Currency.");
                                    }
                                }

                                /**/
                                LogMsg.AppendLine("GLJrnHed.RateGrpCode : " + Convert.ToString(GLJrnHed.RateGrpCode));

                                // Validate Global rate types used for exists

                                var GlbRateGrpRow = (from ttGlbRateGrp in Db.GlbCurrRateGrp.AsQueryable()
                                                     where (ttGlbRateGrp.Company == Session.CompanyID || ttGlbRateGrp.Company == STCompany)
                                                     && ttGlbRateGrp.RateGrpCode == GLJrnHed.RateGrpCode
                                                     && ttGlbRateGrp.GlobalGrp == true
                                                     select new
                                                     {
                                                         Company = ttGlbRateGrp.Company
                                                     }
                                                       ).Distinct().Count();
                                if (GlbRateGrpRow <= 1)
                                {
                                    LogMsg.AppendLine("Used Currency Rate Type : " + Convert.ToString(GLJrnHed.RateGrpCode) + " is not a global rate type group..");
                                    throw new BLException("Used Currency Rate Type : " + Convert.ToString(GLJrnHed.RateGrpCode) + " is not a global rate type group..");
                                }

                                LogMsg.AppendLine("GLJrnDtlMnlRow.BaseCurrencyCode : " + BaseCurrencyCode + " GLJrnDtlMnlRow.ForeignMissionAllocationCurrencyCode : " + ForeignMissionAllocationCurrencyCode);

                                LogMsg.AppendLine("Session.CompanyID  : " + Session.CompanyID + " STCompany : " + STCompany);

                                LogMsg.AppendLine("AllocationDate :" + Convert.ToString(AllocationDate));

                                // There exists valid exchange rates exists in both source and destination
                                var dbExchangeRate = (from ttExchangeRate in Db.CurrExRate.AsQueryable()
                                                      where (ttExchangeRate.Company == Session.CompanyID || ttExchangeRate.Company == STCompany)
                                                      && ttExchangeRate.RateGrpCode == GLJrnHed.RateGrpCode
                                                      && ((ttExchangeRate.SourceCurrCode == BaseCurrencyCode || ttExchangeRate.SourceCurrCode == ForeignMissionAllocationCurrencyCode)
                                                      && (ttExchangeRate.TargetCurrCode == BaseCurrencyCode || ttExchangeRate.TargetCurrCode == ForeignMissionAllocationCurrencyCode))
                                                      && ttExchangeRate.EffectiveDate <= AllocationDate
                                                      select new
                                                      {
                                                          Company = ttExchangeRate.Company
                                                      }
                                                       ).Distinct().Count();

                                if (dbExchangeRate <= 1)
                                {
                                    LogMsg.AppendLine("Exchange Rate Setup not valid. dbExchangeRate : " + Convert.ToString(dbExchangeRate));
                                    throw new BLException("Exchange Rate Setup not valid.");
                                }
                                /**/
                            }
                        }
                        //END - Pritesh Parmar

                        //Added By Pritesh Parmar on 11/08/2016
                        /*Validate for each journal line in Warrant Release there are valid vote definition,
                          bank accounts and the bank fee, and they all have GLControls populated with required
                          information where applicable.*/
                        string WarrantRelBookID = Convert.ToString(glbProc.GetSysParam("WarrantRelBookID")).ToUpper();
                        string PayableAcct = Convert.ToString(glbProc.GetSysParam("PayableAcct").ToUpper()).ToUpper();
                        string WarrantRelJrnCode = Convert.ToString(glbProc.GetSysParam("WarrantRelJrnCode")).ToUpper();
                        string WarrantRelWJrnCode = Convert.ToString(glbProc.GetSysParam("WarrantRelWJrnCode")).ToUpper();
                        string SubBudgetCls = string.Empty;
                        string TrxCtrlNum = string.Empty;
                        string RefTrxCtrlNum = string.Empty;
                        string ReturnMsg = string.Empty;
                        LogMsg.AppendLine("TreasuryCompany : " + TreasuryCompany + "  WarrantRelBookID : " + WarrantRelBookID + "  PayableAcct : " + PayableAcct);

                        foreach (var GLJrnDtlMnlRow in (from ttGLJrnDtlMnl in Db.GLJrnDtlMnl.AsQueryable()
                                                        where ttGLJrnDtlMnl.Company.ToUpper() == TreasuryCompany &&
                                                        ttGLJrnDtlMnl.BookID.ToUpper() == WarrantRelBookID &&
                                                        ttGLJrnDtlMnl.GroupID == GroupId &&
                                                        ttGLJrnDtlMnl.Posted == false
                                                        select ttGLJrnDtlMnl))
                        {
                            var GLJrnHedRowSubBudgetCls = (from ttGLJrnHed in Db.GLJrnHed.AsQueryable()
                                                           where ttGLJrnHed.Company == GLJrnDtlMnlRow.Company &&
                                                           ttGLJrnHed.BookID.ToUpper() == GLJrnDtlMnlRow.BookID.ToUpper() &&
                                                           ttGLJrnHed.FiscalYear == GLJrnDtlMnlRow.FiscalYear &&
                                                           ttGLJrnHed.FiscalYearSuffix == GLJrnDtlMnlRow.FiscalYearSuffix &&
                                                           ttGLJrnHed.JournalCode.ToUpper() == GLJrnDtlMnlRow.JournalCode.ToUpper() &&
                                                           ttGLJrnHed.JournalNum == GLJrnDtlMnlRow.JournalNum &&
                                                           ttGLJrnHed.FiscalCalendarID == GLJrnDtlMnlRow.FiscalCalendarID
                                                           select new
                                                           {
                                                               SubBudgetCls = ttGLJrnHed.SubBudgetCls_c,
                                                               TrxCtrlNum = ttGLJrnHed.TrxCtrlNum_c,
                                                               RefTrxCtrlNum = ttGLJrnHed.RefTrxCtrlNum_c
                                                           }).FirstOrDefault();
                            if (GLJrnHedRowSubBudgetCls != null)
                            {
                                SubBudgetCls = GLJrnHedRowSubBudgetCls.SubBudgetCls;
                                TrxCtrlNum = GLJrnHedRowSubBudgetCls.TrxCtrlNum;
                                RefTrxCtrlNum = GLJrnHedRowSubBudgetCls.RefTrxCtrlNum;
                            }
                            LogMsg.AppendLine("SubBudgetCls : " + SubBudgetCls + "    SegValue2 : " + GLJrnDtlMnlRow.SegValue2 + "    TrxCtrlNum : " + TrxCtrlNum + "    RefTrxCtrlNum : " + RefTrxCtrlNum);

                            /*Validate company and votes in votes defn*/
                            var VoteDefnRowVoteCompany = (from ttVoteDefn in Db.UD01
                                                          where ttVoteDefn.Company.ToUpper() == TreasuryCompany &&
                                                               ttVoteDefn.ShortChar01 == GLJrnDtlMnlRow.SegValue2 &&
                                                               ttVoteDefn.CheckBox03 == true
                                                          select ttVoteDefn.Key1).FirstOrDefault();
                            if (!string.IsNullOrEmpty(VoteDefnRowVoteCompany))
                            {
                                LogMsg.AppendLine("VoteDefnRowVoteCompany : " + VoteDefnRowVoteCompany);
                                var CompanyRow = (from ttCompany in Db.Company
                                                  where ttCompany.Company1.ToUpper() == VoteDefnRowVoteCompany.ToUpper()
                                                  select ttCompany.Company1).FirstOrDefault();
                                if (string.IsNullOrEmpty(CompanyRow))
                                {
                                    throw new BLException("Cannot post. Company " + GLJrnDtlMnlRow.SegValue2 + " is not defined.");
                                }
                            }
                            else
                            {
                                if (GLJrnDtlMnlRow.SegValue1.ToUpper() != PayableAcct)
                                {
                                    throw new BLException("Cannot post. Vote Definition " + GLJrnDtlMnlRow.SegValue2 + " is not defined or Inactive.");
                                }
                            }

                            /*START: Validate Bank Account for Treasury*/
                            if (GLJrnDtlMnlRow.SegValue1.ToUpper() == PayableAcct)        /*Find details for Treasury Bank Account*/
                            {
                                // if (GLJrnDtlMnlRow.JournalCode == WarrantRelJrnCode)   /*Condition not added bcoz it was not exist in 10.1 but its there on 10.0*/

                                LogMsg.AppendLine("PayableAcct = True");
                                var UD102Row = (from ttUD102 in Db.UD102
                                                where ttUD102.Company.ToUpper() == TreasuryCompany &&
                                                     ttUD102.Key1 == SubBudgetCls
                                                select ttUD102).FirstOrDefault();
                                if (UD102Row != null)
                                {
                                    GLJrnDtlMnlRow["TrBankAcct_c"] = UD102Row.ShortChar01;
                                    GLJrnDtlMnlRow["TrDepBankFee_c"] = UD102Row.ShortChar02;
                                    GLJrnDtlMnlRow["TrWithBankFee_c"] = UD102Row.ShortChar03;

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateBankAcct " + "@company = {0},@bankAcctId = {1}", TreasuryCompany, UD102Row.ShortChar01))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("1> ValidateBankAcct.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar01 : " + UD102Row.ShortChar01);

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateBankFee " + "@company = {0},@bankFeeId = {1}", TreasuryCompany, UD102Row.ShortChar02))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("2> ValidateBankFee.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar02 : " + UD102Row.ShortChar02);

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateBankFee " + "@company = {0},@bankFeeId = {1}", TreasuryCompany, UD102Row.ShortChar03))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("3> ValidateBankFee.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar03 : " + UD102Row.ShortChar03);

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateGLControl " + "@company = {0},@key1 = {1},@glControlType = {2},@glAcctContext = {3}", TreasuryCompany, UD102Row.ShortChar01, "Bank Account", "Cash"))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("1> ValiDateGLControl.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar01 : " + UD102Row.ShortChar01);

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateGLControl " + "@company = {0},@key1 = {1},@glControlType = {2},@glAcctContext = {3}", TreasuryCompany, UD102Row.ShortChar02, "Bank Fee", "Cash"))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("2> ValiDateGLControl.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar02 : " + UD102Row.ShortChar02);

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateGLControl " + "@company = {0},@key1 = {1},@glControlType = {2},@glAcctContext = {3}", TreasuryCompany, UD102Row.ShortChar03, "Bank Fee", "Cash"))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("3> ValiDateGLControl.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar03 : " + UD102Row.ShortChar03);
                                }
                                else
                                {
                                    throw new BLException("Cannot post. Bank details for vote " + GLJrnDtlMnlRow.SegValue2 + " and Sub Budget Class " + SubBudgetCls + " has not been setup");
                                }
                            }
                            else
                            {
                                /*Find details for the payment bank accounts*/
                                LogMsg.AppendLine("PayableAcct = False");
                                var UD102ARow = (from ttUD102A in Db.UD102A
                                                 where ttUD102A.Company.ToUpper() == TreasuryCompany &&
                                                     ttUD102A.Key1 == SubBudgetCls &&
                                                     ttUD102A.ChildKey1 == GLJrnDtlMnlRow.SegValue2
                                                 select ttUD102A).FirstOrDefault();
                                if (UD102ARow != null)
                                {
                                    GLJrnDtlMnlRow["VoteBankAcct_c"] = UD102ARow.ShortChar01;
                                    GLJrnDtlMnlRow["VoteDepBankFee_c"] = UD102ARow.ShortChar02;
                                    GLJrnDtlMnlRow["VoteWithBankFee_c"] = UD102ARow.ShortChar03;

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateBankAcct " + "@company = {0},@bankAcctId = {1}", TreasuryCompany, UD102ARow.ShortChar01))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("1> ValidateBankAcct.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar01 : " + UD102ARow.ShortChar01);

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateBankFee " + "@company = {0},@bankFeeId = {1}", TreasuryCompany, UD102ARow.ShortChar02))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("2> ValidateBankFee.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar02 : " + UD102ARow.ShortChar02);

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateBankFee " + "@company = {0},@bankFeeId = {1}", TreasuryCompany, UD102ARow.ShortChar03))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("3> ValidateBankFee.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar03 : " + UD102ARow.ShortChar03);

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateGLControl " + "@company = {0},@key1 = {1},@glControlType = {2},@glAcctContext = {3}", TreasuryCompany, UD102ARow.ShortChar01, "Bank Account", "Cash"))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("1> ValiDateGLControl.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar01 : " + UD102ARow.ShortChar01);

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateGLControl " + "@company = {0},@key1 = {1},@glControlType = {2},@glAcctContext = {3}", TreasuryCompany, UD102ARow.ShortChar02, "Bank Fee", "Cash"))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("2> ValiDateGLControl.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar02 : " + UD102ARow.ShortChar02);

                                    foreach (var DataGetGLAcctDisp in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_ValidateGLControl " + "@company = {0},@key1 = {1},@glControlType = {2},@glAcctContext = {3}", TreasuryCompany, UD102ARow.ShortChar03, "Bank Fee", "Cash"))
                                    {
                                        ReturnMsg = DataGetGLAcctDisp.ReturnMsg;
                                    }
                                    LogMsg.AppendLine("3 > ValiDateGLControl.ReturnMsg : " + ReturnMsg + "  UD102Row.ShortChar03 : " + UD102ARow.ShortChar03);
                                }
                                else
                                {
                                    throw new BLException("Cannot post. Bank details for vote " + GLJrnDtlMnlRow.SegValue2 + " and Sub Budget Class " + SubBudgetCls + " has not been setup");
                                }
                            }
                            /*END: Validate Bank Account for Treasury*/

                            /*START: Assigned original bank account id while warrant release withdrawal*/
                            if (GLJrnDtlMnlRow.JournalCode.ToUpper() == WarrantRelWJrnCode)
                            {
                                LogMsg.AppendLine("WarrantRelJrnCode : " + WarrantRelJrnCode + "     WarrantRelWJrnCode : " + WarrantRelWJrnCode);
                                var GLJrnDtlMnlWRRow = (from ttGLJrnDtlMnl in Db.GLJrnDtlMnl.AsQueryable()
                                                        where ttGLJrnDtlMnl.Company == TreasuryCompany &&
                                                        ttGLJrnDtlMnl.BookID.ToUpper() == GLJrnDtlMnlRow.BookID &&
                                                        ttGLJrnDtlMnl.FiscalYear == GLJrnDtlMnlRow.FiscalYear &&
                                                        ttGLJrnDtlMnl.FiscalYearSuffix == GLJrnDtlMnlRow.FiscalYearSuffix &&
                                                        ttGLJrnDtlMnl.JournalCode.ToUpper() == WarrantRelJrnCode.ToUpper() &&
                                                        ttGLJrnDtlMnl.FiscalCalendarID == GLJrnDtlMnlRow.FiscalCalendarID &&
                                                        ttGLJrnDtlMnl.SegValue1 == GLJrnDtlMnlRow.SegValue1 &&
                                                        ttGLJrnDtlMnl.SegValue2 == GLJrnDtlMnlRow.SegValue2 &&
                                                        ttGLJrnDtlMnl.SegValue3 == GLJrnDtlMnlRow.SegValue3 &&
                                                        ttGLJrnDtlMnl.TrxCtrlNum_c == RefTrxCtrlNum
                                                        select new
                                                        {
                                                            TrBankAcct = ttGLJrnDtlMnl.TrBankAcct_c,
                                                            TrDepBankFee = ttGLJrnDtlMnl.TrDepBankFee_c,
                                                            TrWithBankFee = ttGLJrnDtlMnl.TrWithBankFee_c,
                                                            VoteBankAcct = ttGLJrnDtlMnl.VoteBankAcct_c,
                                                            VoteDepBankFee = ttGLJrnDtlMnl.VoteDepBankFee_c,
                                                            VoteWithBankFee = ttGLJrnDtlMnl.VoteWithBankFee_c
                                                        }).FirstOrDefault();
                                if (GLJrnDtlMnlWRRow != null)
                                {
                                    LogMsg.AppendLine("SegValue2 : " + GLJrnDtlMnlRow.SegValue2);
                                    if (GLJrnDtlMnlRow.SegValue1.ToUpper() == PayableAcct)
                                    {
                                        GLJrnDtlMnlRow["TrBankAcct_c"] = GLJrnDtlMnlWRRow.TrBankAcct;
                                        GLJrnDtlMnlRow["TrDepBankFee_c"] = GLJrnDtlMnlWRRow.TrDepBankFee;
                                        GLJrnDtlMnlRow["TrWithBankFee_c"] = GLJrnDtlMnlWRRow.TrWithBankFee;
                                        LogMsg.AppendLine("TrBankAcct : " + GLJrnDtlMnlWRRow.TrBankAcct + "     TrDepBankFee : " + GLJrnDtlMnlWRRow.TrDepBankFee + "     TrWithBankFee : " + GLJrnDtlMnlWRRow.TrWithBankFee);
                                    }
                                    else
                                    {
                                        GLJrnDtlMnlRow["VoteBankAcct_c"] = GLJrnDtlMnlWRRow.VoteBankAcct;
                                        GLJrnDtlMnlRow["VoteDepBankFee_c"] = GLJrnDtlMnlWRRow.VoteDepBankFee;
                                        GLJrnDtlMnlRow["VoteWithBankFee_c"] = GLJrnDtlMnlWRRow.VoteWithBankFee;
                                        LogMsg.AppendLine("VoteBankAcct : " + GLJrnDtlMnlWRRow.VoteBankAcct + "     VoteDepBankFee : " + GLJrnDtlMnlWRRow.VoteDepBankFee + "     VoteWithBankFee : " + GLJrnDtlMnlWRRow.VoteWithBankFee);
                                    }
                                }
                            }
                        }
                    }
                }
                LogMsg.AppendLine("End ........");
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GLJournalEntry => CheckBeforePost : " + LogMsg.ToString(), ex);
                throw new BLException("GLJournalEntry => CheckBeforePost : " + ex.Message);
            }
        }
        #endregion

        #region System Audit Log
        public void AuditLog(Erp.Tablesets.GLJournalEntryTableset ds)
        {
            try
            {
                DataTable DtAuditLog = new DataTable();
                DtAuditLog = GlobalSysFunctions.GetSysAuditInfo();
                foreach (var ttGLJrnHedRow in (from ttGLH in ds.GLJrnHed.AsEnumerable() select ttGLH))
                    if (ttGLJrnHedRow != null)
                    {
                        ttGLJrnHedRow["ApplicationDateTime_c"] = DtAuditLog.Rows[0]["ApplicationDateTime_c"];
                        ttGLJrnHedRow["ApplicationIPAddress_c"] = DtAuditLog.Rows[0]["ApplicationIPAddress_c"].ToString();
                        ttGLJrnHedRow["ApplicationMACAddress_c"] = DtAuditLog.Rows[0]["ApplicationMACAddress_c"].ToString();
                        ttGLJrnHedRow["ApplicationTimeZone_c"] = DtAuditLog.Rows[0]["ApplicationTimeZone_c"].ToString();
                        ttGLJrnHedRow["ApplicationUserName_c"] = Session.UserID; //AppUserName;
                        ttGLJrnHedRow["ApplicationMotherBoardId_c"] = DtAuditLog.Rows[0]["ApplicationMotherBoardId_c"].ToString();
                        ttGLJrnHedRow["DatabaseDateTime_c"] = DtAuditLog.Rows[0]["DatabaseDateTime_c"];
                        ttGLJrnHedRow["DatabaseIPAddress_c"] = DtAuditLog.Rows[0]["DatabaseIPAddress_c"].ToString();
                        ttGLJrnHedRow["DatabaseMACAddress_c"] = DtAuditLog.Rows[0]["DatabaseMACAddress_c"].ToString();
                        ttGLJrnHedRow["DatabaseTimeZone_c"] = DtAuditLog.Rows[0]["DatabaseTimeZone_c"].ToString();
                        ttGLJrnHedRow["DatabaseUserName_c"] = DtAuditLog.Rows[0]["DatabaseUserName_c"].ToString();
                        ttGLJrnHedRow["DatabaseName_c"] = DtAuditLog.Rows[0]["DatabaseName_c"].ToString();
                        ttGLJrnHedRow["DatabaseHostName_c"] = DtAuditLog.Rows[0]["DatabaseHostName_c"].ToString();
                    }
            }
            catch (Exception ex)
            {
                throw new BLException("BankAdjEntry > AuditLog > " + ex.Message.ToString());
            }
        }
        #endregion System Audit Log

        #region ValidateSTAllocationBeforePost
        //Added By Pritesh Parmar on 02/08/2016
        public void ValidateSTAllocationBeforePost(string company, string groupID, string treasuryCompany, string bookId, out bool payFound, out bool bankAdjFound)
        {
            try
            {
                payFound = false;
                bankAdjFound = false;
                string ISSTCreateBankAdj = Convert.ToString(glbProc.GetSysParam("ISSTCreateBankAdj"));

                decimal TotalPayAmt = 0;
                LogMsg.AppendLine("Start ........");

                var VoteDefnRow = (from ttVoteDefn in Db.UD01.AsQueryable()
                                   where ttVoteDefn.Company.ToUpper() == treasuryCompany &&
                                        ttVoteDefn.Key1 == Session.CompanyID &&
                                        (ttVoteDefn.CheckBox01 == true || ttVoteDefn.CheckBox05 == true) &&
                                        ttVoteDefn.CheckBox03 == true
                                   select ttVoteDefn.Company).FirstOrDefault();
                if (VoteDefnRow != null)
                {
                    LogMsg.AppendLine("Inside VoteDefnRow ........");
                    foreach (var GLJrnHedRow in (from ttGLJrnHed in Db.GLJrnHed.AsQueryable()
                                                 where ttGLJrnHed.Company.ToUpper() == Session.CompanyID &&
                                                 ttGLJrnHed.BookID.ToUpper() == bookId &&
                                                 ttGLJrnHed.GroupID == groupID &&
                                                 ttGLJrnHed.Posted == false
                                                 select new
                                                 {
                                                     Company = ttGLJrnHed.Company,
                                                     BookID = ttGLJrnHed.BookID,
                                                     JournalNum = ttGLJrnHed.JournalNum,
                                                     FiscalYear = ttGLJrnHed.FiscalYear,
                                                     FiscalYearSuffix = ttGLJrnHed.FiscalYearSuffix,
                                                     JournalCode = ttGLJrnHed.JournalCode,
                                                     FiscalCalendarID = ttGLJrnHed.FiscalCalendarID,
                                                     WOFAllocNum = ttGLJrnHed.WOFAllocNum_c,
                                                     MDACompany = ttGLJrnHed.MDACompany_c,
                                                     STCompany = ttGLJrnHed.STCompany_c,
                                                     TrxCtrlNum = ttGLJrnHed.TrxCtrlNum_c,
                                                     TotDebit = ttGLJrnHed.TotDebit,
                                                     CurrencyCode = ttGLJrnHed.CurrencyCode
                                                 }))
                    {
                        /*START: Validate if a posted payment in MDA exists*/
                        /*Find a payment in MDA for this allocation*/
                        LogMsg.AppendLine("Inside GLJrnHedRow ........");
                        if (ISSTCreateBankAdj.ToUpper() == "FALSE")
                        {
                            foreach (var APInvHedRow in (from ttAPInvHed in Db.APInvHed.AsQueryable()
                                                         where ttAPInvHed.Company == GLJrnHedRow.MDACompany &&
                                                         ttAPInvHed.FiscalYear == GLJrnHedRow.FiscalYear &&
                                                         ttAPInvHed.Posted == true &&
                                                         ttAPInvHed.OpenPayable == false &&
                                                         ttAPInvHed.DebitMemo == false &&
                                                         ttAPInvHed.STAllocPymt_c == true
                                                         select new
                                                         {
                                                             Company = ttAPInvHed.Company,
                                                             InvoiceNum = ttAPInvHed.InvoiceNum,
                                                             VendorNum = ttAPInvHed.VendorNum,
                                                             CPay = ttAPInvHed.CPay,
                                                             GlbVendorNum = ttAPInvHed.GlbVendorNum,
                                                             GlbInvoiceNum = ttAPInvHed.GlbInvoiceNum,
                                                             InvoiceAmt = ttAPInvHed.InvoiceAmt,
                                                             STRefNum = ttAPInvHed.STRefNum_c
                                                         }))
                            {
                                LogMsg.AppendLine("Inside APInvHedRow ........");
                                var ApInvDtlRow = (from ttApInvDtl in Db.APInvDtl.AsQueryable()
                                                   where ttApInvDtl.Company == APInvHedRow.Company &&
                                                   ttApInvDtl.InvoiceNum == APInvHedRow.InvoiceNum &&
                                                   ttApInvDtl.VendorNum == APInvHedRow.VendorNum &&
                                                   ttApInvDtl.UnitCost == GLJrnHedRow.TotDebit
                                                   select new
                                                   {
                                                       Company = ttApInvDtl.Company,
                                                       InvoiceNum = ttApInvDtl.InvoiceNum,
                                                       VendorNum = ttApInvDtl.VendorNum
                                                   }).FirstOrDefault();
                                if (ApInvDtlRow != null)
                                {
                                    LogMsg.AppendLine("Inside ApInvDtlRow ........");
                                    /**Find total payments made for this invoice, and if all payments sum
                                    up to the amount of the invoice then we are sure that payments have
                                    been received.
                
                                    TO DO:
                                    1. Take care of multi-currency scenario
                                    2. Take care of situations where we dont have central payment.**/

                                    if (APInvHedRow.CPay == true)
                                    {
                                        LogMsg.AppendLine("Inside CPay == true ........");
                                        var APSystRow = (from ttAPSyst in Db.APSyst.AsQueryable()
                                                         where ttAPSyst.Company == Session.CompanyID
                                                         select ttAPSyst).FirstOrDefault();
                                        if (APSystRow != null)
                                        {
                                            LogMsg.AppendLine("CPayCompany : " + APSystRow.CPayCompany + "  InvoiceNum : " + APInvHedRow.InvoiceNum +
                                                                "  VendorNum : " + APInvHedRow.VendorNum + "  GlbVendorNum : " + APInvHedRow.GlbVendorNum + "  GlbInvoiceNum : " + APInvHedRow.GlbInvoiceNum);
                                            var APTranRow = (from ttAPTran in Db.APTran.AsQueryable()
                                                             where ttAPTran.Company == APSystRow.CPayCompany &&
                                                             ttAPTran.TranType == "Pay" &&
                                                             ttAPTran.InvoiceNum == APInvHedRow.InvoiceNum &&           //Confirm with nitesh sir it supposee to be GlbInvoiceNum
                                                             ttAPTran.VendorNum == APInvHedRow.GlbVendorNum
                                                             select ttAPTran).FirstOrDefault();
                                            if (APTranRow != null)
                                            {
                                                TotalPayAmt = TotalPayAmt + APTranRow.TranAmt;
                                                LogMsg.AppendLine("Inside APTranRow......  TotalPayAmt : " + TotalPayAmt);
                                            }
                                            LogMsg.AppendLine("InvoiceAmt : " + APInvHedRow.InvoiceAmt);
                                            if (TotalPayAmt == APInvHedRow.InvoiceAmt)
                                            {
                                                payFound = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        /**TO DO: This is not a central payment so validate it within
                                           the company itself.**/
                                    }
                                }
                                else
                                {
                                    string STRefNum = Convert.ToString(APInvHedRow.STRefNum);
                                    string STCompany = Convert.ToString(GLJrnHedRow.STCompany);
                                    string TrxCtrlNum = Convert.ToString(GLJrnHedRow.TrxCtrlNum);
                                    string WOFAllocNum = Convert.ToString(GLJrnHedRow.WOFAllocNum);

                                    LogMsg.AppendLine("STRefNum : " + STRefNum + "  STCompany : " + STCompany + "  TrxCtrlNum : " + TrxCtrlNum + "  WOFAllocNum : " + WOFAllocNum);

                                    if (STRefNum == WOFAllocNum)
                                    {
                                        payFound = true;
                                        break;
                                    }
                                }
                            }
                            LogMsg.AppendLine("payFound : " + payFound);
                            /*END: Validate if a posted payment in MDA exists*/

                            /*START: Validate if a posted bank adjustment exists in ST*/

                            /**The posted bank adjustment will be entered using OFC cash account.
                            So that based on the sub budget class specified, in the cash receipt revenue account, corresponding transcation
                            will be created in the Treasury company**/

                            /*21357 - Sub-Treasury/Sub Accountancy/Foreign Mission Allocation Bank Adjustment required before posting allocation*/
                            /*Create a bank adjustment indicating that it is an ST Allocation Receipt, and provide the WoF Allocation 
                             * control number as a reference. The amount and currency of the bank adjustment must be the same as 
                             * the WoF allocation amount.*/

                            LogMsg.AppendLine("TrxCtrlNum : " + GLJrnHedRow.TrxCtrlNum + "    TotDebit : " + GLJrnHedRow.TotDebit + "    CurrencyCode : " + GLJrnHedRow.CurrencyCode);
                            var BankTranRow = (from ttBankTran in Db.BankTran.AsQueryable()
                                               where ttBankTran.Company == Session.CompanyID &&
                                               ttBankTran.GLPosted == true &&
                                               ttBankTran.CurrencyCode == GLJrnHedRow.CurrencyCode &&
                                               ttBankTran.TranAmt == GLJrnHedRow.TotDebit &&
                                               ttBankTran.WOFAllocNum_c == GLJrnHedRow.TrxCtrlNum
                                               select new { ttBankTran.Company, ttBankTran.TranNum }).FirstOrDefault();
                            if (BankTranRow != null)
                            {
                                bankAdjFound = true;
                            }
                            LogMsg.AppendLine("bankAdjFound : " + bankAdjFound);
                            /*END: Validate if a posted bank adjustment exists in ST*/

                        }
                        else
                        {
                            payFound = true;
                            var BankTranRow = (from ttBankTran in Db.BankTran.AsQueryable()
                                               where ttBankTran.Company == GLJrnHedRow.MDACompany &&
                                               ttBankTran.GLPosted == true &&
                                               ttBankTran.CurrencyCode == GLJrnHedRow.CurrencyCode &&
                                               Math.Abs(ttBankTran.TranAmt) == GLJrnHedRow.TotDebit &&
                                               ttBankTran.WOFAllocNum_c == GLJrnHedRow.WOFAllocNum &&
                                               ttBankTran.IsSysGenerated_c == true &&
                                               ttBankTran.TranAmt < 0
                                               select new { ttBankTran.Company, ttBankTran.TranNum }).FirstOrDefault();
                            if (BankTranRow != null)
                            {
                                var BankTranRow1 = (from ttBankTran in Db.BankTran.AsQueryable()
                                                   where ttBankTran.Company == Session.CompanyID &&
                                                   ttBankTran.GLPosted == true &&
                                                   ttBankTran.CurrencyCode == GLJrnHedRow.CurrencyCode &&
                                                   ttBankTran.TranAmt == GLJrnHedRow.TotDebit &&
                                                   ttBankTran.WOFAllocNum_c == GLJrnHedRow.TrxCtrlNum
                                                   select new { ttBankTran.Company, ttBankTran.TranNum }).FirstOrDefault();
                                if (BankTranRow1 != null)
                                {
                                    bankAdjFound = true;
                                }
                            }
                            LogMsg.AppendLine("bankAdjFound : " + bankAdjFound);

                        }
                    }
                    /*END: Validate if a posted payment in MDA exists*/
                }
                LogMsg.AppendLine("End ........");
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString(), ex);
                throw new BLException(ex.Message);
            }
        }
        #endregion

        #region GetSBCByTrxCtrlNum
        //added by mahesh to get SBC by selected WRTrxCtrlNum in SW
        public void GetSBCByTrxCtrlNum(string Company, string TrxCtrlNum)
        {
            try
            {
                foreach (var dbInfo in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_GetSBCByTrxCtrlNum " + " @CurrCompany = {0}, @WRTrxCtrlNbr = {1} ", Company, TrxCtrlNum))
                {
                    SegmentCode = Convert.ToString(dbInfo.SegmentCode);
                    SegmentName = Convert.ToString(dbInfo.SegmentName);
                    LogMsg.AppendLine("2. After assign=> SegmentCode: " + SegmentCode + " SegmentName: " + SegmentName);
                }
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo(LogMsg.ToString(), ex);
                throw new BLException("GetSBCByTrxCtrlNum : " + ex.Message);
            }
        }
        #endregion

    }
}