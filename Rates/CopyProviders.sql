CREATE PROCEDURE [dbo].[CopyProviders]
AS
	INSERT INTO Provider
	SELECT s.Id,s.Tin,s.TinType
	FROM ProviderStage s 
		   LEFT JOIN Provider d ON (d.Id = s.Id)
	WHERE d.Id IS NULL
RETURN 0
