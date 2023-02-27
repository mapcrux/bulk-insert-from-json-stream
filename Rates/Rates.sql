CREATE TABLE [dbo].[Rates](
       [Id] UNIQUEIDENTIFIER NOT NULL,
       [ProviderId] UNIQUEIDENTIFIER NOT NULL,
       [BillingCode] [nvarchar](7) NULL,
       [BillingCodeType] [nvarchar](7) NULL,
       [BillingCodeTypeVersion] [int] NULL,
       [NegotiatedType] [nvarchar](15) NULL,
       [NegotiatedRate] [decimal](18, 2) NULL,
       [ExpirationDate] [datetime] NULL,
       [BillingClass] [nvarchar](50) NULL, 
       [BillingCodeModifier] [nvarchar](50) NULL,
       [AdditionalInformation] [nvarchar](50) NULL,
       [ReportingEntityId] UNIQUEIDENTIFIER NOT NULL, 
       CONSTRAINT [FK_Rates_To_Providers] FOREIGN KEY ([ProviderId]) REFERENCES [Provider]([Id]), 
       CONSTRAINT [FK_Rates_To_ReportingEntity] FOREIGN KEY ([ReportingEntityId]) REFERENCES [ReportingEntity]([Id])
) ON [PRIMARY]

GO