
-- Enable CLR
sp_configure 'clr enabled', 1;
RECONFIGURE WITH OVERRIDE;
GO

--Drop the functions if they already exist
DROP FUNCTION IF EXISTS CURL.XGET
GO
DROP PROCEDURE IF EXISTS CURL.XPOST
GO

--Drop the schema if it already exists
DROP SCHEMA IF EXISTS CURL;
GO

--Drop "trusted assembly flag" if it is set 

DECLARE @hash VARBINARY(64);

SELECT @hash = hash
FROM sys.trusted_assemblies ta
WHERE ta.description = N'SqlClrCurl'

EXEC sp_drop_trusted_assembly @hash;
GO

--Drop the assembly if it already exists
DROP ASSEMBLY IF EXISTS SqlClrCurl;
GO


DECLARE @hash VARBINARY(64);
SELECT @hash = HASHBYTES('SHA2_512', BulkColumn)
FROM OPENROWSET(BULK '