CREATE PROCEDURE [dbo].[TruncateStage]
AS
	truncate table NPIStage
	truncate table ProviderStage
	truncate table RateStage
RETURN 0
