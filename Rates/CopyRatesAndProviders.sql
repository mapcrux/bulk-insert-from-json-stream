CREATE PROCEDURE [dbo].[CopyRatesAndProviders]
AS
	INSERT INTO Provider
	SELECT distinct s.Id,s.Tin,s.TinType
	FROM ProviderStage s 
		   LEFT JOIN Provider d ON (d.Id = s.Id)
	WHERE d.Id IS NULL

	Insert INTO NPI
	SELECT s.NPI,s.ProviderId
	FROM NPIStage s 
		   LEFT JOIN NPI d ON (d.NPI = s.NPI and d.ProviderId = s.ProviderId)
	WHERE d.ProviderId IS NULL
	
	Insert Into Rates
	SELECT 
	   S.Id,
       (CASE WHEN s.ProviderId IS NULL And t.ProviderId IS NOT NULL THEN t.ProviderId ELSE s.ProviderId END) as ProviderId,
       s.BillingCode,
       s.BillingCodeType,
       s.BillingCodeTypeVersion,
       s.NegotiatedType,
       s.NegotiatedRate,
       s.ExpirationDate,
       s.BillingClass, 
       s.BillingCodeModifier,
       s.AdditionalInformation,
       s.ReportingEntityId
	From RateStage s
	    OUTER APPLY 
        (
			SELECT TOP 1 ProviderId
			FROM ProviderStage 
			WHERE ProviderStage.ProviderReference = s.ProviderReference
        ) AS t

RETURN 0
