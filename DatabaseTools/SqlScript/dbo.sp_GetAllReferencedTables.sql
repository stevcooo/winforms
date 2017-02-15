
/****** Object:  StoredProcedure [dbo].[sp_GetAllReferencedTables]    Script Date: 2/15/2017 3:30:05 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_GetAllReferencedTables]
(
	@TableName varchar(1000),
	@ColumnName varchar(1000),
	@UseRecursion bit = 0
)
as
BEGIN

	CREATE TABLE #TMP
	(
		ParentTableName varchar(100),
		TableName varchar(100),
		ColName varchar(100),
		SchemaName varchar(100)
	)

	Insert into #TMP(ParentTableName, TableName, ColName, SchemaName)
	SELECT 
	@TableName as ParentTableName,
	OBJECT_NAME(f.parent_object_id) TableName,
	COL_NAME(fc.parent_object_id,fc.parent_column_id) ColName,
	s.NAME AS SchemaName
	FROM sys.foreign_keys AS f
	JOIN sys.foreign_key_columns AS fc ON f.OBJECT_ID = fc.constraint_object_id
	JOIN sys.tables t  ON t.OBJECT_ID = fc.referenced_object_id
	JOIN sys.schemas s ON t.schema_id = s.schema_id
	WHERE  OBJECT_NAME (f.referenced_object_id) = @TableName
	AND COL_NAME(fc.referenced_object_id,fc.referenced_column_id) = @ColumnName

	if(@UseRecursion = 1)
	BEGIN

		DECLARE 
			@InnerTableName varchar(1000),
			@InnerColumnName varchar(1000)
		DECLARE recurseCursor CURSOR LOCAL --SCROLL STATIC 
		FOR 
		Select TableName, ColName FROM #TMP 
		OPEN recurseCursor 
		FETCH NEXT FROM recurseCursor INTO @InnerTableName, @InnerColumnName

		WHILE @@FETCH_STATUS = 0 
		BEGIN 	   			   
		   exec dbo.sp_GetAllReferencedTables @InnerTableName, @InnerColumnName, 1
		   FETCH NEXT FROM recurseCursor  INTO @InnerTableName, @InnerColumnName
		END 
		CLOSE recurseCursor-- close the cursor
		DEALLOCATE recurseCursor-- Deallocate the cursor
	END

	if((select count(*) from #TMP)>0)
		select * from #TMP
END




