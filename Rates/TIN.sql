CREATE TABLE [dbo].[TIN]
(
    [Id] UNIQUEIDENTIFIER NOT NULL primary key,
	[Tin] NVARCHAR(10) Null,
	[TinType] NVARCHAR(3) Null,	
	[ProviderId] INT NOT NULL,
	constraint [FK_TIN_To_Providers] FOREIGN KEY ([ProviderId]) REFERENCES [Providers]([Id])
)
