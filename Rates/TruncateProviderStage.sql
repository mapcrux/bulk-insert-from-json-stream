CREATE PROCEDURE [dbo].[TruncateProviderStage]
AS
	truncate table NPIStage
	truncate table ProviderStage
RETURN 0
