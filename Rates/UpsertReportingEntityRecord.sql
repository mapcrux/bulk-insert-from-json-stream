CREATE PROCEDURE [dbo].[UpsertReportingEntityRecord]
	@entityName varchar (100),
	@entityNameType varchar(50),
	@id uniqueidentifier,
	@entityDate Date,
	@existingId uniqueidentifier OUTPUT
AS

	SELECT @existingId = id from ReportingEntity 
	where [Name] = @entityName
	and [UpdateDate] = @entityDate
	and [Type] = @entityNameType

	if @existingId is null
	begin
		insert into ReportingEntity (Id, [Name], [Type], [UpdateDate]) values (@id, @entityName, @entityNameType, @entityDate)
	end
	else
	begin
		set @existingId = @id
	end
