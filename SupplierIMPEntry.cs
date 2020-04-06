using System;
using System.Linq;
using System.Data;
using Epicor.Data;
using Erp;
using Ice;
using Stcl.Global.GlobalSysInfo;
using Stcl.Global.GlobalProcedures;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections;
using Ice.Lib.SPLists;
using Newtonsoft.Json.Linq;
using static Stcl.Bpm.SupplierIMPEntry.RabbitMQMsgs;
using RabbitMQ.Client.MessagePatterns;
using System.Web.Script.Serialization;
using Stcl.Global.GlobalMethods;
using System.Data.SqlClient;

/*	Revision History:
*  Version     Project		Author				Date			Purpose																		PBI/Bug			TASK
* 1.0.0.1	   CIFMIS		Shweta Parashar		01-Sep-2015		Validate Special character for VendBank TISS Payment Method					2537		2541
* 1.0.0.2      CIFMIS      Mahesh Deore        13-Oct-2015     System Audit Log Information saving
* 1.0.0.3      CIFMIS      Pritesh Parmar      04-Jan-2016     Validate GL Control of Supplier while supplier linking
* 1.0.0.4      CIFMIS      Pritesh Parmar      08-Jan-2016     Resoved code review issues (Imprest Supplier)
* 1.0.0.5      CIFMIS      Shweta Parashar     12-Jan-2016     Resoved code review issues (Update) EFT Validation	
* 2.0.0.0      CIFMIS      Sangram Kulkarni    04/02/2016      Upgrade to 10.1 
* 2.0.0.1      CIFMIS      Sangram Kulkarni    25/02/2016      Updated code to filter Imprest Suppliers only.
* 2.0.0.2      CIFMIS      Shekhar Chaudhary   4th,May 2016    Upgraded references from 10.1.400.1 to 10.1.400.8
* 2.0.0.3      CIFMIS      PRITESH PARMAR      12th,May 2016   INCORPORATED 10.0 CHANGES TO 10.1 AND RESOLVED CODE REVIEW ISSUES
* 2.0.0.4      CIFMIS      Shekhar Chaudhary   12th,May 2016   Upgraded references from 10.1.400.8 to 10.1.400.9
* 2.0.0.5      CIFMIS      Shekhar Chaudhary   23rd,May 2016   Downgraded references from 10.1.400.9 to 10.1.400.1
* 2.0.0.6      CIFMIS      Shekhar Chaudhary   24th,June 2016  Updated to refer correct customization id(Code Merged from Backup Project)
* 2.0.0.7      CIFMIS      Pritesh Parmar      28-June-2016    Issue Resolved, VSO No : 8121, CG Model - In Supplier & Customer setup form validation message is required for GL Control , Removed Environment.NewLine
* 2.0.0.8      CIFMIS      Mahesh Deore        29-Jul-2016     replaced ttPayMethod.Name == "TISS" with ttPayMethod.Type == 1 
* 2.0.0.9      CIFMIS      Rajesh              04-Jan-2017     VSO BUG Id - 10024 - Information message for verify GL control prompt twice while saving supplier information
*                                                              Also Remove BPM Method Directory in Vendor Update Before Method Stcl.Vendor.UpdateBefore
*                                                              The same BPM call three time                                        
* 2.0.0.10     CIFMIS      Sangram Kulkarni    06/04/2017      Code implemented for Global supplier                                    10778
* 2.0.0.10     CIFMIS      Sangram Kulkarni    06/04/2017      Code changes done for Global supplier                                   13596
* 2.0.0.11     CIFMIS      Rajesh Tiwair       02/06/2017      Create Supplier Id from system using Switch   Bug Id - 12985                          
* 2.0.0.12     CIFMIS      Shekhar Chaudhary   05th,Jul 2017   Upgraded references and method parameters for Epicor ERP Version 10.1.600.5
* 2.0.0.13     CIFMIS      Shekahr Chaudhary   04th,Aug 2017   Changed AsEnumerable() with AsQueryable() for performance improvement for DB Objects.
* 2.0.0.14     CIFMIS		Rajesh Tiwari       23-Feb-2018     Upgrade 10.2 PB ID - 16097 TaskID - 16177
* 2.0.0.15     CIFMIS      Pritesh Parmar      09-Mar-2018     Resolved, 16318 - Error on update supplier with TISS payment method and on CSG supplier entry form    
* 2.0.0.16     CIFMIS      Rajesh              16/08/2018      VSO Bug Id - 17327 Bank branch changes on the Bank account entry should update the bank identifier and Bank Name
* 2.0.0.17     CIFMIS      Amod Loharkar       24/08/2018      VSO Bug Id - 17284, 17329, 17332 Regarding Global Supplier Linking GL Control Validation and Paymethod Linking.
* 2.0.0.18     CIFMIS      Pritesh Parmar      18/12/2018      VSO No : 18600, Provision for Supplier Status in Supplier Master
* 2.0.0.19     CIFMIS      Shekhar Chaudhary   04/03/2018      VSO Bug ID -21645-- Check Discussion section of this bug id for more clarity on code changes.
* 2.0.0.20     CIFMIS      Pritesh Parmar      18/12/2018      VSO No : 22567, MOF: Vendor Field Name should not Allow Special Character
* 2.0.0.21     CIFMIS      Pritesh Parmar      10/06/2019      VSO No : 22568, MOF: Vendor with TRA Check Box as TRUE does not UPDATE in MDA when Vendor is Linked though Multi-Company Direct
* 2.0.0.22     CIFMIS      Rajesh              27/12/2019      VSO ID : 27299 GOTG -Tax ID should be mandatory during supplier creation
*  * -------------------------------------------------------------------------------------------------------------------------------------*/
namespace Stcl.Bpm.SupplierIMPEntry
{
    public class SupplierIMPEntry : ContextBoundBase<ErpContext>
    {
        private static Erp.ErpContext IceDtContext = null;
        //static EventLog objEventLog = new EventLog();
        public SupplierIMPEntry(ErpContext ctx) : base(ctx)
        {
        }

        GlobalProcedures gblProc = new GlobalProcedures(IceDtContext);
        BusinessObject ObjBOnew = new BusinessObject();
        #region UpdateBefore
        public void UpdateBefore(Erp.Tablesets.VendorTableset ds, Ice.Tablesets.ContextTableset ctxx)
        {
            StringBuilder ErrorLine = new StringBuilder();
            StringBuilder LogMsg = new StringBuilder();
            EventLog objEventLog = new EventLog();
            string filename = @"\\10.7.0.11\EpicorData\test\test.txt";
            string newPath = Path.GetFullPath(filename);

            try
            {
                bool Issued = false;
                bool ImprestAdmnGrp = false;
                bool testdata = false;
                string TreasuryCompany = Convert.ToString(gblProc.GetSysParam("TreasuryCompany"));
                bool IsSuppInActive = Convert.ToBoolean(gblProc.GetSysParam("IsSuppInActive"));
                //bool IsTaxIdMandatory = Convert.ToBoolean(gblProc.GetSysParam("IsTaxIdMandatory"));
                bool IsTaxIdMandatory = true;
                var bpmRow = ctxx.BpmData.FirstOrDefault();
                string CustomizationID = string.Empty;
                string TaxPayerId = string.Empty;
                string LegalNametest = string.Empty;
                CustomizationID = ctxx.Client[0].CustomizationId.ToString();
                ErrorLine.AppendLine("CustomizationID : " + CustomizationID);

                if (CustomizationID.ToString() == "Stcl.VendorEntry.ImprestSupplier")
                {
                    string ImprestAdministratorGroup = Convert.ToString(gblProc.GetSysParam("ImprestAdministratorGroup")).ToUpper();
                    string ImprestSupplierGrp = Convert.ToString(gblProc.GetSysParam("ImprestSupplierGrp")).ToUpper();
                    string ImprestBookID = Convert.ToString(gblProc.GetSysParam("ImprestBookID")).ToUpper();

                    ErrorLine.AppendLine("ImprestAdministratorGroup : " + ImprestAdministratorGroup + " ImprestSupplierGrp : " + ImprestSupplierGrp + " ImprestBookID : " + ImprestBookID);

                    foreach (var UserFileRow in (from ttUserFile in Db.UserFile.AsQueryable()
                                                 where ttUserFile.DcdUserID == Session.UserID
                                                 select ttUserFile.GroupList).Distinct())
                    {
                        if (UserFileRow != null)
                        {
                            char[] delimiters = new char[] { '~' };

                            string[] GroupListCount = UserFileRow.Split(delimiters, StringSplitOptions.None);

                            for (int iCount = 0; iCount < GroupListCount.Length; iCount++)
                            {
                                string Key1 = GroupListCount[iCount];

                                if (Key1.ToString().ToUpper() == ImprestAdministratorGroup.ToString())
                                {
                                    ImprestAdmnGrp = true;
                                    break;
                                }
                                else
                                {
                                    ImprestAdmnGrp = false;
                                }
                            }

                            if (ImprestAdmnGrp == false)
                            {
                                throw new BLException("Only Imprest Administrators Can Modify Imprest Suppliers");
                            }

                            ErrorLine.AppendLine("ImprestAdmnGrp : " + ImprestAdmnGrp.ToString());
                        }
                    }
                    var DatattVendorRow = (from ttVendor_Row in ds.Vendor.AsEnumerable()
                                           where !string.IsNullOrEmpty(ttVendor_Row.RowMod)
                                           select ttVendor_Row).FirstOrDefault();
                    if (DatattVendorRow != null)
                    {
                        foreach (var DataVendor in (from ttVendor in Db.Vendor.AsQueryable()
                                                    where ttVendor.Company == TreasuryCompany &&
                                                    ttVendor.VendorID == DatattVendorRow.VendorID &&
                                                    ttVendor.Imprest_c == true
                                                    select new
                                                    {
                                                        ImprestCustID_c = ttVendor.ImprestCustID_c,
                                                        Imprest_c = ttVendor.Imprest_c
                                                    }))
                            if (DataVendor != null)
                            {
                                DatattVendorRow["ImprestCustID_c"] = DataVendor.ImprestCustID_c;
                                DatattVendorRow["Imprest_c"] = DataVendor.Imprest_c;
                            };

                        if (string.IsNullOrEmpty(DatattVendorRow["ImprestCustID_c"].ToString()))
                        {
                            throw new BLException("Imprest Customer can not be blank.");
                        }

                        if (string.IsNullOrEmpty(DatattVendorRow.GroupCode))
                        {
                            throw new BLException("GroupCode can not be blank.");
                        }

                        if (string.IsNullOrEmpty(DatattVendorRow.TermsCode.ToString()))
                        {
                            throw new BLException("TermsCode can not be blank.");
                        }

                        if (string.IsNullOrEmpty(DatattVendorRow.ShipViaCode.ToString()))
                        {
                            throw new BLException("ShipViaCode can not be blank.");
                        }

                        if (string.IsNullOrEmpty(DatattVendorRow.DefaultFOB.ToString()))
                        {
                            throw new BLException("FOB can not be blank.");
                        }

                        if (Convert.ToString(DatattVendorRow.PMUID) == "0")
                        {
                            throw new BLException("Payment Method can not be blank.");
                        }

                        /* check to see if there has been any imprest issued */
                        var DataAPInvHedRow = (from APInvHed_Row in Db.APInvHed.AsQueryable()
                                               where APInvHed_Row.Company == DatattVendorRow.Company
                                               && APInvHed_Row.VendorNum == DatattVendorRow.VendorNum
                                               && APInvHed_Row.ImprestInv_c == true
                                               select APInvHed_Row).Count();
                        {
                            if (DataAPInvHedRow > 0)
                            {
                                Issued = true;
                                ErrorLine.AppendLine("DataAPInvHedRow > ImprestInv_c = true");
                            }
                        }

                        var DataGLJrnHedRow = (from GLJrnHed_Row in Db.GLJrnHed.AsQueryable()
                                               where GLJrnHed_Row.Company == DatattVendorRow.Company
                                               && GLJrnHed_Row.BookID.ToUpper() == ImprestBookID
                                               && GLJrnHed_Row.ImprestCustomer_c == DatattVendorRow.VendorID
                                               && GLJrnHed_Row.Imprest_c == true
                                               && GLJrnHed_Row.TotalImprestOutstanding_c > 0
                                               select GLJrnHed_Row).Count();
                        {
                            if (DataGLJrnHedRow > 0)
                            {
                                Issued = true;
                                ErrorLine.AppendLine("DataGLJrnHedRow > Imprest_c = true");
                            }
                        }

                        /* cannot delete if imprest has been issued */
                        if (Convert.ToString(DatattVendorRow.RowMod) == "D" && Issued == true)
                        {
                            throw new BLException("Cannot Delete This Employee Supplier. Imprest Issues Exist.");
                        }

                        /* cannot change supplier group if issue exists */
                        if (DatattVendorRow != null && DatattVendorRow.GroupCode != DatattVendorRow.GroupCode)
                        {
                            if (DatattVendorRow.UDField<bool>("Imprest_c") != DatattVendorRow.UDField<bool>("Imprest_c") && Issued == true)
                            {
                                throw new BLException("Cannot Change Customer Group or imprest Status.  Imprest Issues Exist.");
                            }
                        }

                        /* if no imprest has been issued then we can modify and default group */
                        if ((DatattVendorRow.RowMod == "A" || DatattVendorRow.RowMod == "U") && Issued == false)
                        {
                            string SupplierGrp = Convert.ToString(ImprestSupplierGrp);
                            DatattVendorRow.GroupCode = SupplierGrp;
                            ErrorLine.AppendLine("SupplierGrp = " + SupplierGrp);
                        }
                    }

                    #region Added By Pritesh Parmar on 28/06/2016 , VSO No : 8121
                    var VendorRow = (from ttVendor in ds.Vendor.AsEnumerable()
                                     where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                     select ttVendor).FirstOrDefault();
                    if (VendorRow != null)
                    {
                        string Message = "Please verify that GL Control must be assigned";
                        Epicor.Customization.Bpm.InfoMessage.Publish(Message, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual);
                    }
                    ErrorLine.AppendLine("Please verify that GL Control must be assigned");
                    #endregion

                    #region code added by Shweta Parashar  for Validate EFT Information
                    ValidateEFT(ds);
                    ErrorLine.AppendLine("ValidateEFT Success");
                    #endregion

                    #region code added by mahesh for System Audit log
                    //AuditLog(ds);
                    //ErrorLine += ", AuditLog Success";
                    #endregion
                }
                else if (CustomizationID.ToString() == "Stcl.VendorEntry.EFT")
                {
                    //below code is added by Mahesh Deore to save system audit log information
                    //below code is added by Shweta Parashar to  Validate EFT Information if Customization ID is "Stcl.VendorEntry.EFT"
                    #region code added by Shweta Parashar  for Validate EFT Information
                    ValidateEFT(ds);
                    ErrorLine.AppendLine("ValidateEFT Success");
                    #endregion

                    //Added By Pritesh Parmar on 28/06/2016 , VSO No : 8121, CG Model - In Supplier & Customer setup form validation message is required for GL Control 
                    #region Added By Pritesh Parmar on 28/06/2016 , VSO No : 8121
                    var VendorRow = (from ttVendor in ds.Vendor.AsEnumerable()
                                     where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                     select ttVendor).FirstOrDefault();

                    if (VendorRow != null)
                    {
                        string Message = "Please verify that GL Control must be assigned";
                        Epicor.Customization.Bpm.InfoMessage.Publish(Message, Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual);
                    }
                    ErrorLine.AppendLine("Please verify that GL Control must be assigned");
                    #endregion
                    //End Pritesh Parmar
                }
                //Added By Pritesh Parmar on 28/06/2016 , VSO No : 18600, Provision for Supplier Status in Supplier Master
                //Applicable to all customizations (General logic)
                #region Added By Pritesh Parmar on 18/12/2018 , VSO No : 18600
                var VendorTypeRow = (from ttVendor in ds.Vendor.AsEnumerable()
                                     where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                     select ttVendor).FirstOrDefault();

                if (VendorTypeRow != null)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(VendorTypeRow.VendorID, @"^[a-zA-Z0-9]+$"))
                    {
                        throw new BLException("Special Characters are not allowed in VendorID");
                    }

                    if (!System.Text.RegularExpressions.Regex.IsMatch(VendorTypeRow.Name, @"^[a-zA-Z0-9\s.\-]+$"))
                    {
                        throw new BLException("Special Characters are not allowed in Vendor Name");
                    }
                    if (IsSuppInActive == true)
                    {
                        if (string.IsNullOrEmpty(VendorTypeRow.TaxPayerID))
                        {
                            throw new BLException("Please enter TIN number(TIN)");
                        }
                        else if (string.IsNullOrEmpty(VendorTypeRow.OrgRegCode))
                        {
                            throw new BLException("Please enter VAT number (Organization Registration Code)");
                        }
                    }
                    if (IsTaxIdMandatory == true)
                    {
                        if (string.IsNullOrEmpty(VendorTypeRow.TaxPayerID))
                        {
                            throw new BLException("Taxpayer Id can not be blank");
                        }
                        else
                        {
                            TaxPayerId = VendorTypeRow.TaxPayerID;
                            string taxpaid = VendorTypeRow.TaxPayerID;
                            string testdata1 = VendorTypeRow.Name;
                            LogMsg.AppendLine("data" + taxpaid + "KKL" + testdata1);
                            File.AppendAllText(newPath, LogMsg.ToString());
                            var VendorTaxIdCount = (from ttVendorTaxId in Db.Vendor.AsQueryable()
                                                    where ttVendorTaxId.Company == VendorTypeRow.Company
                                                         && ttVendorTaxId.VendorID != VendorTypeRow.VendorID
                                                         && ttVendorTaxId.TaxPayerID == TaxPayerId
                                                    select ttVendorTaxId).Count();

                            LogMsg.AppendLine("VendorTaxIdCount==>" + VendorTaxIdCount);
                            File.AppendAllText(newPath, LogMsg.ToString());
                            if (VendorTaxIdCount > 0)
                            {
                                throw new BLException("Supplier Taxpayer Id Should be unique.");
                            }
                        }
                    }


                    ErrorLine.AppendLine("IsTaxIdMandatory : " + IsTaxIdMandatory);
                    ErrorLine.AppendLine("VendorTypeRow.TaxPayerID : " + VendorTypeRow.TaxPayerID);


                    if (IsSuppInActive == true)
                    {
                        if (string.IsNullOrEmpty(VendorTypeRow.TaxPayerID))
                        {
                            throw new BLException("Legal name can not be blank");
                        }
                        else
                        {
                            LegalNametest = VendorTypeRow.Name;
                            LogMsg.AppendLine("LegalNametest34==>" + LegalNametest);
                            File.AppendAllText(newPath, LogMsg.ToString());

                            var VendorLegalnameCount = (from ttVendorLegalName in Db.Vendor.AsQueryable()
                                                        where ttVendorLegalName.Company == VendorTypeRow.Company
                                                             && ttVendorLegalName.VendorID != VendorTypeRow.VendorID
                                                             && ttVendorLegalName.Name == LegalNametest
                                                        select ttVendorLegalName).Count();
                            LogMsg.AppendLine("VendorLegalnameCount==>" + VendorLegalnameCount);
                            File.AppendAllText(newPath, LogMsg.ToString());

                            if (VendorLegalnameCount > 0)
                            {
                                throw new BLException("Supplier Legal name Should be unique.");
                            }
                        }

                    }

                    string VendorType = Convert.ToString(VendorTypeRow["VendorType_c"]);
                    bool Inactive = VendorTypeRow.Inactive;
                    ErrorLine.AppendLine("VendorType : " + VendorType + "   Inactive : " + Inactive);

                    if (VendorType != "SUP" && Inactive == false)
                    {
                        throw new BLException("Supplier can not be active since supplier type is Blacklisted/Suspended.");
                    }

                    if (Inactive == true)
                    {
                        //Since Count() is condider then "if not null" is not required
                        //Just show information message, Do not stop to proceed further (Discussed with Hitesh Sanghani on 19/12/2018)
                        var ReqDetailRowCount = (from ttReqDetail in Db.ReqDetail.AsQueryable()
                                                 where ttReqDetail.Company == VendorTypeRow.Company &&
                                                 ttReqDetail.VendorNum == VendorTypeRow.VendorNum &&
                                                 ttReqDetail.OpenLine == true
                                                 select ttReqDetail.Company).Count();
                        ErrorLine.AppendLine("ReqDetailRowCount : " + ReqDetailRowCount);

                        var SugPoDtlRowCount = (from ttSugPoDtl in Db.SugPoDtl.AsQueryable()
                                                where ttSugPoDtl.Company == VendorTypeRow.Company &&
                                                ttSugPoDtl.VendorNum == VendorTypeRow.VendorNum
                                                select ttSugPoDtl.Company).Count();
                        ErrorLine.AppendLine("SugPoDtlRowCount : " + SugPoDtlRowCount);

                        var RFQVendRowCount = (from ttRFQVend in Db.RFQVend.AsQueryable()
                                               where ttRFQVend.Company == VendorTypeRow.Company &&
                                               ttRFQVend.VendorNum == VendorTypeRow.VendorNum &&
                                               ttRFQVend.OpenItem == true
                                               select ttRFQVend.Company).Count();
                        ErrorLine.AppendLine("RFQVendRowCount : " + RFQVendRowCount);

                        var POHeaderRowCount = (from ttPOHeader in Db.POHeader.AsQueryable()
                                                where ttPOHeader.Company == VendorTypeRow.Company &&
                                                ttPOHeader.VendorNum == VendorTypeRow.VendorNum &&
                                                ttPOHeader.OpenOrder == true
                                                select ttPOHeader.Company).Count();
                        ErrorLine.AppendLine("POHeaderRowCount : " + POHeaderRowCount);

                        var APInvHedRowCount = (from ttAPInvHed in Db.APInvHed.AsQueryable()
                                                where ttAPInvHed.Company == VendorTypeRow.Company &&
                                                ttAPInvHed.VendorNum == VendorTypeRow.VendorNum &&
                                                ttAPInvHed.OpenPayable == true
                                                select ttAPInvHed.Company).Count();
                        ErrorLine.AppendLine("APInvHedRowCount : " + APInvHedRowCount);

                        var RcvDtlRowCount = (from ttRcvDtl in Db.RcvDtl.AsQueryable()
                                              where ttRcvDtl.Company == VendorTypeRow.Company &&
                                              ttRcvDtl.VendorNum == VendorTypeRow.VendorNum &&
                                              ttRcvDtl.Invoiced == false
                                              select ttRcvDtl.Company).Count();
                        ErrorLine.AppendLine("RcvDtlRowCount : " + RcvDtlRowCount);

                        var CheckHedRowCount = (from ttCheckHed in Db.CheckHed.AsQueryable()
                                                where ttCheckHed.Company == VendorTypeRow.Company &&
                                                ttCheckHed.VendorNum == VendorTypeRow.VendorNum &&
                                                ttCheckHed.Posted == false
                                                select ttCheckHed.Company).Count();
                        ErrorLine.AppendLine("CheckHedRowCount : " + CheckHedRowCount);

                        int OutStandTranCount = ReqDetailRowCount + SugPoDtlRowCount + RFQVendRowCount + POHeaderRowCount + APInvHedRowCount + RcvDtlRowCount + CheckHedRowCount;
                        if (OutStandTranCount > 0)
                        {
                            StringBuilder Message = new StringBuilder();
                            Message.AppendLine("This supplier has an outstanding transactions as below...");
                            Message.AppendLine("Requisition : " + ReqDetailRowCount);
                            Message.AppendLine("PO Suggestion : " + SugPoDtlRowCount);
                            Message.AppendLine("RFQ : " + RFQVendRowCount);
                            Message.AppendLine("PO : " + POHeaderRowCount);
                            Message.AppendLine("APInvoice : " + APInvHedRowCount);
                            Message.AppendLine("Goods Receipt Note : " + RcvDtlRowCount);
                            Message.AppendLine("Payment : " + CheckHedRowCount);
                            Epicor.Customization.Bpm.InfoMessage.Publish(Message.ToString(), Ice.Common.BusinessObjectMessageType.Information, Ice.Bpm.InfoMessageDisplayMode.Individual);
                        }
                    }
                    if (VendorTypeRow.Company != TreasuryCompany)
                    {
                        foreach (var DataVendor in (from ttVendor in Db.Vendor.AsQueryable()
                                                    where ttVendor.Company == TreasuryCompany &&
                                                    ttVendor.VendorID == VendorTypeRow.VendorID
                                                    select new
                                                    {
                                                        TRASupplier = ttVendor.TRASupplier_c
                                                    }))
                            if (DataVendor != null)
                            {
                                VendorTypeRow["TRASupplier_c"] = DataVendor.TRASupplier;
                            };
                    }

                    if (IsSuppInActive == true)
                    {
                        //bool IsAllSelected = false;
                        StringBuilder LogMsg1 = new StringBuilder();
                        string filename1 = @"\\10.7.0.11\EpicorData\test\log1.txt";
                        string newPath1 = Path.GetFullPath(filename1);


                        if (ds.Vendor.Count > 0)
                        {
                            if (VendorTypeRow != null)
                            {
                                var dbvendor = (from s in Db.Vendor
                                                where s.VendorID == VendorTypeRow.VendorID
                                                select new
                                                {
                                                    LegalName = s.Name,
                                                    company = s.Company,
                                                }).ToList();

                                if (dbvendor != null)
                                {
                                    foreach (var item1 in dbvendor)
                                    {
                                        if (item1 != null)
                                        {
                                            LogMsg1.AppendLine("ashwiniTest==>" + item1.LegalName + "dt" + VendorTypeRow.LegalName);
                                            File.AppendAllText(newPath1, LogMsg1.ToString());
                                        }
                                    }
                                }
                                //{
                                //    LogMsg1.AppendLine("ashwini==>" + dbvendor.LegalName + VendorTypeRow.LegalName);
                                //    File.AppendAllText(newPath1, LogMsg1.ToString());

                                //    if (VendorTypeRow.LegalName == dbvendor.LegalName)
                                //    {
                                //        throw new BLException("legal name should not be same.");
                                //    }

                                //}
                            }
                        }

                        if (ds.VendBank.Count == 0 && !Inactive)
                        {
                            var VendorRowTestdata = (from ttVendor in ds.Vendor
                                                     join ttVendBank in ds.VendBank on ttVendor.VendorNum equals ttVendBank.VendorNum
                                                     where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                                 string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                         && ttVendor.VendorNum == ttVendBank.VendorNum
                                                     select new
                                                     {
                                                         BankAcctNumber = ttVendBank.BankAcctNumber,
                                                     }).ToList();
                            if (VendorRowTestdata != null)
                            {
                                foreach (var item in VendorRowTestdata)
                                {
                                    if (item != null)
                                    {

                                    }
                                }
                            }
                            if (VendorRowTestdata.Count == 0)
                            {
                                var VendorRowTestdata123 = (from ttVendor in ds.Vendor
                                                            join ttVendBank in Db.VendBank on ttVendor.VendorNum equals ttVendBank.VendorNum
                                                            where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                                        string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                                && ttVendor.PrimaryBankID == ttVendBank.BankID
                                                            select new
                                                            {
                                                                BankAcctNumber = ttVendBank.BankAcctNumber,
                                                            }).ToList();


                                if (VendorRowTestdata123 != null)
                                {

                                    foreach (var item in VendorRowTestdata123)
                                    {
                                        if (item != null)
                                        {
                                            if (String.IsNullOrEmpty(Convert.ToString(item.BankAcctNumber)) && !Inactive)
                                            {
                                                throw new BLException("Please enter bank account number");
                                            }
                                        }
                                    }
                                }
                                if (VendorRowTestdata.Count == 0 && VendorRowTestdata123.Count == 0)
                                {
                                    throw new BLException("Please enter bank account number");
                                }

                            }
                        }

                        if (ds.VendBank.Count == 1)
                        {
                            var VendorRowTestdata456 = (from ttVendor in ds.Vendor
                                                        join ttVendBank in ds.VendBank on ttVendor.VendorNum equals ttVendBank.VendorNum
                                                        where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                                    string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                            && ttVendor.VendorNum == ttVendBank.VendorNum
                                                        select new
                                                        {
                                                            BankAcctNumber = ttVendBank.BankAcctNumber,
                                                        }).ToList();
                            if (VendorRowTestdata456 != null)
                            {
                                foreach (var item in VendorRowTestdata456)
                                {
                                    if (item != null)
                                    {
                                        if (String.IsNullOrEmpty(Convert.ToString(item.BankAcctNumber)) && Inactive)
                                        {
                                            throw new BLException("please enter bank account details 41");
                                        }
                                    }
                                }
                            }

                            var VendorRowTestdata12 = (from ttVendor in ds.Vendor
                                                       join ttVendBank in Db.VendBank on ttVendor.VendorNum equals ttVendBank.VendorNum
                                                       where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                                   string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                           && ttVendor.PrimaryBankID == ttVendBank.BankID
                                                       select new
                                                       {
                                                           BankAcctNumber = ttVendBank.BankAcctNumber,
                                                       }).ToList();

                            if (VendorRowTestdata12 != null)
                            {

                                foreach (var item in VendorRowTestdata12)
                                {
                                    if (item != null)
                                    {
                                        if (String.IsNullOrEmpty(Convert.ToString(item.BankAcctNumber)) && !Inactive)
                                        {
                                            throw new BLException("please enter bank account details 412");
                                        }
                                    }
                                }
                            }
                        }
                        if (ds.VendBank.Count >= 1)
                        {
                            var VendorRowTestdata45 = (from ttVendor in ds.Vendor
                                                       join ttVendBank in ds.VendBank on ttVendor.VendorNum equals ttVendBank.VendorNum
                                                       where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                                   string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                           && ttVendor.VendorNum == ttVendBank.VendorNum
                                                       select new
                                                       {
                                                           BankAcctNumber = ttVendBank.BankAcctNumber,
                                                       }).ToList();

                            var VendorRowTestdata1234 = (from ttVendor in ds.Vendor
                                                         join ttVendBank in Db.VendBank on ttVendor.VendorNum equals ttVendBank.VendorNum
                                                         where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                                     string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                                             && ttVendor.PrimaryBankID == ttVendBank.BankID
                                                         select new
                                                         {
                                                             BankAcctNumber = ttVendBank.BankAcctNumber,
                                                         }).ToList();



                            if (VendorRowTestdata45 != null && VendorRowTestdata1234.Count >= 1)
                            {
                                foreach (var item in VendorRowTestdata45)
                                {
                                    if (item != null)
                                    {

                                        if (String.IsNullOrEmpty(Convert.ToString(item.BankAcctNumber)))
                                        {
                                            throw new BLException("Please enter bank account number");
                                        }
                                        else
                                        {

                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                //End Pritesh Parmar

                //addded By ashwini on 16/03/2020 
                if (ds.VendBank.Count == 1)
                {
                    var VendorRowTestdata12345 = (from ttVendor in ds.VendBank
                                                  join ttVend in Db.Vendor on ttVendor.VendorNum equals ttVend.VendorNum
                                                  where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_DELETED, StringComparison.OrdinalIgnoreCase)) //||
                                                  select new
                                                  {
                                                      Inactive = ttVend.Inactive,
                                                      VendorNum = ttVend.VendorNum
                                                  }).ToList();

                    if (VendorRowTestdata12345 != null)
                    {
                        foreach (var item in VendorRowTestdata12345)
                        {
                            var VendDbcnt = (from s in Db.VendBank where s.VendorNum == item.VendorNum select s).ToList();
                            //if (VendDbcnt.Count() > 1)
                            //{

                            //}
                            if (VendDbcnt.Count() == 0)
                            {
                                if (item.Inactive == false)
                                {
                                    throw new BLException("you cannot delete the bank remit first unchecked inactive ");
                                }
                            }
                        }
                    }
                }
                if (ds.VendBank.Count > 0 || ds.Vendor.Count > 0)
                {

                    var vendorNum = (from ttVendor in ds.VendBank
                                     join ttVendB in ds.Vendor on ttVendor.VendorNum equals ttVendB.VendorNum
                                     where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                     select ttVendor.VendorNum).FirstOrDefault();


                    if (vendorNum != null || ds.Vendor.Count > 0)
                    {
                        List<BankAccountNO> bankaacclist = new List<BankAccountNO>();
                        List<BankAccountNO> dsvendbanklist = new List<BankAccountNO>();
                        var dsbankaccnt = (from s in ds.VendBank where s.VendorNum == vendorNum select new { bnkacc1 = s.BankAcctNumber, bankid1 = s.BankID }).ToList();

                        if (dsbankaccnt.Count > 0)
                        {
                            foreach (var item in dsbankaccnt)
                            {
                                var a = ds.VendBank.Where(x => x.BankID == item.bankid1).Select(x => new { x.BankID, x.BankAcctNumber }).LastOrDefault();
                                if (a != null)
                                {
                                    BankAccountNO bnkacc1 = new BankAccountNO();
                                    bnkacc1.BankAccNo = a.BankAcctNumber;

                                    bool containsItem = bankaacclist.Any(item1 => item1.BankAccNo == a.BankAcctNumber);
                                    if (!containsItem)
                                    {
                                        bankaacclist.Add(bnkacc1);
                                    }
                                }
                            }
                        }

                        //for bank account in db///
                        var dbbankaccnt = (from s in Db.VendBank where s.VendorNum == vendorNum select new { bnkacc = s.BankAcctNumber, bankid = s.BankID }).ToList();
                        if (dbbankaccnt.Count > 0)
                        {
                            BankAccountNO bnkacc = new BankAccountNO();
                            foreach (var item in dbbankaccnt)
                            {
                                bnkacc.BankId = item.bankid;
                                bnkacc.BankAccNo = item.bnkacc;
                                bool containsItem = bankaacclist.Any(item2 => item2.BankId == item.bankid);
                                if (!containsItem)
                                {
                                    bankaacclist.Add(bnkacc);
                                }
                            }
                        }

                        if (VendorTypeRow != null)
                        {
                            var dbvendor = (from s in Db.Vendor
                                            where s.VendorID == VendorTypeRow.VendorID
                                            select new
                                            {
                                                TIN = s.TaxPayerID,
                                                VATNO = s.OrgRegCode,
                                                LegalName = s.Name,
                                                company = s.Company,
                                                inactive = s.Inactive,
                                            }).FirstOrDefault();
                            if (dbvendor != null)
                            {

                                if (VendorTypeRow.TaxPayerID != dbvendor.TIN || VendorTypeRow.OrgRegCode != dbvendor.VATNO)
                                {
                                    testdata = true;
                                    VendorTypeRow.Inactive = true;
                                }
                                else
                                {
                                    testdata = false;
                                }
                            }
                            var vendorrow1 = (from ttVendor in ds.Vendor
                                              join ttVendBank in Db.VendBank on ttVendor.VendorNum equals ttVendBank.VendorNum
                                              where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                             && ttVendor.VendorNum == ttVendBank.VendorNum
                                              select new
                                              {
                                                  bankaccount = ttVendBank.BankAcctNumber,
                                                  bankid = ttVendBank.BankID
                                              }).ToList();

                            if (vendorrow1.Count > 0)
                            {
                                foreach (var item in vendorrow1)
                                {
                                    BankAccountNO dsvendbank = new BankAccountNO();
                                    dsvendbank.BankAccNo = item.bankaccount;
                                    dsvendbank.BankId = item.bankid;
                                    dsvendbanklist.Add(dsvendbank);
                                }
                            }

                            List<string> test = bankaacclist.Select(x => x.BankAccNo).ToList();
                            List<string> test1 = dsvendbanklist.Select(x => x.BankAccNo).ToList();
                            if ((testdata) && (test1.Count > 0))
                            {
                                var vendorDetails = new VendorBankDetails()
                                {
                                    TIN = VendorTypeRow.TaxPayerID,
                                    VAT_No = VendorTypeRow.OrgRegCode,
                                    Legal_Name = dbvendor.LegalName,
                                    Bank_Account_No = dsvendbanklist.Select(x => x.BankAccNo).ToList(),
                                    //Traders_Licence_No = VendorTypeRow.AccountRef,
                                    VendorID = VendorTypeRow.VendorID,
                                    //Company = VendorTypeRow.Company
                                };
                                string xmlString = SendHttpRequest(vendorDetails);

                                MessageResp MSGRESP = JsonConvert.DeserializeObject<MessageResp>(xmlString, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

                                if (MSGRESP.message == "1")
                                {
                                    if (MSGRESP.vendorID == vendorDetails.VendorID)
                                    {
                                        if (MSGRESP.Tin == vendorDetails.TIN && MSGRESP.vatNumber == vendorDetails.VAT_No)
                                        {
                                            VendorTypeRow.Inactive = false;
                                        }
                                    }
                                }
                            }

                            if (test.Count == 1)
                            {
                                var vendorDetails = new VendorBankDetails()
                                {
                                    TIN = dbvendor.TIN,
                                    VAT_No = dbvendor.VATNO,
                                    Legal_Name = dbvendor.LegalName,
                                    Bank_Account_No = bankaacclist.Select(x => x.BankAccNo).ToList(),
                                    //Traders_Licence_No = VendorTypeRow.AccountRef,
                                    VendorID = VendorTypeRow.VendorID,
                                    //Company = VendorTypeRow.Company
                                };
                                string xmlString = SendHttpRequest(vendorDetails);

                                MessageResp MSGRESP = JsonConvert.DeserializeObject<MessageResp>(xmlString, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                                //objEventLog.InformationEvent("VAT Interface", "VAT Interface", xmlString, "CreateNewAPGroup", "ON", 1);

                                if (MSGRESP.message == "1")
                                {

                                    //MSGRESP.company == vendorDetails.Company
                                    if (MSGRESP.vendorID == vendorDetails.VendorID)
                                    {

                                        if (MSGRESP.Tin == vendorDetails.TIN && MSGRESP.vatNumber == vendorDetails.VAT_No)
                                        {
                                            try
                                            {
                                                VendorTypeRow.Inactive = false;
                                                LogMsg.AppendLine("successfully");
                                                File.AppendAllText(newPath, LogMsg.ToString());
                                            }
                                            catch (Exception ex)
                                            {
                                                objEventLog.ExceptionEvent("VAT Interface", "VAT Interface", "failed", ex);
                                            }
                                            // objEventLog.InformationEvent("GoloApp Interface", "GoloApp Interface", Convert.ToString(LogMsg), "CreateNewAPGroup", "ON", 1);
                                        }
                                    }
                                }

                            }
                        }
                    };
                }
                File.AppendAllText(newPath, LogMsg.ToString());
                GlobalSysFunctions.ShowCallerInfo(ErrorLine.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("SupplierIMPEntry  => UpdateBefore : " + ErrorLine.ToString(), ex);
                throw new BLException("-SupplierIMPEntry => UpdateBefore : " + ex.Message.ToString());
            }
        }
        #endregion

        #region
        public string JSONRequestToQueue(string Exchange, string Queuename, VendorBankDetails vendorDetails)
        {
            StringBuilder LogMsg2 = new StringBuilder();
            string filename2 = @"\\10.7.0.11\EpicorData\test\log2.txt";
            string newPath2 = Path.GetFullPath(filename2);

            string vendorData = JsonConvert.SerializeObject(vendorDetails, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            LogMsg2.AppendLine("JSONREQUEST " + vendorData);
            File.AppendAllText(newPath2, LogMsg2.ToString());

            string testResponse1 = "";
            var details = JObject.Parse(vendorData);

            if (!string.IsNullOrEmpty(Exchange))
            {
                string ErrMsg = string.Empty;

                TestCM t = new TestCM();
                t.Message = vendorData;

                var headers = new Dictionary<string, string>();
                //headers.Add("Company", t.Company);
                headers.Add("Type", "LRAVATRequest");

                RabbitMQMsgs.SendMessageToQueue1(Exchange, t, headers, out ErrMsg);

                //LogMsg2.AppendLine("ErrMsg " + ErrMsg);
                //File.AppendAllText(newPath2, LogMsg2.ToString());

                if (ErrMsg.Equals("SUCCESS"))
                {
                    testResponse1 = "Message Sent Successfully !";
                }
                else
                {
                    testResponse1 = ErrMsg;
                }
            }


            return testResponse1;
        }
        #endregion

        #region 
        private string SendHttpRequest(VendorBankDetails vendorDetails)
        {
            EventLog objEventLog = new EventLog();
            StringBuilder LogMsg1 = new StringBuilder();
            string constring = "Data Source=ERP1022DBAPTS;database=ERP10MOFKL;uid=sa;password=$tclp4ss;";
            SqlConnection con = new SqlConnection(constring.ToString());
            int CurrentRequestCount = Dbloags.PreviousResponseCount(con, "000", "LRAVATSendRQ");
            Dbloags.InsertInformationLog(con, "000", "LRAVATSendRQ", "LRAVAT Request Count", CurrentRequestCount);

            string filename1 = @"\\10.7.0.11\EpicorData\test\test.txt";
            string newPath1 = Path.GetFullPath(filename1);
            string vendorData = JsonConvert.SerializeObject(vendorDetails,
            new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            GlobalSysFunctions.ShowCallerInfo("VATJSONREQUEST  - " + vendorData);

            objEventLog.InformationEvent("VAT Interface", "VAT Interface", vendorData, "LRAVATSendRQ", "ON", 1);

            StringBuilder Message = new StringBuilder();
            try
            {
                byte[] postdata = Encoding.UTF8.GetBytes(vendorData);
                HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create("http://10.7.0.11/LRAVATWEBAPIPUBLISH/api/ValidateData");
                httpWReq.Method = "POST";
                httpWReq.ContentType = "application/x-www-form-urlencoded";
                httpWReq.ContentLength = postdata.Length;
                Stream stream = httpWReq.GetRequestStream();
                stream.Write(postdata, 0, postdata.Length);
                string responseString = "";
                using (HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        responseString = reader.ReadToEnd();
                        Message.AppendLine(responseString);
                    }

                }



                LogMsg1.AppendLine("responseString==>" + responseString);
                File.AppendAllText(newPath1, LogMsg1.ToString());
                //MessageResp MRP = Newtonsoft.Json.JsonConvert.DeserializeObject<MessageResp>(responseString);


                int CurrentRequestCount1 = Dbloags.PreviousResponseCount(con, "000", "LRAVATReceiveRS");
                Dbloags.InsertInformationLog(con, "000", "LRAVATReceiveRS", "", CurrentRequestCount1);
                objEventLog.InformationEvent("VAT Interface", "VAT Interface", vendorData, "LRAVATReceiveRS", "ON", 1);
                return responseString;
            }
            catch (HttpRequestException ex)
            {
                int CurrentRequestCount1 = Dbloags.PreviousResponseCount(con, "000", "LRAVATReceiveRS");
                Dbloags.ErrorLog(con, "000", "LRAVATReceiveRS", "LRA VAT Interface", CurrentRequestCount1, ex.Message.ToString());
                objEventLog.ExceptionEvent("VAT Interface", "VAT Interface", ex.Message.ToString(), ex);
                GlobalSysFunctions.ShowCallerInfo("failed: " + ex.Message.ToString());
                throw new BLException(ex.Message);
            }
        }
        #endregion

        #region GetListAfter
        public void GetListAfter(
           ref System.String whereClause,
           ref System.Int32 pageSize,
           ref System.Int32 absolutePage,
           ref System.Boolean morePages,
           Erp.Tablesets.VendorListTableset ds,
            Ice.Tablesets.ContextTableset ctxx)
        {
            try
            {
                whereClause = " Company = '" + Session.CompanyID + "' AND Imprest_c = 1 ";
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("SupplierIMPEntry  => GetListAfter : ", ex);
                throw new BLException("SupplierIMPEntry  => GetListAfter : " + ex.Message);
            }
        }

        public void GetNewVendorAfter(Erp.Tablesets.VendorTableset ds, Ice.Tablesets.ContextTableset context)
        {
            StringBuilder ErrorLine = new StringBuilder();
            StringBuilder LogMsg = new StringBuilder();
            string filename1 = @"\\10.7.0.11\EpicorData\test\test.txt";
            string newPath = Path.GetFullPath(filename1);
            try
            {
                string IsAutoVendorId = Convert.ToString(gblProc.GetSysParam("IsAutoVendorId")).ToUpper();
                var bpmRow = context.Client.FirstOrDefault();
                ErrorLine.AppendLine("bpmrow - CustId " + bpmRow.CustomizationId.ToString());
                if (IsAutoVendorId == "TRUE")

                {
                    var VendorIdRow = (from ttVendor in ds.Vendor
                                       where ttVendor.Company == Session.CompanyID.ToString() &&
                                       string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase)
                                       select ttVendor).FirstOrDefault();
                    if (VendorIdRow != null)
                    {
                        foreach (var DataGroupId in Db.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_AutomaticGroupCreation " + "@company = {0}, @tranType = {1}", Session.CompanyID, "VENDOR"))
                        {
                            ObjBOnew.VendorGroupId = DataGroupId.MaxNo;
                        }
                        VendorIdRow.VendorID = ObjBOnew.VendorGroupId.ToString();
                        bool IsSuppInActive = Convert.ToBoolean(gblProc.GetSysParam("IsSuppInActive"));

                        LogMsg.AppendLine("VendorIdRowbefore==>" + VendorIdRow.Inactive);
                        File.AppendAllText(newPath, LogMsg.ToString());

                        if (IsSuppInActive == true)
                        {
                            VendorIdRow.Inactive = true;

                            LogMsg.AppendLine("vendoridinactiveIN==>" + VendorIdRow.Inactive);
                            File.AppendAllText(newPath, LogMsg.ToString());


                        }

                        LogMsg.AppendLine("VendorIdRowinactiveafter==>" + VendorIdRow.Inactive);
                        File.AppendAllText(newPath, LogMsg.ToString());

                        GlobalSysFunctions.ShowCallerInfo("VendorId Generated - " + VendorIdRow.VendorID.ToString());
                    }

                }


            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("SupplierIMPEntry  => GetNewVendorAfter : ", ex);
                throw new BLException("SupplierIMPEntry => GetNewVendorAfter : " + ex.Message.ToString());
            }
        }
        #endregion

        #region ValidateEFT - Added by Shweta Parashar On 25-08-2015
        public void ValidateEFT(Erp.Tablesets.VendorTableset ds)
        {
            string InfoMessage = string.Empty;
            try
            {
                #region SpecialCharacter
                //----------Added by Shweta Parashar On 25-08-2015 
                //---  Purpose :Special character Validation in Following field For PaymentMethod-Tiss
                #endregion

                #region ValidationSpecialCharacterForPaymentMethod-Tiss
                string ErrorMessage = string.Empty;

                // in below query condition replaced by Mahesh on 29/07/2016. [replaced ttPayMethod.Name == "TISS" with ttPayMethod.Type == 1]
                foreach (var VendorBank in (from ttVendBank in ds.VendBank
                                            join ttPayMethod in Db.PayMethod on ttVendBank.PMUID equals ttPayMethod.PMUID
                                            where (string.Equals(ttVendBank.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(ttVendBank.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase)) &&
                                                   ttPayMethod.Type == 1 &&
                                                   ttVendBank.Company == Session.CompanyID
                                            select new
                                            {
                                                RowMod = ttVendBank.RowMod,
                                                SwiftNum = ttVendBank.SwiftNum,
                                                BankAcctNumber = ttVendBank.BankAcctNumber,
                                                NameOnAccount = ttVendBank.NameOnAccount
                                            }))

                    if (VendorBank != null)
                    {
                        InfoMessage += "VendBank > Row Mod => " + VendorBank.RowMod;
                        ErrorMessage = string.Empty;

                        if (String.IsNullOrEmpty(Convert.ToString(VendorBank.SwiftNum)))
                        {
                            ErrorMessage += "Bank Identifier  \r\n";
                        }
                        else
                        {
                            if (!GlobalSysFunctions.ValidateSpecialCharacter(Convert.ToString(VendorBank.SwiftNum)))
                            {
                                ErrorMessage += "Bank Identifier\r\n";
                            }
                        }
                        InfoMessage += ", VendBank > Bank Identifier success";

                        if (String.IsNullOrEmpty(Convert.ToString(VendorBank.BankAcctNumber)))
                        {
                            ErrorMessage += "Bank Account \r\n";
                        }
                        else
                        {
                            if (!GlobalSysFunctions.ValidateSpecialCharacter(Convert.ToString(VendorBank.BankAcctNumber)))
                            {
                                ErrorMessage += "Bank Account \r\n";
                            }
                        }
                        InfoMessage += ", VendBank > Bank Account success";

                        string PayToName = String.Empty;
                        PayToName = VendorBank.NameOnAccount;
                        InfoMessage += ", VendBank > VendorBank.NameOnAccount : " + VendorBank.NameOnAccount;

                        PayToName = PayToName.Replace("-", "");
                        InfoMessage += ", VendBank > PayToName Before Replace : " + PayToName;

                        PayToName = PayToName.Replace("\\", "");
                        InfoMessage += ", VendBank > PayToName After Replace : " + PayToName;

                        if (String.IsNullOrEmpty(Convert.ToString(VendorBank.NameOnAccount)))
                        {
                            ErrorMessage += "Pay To Name  \r\n";
                        }
                        else
                        {
                            InfoMessage += ", VendBank > Pay To Name Else Condition";
                            if (!GlobalSysFunctions.ValidateSpecialCharacter(Convert.ToString(VendorBank.NameOnAccount)))
                            {
                                ErrorMessage += "Pay To Name \r\n";
                            }
                        }
                        InfoMessage += ", VendBank > Pay To Name Success";

                        if (!String.IsNullOrEmpty(ErrorMessage))
                        {
                            throw new BLException("Special Character and blank values are not allowed,\r\nPlease Update Below Mandatory Fields \r\n" + ErrorMessage);
                        }

                    }
                //----------End

                // in below query condition replaced by Mahesh on 29/07/2016. [replaced ttPayMethod.Name == "TISS" with ttPayMethod.Type == 1]
                foreach (var VendorRow in (from ttVendor in ds.Vendor
                                           join ttVendBank in Db.VendBank on ttVendor.VendorNum equals ttVendBank.VendorNum
                                           join ttPayMethod in Db.PayMethod on ttVendor.PMUID equals ttPayMethod.PMUID
                                           where (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                                       string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                               && ttPayMethod.Type == 1 && ttVendBank.Company == Session.CompanyID && ttPayMethod.Company == Session.CompanyID
                                           select new
                                           {
                                               RowMod = ttVendor.RowMod,
                                               SwiftNum = ttVendBank.SwiftNum,
                                               BankAcctNumber = ttVendBank.BankAcctNumber,
                                               NameOnAccount = ttVendBank.NameOnAccount
                                           }))

                    if (VendorRow != null)
                    {
                        InfoMessage += ", Vendor > Row Mod => " + VendorRow.RowMod;
                        ErrorMessage = string.Empty;

                        if (String.IsNullOrEmpty(Convert.ToString(VendorRow.SwiftNum)))
                        {
                            ErrorMessage += "Bank Identifier  \r\n";
                        }
                        else
                        {
                            if (!GlobalSysFunctions.ValidateSpecialCharacter(Convert.ToString(VendorRow.SwiftNum)))
                            {
                                ErrorMessage += "Bank Identifier \r\n";
                            }
                        }
                        InfoMessage += ", Vendor > Bank Identifier Success";

                        if (String.IsNullOrEmpty(Convert.ToString(VendorRow.BankAcctNumber)))
                        {
                            ErrorMessage += "Bank Account \r\n";
                        }
                        else
                        {
                            if (!GlobalSysFunctions.ValidateSpecialCharacter(Convert.ToString(VendorRow.BankAcctNumber)))
                            {
                                ErrorMessage += "Bank Account \r\n";
                            }
                        }
                        InfoMessage += ", Vendor > Bank Account Success";

                        string PayToName = String.Empty;
                        PayToName = VendorRow.NameOnAccount;
                        InfoMessage += ", Vendor > VendorBank.NameOnAccount : " + VendorRow.NameOnAccount;

                        PayToName = PayToName.Replace("-", "");
                        InfoMessage += ", Vendor > PayToName Before Replace : " + PayToName;

                        PayToName = PayToName.Replace("\\", "");
                        InfoMessage += ", Vendor > PayToName After Replace : " + PayToName;

                        if (String.IsNullOrEmpty(Convert.ToString(PayToName)))
                        {
                            ErrorMessage += "Pay To Name  \r\n";
                        }
                        else
                        {
                            InfoMessage += ", Vendor > Pay To Name Else Condition";
                            if (!GlobalSysFunctions.ValidateSpecialCharacter(Convert.ToString(PayToName)))
                            {
                                ErrorMessage += "Pay To Name \r\n";
                            }
                        }
                        InfoMessage += ", Vendor > Pay To Name Success";

                        if (!String.IsNullOrEmpty(ErrorMessage))
                        {
                            throw new BLException("Special Character and blank values are not allowed,\r\nPlease Update Below Mandatory Fields \r\n" + ErrorMessage);
                        }
                    }
                #endregion
                GlobalSysFunctions.ShowCallerInfo(InfoMessage);
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo(InfoMessage, ex);
                throw new BLException(ex.Message);
            }
        }
        #endregion

        #region PreLinkGlbVendor - Validate GL Control of Supplier while supplier linking, By Pritesh Parmar On 05/01/2016
        public void PreLinkGlbVendor(ref System.String glbCompany,
                                    ref System.Int32 glbVendorNum,
                                    Erp.Tablesets.GlbVendorTableset ds,
                                    ref System.String vMessage,
                                    ref System.Boolean askQuestion,
                                    Ice.Tablesets.ContextTableset context)
        {
            try
            {
                string GlbCompany = glbCompany;
                string GlbVendorNum = Convert.ToString(glbVendorNum);
                bool IsAccrualAcct = Convert.ToBoolean(gblProc.GetSysParam("IsAccrualAcct"));

                var EntityGLCRow = (from ttEntityGLC in Db.EntityGLC.AsQueryable()
                                    where ttEntityGLC.Company == GlbCompany &&
                                    ttEntityGLC.RelatedToFile == "Vendor" &&
                                    ttEntityGLC.Key1 == GlbVendorNum
                                    select ttEntityGLC).Count();
                if (EntityGLCRow == 0 && IsAccrualAcct == false)
                {
                    throw new BLException("GL Control not found for this vendor in ministry / treasury, Please attach GL Control before linking");
                }
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("SupplierIMPEntry  => PreLinkGlbVendor : ", ex);
                throw new BLException("SupplierIMPEntry => PreLinkGlbVendor : " + ex.Message.ToString());
            }
        }
        #endregion

        #region System Audit Log
        public void AuditLog(Erp.Tablesets.VendorTableset ds)
        {
            try
            {
                DataTable DtAuditLog = new DataTable();
                DtAuditLog = GlobalSysFunctions.GetSysAuditInfo();
                foreach (var ttVenHedRow in (from ttVenH in ds.Vendor select ttVenH))
                    if (ttVenHedRow != null)
                    {
                        ttVenHedRow["ApplicationDateTime_c"] = DtAuditLog.Rows[0]["ApplicationDateTime_c"];
                        ttVenHedRow["ApplicationIPAddress_c"] = DtAuditLog.Rows[0]["ApplicationIPAddress_c"].ToString();
                        ttVenHedRow["ApplicationMACAddress_c"] = DtAuditLog.Rows[0]["ApplicationMACAddress_c"].ToString();
                        ttVenHedRow["ApplicationTimeZone_c"] = DtAuditLog.Rows[0]["ApplicationTimeZone_c"].ToString();
                        ttVenHedRow["ApplicationUserName_c"] = Session.UserID; //AppUserName;
                        ttVenHedRow["ApplicationMotherBoardId_c"] = DtAuditLog.Rows[0]["ApplicationMotherBoardId_c"].ToString();
                        ttVenHedRow["DatabaseDateTime_c"] = DtAuditLog.Rows[0]["DatabaseDateTime_c"];
                        ttVenHedRow["DatabaseIPAddress_c"] = DtAuditLog.Rows[0]["DatabaseIPAddress_c"].ToString();
                        ttVenHedRow["DatabaseMACAddress_c"] = DtAuditLog.Rows[0]["DatabaseMACAddress_c"].ToString();
                        ttVenHedRow["DatabaseTimeZone_c"] = DtAuditLog.Rows[0]["DatabaseTimeZone_c"].ToString();
                        ttVenHedRow["DatabaseUserName_c"] = DtAuditLog.Rows[0]["DatabaseUserName_c"].ToString();
                        ttVenHedRow["DatabaseName_c"] = DtAuditLog.Rows[0]["DatabaseName_c"].ToString();
                        ttVenHedRow["DatabaseHostName_c"] = DtAuditLog.Rows[0]["DatabaseHostName_c"].ToString();
                    }
            }
            catch (Exception ex)
            {
                throw new BLException("SupplierIMPEntry => AuditLog : " + ex.Message.ToString());
            }
        }
        #endregion System Audit Log

        #region ChangeBankBranchCode
        public void ChangeBankBranchCode(
           ref System.String proposedBankBranchCode,
           Erp.Tablesets.VendorTableset ds,
           Ice.Tablesets.ContextTableset context)
        {
            try
            {
                string prposedBankBranchCode = proposedBankBranchCode;
                StringBuilder ErrorMsg = new StringBuilder(); ;
                ErrorMsg.AppendLine("ChangeBankBranchCode started");
                ErrorMsg.AppendLine("proposedBankBranchCode - " + proposedBankBranchCode);

                foreach (var VendorRow in ((from ttVendor in ds.VendBank
                                            where ttVendor.Company == Session.CompanyID &&
                                            (string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_ADDED, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(ttVendor.RowMod, IceRow.ROWSTATE_UPDATED, StringComparison.OrdinalIgnoreCase))
                                            select ttVendor)))
                    if (VendorRow != null)
                    {
                        ErrorMsg.AppendLine("VendorRow.RowMod - " + VendorRow.RowMod.ToString());
                        foreach (var DbBankBranch in ((from ttBankBranch in Db.BankBrnch.AsQueryable()
                                                       where ttBankBranch.Company == VendorRow.Company &&
                                                       ttBankBranch.BankBranchCode == VendorRow.BankBranchCode
                                                       select new
                                                       {
                                                           BankIdentiFier = ttBankBranch.SwiftCode_c
                                                       })))
                            if (DbBankBranch != null)
                            {
                                ErrorMsg.AppendLine("DbBankBranch.BankIdentiFier - " + DbBankBranch.BankIdentiFier.ToString());
                                VendorRow.SwiftNum = DbBankBranch.BankIdentiFier;
                            }


                    }
                GlobalSysFunctions.ShowCallerInfo("Change BankBranch - ", ErrorMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo(ex.Message.ToString(), ex);
                throw new BLException("BankAcctEntry => ChangeBankBranchCode => " + ex.Message);

            }
        }
        #endregion ChangeBankBranchCode

        #region LinkGlbVendor
        public void LinkGlbVendor(
            ref System.String glbCompany,
            ref System.Int32 glbVendorNum,
            Erp.Tablesets.GlbVendorTableset ds,
            Erp.Tablesets.VendorTableset ds1,
            Ice.Tablesets.ContextTableset context)
        {
            StringBuilder LogMsg = new StringBuilder();
            //StringBuilder LogMsg = new StringBuilder();
            try
            {
                Int32 GlbVendorNum = glbVendorNum;
                LogMsg.AppendLine("Session.CompanyID = " + Convert.ToString(Session.CompanyID) + " GlbVendorNum = " + Convert.ToString(GlbVendorNum));

                foreach (var GlbVendorRow in (from ttGlbVendor in ds.GlbVendor
                                              where ttGlbVendor.Company == Session.CompanyID
                                              select ttGlbVendor))
                {
                    LogMsg.AppendLine("PrimaryBankID Before = " + Convert.ToString(GlbVendorRow.PrimaryBankID));
                    LogMsg.AppendLine("CompanyID = " + Convert.ToString(GlbVendorRow.Company) + " VendorID = " + Convert.ToString(GlbVendorRow.VendorID) + " VendorNum = " + Convert.ToString(GlbVendorRow.VendorNum));

                    //Set PrimaryBankID blank to resolve (Bank references invalid value issue) faced on (Posted Invoice Update) form
                    GlbVendorRow.PrimaryBankID = string.Empty;

                    LogMsg.AppendLine("PrimaryBankID After = " + Convert.ToString(GlbVendorRow.PrimaryBankID));
                }
                GlobalSysFunctions.ShowCallerInfo("SupplierIMPEntry  => LinkGlbVendor : " + LogMsg.ToString());
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("SupplierIMPEntry  => LinkGlbVendor : ", ex);
                throw new BLException("SupplierIMPEntry => LinkGlbVendor : " + ex.Message.ToString());
            }
        }
        #endregion LinkGlbVendor
    }
}
