/*------------------------------------------------------------------------
    File        : C-IFMIS-E905702-Generic\ServerObjects\IFMIS\csGlobalProcedures.i
    Purpose     : Used in BPM validations for Commitment control. 
    Syntax      :
    Author(s)   : Pritesh Parmar
    Created     : 19-02-2015
    Notes       :
    Updated By  : Shweta Parashar
    Version     : 1.0.0.0
Revision History:
  ----------------------------------------------------------------------
VersionInfo			        Updated By				Updated Date		Purpose			
 1.0.0.1			        Shweta Parashar			26-08-2015			EFT Audit Log Maintain for Recreation of EFT File Payment
 1.0.0.2			        Sangram Kulkarni		12-04-2015			Updated GetSysParam to read budget either from Treasury or SessionCompany
 2.0.0.0			        Mahesh Deore            04/05/2016	        Upgraded references from 10.1.400.1 to 10.1.400.8
 2.0.0.1        CIFMIS      Mahesh Deore            12-05-2016          Upgraded references from 10.1.400.8 to 10.1.400.9
 2.0.0.2        CIFMIS      Pritesh Parmar          12/05/2016	        Resolved code review issues
 2.0.0.3	    CIFMIS      Mahesh Deore            20/05/2016	        Changed the references from 10.1.400.9 changes to 10.1.400.1
 2.0.0.4        CIFMIS      Pritesh Parmar          12/05/2016	        Removed Environment.NewLine, Was not working on EWA
 2.0.0.5        CIFMIS      Rajesh Tiwari           17/05/2017	        VSO Bug Id - 13092 Added New function to get Tax setup from Company configuration
 2.0.0.6        CIFMIS      Shekhar Chaudhari       01/06/2017          GetSessionDetail() method added to get session info in static GlobalSysFunctions class.
 2.0.0.7        CIFMIS      Mahesh Deore            04/07/2017          Upgrade from 10.1.400.1 to 10.1.600.5
 2.0.0.8        CIFMIS      Rajesh                  21/Feb/2018         PBID - 16096, Task Id - 16114
 2.0.0.9        CIFMIS      Rajesh                  13/06/2018          PBID - 17062
 2.0.0.10       CIFMIS      Rajesh                  05/10/2018          VSO Bug Id - 18100, 18105,18104
 2.0.0.11       CIFMIS      Pritesh Parmar          28/01/2019	        VSO Id 21353 - Sub-Treasury/Sub Accountancy/Foreign Mission Warrant of Fund Allocation - AP Invoice Generation
 2.0.0.12       CIFMIS      Rajesh                  25/03/2019	        VSO Id 22443 - MOFKL- AP Approval Entry form showing less amount than the payable amount due to WHT in Company Configuration.
  */

using Erp;
using Ice;
using Stcl.Global.GlobalSysInfo;
using System;
using System.Linq;

namespace Stcl.Global.GlobalProcedures
{
    public class GlobalProcedures : ContextBoundBase<ErpContext>
    {
        private static Erp.ErpContext dataContext = null;


#pragma warning disable CS0618 // Type or member is obsolete
        public GlobalProcedures(ErpContext ctx) : base(ctx)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            dataContext = ctx;
        }
        ErpContext DbctxPr = new ErpContext();

        BusinessObject ObjBO = new BusinessObject();

        #region "public Methods/Functions"

        public string GetMasterCOA(string company)
        {
            try
            {
                string MasterCOA = string.Empty;
                var ttMasterCOA = (DbctxPr.GLSyst.Where(g => g.Company == company).Select(g => g.MasterCOA).FirstOrDefault());


                if (ttMasterCOA != null)
                {
                    MasterCOA = ttMasterCOA;
                }

                if (string.IsNullOrEmpty(MasterCOA))
                {
                    throw new BLException("MasterCOA does not exist in GLSyst table");
                }

                return MasterCOA;
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalProcedures => GetMasterCOA : ", ex);
                throw new BLException("GlobalProcedures= > GetMasterCOA : " + ex.Message);
            }
        }

        public string GetSysParam(string code)
        {
            try
            {
                string SysParamValue = string.Empty;
                string SysParamValueNew = string.Empty;
                string TempSysParamValue = SysParamValue;

                if (code.Trim().ToUpper() == "TREASURYCOMPANY")
                {
                    SysParamValueNew = "ISBudgetFromTreasury";

                    foreach (var DataGetGlobalData in DbctxPr.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_GetGlobalData " + "@code = {0}", SysParamValueNew))
                    {
                        TempSysParamValue = Convert.ToString(DataGetGlobalData.Value);
                    }

                    if (TempSysParamValue.Trim().ToUpper() == "FALSE")
                    {
                        SysParamValue = Session.CompanyID;
                    }
                    else
                    {
                        foreach (var DataGetGlobalData in DbctxPr.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_GetGlobalData " + "@code = {0}", code))
                        {
                            SysParamValue = Convert.ToString(DataGetGlobalData.Value);
                        }
                    }
                }
                else
                {
                    foreach (var DataGetGlobalData in DbctxPr.ExecuteStoreQuery<BusinessObject>("Stcl_CIFMIS_Global_GetGlobalData " + "@code = {0}", code))
                    {
                        SysParamValue = Convert.ToString(DataGetGlobalData.Value);
                    }
                }
                return SysParamValue;
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalProcedures => GetSysParam : ", ex);
                throw new BLException("GlobalProcedures => GetSysParam : " + ex.Message);
            }
        }

        /*  Purpose     : EFT Audit Log Maintain for Recreation of EFT File Payment. 
			Description : This code Records Recreat EFT File payments Audit Log .
		    Parameter   : 
		    AuditLogDateTime : Current Recreate EFT DateTime.
		    EFTFileName      : Recreate EFT File Name.
		    FileGeneratedStatus  : Recreate EFT File Generated Status (True/False).
		    UserId  : Session User ID.
		    Company  : Session Company.
		    TranscationType  : TranscationType (CreateEFT/RecreateEFT).
		    BankAcctId  : Unique Bank Account ID.
		    IPAddress   : Client IP Address.
			Author(s)   : Shweta Parashar
			Created     : 08-12-2015 
         */

        public void SaveEFTAuditLog(string EFTFileName, Boolean FileGeneratedStatus, string UserId, string Company, string TranscationType, string BankAcctId,
            string ClientIPAddress, string ClientMACAddress, string ClientTimeZone, DateTime? ClientCreatedDateTime, string ClientUserName, string ClientCPUId, string ClientMotherBoardId, string FileContent)
        {
            try
            {
                string ApplicationDateTime = string.Empty;
                string ClientDateTime = string.Empty;

                if (Convert.ToString(ClientCreatedDateTime) != "" && ClientCreatedDateTime != null)
                {
                    ClientDateTime = Convert.ToDateTime(ClientCreatedDateTime).ToString("yyyy-MMM-dd hh:mm:ss");
                }
                else
                {
                    ClientCreatedDateTime = null;
                }

                if (Convert.ToString(GlobalSysFunctions.GetApplicationDateTime()) != string.Empty)
                {
                    ApplicationDateTime = GlobalSysFunctions.GetApplicationDateTime().ToString("yyyy-MMM-dd hh:mm:ss");
                }

                DbctxPr.ExecuteStoreCommand("EXEC [dbo].[Stcl_CIFMIS_Global_SaveEFTAuditLog]  '" + EFTFileName + "', " + FileGeneratedStatus + ",'" + UserId + "','" + Company + "','" + TranscationType + "','" + BankAcctId + "','" + GlobalSysFunctions.GetCPUID() + "','" + GlobalSysFunctions.GetIPAddress() + "','" + GlobalSysFunctions.GetMacAddress() + "', '" + GlobalSysFunctions.GetMotherBoardID() + "', '" + GlobalSysFunctions.GetCurrentTimeZone() + "', '" + ApplicationDateTime + "', '" + GlobalSysFunctions.GetApplicationUserName() + "','" + ClientIPAddress + "', '" + ClientMACAddress + "', '" + ClientTimeZone + "', '" + ClientDateTime + "', '" + ClientUserName + "', '" + ClientCPUId + "', '" + ClientMotherBoardId + "','" + FileContent + "'");
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalProcedures => SaveEFTAuditLog : ", ex);
                throw new BLException("GlobalProcedures => SaveEFTAuditLog : " + ex.Message);
            }
        }

        public string GetCompanyWhTaxSetup(string company)
        {
            try
            {
                string EnableWHTax = "FALSE";
                var ttEnableWHTax = (DbctxPr.XbSyst.AsEnumerable().Where(g => g.Company == company ).Select(g => Convert.ToString(g.LACTaxCalcEnabled)).FirstOrDefault());


                if (ttEnableWHTax != null)
                {
                    EnableWHTax = ttEnableWHTax;
                }


                return EnableWHTax.ToUpper();
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalProcedures => GetCompanyWhTaxSetup : ", ex);
                throw new BLException("GlobalProcedures= > GetCompanyWhTaxSetup : " + ex.Message);
            }
        }

        public void GetSessionDetail(out string companyId, out string userId, out string sessionId)
        {
            //Set out parameter blank first then assign value if exists
            companyId = string.Empty;
            userId = string.Empty;
            sessionId = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(Session.CompanyID))
                {
                    companyId = Convert.ToString(Session.CompanyID);
                }

                if (!string.IsNullOrEmpty(Session.UserID))
                {
                    userId = Convert.ToString(Session.UserID);
                }

                if (Session.SessionID != null)
                {
                    sessionId = Convert.ToString(Session.SessionID);
                }
            }
            catch (Exception ex)
            {
                throw new BLException("GlobalProcedures => GetSessionDetail :" + ex.Message);
            }
        }


        public bool IsCompanyST(string treasuryCompany, string company)
        {
            try
            {
                bool IsSubTreasury = false;
                var ttSt = (from ST_Row in DbctxPr.UD01.AsQueryable()
                            where ST_Row.Company == treasuryCompany
                            && ST_Row.Key1 == company
                            select new
                            {
                                IsSubTreasury = ST_Row.CheckBox01,
                                IsForeignMission = ST_Row.CheckBox05
                            }).FirstOrDefault();
                if (ttSt != null)
                {
                    //IsSubTreasury & IsForeignMission (Logic Same)
                    if (ttSt.IsSubTreasury == true)
                    { IsSubTreasury = true; }
                    else if (ttSt.IsForeignMission == true)
                    { IsSubTreasury = true; }
                }
                else
                {
                    IsSubTreasury = false;
                }
                return IsSubTreasury;
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalProcedures => IsCompanyST : ", ex);
                throw new BLException("GlobalProcedures= > IsCompanyST : " + ex.Message);
            }
        }

        public string InsufficientMsg(string treasuryCompany, string company, bool isAccrualAcct)
        {
            try
            {
                bool IsSubTreasury = false;
                string InsufficientMessage = string.Empty;
                var ttSt = (from ST_Row in DbctxPr.UD01.AsQueryable()
                            where ST_Row.Company == treasuryCompany
                            && ST_Row.Key1 == company
                            select new
                            {
                                IsSubTreasury = ST_Row.CheckBox01
                            }).FirstOrDefault();
                if (ttSt != null)
                {
                    IsSubTreasury = ttSt.IsSubTreasury;
                }
                else
                {
                    IsSubTreasury = false;
                }

                if (isAccrualAcct == false)
                {
                    InsufficientMessage = "Allocation";
                }
                else
                {
                    if (IsSubTreasury == false)
                    {
                        InsufficientMessage = "Allocation";
                    }
                    else
                    {
                        InsufficientMessage = "Fund";

                    }

                }
                return InsufficientMessage;
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalProcedures => GetMasterCOA : ", ex);
                throw new BLException("GlobalProcedures= > GetMasterCOA : " + ex.Message);
            }
        }

        public string GetSpecialAcctLits(string treasuryCompany, string code)
        {
            try
            {
                string SpecialAcctLits = string.Empty;
                var ttSplAcct = (from SplAcct_Row in DbctxPr.UD04.AsQueryable()
                                 where SplAcct_Row.Company == treasuryCompany
                                 && SplAcct_Row.Key1 == code
                                 select new
                                 {
                                     SpecialAcctLits = SplAcct_Row.Character01
                                 }).FirstOrDefault();
                if (ttSplAcct != null)
                {
                    SpecialAcctLits = ttSplAcct.SpecialAcctLits;
                }
                else
                {
                    SpecialAcctLits = string.Empty;
                }


                return SpecialAcctLits;
            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalProcedures => GetSpecialAcctLits : ", ex);
                throw new BLException("GlobalProcedures= > GetSpecialAcctLits : " + ex.Message);
            }
        }

        public bool IsStGlAccount(string company, string glAccount)
        {
            try
            {
                bool IsStGlAccount = false;
                string TreasuryCompany = Convert.ToString(GetSysParam("TreasuryCompany"));
                bool ISSTAllocApplicable = Convert.ToBoolean(GetSysParam("ISSTAllocApplicable"));
                bool IsSubWarrantHolderApplicable = Convert.ToBoolean(GetSysParam("IsSubWarrantHolderApplicable"));
                string SubWarrantHolderSegNum = Convert.ToString(GetSysParam("SubWarrantHolder")).ToUpper();
                string STRegionCodeSegNbr = Convert.ToString(GetSysParam("STRegionCodeSegNbr")).ToUpper();
                string NAStRegionCode = Convert.ToString(GetSysParam("NAStRegionCode")).ToUpper();
                string NASubWarrantHolder = Convert.ToString(GetSysParam("NASubWarrantHolder")).ToUpper();

                GlobalSysFunctions.ShowCallerInfo("GLAccount : " + glAccount);

                if (glAccount != string.Empty)
                {
                    string[] ArrGL = glAccount.Split('|');
                    string SWH = IsSubWarrantHolderApplicable == true ? ArrGL[Convert.ToInt32(SubWarrantHolderSegNum) - 1] : string.Empty;
                    string Region = IsSubWarrantHolderApplicable == true ? ArrGL[Convert.ToInt32(STRegionCodeSegNbr) - 1] : string.Empty;

                    if (ISSTAllocApplicable == true && SWH != NASubWarrantHolder && Region != NAStRegionCode)
                    {
                        var UD08RowCnt = (from ttUD08 in DbctxPr.UD08.AsQueryable()
                                          where ttUD08.Company.ToUpper() == TreasuryCompany &&
                                                   ttUD08.Character01 == company &&
                                                   ttUD08.Key1.ToUpper() == SWH
                                          select ttUD08).Count();
                        if (UD08RowCnt == 0)
                        {
                            IsStGlAccount = false;
                            throw new BLException("Invalid GL Account, Select account with Correct Sub Warrant Holder defined (Refer Regional Code Mapping Setup).");
                        }
                        else
                        {
                            IsStGlAccount = true;
                        }

                    }
                    else
                    {
                        IsStGlAccount = false;

                    }
                    return IsStGlAccount;
                }
                else
                { return false; }

            }
            catch (Exception ex)
            {
                GlobalSysFunctions.ShowCallerInfo("GlobalProcedures => IsStGlAccount : ", ex);
                throw new BLException("GlobalProcedures= > IsStGlAccount : " + ex.Message);

            }


        }
        #endregion  "public Methods/Functions"
    }
}
