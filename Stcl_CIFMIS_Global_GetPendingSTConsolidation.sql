USE [ERP10MOFTZ]
GO
/****** Object:  StoredProcedure [dbo].[Stcl_CIFMIS_Global_GetPendingSTConsolidation]    Script Date: 3/18/2020 1:14:10 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[Stcl_CIFMIS_Global_GetPendingSTConsolidation] 					    
	@UserID		NVARCHAR(50)	,
	@STCompCode	NVARCHAR(16)
AS    
/* ================================================================================    
Procedure Used:- For Creating journals for ST Consolidation
Create By: Pritesh Parmar    
Create date: 01,Feb 2017    
Description: Process Object for creating journals for ST Consolidation

SELECT * FROM UD01
SELECT * FROM Stcl_ErrorLog ORDER BY 1 DESC

2.0.0.0	Amod Loharkar       11/12/2018		PBID 17766/18976, added SiteID filteration logic for Cost Center Segregation for MOFKL.
2.0.0.1	Pritesh Parmar      04/02/2019		PBID 21382 - SA/ST Consolidation to Parent Ministry
2.0.0.2 Pritesh Parmar      15/02/2019		21851 - ST Consolidation performance testing with more then 100 lines
2.0.0.3 Juhi Kanojia        02/03/2020		25838 -St Consolidation - Scheduler for ST consolidation Failing

EXEC Stcl_CIFMIS_Global_GetPendingSTConsolidation 'MANAGER', '007'
-- ================================================================================   */
BEGIN  
  
 DECLARE @TreasuryCompany   AS NVARCHAR(50)  
 DECLARE @AdvanceToSTAccount   AS NVARCHAR(50)  
 DECLARE @STConsJrnCode    AS NVARCHAR(50)  
 DECLARE @RunNbr      AS INT = 0  
 DECLARE @tmpMaxRunNo    AS INT = 0  
 DECLARE @STConsoCutOffDate   AS DATETIME  
 DECLARE @MainBookID     AS NVARCHAR(50)  
 DECLARE @NASegValue4    AS NVARCHAR(50)  
 DECLARE @TotalMDACompany   INT  
 DECLARE @MDACompanyRowNo   INT  
 DECLARE @SubWarrantHolder   AS NVARCHAR(50)  
 DECLARE @STRegionCodeSegNbr   AS NVARCHAR(50)  
 DECLARE @VoteSegNbr     AS NVARCHAR(50)  
 DECLARE @SubBudgClassSegNbr   AS NVARCHAR(50)  
 DECLARE @SubCostCenterSegNbr  AS NVARCHAR(50)  
 DECLARE @IsSiteIDFilterApplicable AS NVARCHAR(50)  
  
 IF OBJECT_ID('tempdb.dbo.#STCompany', 'U') IS NOT NULL  
  DROP TABLE #STCompany  
  
 IF OBJECT_ID('tempdb.dbo.#BudClass', 'U') IS NOT NULL   
  DROP TABLE #BudClass  
   
 IF OBJECT_ID('tempdb.dbo.#DTL', 'U') IS NOT NULL  
  DROP TABLE #DTL  
      
 IF OBJECT_ID('tempdb.dbo.#MDACompany', 'U') IS NOT NULL  
  DROP TABLE #MDACompany  
    
 IF OBJECT_ID('tempdb.dbo.#Tmp', 'U') IS NOT NULL  
  DROP TABLE #Tmp  
    
 CREATE TABLE #STCompany   
 (STCompany NVARCHAR(8))   
   
 CREATE TABLE #MDACompany   
 (RowID INT IDENTITY ( 1 , 1 ), MDACompany NVARCHAR(8))   
  
 CREATE TABLE #BudClass   
 (BudClass NVARCHAR(50))   
  
 CREATE TABLE #DTL  
 (  
  Company   NVARCHAR(16),  
  FiscalYear  INT,  
  Description  NVARCHAR(50),  
  JEDate   DATETIME,  
  FiscalPeriod INT,  
  GroupID   NVARCHAR(20),  
  SourceModule NVARCHAR(5),  
  JournalCode  NVARCHAR(4),  
  ConsID   NVARCHAR(100),  
  ConsNo   INT,    
  CommentText  NVARCHAR(100),    
  COACode   NVARCHAR(10),    
  GLAccount  NVARCHAR(400),    
  SegValue1  NVARCHAR(50),    
  SegValue2  NVARCHAR(50),  
  SegValue3  NVARCHAR(50),  
  SegValue4  NVARCHAR(50),  
  SegValue5  NVARCHAR(50),  
  SegValue6  NVARCHAR(50),  
  SegValue7  NVARCHAR(50),  
  SegValue8  NVARCHAR(50),  
  SegValue9  NVARCHAR(50),  
  SegValue10  NVARCHAR(50),  
  SegValue11  NVARCHAR(50),  
  SegValue12  NVARCHAR(50),  
  SegValue13  NVARCHAR(50),  
  SegValue14  NVARCHAR(50),  
  SegValue15  NVARCHAR(50),  
  SegValue16  NVARCHAR(50),  
  SegValue17  NVARCHAR(50),  
  SegValue18  NVARCHAR(50),  
  SegValue19  NVARCHAR(50),  
  SegValue20  NVARCHAR(50),  
  BookID   NVARCHAR(12),  
  CurrencyCode NVARCHAR(4),  
  FiscalYearSuffix NVARCHAR(8),  
  FiscalCalendarID NVARCHAR(12),  
  TranAmount   DECIMAL(20,3),   
  RunNo    INT,  
  VoteCode   NVARCHAR(8),  
  MinistryGlAccount NVARCHAR(400),  
  FiscalPrdFrmJEDt INT,  
  EarliestApplyDate DATETIME,  
  STCompany   NVARCHAR(8),  
  SiteID    NVARCHAR(20)  
 )  
   
 SELECT @TreasuryCompany    = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'TreasuryCompany'  
 SELECT @AdvanceToSTAccount   = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'AdvanceToSTAccount'  
 SELECT @STConsJrnCode    = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'STConsJrnCode'  
 SELECT @MainBookID     = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'MainBookID'  
 SELECT @NASegValue4     = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'NASegValue4'  
 SELECT @SubWarrantHolder   = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'SubWarrantHolder'  
 SELECT @STRegionCodeSegNbr   = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'STRegionCodeSegNbr'  
 SELECT @VoteSegNbr     = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'VoteSegNbr'  
 SELECT @SubBudgClassSegNbr   = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'SubBudgClassSegNbr'  
 SELECT @SubCostCenterSegNbr   = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'SubCostCenterSegNbr'  
 SELECT @STConsoCutOffDate   = CAST(Value as DATE) FROM Stcl_SysParam WITH (NOLOCK) WHERE Code = 'STConsCutOffDate'  
 SELECT @IsSiteIDFilterApplicable = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'IsSiteIDFilterApplicable'  

 IF (@STConsoCutOffDate = '')  
 BEGIN  
   RAISERROR ('ST Consolidation CutOff Date does not exist in Stcl_SysParam', 16,1);  
 END  
  
 IF (@STRegionCodeSegNbr = '')  
 BEGIN  
   RAISERROR ('ST Consolidation RegionCode SegNbr does not exist in Stcl_SysParam', 16,1);  
 END  
  
 IF (@AdvanceToSTAccount = '')  
 BEGIN  
   RAISERROR ('Advance To Sub Treasury GL Account does not exist in Stcl_SysParam', 16,1);  
 END  
   
 IF (@STConsJrnCode = '')  
 BEGIN  
   RAISERROR ('ST Consolidation Journal Code does not exist in Stcl_SysParam', 16,1);  
 END  
  
 INSERT INTO #BudClass  
 SELECT Distinct ChildKey1  
 FROM   Ice.UD100A  WITH (NOLOCK)  
 WHERE  Company = @TreasuryCompany  
   AND CheckBox09 = 1    --CheckBox09 is used for "To be Consolidated" Flag  

 INSERT INTO #MDACompany  
 SELECT key1  
 FROM   Ice.UD01  WITH (NOLOCK)  
 WHERE  Company = @TreasuryCompany  
    AND CheckBox01 = 0    --CheckBox01 is used for IsSubTreasury Flag   
    AND CheckBox02 = 0    --CheckBox02 is used for IsRAS Flag   
    AND CheckBox03 = 1    --CheckBox03 is used for Acive Flag   
    AND CheckBox05 = 0    --CheckBox05 is used for IsForeignMission Flag   

 SET @TotalMDACompany = @@ROWCOUNT  

 SET @MDACompanyRowNo = 1  

 INSERT INTO #STCompany  
 SELECT key1  
 FROM   Ice.UD01  WITH (NOLOCK)  
 WHERE  Company = @TreasuryCompany  
    AND CheckBox01 = 1      --CheckBox01 is used for IsSubTreasury Flag   
    AND CheckBox02 = 0      --CheckBox02 is used for IsRAS Flag   
    AND CheckBox03 = 1      --CheckBox03 is used for Acive Flag  
	   
 SELECT @RunNbr = ISNULL(STConsMaxNo_c,1) + 1 --Used for Consolidation max running number  
 FROM Company  WITH (NOLOCK)  
 WHERE Company = @TreasuryCompany  
 
 UPDATE Company   
 SET STConsMaxNo_c = @RunNbr   
 WHERE Company = @TreasuryCompany  

 SET @tmpMaxRunNo = @RunNbr  
 SET @RunNbr = @RunNbr + 1  

 --INSERT EXPENDITURE INCURRED IN ST COMPANY AND PENDING FOR CONSOLIDATION===============    
 DECLARE @SQLQuery1 NVARCHAR(MAX) = ''  
 SET @SQLQuery1 = N'  
   INSERT INTO #DTL   
   SELECT UD01.Key1 AS VoteCompany,  
     P1.FiscalYear,  
     ''ST CONSOLIDATION : ' + CONVERT(NVARCHAR(12),GETDATE(),106)+''',  
     FP.EndDate  ,  
     P1.FiscalPeriod ,  
     CASE WHEN '''+ @IsSiteIDFilterApplicable + ''' = ''TRUE'' THEN ''S''+RIGHT(CONVERT(NVARCHAR(5),'''+@STCompCode+'''),2)+RIGHT(CONVERT(NVARCHAR(5),P1.FiscalYear),2)+REPLACE(STR(CONVERT(NVARCHAR(5),P1.FiscalPeriod),2),SPACE(1),''0'')
	 +RIGHT(CONVERT(NVARCHAR(5),P1.SegValue'+@SubCostCenterSegNbr+'),2) ELSE  
     ''ST''+RIGHT(CONVERT(NVARCHAR(5),'''+@STCompCode+'''),2)+RIGHT(CONVERT(NVARCHAR(5),P1.FiscalYear),2)+REPLACE(STR(CONVERT(NVARCHAR(5),P1.FiscalPeriod),2),SPACE(1),''0'') END,  
     ''GL''   ,  
     '''+@STConsJrnCode+''',  
     ''CONS''+CONVERT(NVARCHAR(10), DENSE_RANK() OVER (ORDER BY  UD01.Key1,P1.FiscalYear,P1.FiscalPeriod)),  
     CONVERT(NVARCHAR(10), DENSE_RANK() OVER (ORDER BY  UD01.Key1,P1.FiscalYear,P1.FiscalPeriod)),    
     ''ST CONSOLIDATION : ' + CONVERT(NVARCHAR(12),GETDATE(),106)+''',   
     P1.COACode   ,    
     P1.GLAccount  ,    
     P1.SegValue1  ,    
     P1.SegValue2  ,  
     P1.SegValue3  ,  
     P1.SegValue4  ,  
     P1.SegValue5  ,  
     P1.SegValue6  ,  
     P1.SegValue7  ,  
     P1.SegValue8  ,  
     P1.SegValue9  ,  
     P1.SegValue10  ,  
     P1.SegValue11  ,  
     P1.SegValue12  ,  
     P1.SegValue13  ,  
     P1.SegValue14  ,  
     P1.SegValue15  ,  
     P1.SegValue16  ,  
     P1.SegValue17  ,  
     P1.SegValue18  ,  
     P1.SegValue19  ,  
     P1.SegValue20  ,  
     P1.BookID   ,  
     P1.CurrencyCode  ,  
     P1.FiscalYearSuffix ,  
     P1.FiscalCalendarID ,  
     SUM(P1.BookDebitAmount - P1.BookCreditAmount) ,   
     @RunNbr    ,  
     ''''    ,  
     ''''    ,  
     0     ,  
     NULL    ,  
     P1.Company   ,  
     P1.SegValue'+@SubCostCenterSegNbr+'   
   FROM GLJrnDtl P1  WITH (NOLOCK)  
   INNER JOIN Ice.UD01 WITH (NOLOCK) ON UD01.Company = '''+@TreasuryCompany+'''  
       AND UD01.ShortChar01 = P1.SegValue'+@VoteSegNbr+'   --ShortChar01 Is Vote Code  
       AND UD01.CheckBox03 = 1          --CheckBox03 Active/Inactive Flag  
   INNER JOIN Erp.FiscalPer FP WITH (NOLOCK) ON UD01.Key1 = FP.Company      
       AND P1.FiscalCalendarID  = FP.FiscalCalendarID   
       AND P1.FiscalYear = FP.FiscalYear    
       AND P1.FiscalYearSuffix  = FP.FiscalYearSuffix   
       AND P1.FiscalPeriod  = FP.FiscalPeriod  
   INNER JOIN Erp.JrnlCode WITH (NOLOCK) ON  JrnlCode.Company = UD01.Key1  
       AND JrnlCode.JournalCode = '''+@STConsJrnCode+'''  
   WHERE P1.Company = '''+@STCompCode+'''  
    AND P1.Company <> UD01.Key1         --Key1 Is Vote Company  
    AND P1.BookID = '''+@MainBookID+'''  
    AND CAST(P1.JEDate as DATE) > '''+CONVERT(NVARCHAR(10),@STConsoCutOffDate,120)+'''  
    AND P1.SegValue'+@SubBudgClassSegNbr+' IN (SELECT BudClass FROM   #BudClass)  
    AND P1.IsSTCons_c = 0  
    AND P1.STConsNo_c = 0  
    AND P1.STConsGroupId_c = ''''  
   GROUP BY UD01.Key1 ,  
     P1.FiscalYear  ,  
     P1.FiscalPeriod  ,  
     FP.EndDate   ,  
     P1.COACode   ,    
     P1.GLAccount  ,    
     P1.SegValue1  ,    
     P1.SegValue2  ,  
     P1.SegValue3  ,  
     P1.SegValue4  ,  
     P1.SegValue5  ,  
     P1.SegValue6  ,  
     P1.SegValue7  ,  
     P1.SegValue8  ,  
     P1.SegValue9  ,  
     P1.SegValue10  ,  
     P1.SegValue11  ,  
     P1.SegValue12  ,  
     P1.SegValue13  ,  
     P1.SegValue14  ,  
     P1.SegValue15  ,  
     P1.SegValue16  ,  
     P1.SegValue17  ,  
     P1.SegValue18  ,  
     P1.SegValue19  ,  
     P1.SegValue20  ,  
     P1.BookID   ,  
     P1.CurrencyCode  ,  
     P1.FiscalYearSuffix ,  
     P1.FiscalCalendarID ,  
     P1.Company'  
 
 EXEC SP_EXECUTESQL @SQLQuery1 ,N' @RunNbr INT',@RunNbr  

 ---------  
 DECLARE @Columns NVARCHAR(MAX) = ''  
 DECLARE @Iterator INT  
 SET @Iterator = 5  
  
 WHILE (@Iterator <= 20)  
 BEGIN  
  IF (@Iterator = @STRegionCodeSegNbr)  
   BEGIN  
    SET @Columns += '(SELECT DISTINCT key2   
         FROM   Ice.UD08 WITH(NOLOCK)  
         WHERE  Company = '''+@TreasuryCompany+'''  
         AND key1 = #DTL.SegValue'+@SubWarrantHolder+'    
         AND key2 = #DTL.SegValue'+@STRegionCodeSegNbr+'),'  + CHAR(13)  
   END  
  ELSE IF (@Iterator = @SubWarrantHolder)  
   BEGIN  
    SET @Columns += 'SegValue'+@SubWarrantHolder+','  + CHAR(13)  
   END  
  ELSE  
   BEGIN  
    SET @Columns += 'CAST(ISNULL(REPLACE(STR(0,LEN(SegValue'+CAST(@Iterator AS NVARCHAR(2))+')),'' '',''0''),'''')  AS NVARCHAR(50)),' + CHAR(13)   
   END  
  SET @Iterator = @Iterator + 1  
 END   
 ---------  
  
 --FOR BALANCE TRANSACTION  
 DECLARE @SQLQuery2 NVARCHAR(MAX) = ''  
 SET @SQLQuery2 = @SQLQuery2+ N'  
 INSERT INTO #DTL   
    (Company  ,  
    FiscalYear  ,  
    Description  ,  
    JEDate   ,  
    FiscalPeriod ,  
    GroupID   ,  
    SourceModule ,  
    JournalCode  ,  
    ConsID   ,  
    ConsNo   ,    
    CommentText  ,    
    COACode   ,    
    GLAccount  ,    
    SegValue1  ,    
    SegValue2  ,  
    SegValue3  ,  
    SegValue4  ,  
    SegValue5  ,  
    SegValue6  ,  
    SegValue7  ,  
    SegValue8  ,  
    SegValue9  ,  
    SegValue10  ,  
    SegValue11  ,  
    SegValue12  ,  
    SegValue13  ,  
    SegValue14  ,  
    SegValue15  ,  
    SegValue16  ,  
    SegValue17  ,  
    SegValue18  ,  
    SegValue19  ,  
    SegValue20  ,  
    BookID   ,  
    CurrencyCode ,  
    FiscalYearSuffix ,  
    FiscalCalendarID ,  
    TranAmount   ,   
    RunNo    ,  
    VoteCode   ,  
    MinistryGlAccount ,  
    FiscalPrdFrmJEDt ,  
    EarliestApplyDate ,  
    STCompany   ,  
    SiteID  
    )   
  SELECT  Company  ,  
    FiscalYear  ,  
    Description  ,  
    MAX(JEDate)  ,  
    FiscalPeriod ,  
    GroupID   ,  
    SourceModule ,  
    JournalCode  ,  
    ConsID   ,  
    ConsNo   ,    
    CommentText  ,    
    COACode   ,    
    GLAccount  ,    
    '''+@AdvanceToSTAccount+'''  , -- from sysparam Advances To Sub/Treasuries ---------------  
    SegValue2  ,  
    SegValue3  ,  
    SegValue4  ,  
    '+@Columns + '  
    BookID  ,  
    CurrencyCode,  
    FiscalYearSuffix ,  
    FiscalCalendarID ,  
    SUM(TranAmount) * -1,  
    @RunNbr  ,  
    VoteCode   ,  
    MinistryGlAccount ,  
    FiscalPrdFrmJEDt ,  
    EarliestApplyDate ,  
    STCompany   ,  
    SiteID  
 FROM   #DTL  
 WHERE  SegValue1 <> '''+@AdvanceToSTAccount+''' -- Advances To Sub/Treasuries ---------------  
 GROUP BY Company  ,  
    FiscalYear  ,  
    Description  ,  
    FiscalPeriod ,  
    GroupID   ,  
    SourceModule ,  
    JournalCode  ,  
    ConsID   ,  
    ConsNo   ,    
    CommentText  ,    
    COACode   ,    
    GLAccount  ,    
    SegValue2  ,  
    SegValue3  ,  
    SegValue4  ,  
    SegValue5  ,  
       SegValue6  ,  
    SegValue7  ,  
       SegValue8  ,  
       SegValue9  ,  
       SegValue10  ,  
    SegValue11  ,  
       SegValue12  ,  
       SegValue13  ,  
       SegValue14  ,  
       SegValue15  ,  
       SegValue16  ,  
       SegValue17  ,  
       SegValue18  ,  
       SegValue19  ,  
       SegValue20  ,  
    BookID   ,  
    CurrencyCode ,  
    FiscalYearSuffix ,  
    FiscalCalendarID ,  
    RunNo    ,  
    VoteCode   ,  
    MinistryGlAccount ,  
    FiscalPrdFrmJEDt ,  
    EarliestApplyDate ,  
    STCompany,  
    SiteID'    

 EXEC SP_EXECUTESQL @SQLQuery2 ,N' @RunNbr INT',@RunNbr  

 --Validation of Vote definition setup in ministry company  
    UPDATE D   
    SET VoteCode = ShortChar01   
    FROM #DTL D  
    INNER JOIN Ice.UD01 U  ON U.Company = @TreasuryCompany  
  AND U.Key1 = D.Company   
  AND U.CheckBox03 = 1  
 --Validation of GL Account setup in ministry company  

    UPDATE D   
    SET D.MinistryGlAccount = G.GLAccount  
    FROM #DTL D  
    LEFT JOIN Erp.GLAccount G ON D.Company = G.Company   
       AND D.COACode = G.COACode   
       AND D.SegValue1 = G.SegValue1  
       AND D.SegValue2 = G.SegValue2  
       AND D.SegValue3 = G.SegValue3  
       AND D.SegValue4 = G.SegValue4  
       AND D.SegValue5 = G.SegValue5  
       AND D.SegValue6 = G.SegValue6  
       AND D.SegValue7 = G.SegValue7  
       AND D.SegValue8 = G.SegValue8  
       AND D.SegValue9 = G.SegValue9  
       AND D.SegValue10 = G.SegValue10  
       AND D.SegValue11 = G.SegValue11  
       AND D.SegValue12 = G.SegValue12  
       AND D.SegValue13 = G.SegValue13  
       AND D.SegValue14 = G.SegValue14  
       AND D.SegValue15 = G.SegValue15  
       AND D.SegValue16 = G.SegValue16  
       AND D.SegValue17 = G.SegValue17  
       AND D.SegValue18 = G.SegValue18  
       AND D.SegValue19 = G.SegValue19  
       AND D.SegValue20 = G.SegValue20  
       AND G.Active = 1  

     --Validation of FiscalPeriod setup in ministry company  
     UPDATE D   
     SET D.FiscalPrdFrmJEDt = F.FiscalPeriod  
  FROM #DTL D  
  LEFT JOIN Erp.FiscalPer F  ON D.Company =F.Company  
        AND D.FiscalCalendarID = F.FiscalCalendarID   
        AND D.FiscalYear = F.FiscalYear  
        AND D.FiscalYearSuffix = F.FiscalYearSuffix  
        AND D.FiscalPeriod = F.FiscalPeriod  
        AND D.JEDate BETWEEN F.StartDate AND F.EndDate  
 
    --Validation of EarliestApplyDate setup in ministry company  
 UPDATE D  
 SET D.EarliestApplyDate = E.EarliestApplyDate  
 FROM #DTL D  
 LEFT JOIN Erp.EADComp E ON D.Company = E.Company   
      AND D.JEDate >= E.EarliestApplyDate  

 --INSERT 1 START, Check VoteCode, If Vote definition setup does not exist then create error line  
 INSERT INTO Stcl_ErrorLog    
 (   
  ErrorCode,     
  ErrorRecorded,     
  UserId,     
  Company,     
  FiscalYear,     
  FiscalPeriod,  
  RunNo    
 )    
 SELECT    
 'VoteCode',    
 'VoteCode does not found for the company ' + Company,    
 @UserID,  
 Company,    
 FiscalYear,    
 FiscalPeriod,  
 @RunNbr     
 FROM #DTL  
 WHERE VoteCode IS NULL  
 GROUP BY Company,    
  FiscalYear,    
  FiscalPeriod    
    --INSERT 1 END, Check VoteCode  
      
    --INSERT 2 START, Check GL Account existance in ministry  
 INSERT INTO Stcl_ErrorLog    
 (   
  ErrorCode,     
  ErrorRecorded,     
  UserId,     
  Company,     
  FiscalYear,     
  FiscalPeriod,  
  RunNo    
 )    
 SELECT ErrorCode,     
  SUBSTRING(ErrorRecorded,0,CHARINDEX('||', ErrorRecorded)) + ' GL Account OR Regional Code Mapping setup does not exist or Active for ministry company '+ Company +'',     
  UserId,     
  Company,     
  FiscalYear,     
  FiscalPeriod,  
  RunNo    
 FROM  
 (SELECT    
 'GL Account' AS ErrorCode,    
 ISNULL((SegValue1+'|'+SegValue2+ '|'+SegValue3+ '|'+SegValue4+ '|'+SegValue5+ '|'+SegValue6+ '|'+SegValue7+ '|'+SegValue8+ '|'+SegValue9+ '|'+SegValue10+'|'+  
  SegValue11+'|'+SegValue12+'|'+SegValue13+'|'+SegValue14+'|'+SegValue15+'|'+SegValue16+'|'+SegValue17+'|'+SegValue18+'|'+SegValue19+'|'+SegValue20),'') AS ErrorRecorded,    
 @UserID AS UserID,    
 Company,    
 FiscalYear,    
 FiscalPeriod,  
 @RunNbr AS RunNo  
 FROM #DTL  
 WHERE MinistryGlAccount IS NULL) tmp  
 GROUP BY ErrorCode,     
  ErrorRecorded,  
  UserID,     
  Company,     
  FiscalYear,     
  FiscalPeriod,  
  RunNo  
    --INSERT 2 END, Check GL Account existance in ministry  
      
     --INSERT 3 START, Check Fiscal Period from JEDate  
 INSERT INTO Stcl_ErrorLog    
 (   
  ErrorCode,     
  ErrorRecorded,     
  UserId,     
  Company,     
  FiscalYear,     
  FiscalPeriod,  
  RunNo    
 )    
 SELECT    
 'Fiscal Period',    
 CONVERT(NVARCHAR(2),FiscalPeriod) + ' Fiscal Period does not exist in ministry company',    
 @UserID,    
 Company,    
 FiscalYear,    
 FiscalPeriod,  
 @RunNbr      
 FROM #DTL  
 WHERE FiscalPrdFrmJEDt IS NULL   
 GROUP BY Company,    
  FiscalYear,    
  FiscalPeriod   
    --INSERT 3 END, Check Fiscal Period from JEDate  
      
     --INSERT 4 START, Check Earliest Apply Date  
 INSERT INTO Stcl_ErrorLog    
 (   
  ErrorCode,     
  ErrorRecorded,     
  UserId,     
  Company,     
  FiscalYear,     
  FiscalPeriod,  
  RunNo    
 )    
 SELECT    
 'EAD',    
 CONVERT(NVARCHAR(10),JEDate,120) + ' Earliest Apply Date does not match with JEDate in ministry company',    
 @UserID,    
 Company,    
 FiscalYear,    
 FiscalPeriod,  
 @RunNbr      
 FROM #DTL  
 WHERE EarliestApplyDate IS NULL  
 GROUP BY Company,    
 FiscalYear,    
 FiscalPeriod,  
 JEDate    
    --INSERT 4 END, Check Earliest Apply Date  
      
 --Check ST Consolidation Journal Code Exist or Not---------------------------------------------------  
 DECLARE @tmpVoteComp   AS NVARCHAR(50)  
 DECLARE @tmpIsSTJrnCodeFound AS INT  
  
    WHILE(@MDACompanyRowNo <= @TotalMDACompany)  
        BEGIN  
            SELECT @tmpVoteComp = MDACompany  
            FROM #MDACompany  
            WHERE RowID = @MDACompanyRowNo       
              
   SELECT @tmpIsSTJrnCodeFound = COUNT(*)   
   FROM Erp.JrnlCode   
   WHERE Company = @tmpVoteComp  
   AND JournalCode = @STConsJrnCode   
   
   IF (@tmpIsSTJrnCodeFound = 0)  
    BEGIN  
     INSERT INTO Stcl_ErrorLog    
     (   
      ErrorCode,     
      ErrorRecorded,     
      UserId,     
      Company,     
      FiscalYear,     
      FiscalPeriod,  
      RunNo  
     )    
     SELECT    
     'STCONS',    
     'ST Consolidation Journal Code does not exist in ministry company',    
     @UserID,    
     @tmpVoteComp,    
     0,    
     0,  
     @RunNbr      
    END    
            SET @MDACompanyRowNo = @MDACompanyRowNo + 1  
            SET @tmpVoteComp = '' -- RESET VARIABLES  
        END  
  --Check ST Consolidation Journal Code Exist or Not---------------------------------------------------  
  
  IF OBJECT_ID('tempdb.dbo.##DETAIL', 'U') IS NOT NULL   
   DROP TABLE ##DETAIL  
  SELECT * INTO ##DETAIL FROM #DTL    
   
  --DELETE GROUP IF THERE IS AN INVALID TRANSACTION  
  CREATE TABLE #Tmp  
  (  
   TCompany NVARCHAR(20),  
   TGroupID NVARCHAR(20),  
  )  
    
  INSERT INTO #Tmp  
  SELECT D1.Company,D1.GroupID  
  FROM #DTL D1  
  WHERE (D1.VoteCode IS NULL    
   OR D1.MinistryGlAccount IS NULL  
   OR D1.FiscalPrdFrmJEDt  IS NULL  
   OR D1.EarliestApplyDate IS NULL)  
  GROUP BY D1.Company,D1.GroupID   
  
  DELETE FROM #DTL   
  WHERE Company+GroupID IN (SELECT TCompany+TGroupID   
          FROM #DTL    
          INNER JOIN #Tmp ON Company = TCompany AND GroupID = TGroupID)  
  --DELETE GROUP IF THERE IS AN INVALID TRANSACTION  
    
  --TABLE 0,  DO NOT CHANGE SEQUENCE, ALWAYS 0 use group by  
  SELECT DISTINCT Company,GroupID,BookID,JournalCode,@RunNbr AS RunNbr ,VoteCode,FiscalPeriod,ConsID,ConsNo,  
      Description,CurrencyCode,SiteID  
  FROM #DTL  
  WHERE TranAmount <> 0  
  ORDER BY Company,GroupID   
  --TABLE 0  
   
  --TABLE 1 DO NOT CHANGE SEQUENCE, ALWAYS 1  
  SELECT  Company,  
    GroupID,  
    MinistryGlAccount AS GLAccount,  
    SegValue1,  
    SegValue2,  
    SegValue3,  
    SegValue4,  
    SegValue5,  
    SegValue6,  
    SegValue7,  
    SegValue8,  
    SegValue9,  
    SegValue10,  
    SegValue11,  
    SegValue12,  
    SegValue13,  
    SegValue14,  
    SegValue15,  
    SegValue16,  
    SegValue17,  
    SegValue18,  
    SegValue19,  
    SegValue20,  
    SUM(TranAmount)  AS TransAmt,  
    CurrencyCode,  
    MAX(JEDate)   AS JEDate,     
    Description,  
    CommentText   AS DtlComm,  
    SiteID  
  FROM #DTL  
  WHERE TranAmount <> 0  
  GROUP BY Company,  
    MinistryGlAccount,  
    SegValue1  ,    
    SegValue2  ,  
    SegValue3  ,  
    SegValue4  ,  
    SegValue5  ,  
    SegValue6  ,  
    SegValue7  ,  
    SegValue8  ,  
    SegValue9  ,  
    SegValue10  ,  
    SegValue11  ,  
    SegValue12  ,  
    SegValue13  ,  
    SegValue14  ,  
    SegValue15  ,  
    SegValue16  ,  
    SegValue17  ,  
    SegValue18  ,  
    SegValue19  ,  
    SegValue20  ,  
    CurrencyCode ,  
    CommentText  ,   
    Description  ,  
    GroupID   ,  
    ConsID    ,  
    SiteID  
  --TABLE 1  
  END 