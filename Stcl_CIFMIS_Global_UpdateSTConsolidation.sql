USE [ERP10MOFTZ]
GO
/****** Object:  StoredProcedure [dbo].[Stcl_CIFMIS_Global_UpdateSTConsolidation]    Script Date: 3/24/2020 12:06:35 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE [dbo].[Stcl_CIFMIS_Global_UpdateSTConsolidation] 
	@STComp		AS NVARCHAR(10),
	@VoteCode   AS NVARCHAR(20),
	@GroupId	AS NVARCHAR(20),	
	@FiscYr		AS INT,
	@FiscPr		AS INT,
	@RunNbr		AS INT
AS
-- ================================================    
-- Procedure Used:- For updating ST Consolidation journals in sub treasury company
-- Create By: Pritesh Parmar    
-- Create date: 01,Feb 2017    

-- EXEC Stcl_CIFMIS_Global_UpdateSTConsolidation '800', '04','ST001607',2020,7,19
-- ================================================   
  BEGIN

	DECLARE @TreasuryCompany		AS VARCHAR(50)
	DECLARE @MainBookID				AS VARCHAR(50)
	DECLARE @NASegValue4			AS VARCHAR(50)
	DECLARE @VoteSegNbr				AS VARCHAR(50)
	DECLARE @SubBudgClassSegNbr		AS VARCHAR(50)
	DECLARE @SubCostCenterSegNbr	AS VARCHAR(50)


	SELECT @TreasuryCompany = Value FROM Stcl_SysParam   WITH (NOLOCK)	WHERE Code = 'TreasuryCompany'		
	SELECT @MainBookID = Value 	FROM Stcl_SysParam   WITH (NOLOCK)	WHERE Code = 'MainBookID'
	SELECT @NASegValue4 = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'NASegValue4'
	SELECT @VoteSegNbr = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'VoteSegNbr'
	SELECT @SubBudgClassSegNbr = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'SubBudgClassSegNbr'
	SELECT @SubCostCenterSegNbr = Value FROM Stcl_SysParam  WITH (NOLOCK) WHERE Code = 'SubCostCenterSegNbr'

	IF OBJECT_ID('tempdb.dbo.#BudClass', 'U') IS NOT NULL	
		DROP TABLE #BudClass

	CREATE TABLE #BudClass 
	(BudClass VARCHAR(50))	

	INSERT INTO #BudClass
	SELECT ChildKey1
	FROM   Ice.UD100A  WITH (NOLOCK)
	WHERE  Company = @TreasuryCompany
			AND CheckBox09 = 1				--CheckBox09 is used for "To be Consolidated" Flag

	DECLARE @SQLQuery1 NVARCHAR(MAX) = ''
	SET @SQLQuery1 = N'UPDATE GLJrnDtl
					SET	IsSTCons_c = 1,
						STConsNo_c = @RunNbr,
						STConsGroupId_c = '''+@GroupId+'''
					WHERE  Company = '''+@STComp+'''
						AND BookID = '''+@MainBookID+'''
						AND FiscalYear = @FiscYr
						AND SegValue'+@VoteSegNbr+' = '''+@VoteCode+'''
						AND FiscalPeriod = @FiscPr
						AND SegValue'+@SubBudgClassSegNbr+' IN (SELECT BudClass FROM #BudClass)
						AND SegValue'+@SubCostCenterSegNbr+' <> '''+@NASegValue4+'''
						AND IsSTCons_c = 0
						AND STConsNo_c = 0
						AND STConsGroupId_c = '''''

	--PRINT @SQLQuery1
	EXEC SP_EXECUTESQL @SQLQuery1 ,N' @RunNbr INT, @FiscYr INT,@FiscPr INT',@RunNbr,@FiscYr,@FiscPr
  END








