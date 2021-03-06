USE [ERP10GOTG]
GO
/****** Object:  StoredProcedure [dbo].[Stcl_CIFMIS_GOTG_FsContigencyFund]    Script Date: 6/3/2020 12:27:35 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[Stcl_CIFMIS_GOTG_FsContigencyFund]
	@ReportDate		DATETIME
AS
/********************************************************************************    
Author:		Soft-Tech Consultants
Object :	Stcl_CIFMIS_GOTG_FsContigencyFund
Remarks:	Get Contigency Fund Details    
----------------------------------------------------------------------------------
 Version    Modified By		Date			Comments  
----------------------------------------------------------------------------------
 1.0.0.0    Juhi Kanojia	02/06/2020	    29968 - Creating/ Modifying Stored Procedure For The report
												FIRST CREATED, CONTIGENCY FUND REPORT FOR GOTG	

EXEC Stcl_CIFMIS_GOTG_FsContigencyFund '2020-06-02'
********************************************************************************************/
BEGIN

	DECLARE @CompLogoPath		NVARCHAR(MAX),
			@CompanyName		NVARCHAR(MAX),
			@BudgetBookID		NVARCHAR(MAX),
			@TreasuryCompany	NVARCHAR(MAX),
			@BRJrnCode			NVARCHAR(MAX),
			@CurrentYear		INT,  
			@PreviousYear		INT,  
			@PrevOfPrevYear		INT,  
			@StartOfYear		DATETIME,  
			@EndOfYear			DATETIME  ,
			@CompName			NVARCHAR(MAX),
			@AllocAcctsBookID	NVARCHAR(MAX) ,
			@DEVBudgetClass		NVARCHAR(50),
			@RECBudgetClass		NVARCHAR(50),
			@Divide				DECIMAL(15,2)

	SET @CompLogoPath		=	(SELECT Value FROM Stcl_SysParam WITH(NOLOCK)WHERE Code = 'CompanyLogoPath') 
	SET @CompanyName		=	UPPER((SELECT Value FROM Stcl_SysParam WITH(NOLOCK)WHERE Code = 'CompanyName'))
	SET @BudgetBookID		=	UPPER((SELECT Value FROM Stcl_SysParam WITH(NOLOCK)WHERE Code = 'BudgetBookID'))
	SET @TreasuryCompany	=	UPPER((SELECT Value FROM Stcl_SysParam WITH(NOLOCK)WHERE Code = 'TreasuryCompany'))
	SET @BRJrnCode			=	UPPER((SELECT Value FROM Stcl_SysParam WITH(NOLOCK)WHERE Code = 'BRJrnCode'))
	SET @CompName			=	UPPER((SELECT Company FROM ERP.Company WHERE Name='Centralized Services'))
	SET @AllocAcctsBookID	=	(SELECT Value FROM Stcl_SysParam WITH(NOLOCK) WHERE Code = 'AllocAcctsBookID') 
	SET @RECBudgetClass		=	(SELECT Value FROM Stcl_SysParam WITH(NOLOCK) WHERE Code = 'RECBudgetClass') 
	SET @DEVBudgetClass		=	(SELECT Value FROM Stcl_SysParam WITH(NOLOCK) WHERE Code = 'DEVBudgetClass')
	SET @Divide				=	1000   

	SELECT @CurrentYear		=	YEAR(@ReportDate),  
		   @PreviousYear	=	YEAR(@ReportDate)-1,  
		   @PrevOfPrevYear	=	YEAR(@ReportDate)-2  
	
	SELECT @StartOfYear		=	DATEADD(YY, DATEDIFF(YY,0,@ReportDate),0),  
		   @EndOfYear		=	GETDATE()  
	
	CREATE TABLE #BEReallocated  
			  (  
			  Company		NVARCHAR(10),  
			  CompanyName	NVARCHAR(50),  
			  Reallocated	NVARCHAR(5),
			  SegValue1		NVARCHAR(200),  
			  GLAccount		NVARCHAR(200),  
			  AccountDesc	NVARCHAR(50),  
			  PrevOfPrevAmt DECIMAL(23,3),  
			  PrevReallAmt	DECIMAL(23,3),  
			  CurrReallAmt	DECIMAL(23,3),
			  CurrencyCode  NVARCHAR(50),
			  BudgetClass	NVARCHAR(100),
		      BudgetClassDesc NVARCHAR(100)
		      )   
			    
	/****** *Budget For BE 015 ******/  
	INSERT INTO #BEReallocated
	SELECT gmd.WRCompany_c	AS	Company,
			c.Name			AS	CompanyName,
			'FROM'			AS	Reallocated,
			d.SegValue1,
			d.GLAcctDisp	AS GLAccount,
			d.AccountDesc,
			CASE WHEN g.FiscalYear = @PrevOfPrevYear and g.SourceModule='GL' THEN SUM(BookDebitAmount-BookCreditAmount)
				 WHEN g.FiscalYear = @PrevOfPrevYear THEN SUM(BookDebitAmount)
				 ELSE 0
			END AS PrevOfPrevAmt,
			CASE WHEN g.FiscalYear = @PreviousYear and g.SourceModule='GL'  THEN SUM(BookDebitAmount-BookCreditAmount)
				 WHEN g.FiscalYear = @PreviousYear THEN SUM(BookDebitAmount)
				 ELSE 0  
			END AS PrevReallAmt,
			CASE WHEN g.FiscalYear = @CurrentYear and g.SourceModule='GL'  THEN SUM(BookDebitAmount-BookCreditAmount)
				 WHEN g.FiscalYear = @CurrentYear THEN SUM(BookDebitAmount)
				 ELSE 0
			END AS CurrReallAmt,
			g.CurrencyCode,
			ISNULL(sbc.Key1,'')			AS	BudgetClass,
			ISNULL(sbc.Character01,'')	AS	BudgetClassDesc
	FROM GLJrnDtl g WITH (NOLOCK)
	INNER JOIN GLJrnDtlMnl gmd WITH (NOLOCK)
			ON	gmd.Company				=	g.Company
			AND gmd.BookID				=	g.BookID
			AND gmd.FiscalYear			=	g.FiscalYear
			AND gmd.FiscalYearSuffix	=	g.FiscalYearSuffix
			AND gmd.JournalCode			=	g.JournalCode
			AND gmd.JournalNum			=	g.JournalNum
			AND gmd.FiscalCalendarID	=	g.FiscalCalendarID
	INNER JOIN Erp.GLAcctDisp d WITH (NOLOCK)
			ON  g.Company	= d.Company
			AND g.COACode	= d.COACode
			AND g.GLAccount = d.GLAccount
			AND g.SegValue1	= d.SegValue1
	INNER JOIN Erp.COASegValues p WITH (NOLOCK)
			ON p.company	= g.company
			AND p.COACode	= g.COACode
			AND p.SegmentNbr	= 1
			AND p.SegmentCode= g.SegValue1
	INNER JOIN Erp.COAActCat coacat WITH (NOLOCK)	-- GFS Category
			ON coacat.Company = p.Company
			 AND coacat.COACode= p.COACode
			 AND coacat.CategoryID = p.Category
	INNER JOIN Ice.UD100A Sbc1 WITH (NOLOCK)
			ON Sbc1.Company = @TreasuryCompany
			AND Sbc1.ChildKey1 = g.SegValue3
	INNER JOIN Ice.UD100 Sbc WITH (NOLOCK)
			ON Sbc.Company = Sbc1.Company
			AND Sbc.Key1 = Sbc1.Key1
	LEFT JOIN ICE.UD102A h WITH (NOLOCK)
			ON h.Company = @TreasuryCompany
			AND h.Key1 = g.SegValue3
			AND h.ChildKey1 = g.SegValue2
	INNER JOIN Company c WITH (NOLOCK)
			ON c.Company   = gmd.WRCompany_c
	WHERE		g.Company = @TreasuryCompany
			AND g.BookID = @BudgetBookID
			AND g.FiscalYear BETWEEN @PrevOfPrevYear AND @CurrentYear
			AND g.Posted = 1
			AND g.GLAccount		= gmd.GLAccount
			AND g.SegValue1 = gmd.SegValue1
			AND gmd.WRCompany_c = @CompName
			And g.SegValue2 = '15'
			AND p.NormalBalance = 'D'
			AND p.Category <> 'CUR_ASSETS'
			AND Sbc1.Key1 IN (@RECBudgetClass,@DEVBudgetClass)
			AND p.SegmentCode IN ('2216104','2212103','2216113','2111103','2216113')
	GROUP  BY gmd.WRCompany_c,
			  c.Name,
			  g.FiscalYear,
			  d.SegValue1,
			  d.GLAcctDisp,
			  d.AccountDesc,
			  g.CurrencyCode,
			  g.SourceModule,
			  g.SegValue1,
			  sbc.Key1,
			  sbc.Character01
	
	/***************Budget Re-allocation of Un-allocated Expenditure 015  ****************/  
	Create Table #Reallocate
			(
			Company          NVARCHAR(16),
			BookID	         NVARCHAR(24),
			FiscalYear       int,
			FiscalYearSuffix NVARCHAR(16),
			JournalCode	     NVARCHAR(8),
			JournalNum       int,
			FiscalCalendarID NVARCHAR(24),
			GroupId          NVARCHAR(18)
			)
	
	Insert into #Reallocate
	Select gmd.Company,
	       gmd.BookID,
		   gmd.FiscalYear,
		   gmd.FiscalYearSuffix,
		   gmd.JournalCode,
		   gmd.JournalNum,
		   gmd.FiscalCalendarID,
		   gmd.GroupId
	FROM GLJrnDtlMnl gmd
	Where	gmd.Company = @TreasuryCompany
		AND gmd.BookID = @BudgetBookID
		AND gmd.JournalCode = @BRJrnCode
		AND gmd.FiscalYear BETWEEN @PrevOfPrevYear AND @CurrentYear
		AND gmd.Posted = 1
		AND gmd.WRCompany_c = @CompName
		AND gmd.TransAmt < 0
	
	INSERT INTO #BEReallocated  
	SELECT  gmdd.WRCompany_c	AS	Company,
			c.Name				AS	CompanyName,
			'TO'				AS	Reallocated,
			d.SegValue1,
			d.GLAcctDisp		AS	GLAccount,
			p.SegmentName		AS	AccountDesc,
			CASE WHEN g.FiscalYear = @PrevOfPrevYear THEN SUM(g.BookDebitAmount)
				 ELSE 0
			END AS PrevOfPrevAmt,
			CASE WHEN g.FiscalYear = @PreviousYear THEN SUM(g.BookDebitAmount)
				 ELSE 0
			END AS PrevReallAmt,
			CASE WHEN g.FiscalYear = @CurrentYear THEN SUM(g.BookDebitAmount)
				 ELSE 0
			END AS CurrReallAmt,
			g.CurrencyCode,
			ISNULL(sbc.Key1,'')			AS	BudgetClass,
			ISNULL(sbc.Character01,'')	AS	BudgetClassDesc
	FROM   GLJrnDtl g WITH (NOLOCK)  
	INNER JOIN GLJrnDtlMnl gmdd WITH (NOLOCK)
			ON	gmdd.Company			=	g.Company
			AND gmdd.BookID				=	g.BookID
			AND gmdd.FiscalYear			=	g.FiscalYear
			AND gmdd.FiscalYearSuffix	=	g.FiscalYearSuffix
			AND gmdd.JournalCode		=	g.JournalCode
			AND gmdd.JournalNum			=	g.JournalNum
			AND gmdd.FiscalCalendarID	=	g.FiscalCalendarID
	INNER JOIN #Reallocate t
			ON	gmdd.Company			=	t.Company 
			AND gmdd.BookID				=	t.BookID
			AND gmdd.FiscalYear			=	t.FiscalYear
			AND gmdd.FiscalYearSuffix	=	t.FiscalYearSuffix
			AND gmdd.JournalCode		=	t.JournalCode
			AND gmdd.JournalNum			=	t.JournalNum
			AND gmdd.FiscalCalendarID	=	t.FiscalCalendarID
	INNER JOIN Erp.GLAcctDisp d WITH (NOLOCK)
			ON	g.Company	= d.Company
			AND g.COACode	= d.COACode
			AND g.GLAccount = d.GLAccount
			AND g.SegValue1	= d.SegValue1
	INNER JOIN COASegValues p WITH (NOLOCK)
			ON	p.company		= g.company
			AND p.COACode		= g.COACode
			AND p.SegmentNbr	= 1
			AND p.SegmentCode	= g.SegValue1  
	INNER JOIN Ice.UD100A Sbc1 WITH(NOLOCK)
			ON	Sbc1.Company	= @TreasuryCompany
			AND Sbc1.ChildKey1	= g.SegValue3 
	INNER JOIN Ice.UD100 Sbc WITH	(NOLOCK)
			ON	Sbc.Company = Sbc1.Company
			AND Sbc.Key1	= Sbc1.Key1  
	LEFT JOIN ICE.UD102A h WITH (NOLOCK)
			ON	h.Company	= @TreasuryCompany
			AND h.Key1		= g.SegValue3
			AND h.ChildKey1 = g.SegValue2     
	INNER JOIN Company c WITH (NOLOCK)
			ON	g.Company	= @TreasuryCompany
			AND c.Company	= gmdd.WRCompany_c 
	WHERE	g.Company = @TreasuryCompany
		AND g.BookID = @BudgetBookID
		AND g.FiscalYear BETWEEN @PrevOfPrevYear AND @CurrentYear
		AND g.JournalCode = @BRJrnCode
		AND g.Posted = 1
		AND g.GLAccount = gmdd.GLAccount
		AND gmdd.WRCompany_c <>	@CompName
		AND gmdd.TransAmt > 0
		AND Sbc1.Key1 in (@RECBudgetClass,@DEVBudgetClass)
	GROUP  BY	gmdd.WRCompany_c,
				c.Name,
				g.FiscalYear,
				d.SegValue1,
				d.GLAcctDisp,
				p.SegmentName,
				g.CurrencyCode,
				sbc.Key1,
				sbc.Character01
	
	SELECT  Company,
			CompanyName,
			Reallocated,
		    @CurrentYear	AS	CurrentYear,
			@PreviousYear	AS	PreviousYear,
			@StartOfYear	AS	StartOfYear,
			@EndOfYear		AS	EndOfYear,
			CASE WHEN Reallocated = 'FROM' THEN 'Additions through appropriations'
				 WHEN Reallocated = 'TO' THEN 'Withdrawals for National emergencies'
			END	AS	Allocated,
		    SegValue1,
			GLAccount,
			AccountDesc,
			CASE WHEN SUM(PrevOfPrevAmt) <> 0 THEN SUM(PrevOfPrevAmt)/@Divide
				 ELSE 0
			END	AS	PrevOfPrevAmt,
			CASE WHEN SUM(PrevReallAmt) <> 0 THEN SUM(PrevReallAmt)/@Divide
				 ELSE 0
			END	AS	PrevReallAmt,
			CASE WHEN SUM(CurrReallAmt) <> 0   THEN SUM(CurrReallAmt)/@Divide
				 ELSE 0
			END	AS	CurrReallAmt,
			ISNULL(SUM(CurrReallAmt), 0)/@Divide AS CurrenBudgetBalance,
			ISNULL(SUM(PrevReallAmt), 0)/@Divide AS PrevBudgetBalance,
			BudgetClass,
			BudgetClassDesc,
			@CompLogoPath	AS	CompLogoPath,
			@CompanyName	AS	TrCompanyName
	FROM #BEReallocated
	GROUP BY Company,
			 CompanyName,
			 Reallocated,
			 SegValue1,
			 GLAccount,
		     AccountDesc,
			 BudgetClass,
			 BudgetClassDesc
	ORDER BY Reallocated
	
	DROP TABLE #Reallocate
	DROP TABLE #BEReallocated

END