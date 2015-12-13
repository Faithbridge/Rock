/*
	This script deletes/repopulates metric values for:
		- KidSpring Attendance
			- Service Numbers
			- Total Numbers
		- Fuse Attendance
			- Total Numbers
*/

-- Constants
DECLARE @metricValueTypeMeasure AS INT = 0; -- From enum in Rock code
DECLARE @defaultOrder AS INT = 0;

-- Get entity Ids for this instance of Rock
DECLARE @campusEntityTypeId AS INT = (SELECT Id FROM EntityType WHERE Name = 'Rock.Model.Campus');
DECLARE @metricCategoryEntityTypeId AS INT = (SELECT Id FROM EntityType WHERE Name = 'Rock.Model.MetricCategory');
DECLARE @ksCategoryId AS INT = (SELECT Id FROM Category WHERE Name = 'KidSpring Attendance' AND EntityTypeId = @metricCategoryEntityTypeId);
DECLARE @fuseCategoryId AS INT = (SELECT Id FROM Category WHERE Name = 'Fuse Attendance' AND EntityTypeId = @metricCategoryEntityTypeId);
DECLARE @ksServiceMetricId AS INT = (SELECT m.Id FROM [Metric] m JOIN MetricCategory mc ON mc.MetricId = m.Id WHERE mc.CategoryId = @ksCategoryId AND m.[Title] = 'Service Numbers');
DECLARE @ksTotalMetricId AS INT = (SELECT m.Id FROM [Metric] m JOIN MetricCategory mc ON mc.MetricId = m.Id WHERE mc.CategoryId = @ksCategoryId AND m.[Title] = 'Total Numbers');
DECLARE @fuseTotalMetricId AS INT = (SELECT m.Id FROM [Metric] m JOIN MetricCategory mc ON mc.MetricId = m.Id WHERE mc.CategoryId = @fuseCategoryId AND m.[Title] = 'Total Numbers');

-- Delete existing metric values
DELETE FROM MetricValue WHERE MetricId IN (@ksServiceMetricId, @ksTotalMetricId, @fuseTotalMetricId);

-- Re/create temp table that stores the parent group ids of all KidSpring attendance groups we are interested in
IF OBJECT_ID('tempdb..#tempKidSpringAttendeeParentGroupIds') IS NOT NULL DROP TABLE #tempKidSpringAttendeeParentGroupIds;

SELECT
	Id
INTO 
	#tempKidSpringAttendeeParentGroupIds
FROM
	[Group]
WHERE
	Name IN (
		'Nursery Attendee',
		'Preschool Attendee',
		'Elementary Attendee',
		'Special Needs Attendee'
	);

-- Re/create a temp table that has Sunday schedules with a time field (looks like '0915', '1115', etc)
IF OBJECT_ID('tempdb..#tempSchedules') IS NOT NULL DROP TABLE #tempSchedules;

SELECT
	[Id],
	REPLACE(Name, 'Sunday ', '') AS [Time]
INTO
	#tempSchedules
FROM [Schedule]
WHERE
	Name like 'Sunday %';

-- Re/create temp table with counts of kids attending by campus per service
IF OBJECT_ID('tempdb..#tempByCampus') IS NOT NULL DROP TABLE #tempByCampus;

SELECT
	COUNT(a.[Id]) AS Value,
	a.[CampusId],
	CONVERT(DATETIME, CONCAT(a.SundayDate, ' ', s.[Time])) AS [Date]
INTO
	#tempByCampus
FROM 
	[Attendance] a
	JOIN [Group] g ON a.GroupId = g.Id
	JOIN [#tempKidSpringAttendeeParentGroupIds] p ON p.Id = g.ParentGroupId
	JOIN #tempSchedules s ON s.Id = a.ScheduleId
WHERE 
	a.DidAttend = 1
GROUP BY
	a.CampusId,
	a.SundayDate,
	s.[Time];

-- Insert values for the "by service" metric
INSERT INTO [MetricValue] (
	[MetricValueType]
    ,[YValue]
    ,[Order]
    ,[MetricId]
    ,[MetricValueDateTime]
    ,[Guid]
    ,[EntityId]
	,[CreatedDateTime]
) SELECT
	@metricValueTypeMeasure,
	v.Value,
	@defaultOrder,
	@ksServiceMetricId,
	v.[Date],
	NEWID(),
	v.CampusId,
	GETDATE()
FROM
	#tempByCampus v;

-- Insert values for the "total" metric
INSERT INTO [MetricValue] (
	[MetricValueType]
    ,[YValue]
    ,[Order]
    ,[MetricId]
    ,[MetricValueDateTime]
    ,[Guid]
    ,[EntityId]
	,[CreatedDateTime]
) SELECT
	@metricValueTypeMeasure,
	SUM(v.Value),
	@defaultOrder,
	@ksTotalMetricId,
	CONVERT(DATE, v.[Date]),
	NEWID(),
	v.CampusId,
	GETDATE()
FROM
	#tempByCampus v
GROUP BY
	CONVERT(DATE, v.[Date]),
	v.CampusId;

-- Insert values for the "total" Fuse metric
WITH cteFuseValues AS (
	SELECT
		a.CampusId,
		COUNT(a.Id) AS Value,
		CONVERT(DATE, a.StartDateTime) AS [Date]
	FROM 
		Attendance a
		JOIN [Group] g ON a.GroupId = g.Id
		JOIN [Group] p ON g.ParentGroupId = p.Id
	WHERE
		p.Name = 'Fuse Attendee'
	GROUP BY
		a.CampusId,
		CONVERT(DATE, a.StartDateTime)
)
INSERT INTO [MetricValue] (
	[MetricValueType]
    ,[YValue]
    ,[Order]
    ,[MetricId]
    ,[MetricValueDateTime]
    ,[Guid]
    ,[EntityId]
	,[CreatedDateTime]
) SELECT
	@metricValueTypeMeasure,
	v.Value,
	@defaultOrder,
	@fuseTotalMetricId,
	v.[Date],
	NEWID(),
	v.CampusId,
	GETDATE()
FROM
	cteFuseValues v;