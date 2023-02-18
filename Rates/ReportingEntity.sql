CREATE TABLE [dbo].[ReportingEntity]
(
	[Id] uniqueidentifier NOT NULL PRIMARY KEY, 
    [Name] VARCHAR(100) NULL, 
    [Type] VARCHAR(50) NULL, 
    [UpdateDate] DATE NULL
)
