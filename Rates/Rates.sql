CREATE TABLE [dbo].[Rates](
       [Id] UNIQUEIDENTIFIER NOT NULL,
       [Tin] [nvarchar](10) NULL,
       [TinType] [nvarchar](3) NULL,
       [BillingCode] [nvarchar](7) NULL,
       [BillingCodeType] [nvarchar](7) NULL,
       [BillingCodeTypeVersion] [int] NULL,
       [NegotiatedType] [nvarchar](15) NULL,
       [NegotiatedRate] [decimal](18, 0) NULL,
       [ExpirationDate] [datetime] NULL,
       [BillingClass] [nvarchar](15) NULL
) ON [PRIMARY]

GO