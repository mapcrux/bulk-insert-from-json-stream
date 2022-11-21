CREATE TABLE [dbo].[NPI]
(
	[NPI] INT NOT NULL,
	[ProviderId] INT NOT NULL, 
    CONSTRAINT [FK_NPI_To_Providers] FOREIGN KEY ([ProviderId]) REFERENCES Providers([Id])
)
