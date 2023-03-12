CREATE PROCEDURE [dbo].[CopyProviders]
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
RETURN 0
