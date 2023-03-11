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
       [ReportingEntityId] UNIQUEIDENTIFIER NOT NULL
) ON [PRIMARY]

GO