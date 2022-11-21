CREATE TABLE [dbo].[Rates](
       [Id] UNIQUEIDENTIFIER NOT NULL,
       [ProviderId] int NOT NULL,
       [BillingCode] [nvarchar](7) NULL,
       [BillingCodeType] [nvarchar](7) NULL,
       [BillingCodeTypeVersion] [int] NULL,
       [NegotiatedType] [nvarchar](15) NULL,
       [NegotiatedRate] [decimal](18, 0) NULL,
       [ExpirationDate] [datetime] NULL,
       [BillingClass] [nvarchar](50) NULL, 
       [ServiceCode] [nvarchar](15) NULL,
       [BillingCodeModifier] [nvarchar](50) NULL,
       [AdditionalInformation] [nvarchar](50) NULL,
    CONSTRAINT [FK_Rates_To_Providers] FOREIGN KEY ([ProviderId]) REFERENCES [Providers]([Id])
) ON [PRIMARY]

GO