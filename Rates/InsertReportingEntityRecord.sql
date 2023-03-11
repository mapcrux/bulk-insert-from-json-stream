CREATE PROCEDURE [dbo].[UpsertReportingEntityRecord]
	@entityName varchar (100),
	@entityNameType varchar(50),
	@id uniqueidentifier,
	@entityDate Date
AS

	insert into ReportingEntity (Id, [Name], [Type], [UpdateDate]) values (@id, @entityName, @entityNameType, @entityDate)