CREATE TABLE [dbo].[TINStage]
(
    [Id] UNIQUEIDENTIFIER NOT NULL primary key,
	[Tin] NVARCHAR(10) Null,
	[TinType] NVARCHAR(3) Null,	
	[ProviderId] UNIQUEIDENTIFIER NOT NULL
)
